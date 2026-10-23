using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class CoordinateArea
        {
            public Vector2Int Cell;public int Family,Counter=-1;public float Age;
            public NativeTargetEffects.Pattern Pattern;public readonly HashSet<short> Owners=new();public SpriteRenderer[] Bodies;
        }
        readonly Dictionary<(int,Vector2Int),CoordinateArea> coordinateAreas=new();
        readonly List<(int,Vector2Int)> expiredCoordinateAreas=new();
        public int RetainedCoordinateAreaCount => coordinateAreas.Count;
        NativeTargetEffects coordinateGroundCatalog;
        public static int NativeMeteorCount(int postCounter)=>postCounter>=1 && postCounter<10?(11-postCounter)%7+1:0;
        public static Vector2 NativeMeteorOffset(int postCounter,int index,int degrees,int radiusX,int radiusY)
        {
            int b=11-postCounter;double angle=degrees*3.1415926535/180;
            return new Vector2(8*(b-4+index)+(int)(radiusX*System.Math.Cos(angle)+.5),
                15*b+15*index-32-(int)(radiusY*System.Math.Sin(angle)+3*index+.5));
        }
        void StartCoordinateArea(int magicId,short targetId,Vector2Int cell,bool reset)
        {
            coordinateGroundCatalog??=Resources.Load<NativeTargetEffects>("Effects/NativeCoordinateGroundEffects");
            presentationCatalog??=Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog");
            if(!IsBound || coordinateGroundCatalog==null || !coordinateGroundCatalog.TryGet(magicId,out var pattern)
                || presentationCatalog==null || !presentationCatalog.TryGet(magicId,out var entry))return;
            var target=visuals.Find(v=>v.UnitId==targetId);if(target.Body==null)return;
            var key=(entry.Family,cell);
            if(!coordinateAreas.TryGetValue(key,out var area))
            {
                area=new CoordinateArea{Cell=cell,Family=entry.Family,Pattern=pattern,Bodies=new SpriteRenderer[entry.Family==4?7:1]};coordinateAreas.Add(key,area);
                for(int i=0;i<area.Bodies.Length;i++)
                {
                    var go=new GameObject("Native Coordinate Area "+entry.Family+" "+cell+" "+i);go.transform.SetParent(transform,false);
                    area.Bodies[i]=go.AddComponent<SpriteRenderer>();area.Bodies[i].enabled=false;
                }
                reset=true;
            }
            if(reset){area.Owners.Clear();area.Age=0;area.Counter=-1;}
            area.Owners.Add(targetId);
            foreach(var body in area.Bodies){body.gameObject.layer=target.Body.gameObject.layer;body.sortingLayerID=target.Body.sortingLayerID;body.sortingOrder=target.Body.sortingOrder+1;}
        }
        void StartLocalCoordinateArea(int id,short targetId)
        {
            var target=visuals.Find(v=>v.UnitId==targetId);if(target.Body==null)return;
            var cell=ground.WorldToCell(target.Body.transform.position);StartCoordinateArea(id,targetId,new Vector2Int(cell.x,bounds.yMax-1-cell.y),true);
        }
        void StopCoordinateAreaTarget(short id)
        {foreach(var area in coordinateAreas.Values)if(area.Owners.Remove(id) && area.Owners.Count==0)foreach(var body in area.Bodies)body.enabled=false;}
        void AdvanceCoordinateAreas(float seconds)
        {
            expiredCoordinateAreas.Clear();
            foreach(var pair in coordinateAreas)
            {
                var area=pair.Value;
                area.Age+=seconds;int step=NativeTargetEffects.CounterAt(area.Age),post=step+1;bool owner=false;
                foreach(short id in area.Owners)if(battle.TryGetUnit(id,out var unit) && unit.LifeState!=UnitLifeState.Removed)
                {owner=true;if(area.Family==4 && targetEffects.TryGetValue(id,out var target))post=target.Counter;break;}
                bool active=IsBound && !battle.IsBattleFinished && owner && step>=0 && post>0 && post<10;
                int count=area.Family==4?NativeMeteorCount(post):1;
                for(int i=0;i<area.Bodies.Length;i++)area.Bodies[i].enabled=active && i<count;
                if(!IsBound || battle.IsBattleFinished || !owner || step<0 || post>=10)
                {expiredCoordinateAreas.Add(pair.Key);continue;}
                if(!active || area.Counter==step)continue;
                var anchor=ground.GetCellCenterWorld(new Vector3Int(area.Cell.x,bounds.yMax-1-area.Cell.y,0));var cell=ground.layoutGrid.cellSize;
                for(int i=0;i<count;i++)
                {
                    var body=area.Bodies[i];Vector2 offset=Vector2.zero;
                    if(area.Family==4)
                    {
                        // Native multiplies dirx32 by rand before remainder4: every source frame is0.
                        effectRandom.Next();effectRandom.Next();
                        offset=NativeMeteorOffset(post,i,effectRandom.Next(360),effectRandom.Next(10),effectRandom.Next(25));
                    }
                    body.sprite=area.Pattern.Frame(area.Family==4?0:post);
                    body.transform.position=anchor+ground.transform.TransformVector(new Vector3(offset.x*cell.x/32,offset.y*cell.y/32,0));
                }
                area.Counter=step;
            }
            foreach(var key in expiredCoordinateAreas)
            {
                var area=coordinateAreas[key];
                foreach(var body in area.Bodies)if(body!=null)ReleasePreview(body.gameObject);
                coordinateAreas.Remove(key);
            }
            expiredCoordinateAreas.Clear();
        }
        void SuspendCoordinateAreas()=>ReleaseCoordinateAreas();
        void ReleaseCoordinateAreas()
        {
            foreach(var area in coordinateAreas.Values)foreach(var body in area.Bodies)if(body!=null)
            {body.enabled=false;ReleasePreview(body.gameObject);}
            coordinateAreas.Clear();expiredCoordinateAreas.Clear();
        }
    }
}
