using System;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Events;
namespace DarkSaver.Tests
{
    public static class OriginalTutorialRangeChecks
    {
        public static void Run(Action<bool,string> check)
        {
            var p=new NativeTutorialProgress {step=29,actors=new[]{
                new TutorialActor {id=0,job=100,x=4,y=7,hp=95,tick=999},
                new TutorialActor {id=100,job=125,x=5,y=6},
                new TutorialActor {id=400,job=301,x=5,y=7},
                new TutorialActor {id=410,job=301,x=6,y=7}}};
            var diamond=new[]{(4,7),(3,7),(5,7),(4,6),(4,8)};
            bool Enemy(TutorialActor a)=>a.id/100==4;
            check(OriginalTutorialRanges.MovementBudget==12 && OriginalTutorialRanges.AttackRadius==1,"native temporary character defaults, not job supplements");
            check(OriginalTutorialRanges.TryBuild(p,0,TutorialRangeMode.Move,20,15,diamond,0,null,out var move)
                && move.Length==5 && move.All(c=>!c.Target),"movement display preserves supplied terrain area without attack targets");
            check(OriginalTutorialRanges.TryBuild(p,0,TutorialRangeMode.Attack,20,15,diamond,0,Enemy,out var attack)
                && attack.Single(c=>c.Target).X==5 && attack.Single(c=>c.Target).Y==7,"adjacent original practice monster highlighted");
            check(!attack.Any(c=>c.X==6 && c.Y==7),"distant monster is not shown as in attack range");
            check(OriginalTutorialRanges.TryBuild(p,0,TutorialRangeMode.Magic,20,15,null,3,Enemy,out var magic)
                && magic.Length==25 && magic.Count(c=>c.Target)==2,"fireball distance3 native Manhattan area and both eligible targets");
            check(!magic.Any(c=>c.X==7 && c.Y==8) && magic.Any(c=>c.X==7 && c.Y==7),"casting range is not square or Euclidean");
            check(!magic.Single(c=>c.X==5 && c.Y==6).Target,"allied instructor is not painted as hostile");
            p.actors[2].dead=true;p.actors[3].removed=true;
            check(OriginalTutorialRanges.TryBuild(p,0,TutorialRangeMode.Magic,20,15,null,3,Enemy,out var after)
                && after.All(c=>!c.Target),"corpse and removed records lose red highlight");
            p.actors[0].x=0;p.actors[0].y=0;
            check(OriginalTutorialRanges.TryBuild(p,0,TutorialRangeMode.Magic,20,15,null,3,Enemy,out var edge)
                && edge.Length==10 && edge.All(c=>c.X>=0 && c.Y>=0),"native map boundary clips cast diamond");
            check(!OriginalTutorialRanges.TryBuild(p,0,TutorialRangeMode.Magic,20,15,null,-1,Enemy,out _),"unresolved negative casting modes are not guessed");
            check(!OriginalTutorialRanges.TryBuild(p,0,TutorialRangeMode.Attack,20,15,new[]{(-1,7)},0,Enemy,out _),"invalid supplied terrain cell is rejected");
            check(OriginalTutorialRanges.TryBuild(p,0,TutorialRangeMode.None,20,15,null,0,null,out var none) && none.Length==0,"closed mode leaves no retained overlay");
            check(p.step==29 && p.actors[0].hp==95 && p.actors[0].tick==999 && p.actors[0].x==0,"display queries do not progress, damage, spend ticks or move");
        }
    }
}
