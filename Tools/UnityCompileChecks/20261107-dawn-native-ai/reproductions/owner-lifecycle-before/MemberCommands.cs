using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public static class OriginalMemberCommands
    {
        public static bool ValidTargets(int[] targets)=>targets==null||targets.Length<=1000&&targets.All(id=>id>=0&&id<=999)&&targets.Distinct().Count()==targets.Length;
        public static int? MovementType(TrainingUnit unit,OriginalCombatCatalog catalog)
            =>unit?.movementType??(catalog?.version>=3?catalog.Job(unit?.job??-1)?.rawMovementType:null);
        public static bool Controlled(TrainingUnit[] units,int member)
            =>member>0&&member<10&&OriginalCommandRange.Living(units?.FirstOrDefault(u=>u.id==member))
                &&units?.FirstOrDefault(u=>u.id==0)?.canCommand==true&&OriginalCommandRange.Living(units.FirstOrDefault(u=>u.id==0));
        public static HashSet<TutorialTile> StaticBlocked(WorldMapDefinition map)
        {
            var blocked=new HashSet<TutorialTile>(map.npcs.Select(n=>new TutorialTile(n.x,n.y)));
            foreach(var portal in map.portals)foreach(var cell in portal.cells)blocked.Add(cell.Tile);
            return blocked;
        }
        public static List<TutorialTile> Route(WorldMapDefinition map,OriginalCombatCatalog catalog,TrainingUnit[] units,int member,int x,int y)
        {
            var actor=units?.FirstOrDefault(u=>u.id==member);int? type=MovementType(actor,catalog);var end=new TutorialTile(x,y);
            if(actor==null||!actor.canMove||!actor.movementBudget.HasValue||!type.HasValue||!OriginalRegroupMovement.ValidMovementType(type)
                ||x<=0||y<=0||x>=200||y>=200||!map.map.Contains(end)||actor.Tile==end||units.Any(u=>u.id!=member&&OriginalCommandRange.OccupiesTile(u)&&u.Tile==end))return null;
            var scores=Reachable(map,catalog,units,member);if(scores==null||scores[y*map.map.width+x]<1)return null;
            var blocked=StaticBlocked(map);foreach(var u in units)if(u.id/50!=member/50&&OriginalCommandRange.OccupiesTile(u))blocked.Add(u.Tile);
            return map.map.Path(actor.Tile,end,actor.movementBudget.Value+7,blocked,type.Value);
        }
        public static int[] Reachable(WorldMapDefinition map,OriginalCombatCatalog catalog,TrainingUnit[] units,int member)
        {
            var actor=units?.FirstOrDefault(u=>u.id==member);int? type=MovementType(actor,catalog);
            if(actor==null||!actor.canMove||!actor.movementBudget.HasValue||!type.HasValue||!OriginalRegroupMovement.ValidMovementType(type))return null;
            var occupied=units.Where(OriginalCommandRange.OccupiesTile).GroupBy(u=>u.Tile).ToDictionary(g=>g.Key,g=>g.FirstOrDefault(u=>u.id/50!=member/50)?.id??g.First().id);
            return OriginalRegroupMovement.Reachable(map.map,actor,actor.movementBudget.Value,type.Value,occupied,StaticBlocked(map));
        }
        public static bool CanAttack(WorldMapDefinition map,OriginalCombatCatalog catalog,TrainingUnit[] units,int member,int target,bool nativeAi=false)
        {
            var actor=units?.FirstOrDefault(u=>u.id==member);var enemy=units?.FirstOrDefault(u=>u.id==target);
            if(actor?.canAttack!=true||!actor.attackRange.HasValue||actor.attackTargets==null||!actor.attackTargets.Contains(target)
                ||target<10||!(nativeAi?OriginalSoldierActions.Targetable(enemy):OriginalCommandRange.Living(enemy)))return false;
            var area=OriginalBasicAttackArea.Weights(map.map,catalog,actor);
            return area!=null&&map.map.Contains(enemy.Tile)&&area[enemy.y*map.map.width+enemy.x]>0;
        }
        public static bool Same(TrainingUnit before,TrainingUnit after)=>before.troopOrder==after.troopOrder&&OriginalTroopOrders.SameExceptOrder(before,after);
    }
    public static class OriginalBasicAttackArea
    {
        public static int[] Weights(TutorialMap map,OriginalCombatCatalog catalog,TrainingUnit actor)
        {
            int? type=OriginalMemberCommands.MovementType(actor,catalog);
            if(actor==null||!actor.attackRange.HasValue||!type.HasValue||!OriginalRegroupMovement.ValidMovementType(type)||!map.Contains(actor.Tile))return null;
            int radius=actor.attackRange.Value;var result=Enumerable.Repeat(-1,map.width*map.height).ToArray();
            if(type==5)
            {
                for(int x=0;x<map.width;x++)for(int y=0;y<map.height;y++)
                {long distance=OriginalCommandRange.Distance(actor.x,actor.y,x,y);if(distance<=radius)result[y*map.width+x]=(int)(radius+1-distance);}
            }
            else
            {
                result[actor.y*map.width+actor.x]=radius;
                Action<int,int,int,int> spread=null;
                spread=(x,y,left,direction)=>
                {
                    if(x<0||y<0||x>=map.width||y>=map.height||left<1||result[y*map.width+x]>=left)return;
                    int terrain=map.Terrain(new TutorialTile(x,y),type.Value*3+2);if(terrain==99||terrain==-99)return;
                    result[y*map.width+x]=left;if(--left<1)return;
                    if(direction==1){spread(x-1,y,left,1);spread(x,y-1,left,1);spread(x,y+1,left,1);}
                    else if(direction==2){spread(x-1,y,left,2);spread(x+1,y,left,2);spread(x,y-1,left,2);}
                    else if(direction==3){spread(x+1,y,left,3);spread(x,y-1,left,3);spread(x,y+1,left,3);}
                    else {spread(x-1,y,left,4);spread(x+1,y,left,4);spread(x,y+1,left,4);}
                };
                spread(actor.x-1,actor.y,radius,1);spread(actor.x+1,actor.y,radius,3);spread(actor.x,actor.y-1,radius,2);spread(actor.x,actor.y+1,radius,4);
            }
            result[actor.y*map.width+actor.x]=-1;return result;
        }
    }
}
