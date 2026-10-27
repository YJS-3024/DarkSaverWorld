using System;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Events;
namespace DarkSaver.Tests
{
    public static class OriginalTutorialMovementChecks
    {
        public static void Run(Action<bool,string> check)
        {
            var origin=new CombatGridCell(0,0);var target=new CombatGridCell(3,2);
            int? Clear(CombatGridCell c)=>1;int? Empty(CombatGridCell c)=>null;
            bool Plan(int w,int h,CombatGridCell end,Func<CombatGridCell,int?> cost,Func<CombatGridCell,int?> occupant,
                out System.Collections.Generic.IReadOnlyList<CombatGridCell> path)=>OriginalTutorialMovement.TryPlan(w,h,0,origin,end,cost,occupant,false,out path,out _);
            check(Plan(5,5,target,Clear,Empty,out var direct) && direct.Last().Equals(target) && direct.Count==3,"native cardinal backtracking compresses only adjacent diagonal pairs");
            int? Wall(CombatGridCell c)=>c.X==1 && c.Y<3?99:1;
            check(Plan(5,5,target,Wall,Empty,out var detour) && detour.All(c=>Wall(c)!=99) && detour.Last().Equals(target),"terrain wall is routed around, not walked through");
            int? Barrier(CombatGridCell c)=>c.X==1?99:1;
            check(!Plan(5,5,target,Barrier,Empty,out var rejected) && rejected.Count==0,"no fabricated straight route through a sealed wall");
            int? Enemy(CombatGridCell c)=>c.X==1 && c.Y<4?400:(int?)null;
            check(Plan(5,5,target,Clear,Enemy,out var aroundEnemy) && aroundEnemy.All(c=>!Enemy(c).HasValue),"other original group blocks route expansion");
            check(!Plan(5,5,target,Clear,c=>c.Equals(target)?1:(int?)null,out _),"same-group actor still prevents stopping on its occupied destination");
            check(Plan(22,1,new CombatGridCell(19,0),Clear,Empty,out var nativeBudget) && nativeBudget.Count==19,"MoveChar uses temporary movement12 plus native7");
            check(!Plan(22,1,new CombatGridCell(20,0),Clear,Empty,out _),"MoveChar budget is finite rather than an unrestricted path");
            check(!Plan(5,5,new CombatGridCell(-1,0),Clear,Empty,out _),"out of map destinations are rejected");
            check(OriginalTutorialTerrain.TryGet(0,0,out int attack,out int defense,out int movement) && attack==5 && defense==5 && movement==1,"original terrain attack/defense/movement triple preserved");
            check(OriginalTutorialTerrain.TryGet(4,0,out attack,out defense,out movement) && attack==25 && defense==25 && movement==2,"terrain4 is not flattened into a generic floor");
            check(OriginalTutorialTerrain.TryGet(6,0,out attack,out defense,out movement) && movement==99
                && OriginalTutorialTerrain.Display(movement)=="**" && OriginalTutorialTerrain.Display(-99)=="**","native terrain sentinel uses original double-star text");
            check(!OriginalTutorialTerrain.TryGet(27,0,out _,out _,out _) && !OriginalTutorialTerrain.TryGet(0,10,out _,out _,out _),"unknown terrain/troop is not guessed");
            for(int a=0;a<27;a++)for(int t=0;t<10;t++)
                check(OriginalTutorialTerrain.TryGet(a,t,out _,out _,out int expected) && CombatTerrainTraversal.TryGetMovementValue(a,t,out int actual) && expected==actual,
                    "all native terrain movement columns agree with traversal "+a+"/"+t);
        }
    }
}
