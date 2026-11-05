using System;
using UnityEngine;
using UnityEditor;
namespace UnityEngine
{
    internal enum EventType { MouseUp,Repaint }
    internal sealed class Event {public static Event current=new Event();public EventType type;}
    internal static class GUI {public static string focused="coordinate";public static void FocusControl(string name){focused=name;}}
}
namespace UnityEditor
{
    internal static class EditorGUIUtility {public static bool editingTextField=true;}
    internal static class EditorApplication {public static Action delayCall;public static void Drain(){var callbacks=delayCall;delayCall=null;callbacks?.Invoke();}}
}
internal sealed partial class QueueHost
{
    private int queuedSave;private bool saveScheduled;
    public bool dirty=true,dirtyGateways=true,fail;
    public int coordinate=10,savedCoordinate=-1,saves,repaints;
    public void Repaint(){repaints++;}
    private bool SaveGateways(){if(fail)return false;saves++;savedCoordinate=coordinate;dirtyGateways=false;return true;}
    private void SaveChanges(){if(SaveGateways())dirty=false;}
    public void Queue(bool gatewaysOnly)=>QueueEditorSave(gatewaysOnly);
    public void Draw(EventType type){Event.current.type=type;DispatchQueuedSave();}
    public bool Scheduled=>saveScheduled;
}
internal static class SaveQueueChecks
{
    static int checks;static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    public static void Main()
    {
        var host=new QueueHost();host.Queue(true);
        Check(host.saves==0&&host.dirtyGateways,"click queues save without writing the uncommitted value");
        Check(GUI.focused==null&&!EditorGUIUtility.editingTextField,"focus is released to commit delayed controls");
        host.Draw(EventType.MouseUp);Check(!host.Scheduled&&EditorApplication.delayCall==null,"mouse event cannot save before repainting delayed controls");
        host.coordinate=37;host.Draw(EventType.Repaint);Check(host.Scheduled&&host.saves==0,"repaint only schedules post-GUI work");
        host.Draw(EventType.Repaint);EditorApplication.Drain();
        Check(host.saves==1&&host.savedCoordinate==37,"one callback persists the latest committed coordinate");
        Check(!host.dirtyGateways&&host.dirty&&!host.Scheduled,"gateway-only success keeps independent monster edits");
        Check(host.repaints>=2,"queue and completion repaint the marker");
        host.Draw(EventType.Repaint);EditorApplication.Drain();Check(host.saves==1,"idle repaints never repeat a completed save");
        var all=new QueueHost();all.Queue(true);all.Queue(false);all.Draw(EventType.Repaint);EditorApplication.Drain();
        Check(all.saves==1&&!all.dirty&&!all.dirtyGateways,"save-all merges a pending gateway-only request");
        var failed=new QueueHost{fail=true};failed.Queue(true);failed.Draw(EventType.Repaint);EditorApplication.Drain();
        Check(failed.dirtyGateways&&failed.dirty&&failed.saves==0&&!failed.Scheduled,"rejected write keeps dirty state and releases scheduling state");
        failed.fail=false;failed.coordinate=52;failed.Queue(true);failed.Draw(EventType.Repaint);EditorApplication.Drain();
        Check(failed.saves==1&&failed.savedCoordinate==52&&!failed.dirtyGateways,"failure can be corrected and retried");
        int layoutSaveChecks=LayoutSaveChecks.Run();
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{checks,layoutSaveChecks,unityGuiExecuted=false,scope="Exact production queue and Save methods; actual isolated atomic editor/export file store and content-hash equality, with GUI/AssetDatabase boundaries stubbed. No Unity Editor event/import execution."}));
    }
}
