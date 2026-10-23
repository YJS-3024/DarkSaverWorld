using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class GroundBurstCaster
        {
            public GameObject Root;public SpriteRenderer Caster;public readonly List<SpriteRenderer> Bodies=new();
            public NativeTargetEffects.Pattern Pattern;public int Area,Counter,Step;public float Age;public bool Playing;
        }
        readonly Dictionary<short,GroundBurstCaster> groundBurstCasters=new();
        NativeGroundBurstCasterEffects groundBurstCasterCatalog;
        void StartGroundBurst(int id,short casterId)
        {
            groundBurstCasterCatalog??=Resources.Load<NativeGroundBurstCasterEffects>("Effects/NativeGroundBurstCasterEffects");
            if(!isActiveAndEnabled || !IsBound || groundBurstCasterCatalog==null || !groundBurstCasterCatalog.TryGet(id,out int area,out var pattern))return;
            var caster=visuals.Find(v=>v.UnitId==casterId);if(caster.Body==null)return;
            if(thunderCasters.TryGetValue(casterId,out var charge))StopMagicVisual(charge);
            if(!groundBurstCasters.TryGetValue(casterId,out var effect))
            {
                var root=new GameObject("Native Ground Burst Caster "+casterId);root.transform.SetParent(transform,false);
                effect=new GroundBurstCaster{Root=root};groundBurstCasters.Add(casterId,effect);
            }
            StopGroundBurst(effect);effect.Caster=caster.Body;effect.Area=area;effect.Pattern=pattern;
            effect.Counter=0;effect.Step=-1;effect.Age=0;effect.Playing=true;
        }
        void AdvanceGroundBursts(float seconds)
        {
            foreach(var pair in groundBurstCasters)
            {
                var effect=pair.Value;if(!effect.Playing)continue;
                effect.Age+=seconds;int elapsed=NativeTargetEffects.CounterAt(effect.Age);
                if(!IsBound || battle.IsBattleFinished || effect.Caster==null || elapsed<0
                    || !battle.TryGetUnit(pair.Key,out var unit) || unit.LifeState==UnitLifeState.Removed)
                {StopGroundBurst(effect);continue;}
                int end=(int)System.Math.Min(elapsed,effect.Step+256d);
                for(int step=effect.Step+1;step<=end;step++)
                {
                    if(effect.Counter>=effect.Pattern.LifetimeCounters){StopGroundBurst(effect);break;}
                    int draw=effect.Counter;
                    effect.Counter=NativeGroundBurstCasterEffects.NextCounter(draw,draw==0?effectRandom.Next(5):0);effect.Step=step;
                    DrawGroundBurst(effect,draw);
                }
            }
        }
        void DrawGroundBurst(GroundBurstCaster effect,int counter)
        {
            var source=ground.WorldToCell(effect.Caster.transform.position);int used=0;
            // Native visits X first, Y second, clipping the setarea8 mask to map dimensions.
            for(int x=System.Math.Max(bounds.xMin,source.x-effect.Area);x<=System.Math.Min(bounds.xMax-1,source.x+effect.Area);x++)
            for(int nativeY=System.Math.Max(0,bounds.yMax-1-source.y-effect.Area);nativeY<=System.Math.Min(bounds.size.y-1,bounds.yMax-1-source.y+effect.Area);nativeY++)
            {
                int y=bounds.yMax-1-nativeY;
                if(!NativeGroundBurstCasterEffects.Contains(effect.Area,x-source.x,y-source.y))continue;
                if(used==effect.Bodies.Count)
                {
                    var go=new GameObject("Cell "+used);go.transform.SetParent(effect.Root.transform,false);effect.Bodies.Add(go.AddComponent<SpriteRenderer>());
                }
                var body=effect.Bodies[used++];body.gameObject.layer=effect.Caster.gameObject.layer;
                body.sortingLayerID=effect.Caster.sortingLayerID;body.sortingOrder=effect.Caster.sortingOrder+1;
                body.sprite=NativeGroundBurstCasterEffects.Frame(effect.Pattern,counter,effectRandom.Next(3));body.enabled=body.sprite!=null;
                body.transform.position=ground.GetCellCenterWorld(new Vector3Int(x,y,0)); // Ground effect: never moved to body center.
            }
            for(int i=used;i<effect.Bodies.Count;i++)effect.Bodies[i].enabled=false;
        }
        static void StopGroundBurst(GroundBurstCaster effect){effect.Playing=false;foreach(var body in effect.Bodies)if(body!=null)body.enabled=false;}
        void StopGroundBurstCaster(short id){if(groundBurstCasters.TryGetValue(id,out var effect))StopGroundBurst(effect);}
        void SuspendGroundBursts(){foreach(var effect in groundBurstCasters.Values)StopGroundBurst(effect);}
        void ReleaseGroundBursts(){SuspendGroundBursts();foreach(var effect in groundBurstCasters.Values)if(effect.Root!=null)ReleasePreview(effect.Root);groundBurstCasters.Clear();}
    }
}
