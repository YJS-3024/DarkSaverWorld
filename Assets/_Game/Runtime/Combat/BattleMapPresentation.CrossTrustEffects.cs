using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class CrossTrustVisual { public GameObject Root; public SpriteRenderer Caster; public SpriteRenderer[] Bodies; public float Age; public int Lifetime; public bool Playing; }
        readonly Dictionary<short,CrossTrustVisual> crossTrustCasters = new();
        NativeCrossTrustEffects crossTrustCatalog;
        bool SupportsCrossTrust(int id)
        {
            crossTrustCatalog ??= Resources.Load<NativeCrossTrustEffects>("Effects/NativeCrossTrustEffects");
            return crossTrustCatalog != null && crossTrustCatalog.TryGetLifetime(id,out _);
        }
        bool StartCrossTrust(int id, short casterId)
        {
            if (!isActiveAndEnabled || !IsBound || !SupportsCrossTrust(id)) return false;
            var caster = visuals.Find(v => v.UnitId == casterId); if (caster.Body == null) return false;
            if (!crossTrustCasters.TryGetValue(casterId,out var effect))
            {
                var root = new GameObject("Native Cross Trust Caster "+casterId); root.transform.SetParent(transform,false);
                effect = new CrossTrustVisual { Root=root, Bodies=new SpriteRenderer[16] }; crossTrustCasters.Add(casterId,effect);
                for (int i=0;i<16;i++)
                {
                    var go = new GameObject("Segment "+i); go.transform.SetParent(root.transform,false);
                    effect.Bodies[i] = go.AddComponent<SpriteRenderer>();
                }
            }
            if (thunderCasters.TryGetValue(casterId,out var charge)) StopMagicVisual(charge);
            crossTrustCatalog.TryGetLifetime(id,out effect.Lifetime);
            effect.Caster=caster.Body; effect.Age=0; effect.Playing=true;
            foreach(var body in effect.Bodies)
            {
                body.gameObject.layer=caster.Body.gameObject.layer; body.sortingLayerID=caster.Body.sortingLayerID;
                body.sortingOrder=caster.Body.sortingOrder+1; body.enabled=false;
            }
            return true;
        }
        void ClearCrossTrustTarget(short id)
        {
            StopNativeTargetEffect(id); StopTornadoTarget(id);
            if (fireballs.TryGetValue(id,out var old)) StopMagicVisual(old);
        }
        void StopCrossTrustCaster(short id) { if(crossTrustCasters.TryGetValue(id,out var effect))StopCrossTrust(effect); }
        void AdvanceCrossTrust(float seconds)
        {
            foreach(var pair in crossTrustCasters)
            {
                var effect=pair.Value; if (!effect.Playing) continue;
                effect.Age+=seconds; int counter=NativeTargetEffects.CounterAt(effect.Age);
                if (!IsBound || battle.IsBattleFinished || effect.Caster==null || counter<0 || counter>=effect.Lifetime
                    || !battle.TryGetUnit(pair.Key,out var unit) || unit.LifeState==UnitLifeState.Removed)
                { StopCrossTrust(effect); continue; }
                for(int i=0;i<16;i++)
                {
                    var body=effect.Bodies[i]; body.enabled=crossTrustCatalog.TryGetSegment(counter,i/4,i%4,out var sprite,out var pixels);
                    if(!body.enabled)continue;
                    body.sprite=sprite; var cell=ground.layoutGrid.cellSize;
                    body.transform.position=effect.Caster.transform.position+ground.transform.TransformVector(new Vector3(pixels.x*cell.x/32,pixels.y*cell.y/32,0));
                }
            }
        }
        static void StopCrossTrust(CrossTrustVisual effect) { effect.Playing=false; foreach(var body in effect.Bodies) if(body!=null)body.enabled=false; }
        void SuspendCrossTrust() { foreach(var effect in crossTrustCasters.Values) StopCrossTrust(effect); }
        void ReleaseCrossTrust()
        {
            SuspendCrossTrust(); foreach(var effect in crossTrustCasters.Values) if(effect.Root!=null)ReleasePreview(effect.Root);
            crossTrustCasters.Clear();
        }
    }
}
