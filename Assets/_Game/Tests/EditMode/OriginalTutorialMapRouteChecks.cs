using System;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Maps;
namespace DarkSaver.Tests
{
    public static class OriginalTutorialMapRouteChecks
    {
        public static void Run(Action<bool,string> check,string source,byte[] mapBytes)
        {
            var map=MrcMapReader.Read(mapBytes);
            Run(check,source,map.Width,map.Height,(x,y)=>map.TileAt(x,y).TerrainAttribute);
        }
        public static void Run(Action<bool,string> check,string source,int width,int height,Func<int,int,int> attribute)
        {
            check(width==20 && height==15,"original tutorial301 dimensions");
            var script=new OriginalTutorialScript(source,100);var state=new NativeTutorialProgress();script.Prepare(state);
            bool tie=false;int count=0,guard=0;
            void Route(int id,int x,int y)
            {
                var actor=OriginalTutorialScript.Actor(state,id);
                int? Cost(CombatGridCell c)=>CombatTerrainTraversal.TryGetMovementValue(attribute(c.X,c.Y),0,out var cost)?cost:(int?)null;
                int? Occupant(CombatGridCell c)=>state.actors.FirstOrDefault(a=>a.id!=id && !a.dead && !a.removed && a.x==c.X && a.y==c.Y)?.id;
                check(OriginalTutorialMovement.TryPlan(width,height,id,new CombatGridCell(actor.x,actor.y),new CombatGridCell(x,y),Cost,Occupant,tie,out var path,out var next),"actual301 source destination reachable: "+id+"->"+x+","+y);
                check(path.Count==0 && actor.x==x && actor.y==y || path.Count>0 && path.Last().Equals(new CombatGridCell(x,y)),"original route reaches the literal destination");
                tie=next;count++;
            }
            while(!state.IsComplete && guard++<1000)
            {
                var moving=state.actors.FirstOrDefault(a=>a.moving);
                if(moving!=null){Route(moving.id,moving.targetX,moving.targetY);OriginalTutorialScript.CompleteMovement(state,moving.id);script.Prepare(state);continue;}
                var c=script.Current(state);int N(int i)=>int.Parse(c.Arguments[i]);bool accepted;
                switch(c.Name)
                {
                    case "DO":accepted=script.Input(state,"Delay");break;
                    case "WAITMOUSEBUTTON":accepted=script.Input(state,"Click");break;
                    case "WAITKEYBOARD":accepted=script.Input(state,"Key",key:c.Arguments[0]);break;
                    case "WAITMOUSEMOVE":accepted=script.Input(state,"Hover",N(0));break;
                    case "WAITTOPMENU":accepted=script.Input(state,"TopMenu",N(0));break;
                    case "WAITMOVEMENU":accepted=script.Input(state,"MoveMenu",N(0));break;
                    case "WAITMOVEPOS":Route(N(0),N(1),N(2));accepted=script.Input(state,"MovePosition",N(0),x:N(1),y:N(2));break;
                    case "WAITATTACKMENU":accepted=script.Input(state,"AttackMenu",N(0));break;
                    case "WAITATTACKCHAR":accepted=script.Input(state,"Attack",N(0),N(1));break;
                    case "WAITRESTMENU":accepted=script.Input(state,"Rest",N(0));break;
                    case "WAITMAGICMENU":accepted=script.Input(state,"Magic",N(1),N(0)==2?N(2):-1,stage:N(0));break;
                    case "WAITORDERMENU":accepted=script.Input(state,"Order",N(1),stage:N(0));break;
                    case "WAITALLYBUTTON":accepted=script.Input(state,"Ally");break;
                    default:throw new Exception("Unexpected original wait "+c.Name);
                }
                check(accepted,"original typed input after route"+guard);
            }
            check(state.IsComplete && guard<1000 && count==14,"all original301 movement destinations lead to END without terrain bypass");
        }
    }
}
