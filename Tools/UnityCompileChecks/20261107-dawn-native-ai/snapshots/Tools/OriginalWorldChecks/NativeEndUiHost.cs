using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;
namespace UnityEngine
{
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
    public struct Color {public int code;public static Color clear=>new Color{code=0};public static Color white=>new Color{code=1};}
    public enum KeyCode {Escape}
    public static class Input {public static bool escape,right;public static bool GetKeyDown(KeyCode key)=>escape;public static bool GetMouseButtonDown(int button)=>right;}
    public sealed class GameObject
    {
        public bool active=true;private readonly Dictionary<Type,object> components=new Dictionary<Type,object>();
        public void SetActive(bool value){active=value;}
        public T AddComponent<T>()where T:new(){var result=new T();components.Add(typeof(T),result);return result;}
        public T Get<T>()=>(T)components[typeof(T)];
    }
    public sealed class RectTransform
    {
        public readonly GameObject gameObject=new GameObject();public string name;public Rect rect;public RectTransform parent;public int raised;
        public void SetAsLastSibling(){raised++;}
    }
}
namespace UnityEngine.UI
{
    public sealed class Image {public UnityEngine.Color color;}
    public enum HorizontalWrapMode {Overflow}
    public sealed class Text {public string text;public UnityEngine.Color color;public HorizontalWrapMode horizontalOverflow;}
    public sealed class Button
    {
        public sealed class Click {private event Action action;public void AddListener(Action value){action+=value;}public void Invoke(){action?.Invoke();}}
        public readonly Click onClick=new Click();
    }
}
namespace DarkSaver.Original.World
{
    // Renderer, server transport and Unity input are explicit managed boundaries.
    // PaintNativeEndNotice and the Update input branch are extracted unchanged.
    public sealed partial class WorldSceneRoot
    {
        public WorldView view;
        public UnityEngine.RectTransform overlay=new UnityEngine.RectTransform(),notice,nativeEndNotice;
        public UnityEngine.UI.Text nativeEndNoticeText;
        public bool showingActions=true;public string trainingMode="attack";
        public readonly List<UnityEngine.RectTransform> groups=new List<UnityEngine.RectTransform>();
        public int frameBuilds,handled,closed,anchors;
        public sealed class Server {public int continues;public WorldReply Request_ContinueScenario(){continues++;return new WorldReply(WorldFailure.None);}}
        public readonly Server server=new Server();
        private UnityEngine.RectTransform Group(UnityEngine.RectTransform parent,string name,UnityEngine.Rect rect)
        {var value=new UnityEngine.RectTransform{parent=parent,name=name,rect=rect};groups.Add(value);return value;}
        private void BuildCampaignNoticeFrame(UnityEngine.RectTransform frame){frameBuilds++;}
        private UnityEngine.UI.Text Label(UnityEngine.RectTransform parent,string name,UnityEngine.Rect rect,string text)=>new UnityEngine.UI.Text{text=text};
        private static string NativeUnitText(byte[] name)=>Encoding.ASCII.GetString(name).Split('\0')[0];
        private void AnchorPersonalLine(UnityEngine.UI.Text text,int x,int y,int width){if(x!=280||y!=23||width!=6)throw new Exception("native notice text anchor");anchors++;}
        private void Handle(WorldReply reply){handled++;}
        private void CloseNotice(){closed++;notice=null;}
        public void TestPaint()=>PaintNativeEndNotice();
        public void TestInput()=>HandleExtractedEndInput();
        public bool TestWaitingResult=>WaitingScenarioResult;
    }
}
internal static class NativeEndUiChecks
{
    private static int assertions;
    private static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
    private static WorldSceneRoot Root(int outcome=1)
    {
        return new WorldSceneRoot{view=new WorldView{record=new WorldRecord{campaignPhase="explore",scenarioVisit=new CampaignVisitRecord{nativeEnd=new NativeScenarioEndRecord{
            stage=NativeScenarioEndRecord.Notice,winner=Encoding.ASCII.GetBytes("Team"),table=new CampaignResultRecord{victory=outcome==1,outcome=outcome==1?"victory":"defeat"}}}}}};
    }
    public static void Main()
    {
        foreach(int outcome in new[]{1,3})
        {
            var root=Root(outcome);root.TestPaint();
            Check(root.nativeEndNotice.gameObject.active&&root.groups.Count==2&&root.frameBuilds==1,"one notice blocker and native frame created");
            var blocker=root.groups[0].rect;var frame=root.groups[1].rect;
            Check(blocker.x==0&&blocker.y==0&&blocker.width==640&&blocker.height==480&&frame.x==40&&frame.y==130&&frame.width==562&&frame.height==66,"native D150 inclusive rectangle is represented by562x66 at40,130");
            Check(root.nativeEndNoticeText.color.code==1&&root.nativeEndNotice.gameObject.Get<UnityEngine.UI.Image>().color.code==0&&root.anchors==1,"source white text, transparent input blocker and native text anchor");
            Check(root.nativeEndNoticeText.text==(outcome==1?"Team님이 승리하셨습니다.":"주지휘관이 죽었습니다."),"source outcome text and received winner bytes");
            Check(!root.showingActions&&root.trainingMode==null&&!root.TestWaitingResult,"notice hides actions and waits before result");
            root.TestPaint();Check(root.groups.Count==2&&root.frameBuilds==1,"repaint reuses notice objects");
            UnityEngine.Input.escape=true;root.TestInput();Check(root.server.continues==0,"Escape does not invent a native notice acknowledgement");UnityEngine.Input.escape=false;
            var hit=root.nativeEndNotice.gameObject.Get<UnityEngine.UI.Button>();hit.onClick.Invoke();Check(root.server.continues==1&&root.handled==1,"actual notice click callback dispatches continuation once");
            root.notice=new UnityEngine.RectTransform();int raised=root.nativeEndNotice.raised;
            root.TestPaint();hit.onClick.Invoke();Check(root.nativeEndNotice.raised==raised&&root.server.continues==1,"generic error notice is not covered or acknowledged through the underlying full-screen button");
            UnityEngine.Input.right=true;root.TestInput();Check(root.closed==1&&root.notice==null&&root.server.continues==1,"right click closes generic error without advancing the underlying native notice");
            root.TestInput();Check(root.server.continues==2,"next right click acknowledges native notice");UnityEngine.Input.right=false;
            root.view.record.scenarioVisit.nativeEnd.stage=NativeScenarioEndRecord.Result;root.TestPaint();Check(!root.nativeEndNotice.gameObject.active&&root.TestWaitingResult,"result stage hides notice and enables the existing result window");
            UnityEngine.Input.escape=true;root.TestInput();Check(root.server.continues==2,"escape does not skip native result confirmation");UnityEngine.Input.escape=false;
            root.view.record.scenarioVisit.nativeEnd.stage=NativeScenarioEndRecord.Outro;root.TestPaint();Check(!root.nativeEndNotice.gameObject.active&&!root.TestWaitingResult,"DEE stage does not reopen result");
            root.view.record.scenarioVisit=null;root.TestPaint();Check(!root.nativeEndNotice.gameObject.active,"source-map return keeps native notice hidden");
        }
        var legacy=Root();legacy.view.record.scenarioVisit.nativeEnd=null;legacy.view.record.campaignPhase="complete";
        Check(legacy.TestWaitingResult,"legacy explicit result stage retains its compatible wait");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Unchanged production native-end paint method, result wait expression and Update input branch executed with managed renderer/input/server boundaries. Native frame placement, outcome text, one-time construction, generic error popup depth/close, click dispatch and notice/result/outro/return visibility. Not Unity null semantics, EventSystem, pixels, physical input or actual Play."}));
    }
}
