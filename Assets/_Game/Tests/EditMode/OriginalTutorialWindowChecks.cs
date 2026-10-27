using System;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Events;
namespace DarkSaver.Tests
{
    public static class OriginalTutorialWindowChecks
    {
        public static void Run(Action<bool,string> check,string source)
        {
            var counter=new OriginalTutorialTickCounter();
            check(counter.Read(999,0,20)==20,"native SetTickMenu clamps999 to maximum");
            counter.Advance(100,2,20);check(counter.Read(999,0,20)==20,"full display cannot overflow");
            check(counter.Read(2,1,20)==2,"new CHARMENU resets the displayed value");
            counter.Advance(.25f,2,20);check(counter.Read(2,1,20)==2,"half-point retained before the next point");
            counter.Advance(.25f,2,20);check(counter.Read(2,1,20)==3,"user-selected two points per second");
            check(counter.Read(2,2,20)==2,"same literal in a new command resets elapsed recovery");
            counter.Advance(float.NaN,2,20);counter.Advance(-1,2,20);counter.Advance(float.PositiveInfinity,2,20);
            check(counter.Read(2,2,20)==2,"invalid clock input preserves state");
            check(counter.Read(-1,3,20)==0,"native negative tick clamps to zero");
            int[] ids={52,53,54,55,56,57,58,59,38};string[] keys={"F2","F3","F4","F6","F5","F8","F1","F9","F12"};
            for(int i=0;i<ids.Length;i++)check(OriginalTutorialScript.WindowKeyForControl(ids[i])==keys[i],"native gscene4 control"+ids[i]);
            check(!OriginalTutorialScript.IsWindowKey("F7") && OriginalTutorialScript.WindowKeyForControl(60)==null,"unknown upper Saver F7 is not fabricated");
            var script=new OriginalTutorialScript(source,100);
            var commands=EventScriptParser.Parse(source);
            int Find(string name,string key)=>Enumerable.Range(0,commands.Count).First(i=>commands[i].Name==name && commands[i].Arguments[0]==key);
            var shown=new NativeTutorialProgress{step=Find("SHOWDIALOG","F5"),dialogs=new[]{"F5"}};script.Prepare(shown);
            check(!shown.dialogs.Contains("F5"),"native SHOWDIALOG toggles an already visible window");
            var hidden=new NativeTutorialProgress{step=Find("HIDEDIALOG","F5")};script.Prepare(hidden);
            check(hidden.dialogs.Contains("F5"),"native HIDEDIALOG shares the same toggle handler");
            var f7=new NativeTutorialProgress{step=Find("SHOWDIALOG","F7")};script.Prepare(f7);
            check(!f7.dialogs.Contains("F7"),"native script F7 reaches the default key branch without inventing a dialog");
            var p=new NativeTutorialProgress();script.Prepare(p);
            check(!script.Input(p,"WindowKey",key:"F5"),"DISABLE KEYBOARD rejects arbitrary F-key");
            check(!script.Input(p,"WindowButton",actor:56),"closed F12 rejects stale buttons");
            var moveState=new NativeTutorialProgress{actors=new[]{new TutorialActor{id=0,x=1,y=1,targetX=2,targetY=2,moving=true}}};
            bool invalidFacing=false;try{OriginalTutorialScript.CompleteMovement(moveState,0,"DIAGONAL");}catch(ArgumentException){invalidFacing=true;}
            check(invalidFacing && moveState.actors[0].moving && moveState.actors[0].x==1,"invalid movement facing is rejected before state changes");
            OriginalTutorialScript.CompleteMovement(moveState,0,"RIGHT");check(moveState.actors[0].facing=="RIGHT" && !moveState.actors[0].moving,"last native route facing survives movement completion");
            bool f12=false,keyWait=false,tickDemo=false,attackFacing=false;int guard=0;
            while(!p.IsComplete && guard++<1000)
            {
                var command=script.Current(p);
                if(script.IsTickDemonstration(p))tickDemo=true;
                if(p.dialogs.Contains("F12") && !f12)
                {
                    int step=p.step;string before=string.Join(";",p.actors.Select(a=>$"{a.id},{a.x},{a.y},{a.hp},{a.mp},{a.tick}"));
                    check(script.Input(p,"WindowButton",actor:56) && p.dialogs.Contains("F5") && p.step==step,"F12 opens commander window without consuming the lesson");
                    check(script.Input(p,"WindowButton",actor:56) && !p.dialogs.Contains("F5") && p.step==step,"F12 toggles commander window without consuming the lesson");
                    check(script.Input(p,"WindowButton",actor:54) && p.dialogs.Contains("F4") && p.step==step,"F12 victory button shows the original window");
                    check(script.Input(p,"WindowButton",actor:54) && p.dialogs.Contains("F4") && p.step==step,"repeated native show button does not hide an already open window");
                    check(script.Input(p,"WindowClose",key:"F4") && p.step==step,"native close removes only the selected window");
                    check(!script.Input(p,"WindowButton",actor:999) && p.step==step,"unknown native button cannot advance");
                    check(script.Input(p,"WindowClose",key:"F12") && !p.dialogs.Contains("F12") && p.step==step,"close dismisses only its own window");
                    check(before==string.Join(";",p.actors.Select(a=>$"{a.id},{a.x},{a.y},{a.hp},{a.mp},{a.tick}")),"window actions preserve practice stats and actor positions");f12=true;
                }
                if(command.Name=="WAITKEYBOARD" && p.keyboard && !keyWait)
                {
                    int step=p.step;check(!script.Input(p,"WindowKey",key:"F5") && p.step==step,"native tutorial ignores another F-key during a key wait");
                    check(!p.dialogs.Contains("F5"),"ignored key cannot create a dialog as a side effect");keyWait=true;
                }
                var input=LocalOnboardingChecks.RequiredInput(p,script);
                bool ok;
                if(input.Kind=="MovementCompleted") {OriginalTutorialScript.CompleteMovement(p,input.Actor);script.Prepare(p);ok=true;}
                else ok=script.Input(p,input.Kind,input.Actor,input.Target,input.X,input.Y,input.Key,input.Stage);
                check(ok,"original typed input remains reachable after window actions"+guard);
                if(input.Kind=="Attack" && !attackFacing)
                {
                    check(OriginalTutorialScript.Actor(p,0).facing=="RIGHT" && OriginalTutorialScript.Actor(p,400).facing=="LEFT","original adjacent attack keeps both facing directions after its FX");attackFacing=true;
                }
            }
            check(guard<1000 && p.IsComplete && f12 && keyWait && tickDemo,"all original tutorial waits still lead to END");
        }
    }
}
