using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class TargetEffect { public short Id; public SpriteRenderer Body, Target; public NativeTargetEffects.Pattern Pattern; public float Age; public bool Playing; public int Counter, DrawCounter, Step; }
        readonly Dictionary<short, TargetEffect> targetEffects = new();
        NativeTargetEffects targetEffectCatalog;
        NativeTargetEffects shrinkingTargetEffectCatalog;
        NativeTargetEffects slowTargetEffectCatalog;
        NativeTargetEffects heldShrinkTargetEffectCatalog;
        NativeTargetEffects delayedTargetEffectCatalog;
        NativeTargetEffects groundExplosionCatalog;
        NativeTargetEffects coordinateTargetCatalog;
        NativeTargetEffects meteorTargetCatalog;
        // Unity character sprites have foot pivots; icon-like fxptn 8 effects follow the full frame center.
        public static Vector3 GetNativeTargetEffectAnchor(SpriteRenderer body, NativeTargetEffects.Pattern pattern)
            => body == null ? Vector3.zero : pattern != null && pattern.CenterOnUnit
                ? GetUnitMenuAnchor(body) : body.transform.position;
        bool TryGetNativeTargetPattern(int id, out NativeTargetEffects.Pattern pattern)
        {
            targetEffectCatalog ??= Resources.Load<NativeTargetEffects>("Effects/NativeTargetEffects");
            shrinkingTargetEffectCatalog ??= Resources.Load<NativeTargetEffects>("Effects/NativeShrinkingTargetEffects");
            slowTargetEffectCatalog ??= Resources.Load<NativeTargetEffects>("Effects/NativeSlowTargetEffects");
            heldShrinkTargetEffectCatalog ??= Resources.Load<NativeTargetEffects>("Effects/NativeHeldShrinkTargetEffects");
            delayedTargetEffectCatalog ??= Resources.Load<NativeTargetEffects>("Effects/NativeDelayedTargetEffects");
            groundExplosionCatalog ??= Resources.Load<NativeTargetEffects>("Effects/NativeGroundExplosionEffects");
            coordinateTargetCatalog??=Resources.Load<NativeTargetEffects>("Effects/NativeCoordinateTargetEffects");
            meteorTargetCatalog??=Resources.Load<NativeTargetEffects>("Effects/NativeMeteorTargetEffects");
            if (targetEffectCatalog != null && targetEffectCatalog.TryGet(id, out pattern)) return true;
            if (shrinkingTargetEffectCatalog != null && shrinkingTargetEffectCatalog.TryGet(id, out pattern)) return true;
            if (slowTargetEffectCatalog != null && slowTargetEffectCatalog.TryGet(id, out pattern)) return true;
            if (heldShrinkTargetEffectCatalog != null && heldShrinkTargetEffectCatalog.TryGet(id, out pattern)) return true;
            if (delayedTargetEffectCatalog != null && delayedTargetEffectCatalog.TryGet(id, out pattern)) return true;
            if (groundExplosionCatalog != null && groundExplosionCatalog.TryGet(id, out pattern)) return true;
            if(meteorTargetCatalog!=null && meteorTargetCatalog.TryGet(id,out pattern))return true;
            if(coordinateTargetCatalog!=null && coordinateTargetCatalog.TryGet(id,out pattern))return true;
            pattern = null; return false;
        }
        bool SupportsNativeTargetEffect(int id)
        {
            return TryGetNativeTargetPattern(id, out _);
        }
        bool StartNativeTargetEffect(int magicId, short casterId, short targetId)
        {
            if (!isActiveAndEnabled || !IsBound || !SupportsNativeTargetEffect(magicId)) return false;
            var target = visuals.Find(x => x.UnitId == targetId);
            if (target.Body == null) return false;
            StopCoordinateAreaTarget(targetId);
            StopTornadoTarget(targetId);
            if (fireballs.TryGetValue(targetId, out var old)) StopMagicVisual(old);
            if (thunderCasters.TryGetValue(casterId, out var charge)) StopMagicVisual(charge);
            if (!targetEffects.TryGetValue(targetId, out var effect))
            {
                var go = new GameObject("Native Target Effect " + targetId); go.layer = target.Body.gameObject.layer; go.transform.SetParent(transform, false);
                effect = new TargetEffect { Id = targetId, Body = go.AddComponent<SpriteRenderer>() }; targetEffects.Add(targetId, effect);
            }
            TryGetNativeTargetPattern(magicId, out effect.Pattern);
            effect.Body.gameObject.layer = target.Body.gameObject.layer;
            effect.Target = target.Body; effect.Age = 0; effect.Playing = true;
            effect.Counter = 0; effect.DrawCounter = -1; effect.Step = -1;
            effect.Body.enabled = false; effect.Body.sortingLayerID = target.Body.sortingLayerID; effect.Body.sortingOrder = target.Body.sortingOrder + 1;
            return true;
        }
        void StopNativeTargetEffect(short id)
        {
            StopMagicTargetAudio(id);
            StopCoordinateAreaTarget(id);
            if (!targetEffects.TryGetValue(id, out var effect)) return;
            effect.Playing = false; if (effect.Body != null) effect.Body.enabled = false;
        }
        void AdvanceNativeTargetEffects(float seconds)
        {
            foreach (var effect in targetEffects.Values)
            {
                if (!effect.Playing) continue;
                effect.Age += seconds; int counter = NativeTargetEffects.CounterAt(effect.Age);
                if (effect.Pattern.RandomGround && counter >= 0)
                {
                    int elapsed = (int)System.Math.Min(counter, effect.Step + 256d);
                    for (int step = effect.Step + 1; step <= elapsed; step++)
                    {
                        if (effect.Counter >= effect.Pattern.LifetimeCounters) { StopNativeTargetEffect(effect.Id); break; }
                        effect.DrawCounter = effect.Counter;
                        effect.Counter = NativeTargetEffects.NextGroundCounter(effect.Counter, effectRandom.Next(5), effectRandom.Next(3));
                        effect.Step = step;
                    }
                    if (!effect.Playing) continue;
                    counter = effect.DrawCounter;
                }
                var sprite = effect.Pattern.Frame(counter);
                if (!IsBound || battle.IsBattleFinished || counter < 0 || counter >= effect.Pattern.LifetimeCounters || effect.Body == null || effect.Target == null
                    || !battle.TryGetUnit(effect.Id, out var unit) || unit.LifeState == UnitLifeState.Removed)
                { StopNativeTargetEffect(effect.Id); continue; }
                AdvanceMagicTargetAudio(effect.Id,counter,effect.Pattern.RandomGround?effect.Step:-1);
                if (sprite == null) { effect.Body.enabled = false; continue; } // Native delayed-impact waiting interval.
                var delta = effect.Pattern.Offset(sprite.rect.width, sprite.rect.height);
                var scale = effect.Pattern.Scale(counter, sprite.rect.width, sprite.rect.height);
                effect.Body.transform.localScale = new Vector3(scale.x, scale.y, 1);
                var cell = ground.layoutGrid.cellSize;
                effect.Body.sprite = sprite; effect.Body.transform.position = GetNativeTargetEffectAnchor(effect.Target, effect.Pattern)
                    + ground.transform.TransformVector(new Vector3(delta.x * cell.x / 32, delta.y * cell.y / 32, 0));
                effect.Body.enabled = true;
            }
        }
        void SuspendNativeTargetEffects() { foreach (var id in targetEffects.Keys) StopNativeTargetEffect(id); }
        void ReleaseNativeTargetEffects()
        {
            SuspendNativeTargetEffects();
            foreach (var effect in targetEffects.Values) if (effect.Body != null) ReleasePreview(effect.Body.gameObject);
            targetEffects.Clear();
        }
    }
}
