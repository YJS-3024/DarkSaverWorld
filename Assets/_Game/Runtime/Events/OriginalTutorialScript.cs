using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Core;
namespace DarkSaver.Events
{
    // Executes the recovered TUTORIAL.DEO. No combat formula, rewards or permanent grants.
    public sealed class OriginalTutorialScript
    {
        private readonly IReadOnlyList<EventCommand> commands;
        private readonly int playerJob;
        public OriginalTutorialScript(string source,int job)
        {
            if(source==null)throw new ArgumentException("Missing original tutorial script.");
            using(var sha=System.Security.Cryptography.SHA256.Create())
                if(BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source.Replace("\r","")))).Replace("-","").ToLowerInvariant()!="eb5216f5c0cf3b6a6b3a61cbd3f70876b1e4ed5997295924a2d9fbe5070e715d")
                    throw new ArgumentException("Original tutorial source hash differs.");
            commands=EventScriptParser.Parse(source);playerJob=job;
            if(commands.Count!=NativeTutorialProgress.Complete || commands[0].Name!="LOADSCENE" || commands[commands.Count-1].Name!="END")
                throw new ArgumentException("Original tutorial script revision does not match.");
        }
        public EventCommand Current(NativeTutorialProgress state)=>state.step<commands.Count?commands[state.step]:default;
        public bool IsTickDemonstration(NativeTutorialProgress state)=>state.step>0 && state.step<commands.Count
            && commands[state.step].Name=="DO" && commands[state.step-1].Name=="CHARMENU";
        public static bool IsWait(string name)=>name!=null && (name.StartsWith("WAIT",StringComparison.Ordinal) || name=="DO" || name=="DO2");
        public void Prepare(NativeTutorialProgress state)
        {
            if(state.version!=2)throw new InvalidOperationException("Migrate legacy tutorial state first.");
            while(state.step<commands.Count)
            {
                var c=commands[state.step];
                if(IsWait(c.Name))
                {
                    if((c.Name=="DO2" || c.Name=="WAITMOVEEND" || c.Name=="WAITFORMOVEEND") && !Actor(state,N(c,0)).moving)
                    {state.step++;continue;}
                    return;
                }
                Execute(state,c);state.step++;
            }
            state.finished=true;
        }
        public bool Input(NativeTutorialProgress state,string kind,int actor=-1,int target=-1,int x=-1,int y=-1,string key=null,int stage=-1)
        {
            Prepare(state);if(state.finished)return false;var c=Current(state);bool matches=false;
            // Native dialog buttons do not synthesize keyboard waits or skip the current lesson.
            if(kind=="WindowClose")
            {
                if(!IsWindowKey(key) || !state.dialogs.Contains(key))return false;
                state.dialogs=state.dialogs.Where(k=>k!=key).ToArray();return true;
            }
            if(kind=="WindowButton")
            {
                if(!state.dialogs.Contains("F12"))return false;
                string window=WindowKeyForControl(actor);if(window==null)return false;
                // Native F5 button toggles its roster; other entries show their window, close hides F12.
                if(actor==56 || actor==38)Toggle(state,window);
                else if(!state.dialogs.Contains(window))state.dialogs=state.dialogs.Concat(new[]{window}).ToArray();
                return true;
            }
            if(kind=="WindowKey")
            {
                if(!state.keyboard || !IsWindowKey(key))return false;
                // GameCtl0x10002ED7..2F1F: tutorial keyboard input is gated to its active key wait.
                return c.Name=="WAITKEYBOARD" && key==c.Arguments[0] && Input(state,"Key",key:key);
            }
            switch(c.Name)
            {
                case "WAITMOUSEBUTTON":matches=kind=="Click";break;
                case "DO":matches=kind=="Delay";break; // The presenter waits the literal millisecond interval.
                case "WAITKEYBOARD":matches=state.keyboard && kind=="Key" && key==c.Arguments[0];break;
                case "WAITMOUSEMOVE":matches=state.mouseMove && kind=="Hover" && actor==N(c,0);break;
                case "WAITTOPMENU":matches=state.mouseMenu && kind=="TopMenu" && actor==N(c,0);break;
                case "WAITMOVEMENU":matches=state.mouseMenu && kind=="MoveMenu" && actor==N(c,0);break;
                case "WAITMOVEPOS":
                    matches=state.mouseMenu && kind=="MovePosition" && actor==N(c,0) && x==N(c,1) && y==N(c,2) && (key==null || IsFacing(key));
                    if(matches){var a=Actor(state,actor);if(key!=null)a.facing=key;a.x=x;a.y=y;a.targetX=x;a.targetY=y;a.moving=false;}break;
                case "WAITMOVEEND":case "WAITFORMOVEEND":case "DO2":
                    matches=kind=="MoveEnd" && actor==N(c,0) && !Actor(state,actor).moving;break;
                case "WAITATTACKMENU":matches=state.mouseMenu && kind=="AttackMenu" && actor==N(c,0);break;
                case "WAITATTACKCHAR":matches=state.mouseMenu && kind=="Attack" && actor==N(c,0) && target==N(c,1);break;
                case "WAITRESTMENU":matches=state.mouseMenu && kind=="Rest" && actor==N(c,0);break;
                case "WAITMAGICMENU":matches=state.mouseMenu && kind=="Magic" && stage==N(c,0) && actor==N(c,1)
                    && (stage!=2 || target==N(c,2));break;
                case "WAITORDERMENU":matches=state.mouseMenu && kind=="Order" && stage==N(c,0) && actor==N(c,1);break;
                case "WAITALLYBUTTON":matches=kind=="Ally" && state.dialogs.Contains("F3");break;
            }
            if(!matches)return false;
            if(c.Name=="WAITATTACKCHAR")
            {
                var from=Actor(state,actor);var to=Actor(state,target);
                string facing=FacingDelta(to.x-from.x,to.y-from.y);if(facing!=null)from.facing=facing;
                facing=FacingDelta(from.x-to.x,from.y-to.y);if(facing!=null)to.facing=facing;
            }
            if(c.Name=="WAITTOPMENU")state.topMenu=actor;
            if(c.Name=="WAITMOVEMENU" || c.Name=="WAITATTACKMENU" || c.Name=="WAITMAGICMENU" || c.Name=="WAITORDERMENU")state.topMenu=-1;
            if(c.Name=="WAITKEYBOARD")Toggle(state,key);
            switch(c.Name)
            {
                case "WAITMOVEMENU":state.activeCommand="Move";break;
                case "WAITATTACKMENU":state.activeCommand="Attack";break;
                case "WAITMAGICMENU":state.practiceMenu=stage==0?"Magic":"";state.activeCommand=stage==1?"MagicTarget":"";break;
                case "WAITORDERMENU":state.practiceMenu=stage==0?"Order":"";break;
                case "WAITMOVEPOS":case "WAITATTACKCHAR":state.activeCommand="";break;
            }
            state.step++;Prepare(state);return true;
        }
        public static bool IsFacing(string value)=>value=="UP" || value=="DOWN" || value=="LEFT" || value=="RIGHT";
        public static string FacingDelta(int dx,int dy)=>dx<0?"LEFT":dx>0?"RIGHT":dy<0?"UP":dy>0?"DOWN":null;
        public static void CompleteMovement(NativeTutorialProgress state,int id,string facing=null)
        {
            if(facing!=null && !IsFacing(facing))throw new ArgumentException("Unknown native facing.");
            var a=Actor(state,id);if(facing!=null)a.facing=facing;a.x=a.targetX;a.y=a.targetY;a.moving=false;
        }
        public static TutorialActor Actor(NativeTutorialProgress s,int id)=>Array.Find(s.actors,a=>a.id==id)
            ?? throw new InvalidOperationException("Unknown tutorial actor "+id);
        private static int N(EventCommand c,int i)=>int.Parse(c.Arguments[i],System.Globalization.CultureInfo.InvariantCulture);
        public static bool IsWindowKey(string key)=>key=="F1" || key=="F2" || key=="F3" || key=="F4" || key=="F5" || key=="F6" || key=="F8" || key=="F9" || key=="F12";
        // GameCtl 0x10004B05 jump table, original gscene4 control IDs (F7 has no button).
        public static string WindowKeyForControl(int nativeId)=>nativeId switch {
            52=>"F2",53=>"F3",54=>"F4",55=>"F6",56=>"F5",57=>"F8",58=>"F1",59=>"F9",38=>"F12",_=>null};
        private static void Toggle(NativeTutorialProgress s,string key)
        {s.dialogs=s.dialogs.Contains(key)?s.dialogs.Where(k=>k!=key).ToArray():s.dialogs.Concat(new[]{key}).ToArray();}
        private void Execute(NativeTutorialProgress s,EventCommand c)
        {
            switch(c.Name)
            {
                case "LOADSCENE":s.map=N(c,0);break;
                case "PLAYMIDI":s.music=c.Arguments[0];break;
                case "SETPOSITION":s.cameraX=N(c,0);s.cameraY=N(c,1);break;
                case "TUTORIAL":s.initialized=true;break;
                case "ENABLE":case "DISABLE":
                    bool enable=c.Name=="ENABLE";
                    switch(c.Arguments[0]){case "MOUSEMENU":s.mouseMenu=enable;break;case "KEYBOARD":s.keyboard=enable;break;case "MOUSEMOVE":s.mouseMove=enable;break;default:throw new InvalidOperationException("Unknown input gate");}break;
                case "CHARPOS":
                    var actor=new TutorialActor {id=N(c,1),job=c.Arguments[0]=="COM0"?playerJob:N(c,0),x=N(c,2),y=N(c,3),targetX=N(c,2),targetY=N(c,3)};
                    s.actors=s.actors.Where(a=>a.id!=actor.id).Concat(new[]{actor}).ToArray();break;
                case "CHARABILITY":OriginalTutorialAbilities.Apply(Actor(s,N(c,0)));break;
                case "CHARSETMAGIC":Actor(s,N(c,0)).magic=N(c,2);break;
                case "CHARSETSKILL":Actor(s,N(c,0)).skill=N(c,2);break;
                case "CHARMENU":var a=Actor(s,N(c,0));a.tick=N(c,1);a.tickEpoch++;if(c.Arguments.Length>2)a.mask=N(c,2);break;
                case "MESSAGE":
                    s.messageMode=N(c,0);s.portrait=c.Arguments[1];
                    // Blank MESSAGE clears the previous text too.
                    s.message=c.Arguments.Length<=4?"":c.SourceText.Substring(AfterTokens(c.SourceText,5));break;
                case "SETTOPMENU":s.topMenu=-1;s.practiceMenu="";s.activeCommand="";s.menuEpoch++;break;
                case "DISPLAYPOS":if(c.Arguments[0]=="CLEAR"){s.highlightX=s.highlightY=-1;}else{s.highlightX=N(c,0);s.highlightY=N(c,1);}break;
                case "CHARMOVE":var mover=Actor(s,N(c,0));mover.targetX=N(c,1);mover.targetY=N(c,2);mover.moving=mover.x!=mover.targetX || mover.y!=mover.targetY;break;
                case "HPSET":Actor(s,N(c,0)).hp=N(c,1);break;
                // Neither early nor late GameCtl recognizes these two literal spellings.
                // Do not silently turn them into HPSET/MPSET aliases and change native behavior.
                case "SETHP":case "SETMP":s.unappliedLiteralCount++;s.lastUnappliedLiteral=c.Name;break;
                case "KILLCHAR1":Actor(s,N(c,0)).removed=true;break;
                case "KILLCHAR2":Actor(s,N(c,0)).dead=true;break;
                // Both native command names share0x10011FDC and invoke the same F-key toggle.
                case "SHOWDIALOG":case "HIDEDIALOG":if(IsWindowKey(c.Arguments[0]))Toggle(s,c.Arguments[0]);break;
                case "CLEARALLDIALOG":s.dialogs=Array.Empty<string>();break;
                case "SETVICTORY":s.victory=string.Join(" ",c.Arguments);break;
                case "SETALLYINFO":s.ally=N(c,0);break;
                case "CHARDIR":Actor(s,N(c,0)).facing=c.Arguments[1];break;
                case "ENDGAME":break; // END commits completion, then the presenter restores the town view.
                case "END":s.finished=true;break;
                default:throw new InvalidOperationException("Unimplemented tutorial command "+c.Name+" at "+c.LineNumber);
            }
        }
        private static int AfterTokens(string s,int count)
        {int i=0;for(int n=0;n<count;n++){while(i<s.Length && char.IsWhiteSpace(s[i]))i++;while(i<s.Length && !char.IsWhiteSpace(s[i]))i++;}while(i<s.Length&&char.IsWhiteSpace(s[i]))i++;return i;}
    }
}
