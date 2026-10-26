using DarkSaver.UI;
using System.Linq;
using System;
using System.IO;
using DarkSaver.Combat;
using UnityEngine;
using DarkSaver.EditorTools.CanvasTesting;

namespace DarkSaver.EditorTools
{
    public static partial class BattlePlayVerification
    {
        static int nativeMenuStep;
        static int nativeSizeStep;
        static int nativeInspectionStep;
        static T BattleMenuElement<T>(string key) where T : VisualElement
        {
            foreach(var doc in presentation.GetComponentsInChildren<InGameUi>().Select(CanvasProbe.For))
            { var element=doc.rootVisualElement.Q<T>(key); if(element!=null) return element; }
            return null;
        }
        static void ClickBattleMenu(string key)
        {
            var button=BattleMenuElement<Button>(key);
            if(button==null || !button.enabledInHierarchy || button.resolvedStyle.display==DisplayStyle.None)
                throw new InvalidOperationException("Battle button unavailable: "+key);
            var point=button.worldBound.center; var picked=button.panel.Pick(point);
            if(picked!=button && !button.Contains(picked)) throw new InvalidOperationException("Battle button obstructed: "+key);
            using(var down=PointerDownEvent.GetPooled(new Event {type=EventType.MouseDown,button=0,mousePosition=point})) button.SendEvent(down);
            using(var up=PointerUpEvent.GetPooled(new Event {type=EventType.MouseUp,button=0,mousePosition=point})) button.SendEvent(up);
        }
        static bool VerifyNativeBattleMenu()
        {
            // One frame interval per action lets retained UI layout and picking settle.
            switch(nativeMenuStep++)
            {
                case 0:
                    nativeSizeStep=0; nativeInspectionStep=0; UnityEditor.SessionState.SetInt(Key+"helpScrollStep",0);
                    if(!presentation.TryOpenSystemMenu()) throw new InvalidOperationException("Cannot open battle menu");
                    return true;
                case 1:
                    string[] keys={"system-roster","system-alliance","system-victory","system-controls","system-minimap","system-retreat"};
                    for(int i=0;i<keys.Length;i++)
                    {
                        var button=BattleMenuElement<Button>(keys[i]);
                        if(button==null || button.worldBound.width<=0 || button.worldBound.yMax>Rendering.GameScreen.Height)
                            throw new InvalidOperationException("Battle menu layout invalid: "+keys[i]);
                        if(i>0 && BattleMenuElement<Button>(keys[i-1]).worldBound.Overlaps(button.worldBound))
                            throw new InvalidOperationException("Battle menu buttons overlap");
                    }
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"native-battle-menu.png")); return true;
                case 2: ClickBattleMenu("system-minimap"); return true;
                case 3:
                    if(!presentation.IsMinimapOpen || BattleMenuElement<VisualElement>("minimap-ground")==null)
                        throw new InvalidOperationException("Minimap did not render");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"native-battle-minimap.png")); return true;
                case 4: ClickBattleMenu("system-back"); return true;
                case 5: ClickBattleMenu("system-alliance"); return true;
                case 6:
                    RequireWindow(3);
                    if(nativeSizeStep==0)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"alliance-live-names.png"));
                        nativeSizeStep=-1; nativeMenuStep--; return true;
                    }
                    if(nativeSizeStep==-1)
                    {
                        nativeSizeStep=0;
                        ClickBattleMenu("native-window-3-control-0");
                        if(!presentation.IsBattleWindowMinimized) throw new InvalidOperationException("Alliance minimize failed");
                        nativeSizeStep++; nativeMenuStep--; return true;
                    }
                    if(nativeSizeStep==1)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"native-alliance-minimized.png"));
                        nativeSizeStep++; nativeMenuStep--; return true;
                    }
                    if(nativeSizeStep==2)
                    {
                        ClickBattleMenu("native-window-minimized");
                        if(presentation.IsBattleWindowMinimized) throw new InvalidOperationException("Alliance restore failed");
                        nativeSizeStep++; nativeMenuStep--; return true;
                    }
                    if(nativeSizeStep==3)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"native-alliance-restored.png"));
                        nativeSizeStep++; nativeMenuStep--; return true;
                    }
                    Note("PASS: original alliance minimize/restore through hit-tested PointerDown/Up");
                    presentation.TryHandleCancelInput(false); return true;
                case 7: ClickBattleMenu("system-victory"); return true;
                case 8:
                    RequireWindow(5); ScreenCapture.CaptureScreenshot(Path.Combine(output,"native-battle-victory.png")); return true;
                case 9: presentation.TryCloseBattleWindowShell(); return true;
                case 10: ClickBattleMenu("system-controls"); return true;
                case 11:
                    RequireWindow(7);
                    int helpStep=UnityEditor.SessionState.GetInt(Key+"helpScrollStep",0);
                    UnityEditor.SessionState.SetInt(Key+"helpScrollStep",helpStep+1);
                    if(helpStep==0) {
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"native-battle-help-text.png"));nativeMenuStep--;return true;
                    }
                    if(helpStep==1) {ClickBattleMenu("native-battle-help-down");nativeMenuStep--;return true;}
                    if(helpStep==2) {
                        if(BattleMenuElement<ScrollView>("native-battle-help-scroll").scrollOffset.y<=0)throw new InvalidOperationException("Native help down did not scroll");
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"native-battle-help-scrolled.png"));nativeMenuStep--;return true;
                    }
                    presentation.TryCloseBattleWindowShell();return true;
                case 12: battle.CloseAlliedRoster(); ClickBattleMenu("system-roster"); return true;
                case 13:
                    if(nativeInspectionStep==0)
                    {
                        if(!battle.IsAlliedRosterVisible || presentation.IsSystemMenuOpen || !battle.TryInspectUnit(100))
                            throw new InvalidOperationException("Information window did not reopen");
                    }
                    else if(nativeInspectionStep==1)
                    {
                        ClickBattleMenu("inspect-compact-button");
                        if(battle.IsAlliedRosterVisible)throw new InvalidOperationException("Two information windows visible");
                        if(!battle.IsUnitInformationCompact)throw new InvalidOperationException("Compact information failed");
                    }
                    else if(nativeInspectionStep==2) ScreenCapture.CaptureScreenshot(Path.Combine(output,"unit-information-compact.png"));
                    else if(nativeInspectionStep==3)
                    {
                        ClickBattleMenu("inspect-expand");
                        if(battle.IsAlliedRosterVisible)throw new InvalidOperationException("Two information windows visible");
                        if(battle.IsUnitInformationCompact)throw new InvalidOperationException("Expanded information failed");
                    }
                    else if(nativeInspectionStep==4) ScreenCapture.CaptureScreenshot(Path.Combine(output,"unit-information-expanded.png"));
                    else
                    {
                        ClickBattleMenu("inspect-close");
                        if(battle.TryGetInspectedUnit(out _))throw new InvalidOperationException("Information close failed");
                        Note("PASS: live same-unit information compact/expanded and close through hit-tested PointerDown/Up");
                        presentation.TryOpenSystemMenu(); return true;
                    }
                    nativeInspectionStep++; nativeMenuStep--; return true;
                case 14: ClickBattleMenu("system-back"); return true;
                case 15:
                    if(presentation.IsSystemMenuOpen)throw new InvalidOperationException("Menu close failed");
                    presentation.TryOpenSystemMenu(); return true;
                default:
                    Note("PASS: six native menu entries; hit-tested PointerDown/Up minimap/alliance/victory/help/info; cancel and menu return");
                    return false;
            }
        }
        static void RequireWindow(int id)
        {
            if(!presentation.IsBattleWindowShellOpen || presentation.BattleWindowShellId!=id)
                throw new InvalidOperationException("Wrong battle window: "+id);
        }
    }
}