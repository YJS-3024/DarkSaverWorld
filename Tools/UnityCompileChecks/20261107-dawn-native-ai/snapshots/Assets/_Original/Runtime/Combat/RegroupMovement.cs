using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed class BattleRegroupPlan
    {
        public int member,commander,x,y,searchRadius,movementType;
        public long memberLife,commanderLife;
        public TutorialTile[] path;
    }
    public static class OriginalRegroupMovement
    {
        public static bool ValidMovementType(int? type)=>!type.HasValue||type>=0&&type<10;
        public static BattleRegroupPlan Plan(TutorialMap map,OriginalCombatCatalog catalog,TrainingUnit[] units,int member,HashSet<TutorialTile> blocked)
        {
            // This adapter owns commander0; other received players' groups are never auto-controlled.
            if(map==null||catalog==null||units==null||member<1||member>9)return null;
            var actor=units.FirstOrDefault(u=>u.id==member);var leader=units.FirstOrDefault(u=>u.id==OriginalCommandRange.CommanderId(member));
            var range=OriginalCommandRange.Evaluate(units).FirstOrDefault(r=>r.member==member);
            if(actor==null||leader?.canCommand!=true||range?.regroup!=true||!actor.canMove||!actor.movementBudget.HasValue||!actor.nativeState.HasValue)return null;
            blocked=blocked??new HashSet<TutorialTile>();
            uint state=actor.nativeState.Value;
            // ExecuteSoldierAI: a queued movement branch, or an idle soldier with full AP.
            // State10 without20 executes SoldierAIAction instead; state1 is already moving.
            if((state&1)!=0||((state&0x20)==0&&((state&0x10)!=0||actor.ap!=actor.maxAp)))return null;
            int radius=range.regroupSearchRadius.Value;
            // GetMovePos mode1/2 may enter MoveAttack inside its search radius. Do not substitute
            // nearest-position movement for that separate combat targeting branch.
            bool moveAttack=leader.troopOrder==4||leader.troopOrder==2&&actor.hp>=(long)actor.maxHp*50/100;
            if(moveAttack&&range.distance<=radius)return null;
            if(!actor.movementType.HasValue&&catalog.version<3)return null;
            int? type=actor.movementType??catalog.Job(actor.job)?.rawMovementType;
            if(!type.HasValue||!ValidMovementType(type)||actor.movementBudget<0||actor.movementBudget>100000||!map.Contains(actor.Tile))return null;
            var occupied=units.Where(OriginalCommandRange.OccupiesTile).GroupBy(u=>u.Tile).ToDictionary(g=>g.Key,g=>g.FirstOrDefault(u=>u.id/50!=member/50)?.id??g.First().id);
            var scores=Reachable(map,actor,actor.movementBudget.Value,type.Value,occupied,blocked);
            long bestWeight=-1;int remaining=99,bestX=-1,bestY=-1;
            // Native nearest helper overlays SetArea(center,49), then scans X outside/Y inside.
            // First maximize area weight; ties choose the smallest remaining movement score <99.
            for(int x=0;x<map.width;x++)for(int y=0;y<map.height;y++)
            {
                var tile=new TutorialTile(x,y);int score=scores[y*map.width+x];
                if(score<0||occupied.ContainsKey(tile)||blocked.Contains(tile))continue;
                long distance=OriginalCommandRange.Distance(leader.x,leader.y,x,y);
                long weight=distance<=50?50-distance:distance<=(long)radius+1?(long)radius+1-distance:-1;
                if(weight>bestWeight){bestWeight=weight;remaining=99;bestX=bestY=-1;}
                if(weight==bestWeight&&weight>=0&&score<remaining){remaining=score;bestX=x;bestY=y;}
            }
            // MoveCharPath only accepts positive native tile coordinates below200.
            if(bestX<=0||bestY<=0||bestX>=200||bestY>=200)return null;
            var pathBlocked=new HashSet<TutorialTile>(blocked);
            foreach(var pair in occupied)if(pair.Value/50!=member/50)pathBlocked.Add(pair.Key);
            var path=map.Path(actor.Tile,new TutorialTile(bestX,bestY),actor.movementBudget.Value+7,pathBlocked,type.Value);
            if(path==null||path.Count==0)return null;
            return new BattleRegroupPlan{member=member,commander=leader.id,memberLife=actor.life,commanderLife=leader.life,x=bestX,y=bestY,searchRadius=radius,movementType=type.Value,path=path.ToArray()};
        }
        internal static int[] Reachable(TutorialMap map,TrainingUnit actor,int budget,int type,Dictionary<TutorialTile,int> occupied,HashSet<TutorialTile> blocked)
        {
            var scores=Enumerable.Repeat(-1,map.width*map.height).ToArray();
            // Connected battle SetArea2/4 excludes the outside border even for flying units.
            if(actor.x<1||actor.y<1||actor.x>=map.width-1||actor.y>=map.height-1)return scores;
            scores[actor.y*map.width+actor.x]=budget+1;
            var queue=new Queue<(TutorialTile tile,int score)>();
            Action<TutorialTile,int> spread=(tile,score)=>
            {queue.Enqueue((new TutorialTile(tile.X-1,tile.Y),score));queue.Enqueue((new TutorialTile(tile.X+1,tile.Y),score));queue.Enqueue((new TutorialTile(tile.X,tile.Y-1),score));queue.Enqueue((new TutorialTile(tile.X,tile.Y+1),score));};
            spread(actor.Tile,budget);
            while(queue.Count>0)
            {
                var step=queue.Dequeue();var tile=step.tile;
                if(tile.X<1||tile.Y<1||tile.X>=map.width-1||tile.Y>=map.height-1||step.score<1||step.score<=scores[tile.Y*map.width+tile.X]||blocked.Contains(tile))continue;
                int cost=map.Terrain(tile,type*3+2);
                if(cost<0||cost==99||occupied.TryGetValue(tile,out int other)&&other/50!=actor.id/50)continue;
                scores[tile.Y*map.width+tile.X]=step.score;
                if(step.score-cost>0)spread(tile,step.score-cost);
            }
            return scores;
        }
        public static bool SameExceptMovement(TrainingUnit before,TrainingUnit after)
        {
            var adjusted=after.Copy();adjusted.x=before.x;adjusted.y=before.y;adjusted.direction=before.direction;adjusted.ap=before.ap;adjusted.nativeState=before.nativeState;
            return before.troopOrder==after.troopOrder&&OriginalTroopOrders.SameExceptOrder(before,adjusted);
        }
    }
}
