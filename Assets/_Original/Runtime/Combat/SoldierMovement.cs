using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed class SoldierMovementChoice
    {
        public BattleRegroupPlan plan;
        public int hintAfter;
        public bool unavailable;
    }
    public sealed class NativeAiPosition
    {
        public int x=-1,y=-1,cursor,restHint;
        public bool unavailable;
    }
    public static class OriginalSoldierMovement
    {
        public static SoldierMovementChoice Find(WorldMapDefinition definition,OriginalCombatCatalog catalog,TrainingUnit[] units,int member,int hint,Func<int,int> choose)
        {
            var result=new SoldierMovementChoice{hintAfter=hint};
            var actor=units.First(u=>u.id==member);var leader=units.First(u=>u.id==0);var map=definition.map;
            bool native=OriginalSoldierActions.NativeContext(units,member),follow=!native||OriginalSoldierActions.Active(leader);
            if(!native&&!actor.canMove)return result;
            int? kind=OriginalMemberCommands.MovementType(actor,catalog);
            if(!actor.movementBudget.HasValue||follow&&!leader.commandRange.HasValue||!kind.HasValue||!OriginalRegroupMovement.ValidMovementType(kind)){result.unavailable=true;return result;}
            int radius=follow?Math.Max(2,leader.commandRange.Value):20,mode=OriginalSoldierActions.ActionMode(units,member);
            var position=FindPosition(definition,catalog,units,member,follow?leader.x:actor.x,follow?leader.y:actor.y,radius,mode,hint,choose,!native);
            result.hintAfter=position.restHint;result.unavailable=position.unavailable;
            if(position.unavailable||position.x<=0||position.y<=0||position.x>=200||position.y>=200)return result;
            var blocked=OriginalMemberCommands.StaticBlocked(definition);
            var occupied=units.Where(OriginalCommandRange.OccupiesTile).GroupBy(u=>u.Tile).ToDictionary(g=>g.Key,g=>g.FirstOrDefault(u=>u.id/50!=member/50)?.id??g.First().id);
            var target=new TutorialTile(position.x,position.y);
            var pathBlocked=new HashSet<TutorialTile>(blocked);foreach(var p in occupied)if(p.Value/50!=member/50)pathBlocked.Add(p.Key);
            var path=map.Path(actor.Tile,target,actor.movementBudget.Value+7,pathBlocked,kind.Value);
            if(path==null||path.Count==0)return result;
            result.plan=new BattleRegroupPlan{member=member,commander=0,memberLife=actor.life,commanderLife=leader.life,x=target.X,y=target.Y,movementType=kind.Value,searchRadius=radius,path=path.ToArray()};return result;
        }
        // UserUnit03480, with its complete exported ABI. A commander supplies
        // an AREA/follow/self region; a follower supplies its commander region.
        // cursor and the actor's1FC rest hint are separate native storage.
        public static NativeAiPosition FindPosition(WorldMapDefinition definition,OriginalCombatCatalog catalog,TrainingUnit[] units,int member,
            int centerX,int centerY,int radius,int mode,int restHint,Func<int,int> choose,bool ownFollower=false,int cursor=0)
        {
            var result=new NativeAiPosition{cursor=cursor,restHint=restHint};
            var actor=units?.FirstOrDefault(u=>u.id==member);var map=definition?.map;
            int? kind=OriginalMemberCommands.MovementType(actor,catalog);
            if(actor==null||map==null||!actor.movementBudget.HasValue||!kind.HasValue||!OriginalRegroupMovement.ValidMovementType(kind)||!map.Contains(actor.Tile))
            {result.unavailable=true;return result;}
            if(mode!=1&&mode!=2&&mode!=3)return result;
            result.cursor=-1;
            var blocked=OriginalMemberCommands.StaticBlocked(definition);
            var occupied=units.Where(OriginalCommandRange.OccupiesTile).GroupBy(u=>u.Tile).ToDictionary(g=>g.Key,g=>g.FirstOrDefault(u=>u.id/50!=member/50)?.id??g.First().id);
            var scores=OriginalRegroupMovement.Reachable(map,actor,actor.movementBudget.Value,kind.Value,occupied,blocked);
            bool attackMove=mode==2||mode==1&&actor.hp>=(long)actor.maxHp*50/100;
            //03480 clamps only the healthy mode1 MoveAttack argument. Mode2
            // and low-HP/defensive nearest-position searches keep the raw radius.
            if(mode==1&&attackMove)radius=Math.Max(1,radius);
            TutorialTile? target=null;bool useNearest=true;
            if(attackMove&&OriginalCommandRange.Distance(actor.x,actor.y,centerX,centerY)<=radius)
            {
                var inRange=OriginalSoldierActions.Candidates(definition,catalog,units,actor,ownFollower);
                if(inRange==null||!actor.attackRange.HasValue){result.unavailable=true;return result;}
                if(inRange.Length>0)
                {
                    if(choose==null){result.unavailable=true;return result;}
                    int index=choose(inRange.Length);if(index<0||index>=inRange.Length)result.unavailable=true;
                    return result; // Already able to attack: native MoveAttack requests no movement.
                }
                var danger=Enumerable.Repeat(-1,map.width*map.height).ToArray();int dangerRadius=Math.Min(radius,12);
                //030B0 includes every enemy still occupying mainmap, even
                // a corpse that no longer qualifies for an attack query.
                foreach(var enemy in units.Where(OriginalCommandRange.OccupiesTile).OrderBy(u=>u.x).ThenBy(u=>u.y))
                {
                    if(OriginalCommandRange.Distance(centerX,centerY,enemy.x,enemy.y)>(long)actor.attackRange.Value+radius+2)continue;
                    bool? permitted=OriginalSoldierActions.Enemy(actor,enemy);
                    if(!permitted.HasValue){result.unavailable=true;return result;}if(permitted==false)continue;
                    // SetArea overwrites earlier overlapping areas; it is not a sum/max heatmap.
                    for(int x=0;x<map.width;x++)for(int y=0;y<map.height;y++)
                    {long d=OriginalCommandRange.Distance(enemy.x,enemy.y,x,y);if(d<=dangerRadius+1)danger[y*map.width+x]=(int)(dangerRadius+1-d);}
                }
                int best=-1;
                for(int x=0;x<map.width;x++)for(int y=0;y<map.height;y++)
                {
                    var tile=new TutorialTile(x,y);int index=y*map.width+x;
                    if(scores[index]<0||OriginalCommandRange.Distance(centerX,centerY,x,y)>radius||blocked.Contains(tile))continue;
                    if(occupied.TryGetValue(tile,out int occupant)&&occupant!=member)continue;
                    if(danger[index]>best)best=danger[index];
                }
                if(best>=0)
                {
                    useNearest=false;int remaining=99;
                    for(int x=0;x<map.width;x++)for(int y=0;y<map.height;y++)
                    {
                        var tile=new TutorialTile(x,y);int index=y*map.width+x;
                        if(scores[index]<0||OriginalCommandRange.Distance(centerX,centerY,x,y)>radius||blocked.Contains(tile)||occupied.ContainsKey(tile)||danger[index]!=best)continue;
                        if(scores[index]<remaining){remaining=scores[index];target=tile;}
                    }
                }
                else if(actor.hp<actor.maxHp)result.restHint=99;
            }
            if(useNearest)
            {
                long best=-1;int remaining=99;
                for(int x=0;x<map.width;x++)for(int y=0;y<map.height;y++)
                {
                    var tile=new TutorialTile(x,y);int score=scores[y*map.width+x];
                    if(score<0||occupied.ContainsKey(tile)||blocked.Contains(tile))continue;
                    long distance=OriginalCommandRange.Distance(centerX,centerY,x,y);
                    long weight=distance<=50?50-distance:distance<=(long)radius+1?(long)radius+1-distance:-1;
                    if(weight>best){best=weight;remaining=99;target=null;}
                    if(weight==best&&weight>=0&&score<remaining){remaining=score;target=tile;}
                }
            }
            if(target.HasValue){result.x=target.Value.X;result.y=target.Value.Y;}return result;
        }
    }
}
