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
            switch(c.Name)
            {
                case "WAITMOUSEBUTTON":matches=kind=="Click";break;
                case "DO":matches=kind=="Delay";break; // The presenter waits the literal millisecond interval.
                case "WAITKEYBOARD":matches=state.keyboard && kind=="Key" && key==c.Arguments[0];break;
                case "WAITMOUSEMOVE":matches=state.mouseMove && kind=="Hover" && actor==N(c,0);break;
                case "WAITTOPMENU":matches=state.mouseMenu && kind=="TopMenu" && actor==N(c,0);break;
                case "WAITMOVEMENU":matches=state.mouseMenu && kind=="MoveMenu" && actor==N(c,0);break;
                case "WAITMOVEPOS":
                    matches=state.mouseMenu && kind=="MovePosition" && actor==N(c,0) && x==N(c,1) && y==N(c,2);
                    if(matches){var a=Actor(state,actor);a.x=x;a.y=y;a.targetX=x;a.targetY=y;a.moving=false;}break;
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
        public static void CompleteMovement(NativeTutorialProgress state,int id)
        {var a=Actor(state,id);a.x=a.targetX;a.y=a.targetY;a.moving=false;}
        public static TutorialActor Actor(NativeTutorialProgress s,int id)=>Array.Find(s.actors,a=>a.id==id)
            ?? throw new InvalidOperationException("Unknown tutorial actor "+id);
        private static int N(EventCommand c,int i)=>int.Parse(c.Arguments[i],System.Globalization.CultureInfo.InvariantCulture);
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
                case "CHARMENU":var a=Actor(s,N(c,0));a.tick=N(c,1);if(c.Arguments.Length>2)a.mask=N(c,2);break;
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
                case "SHOWDIALOG":if(!s.dialogs.Contains(c.Arguments[0]))Toggle(s,c.Arguments[0]);break;
                case "HIDEDIALOG":s.dialogs=s.dialogs.Where(k=>k!=c.Arguments[0]).ToArray();break;
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
