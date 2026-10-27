using System;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Events;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class FirstPlayTutorial
    {
        private bool practiceVerticalFirst;
        private static string PracticeFacing(GameObject actor)
        {
            var facing=actor.GetComponent<DarkSaver.Player.DirectionalSpriteAnimator>().Facing;
            return facing.x<0?"LEFT":facing.x>0?"RIGHT":facing.y>0?"UP":"DOWN";
        }
        private static CombatGridCell NativeCell(Vector3 position)=>new CombatGridCell(Mathf.FloorToInt(position.x),14-Mathf.FloorToInt(position.y));
        private bool TryPlanPracticeMotion(GameObject actor,Vector3 end,out PracticeMotion motion)
        {
            motion=null;if(!EnsurePracticeTerrain())return false;
            int id=actors.First(kv=>kv.Value==actor).Key;
            var bounds=rangeGround.cellBounds;
            int? Cost(CombatGridCell cell)
            {
                var native=new Vector3Int(cell.X,bounds.yMax-1-cell.Y,0);
                return rangeGround.HasTile(native) && BattleMapPresentation.TryGetAttribute(rangeTerrain,bounds,cell,out var attribute)
                    && CombatTerrainTraversal.TryGetMovementValue(attribute,OriginalTutorialRanges.TroopColumn,out int cost)?cost:(int?)null;
            }
            int? Occupant(CombatGridCell cell)
            {
                foreach(var a in progress.actors)
                {
                    if(a.id==id || a.removed || a.dead && !IsPracticeCorpseVisible(a.id))continue;
                    if(actors.TryGetValue(a.id,out var body))
                    {
                        var occupied=motions.TryGetValue(body,out var moving)?NativeCell(moving.next):NativeCell(body.transform.position);
                        if(occupied.Equals(cell))return a.id;
                    }
                }
                return null;
            }
            var from=NativeCell(actor.transform.position);var target=NativeCell(end);
            if(!OriginalTutorialMovement.TryPlan(bounds.size.x,bounds.size.y,id,from,target,Cost,Occupant,practiceVerticalFirst,out var path,out var next))return false;
            if(path.Count==0)return false;
            motion=new PracticeMotion {from=actor.transform.position,end=end,path=path,index=0,next=World(path[0].X,path[0].Y)};
            practiceVerticalFirst=next;return true;
        }
    }
}
