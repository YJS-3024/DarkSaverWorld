using System.Collections.Generic;
using DarkSaver.Network;
using UnityEngine;
namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class FireballVisual
        {
            public short TargetId;
            public SpriteRenderer Body, TargetBody, Overlay;
            public Vector3 Source;
            public Vector3 PreviousTravelPoint;
            public float Age;
            public int Counter = -1;
            public bool Playing;
            public NativeFireballEffects Frames;
            public NativeThunderEffects Thunder;
            public int ThunderCounter;
        }
        readonly Dictionary<short, FireballVisual> fireballs = new();
        readonly System.Random effectRandom = new(); // Visual variation only, never combat RNG.
        NativeFireballEffects fireballCatalog;
        NativeProjectileEffectsCatalog projectileCatalog;
        NativeProjectileEffectsCatalog lavaCatalog;
        NativeProjectileEffectsCatalog linearProjectileCatalog;
        NativeProjectileEffectsCatalog curvedDirectionalCatalog;
        NativeThunderEffects thunderCatalog;
        readonly Dictionary<short, FireballVisual> thunderCasters = new();
        BattlePacketReceiver magicEffectReceiver;
        public int ActiveMagicEffectCount
        {
            get
            {
                int count = 0; foreach (var effect in fireballs.Values) if (effect.Playing) count++;
                foreach (var effect in crossTrustCasters.Values) if (effect.Playing) count++;
                foreach (var effect in levelUpCircles.Values) if (effect.Playing) count++;
                foreach (var effect in salamanderCasters.Values) if (effect.Playing) count++;
                foreach (var effect in thrustBeams.Values) if (effect.Playing) count++;
                foreach (var effect in groundBurstCasters.Values) if (effect.Playing) count++;
                foreach (var effect in expandingCasters.Values) if (effect.Playing) count++;
                foreach (var effect in targetEffects.Values) if (effect.Playing) count++;
                foreach (var effect in tornadoTargets.Values)
                { int step = NativeFireballEffects.CounterAt(effect.Age, 4); if (step >= 0 && step < 10) count++; }
                return count;
            }
        }
        void ListenMagicEffects()
        {
            battle.MagicPresentationStarted += OnLocalMagicEffect;
            battle.MagicCastPresentationStarted += OnLocalMagicCast;
            magicEffectReceiver = GetComponent<BattlePacketReceiver>();
            if (magicEffectReceiver != null) magicEffectReceiver.MagicEffectReceived += OnNativeMagicEffect;
        }
        void OnLocalMagicCast(MagicRequest request)
        {
            if(learnedMagic.TryGetValue(request.ActingUnitId,out var known) && known.Catalog.TryGet(request.MagicId,out var ability))
                AddCombatFeedback(request.ActingUnitId,ability.Name,1);
            var targets = new short[request.TargetUnitIds.Count];
            for (int i = 0; i < targets.Length; i++) targets[i] = request.TargetUnitIds[i];
            // Reuse the recovered visual path without dispatching a fake response.
            // Value is not inferred from cost; this renderer does not consume it.
            OnNativeMagicEffect(new MagicEffectNotification(request.MagicId, request.ActingUnitId,
                request.X, request.Y, 0, targets));
        }
        void OnLocalMagicEffect(CombatRequest request)
        {
            StopSharedCaster(request.ActorId);
            StopExpandingCasterUnit(request.ActorId);StartExpandingCaster(request.MagicId,request.ActorId);
            StopGroundBurstCaster(request.ActorId);StartGroundBurst(request.MagicId,request.ActorId);
            StopThrustBeamCaster(request.ActorId);
            StopSalamanderCaster(request.ActorId); StartLocalSalamander(request.MagicId,request.ActorId,request.TargetId);
            StopCrossTrustCaster(request.ActorId);
            StopLevelUpCircleCaster(request.ActorId); StartLevelUpCircle(request.MagicId,request.ActorId);
            if (StartCrossTrust(request.MagicId, request.ActorId)) { ClearCrossTrustTarget(request.TargetId);BeginMagicAudio(request.MagicId,request.ActorId,new[]{request.TargetId}); return; }
            var target = visuals.Find(v => v.UnitId == request.TargetId);
            if (SupportsTornado(request.MagicId) && target.Body != null)
            {
                var cell = ground.WorldToCell(target.Body.transform.position);
                StartTornado(request.MagicId, request.ActorId, request.TargetId, new Vector2Int(cell.x, bounds.yMax - 1 - cell.y), true);
            }
            else if (!StartNativeTargetEffect(request.MagicId, request.ActorId, request.TargetId)) StartFireball(request.MagicId, request.ActorId, request.TargetId);
            StartSharedCaster(request.MagicId,request.ActorId);
            StartLocalCoordinateArea(request.MagicId,request.TargetId);
            BeginMagicAudio(request.MagicId,request.ActorId,new[]{request.TargetId});
        }
        void OnNativeMagicEffect(MagicEffectNotification notice)
        {
            StopSharedCaster(notice.CasterUnitId);
            StopExpandingCasterUnit(notice.CasterUnitId);StartExpandingCaster(notice.MagicId,notice.CasterUnitId);
            StopGroundBurstCaster(notice.CasterUnitId);StartGroundBurst(notice.MagicId,notice.CasterUnitId);
            StopThrustBeamCaster(notice.CasterUnitId);StartThrustBeam(notice.MagicId,notice.CasterUnitId,notice.X);
            StopSalamanderCaster(notice.CasterUnitId); StartSalamander(notice.MagicId,notice.CasterUnitId,new Vector2Int(notice.X,notice.Y));
            StopCrossTrustCaster(notice.CasterUnitId);
            StopLevelUpCircleCaster(notice.CasterUnitId); StartLevelUpCircle(notice.MagicId,notice.CasterUnitId);
            if (StartCrossTrust(notice.MagicId,notice.CasterUnitId))
            { foreach(var target in notice.TargetUnitIds) ClearCrossTrustTarget(target);BeginMagicAudio(notice.MagicId,notice.CasterUnitId,notice.TargetUnitIds); return; }
            if (SupportsTornado(notice.MagicId))
            {
                bool first = true;
                var distinct = new HashSet<short>();
                foreach (var target in notice.TargetUnitIds)
                { if (!distinct.Add(target)) continue; StartTornado(notice.MagicId, notice.CasterUnitId, target, new Vector2Int(notice.X, notice.Y), first); first = false; }
                StartSharedCaster(notice.MagicId,notice.CasterUnitId);
                BeginMagicAudio(notice.MagicId,notice.CasterUnitId,notice.TargetUnitIds);
                return;
            }
            var seen = new HashSet<short>();
            foreach (var target in notice.TargetUnitIds)
                if (seen.Add(target) && !StartNativeTargetEffect(notice.MagicId, notice.CasterUnitId, target)) StartFireball(notice.MagicId, notice.CasterUnitId, target);
            StartSharedCaster(notice.MagicId,notice.CasterUnitId);
            bool firstCoordinate=true;foreach(short target in seen){StartCoordinateArea(notice.MagicId,target,new Vector2Int(notice.X,notice.Y),firstCoordinate);firstCoordinate=false;}
            BeginMagicAudio(notice.MagicId,notice.CasterUnitId,notice.TargetUnitIds);
        }
        bool StartFireball(int magicId, short casterId, short targetId)
        {
            if (!isActiveAndEnabled || !IsBound) return false;
            StopNativeTargetEffect(targetId);
            StopTornadoTarget(targetId);
            if (thunderCasters.TryGetValue(casterId, out var oldCharge))
            { oldCharge.Playing = false; if (oldCharge.Body != null) oldCharge.Body.enabled = false; }
            projectileCatalog ??= Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeProjectileEffects");
            thunderCatalog ??= Resources.Load<NativeThunderEffects>("Effects/NativeThunderEffectsV2");
            fireballCatalog ??= Resources.Load<NativeFireballEffects>("Effects/NativeFireballEffects");
            NativeFireballEffects frames = null;
            if (projectileCatalog != null) projectileCatalog.TryGet(magicId, out frames);
            lavaCatalog ??= Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeLavaEffects");
            if (frames == null && lavaCatalog != null) lavaCatalog.TryGet(magicId, out frames);
            linearProjectileCatalog ??= Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeLinearProjectileEffects");
            if (frames == null && linearProjectileCatalog != null) linearProjectileCatalog.TryGet(magicId, out frames);
            curvedDirectionalCatalog ??= Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeCurvedDirectionalEffects");
            if (frames == null && curvedDirectionalCatalog != null) curvedDirectionalCatalog.TryGet(magicId, out frames);
            if (frames == null && fireballCatalog != null && fireballCatalog.Supports(magicId)) frames = fireballCatalog;
            bool thunder = thunderCatalog != null && thunderCatalog.Supports(magicId);
            if (!thunder && (frames == null || !frames.HasValidFrames))
            {
                // A new unknown pattern must not keep displaying the previous magic.
                if (fireballs.TryGetValue(targetId, out var prior))
                { StopMagicVisual(prior); }
                return false;
            }
            var caster = visuals.Find(v => v.UnitId == casterId); var target = visuals.Find(v => v.UnitId == targetId);
            if (caster.Body == null || target.Body == null) return false;
            if (!fireballs.TryGetValue(targetId, out var effect))
            {
                var go = new GameObject("Native Fireball " + targetId); go.transform.SetParent(transform, false);
                effect = new FireballVisual { TargetId = targetId, Body = go.AddComponent<SpriteRenderer>() };
                fireballs.Add(targetId, effect);
            }
            effect.Source = caster.Body.transform.position; effect.TargetBody = target.Body;
            effect.PreviousTravelPoint = effect.Source; effect.Body.sprite = null;
            effect.Body.gameObject.layer = target.Body.gameObject.layer;
            effect.Frames = frames;
            effect.Thunder = thunder ? thunderCatalog : null; effect.ThunderCounter = 0;
            if (effect.Overlay != null) effect.Overlay.enabled = false;
            effect.Age = 0; effect.Counter = -1; effect.Playing = true; effect.Body.enabled = false;
            effect.Body.sortingLayerID = target.Body.sortingLayerID; effect.Body.sortingOrder = target.Body.sortingOrder + 1;
            if (thunder)
            {
                if (effect.Overlay == null)
                {
                    var hit = new GameObject("Native Thunder Impact " + targetId); hit.transform.SetParent(transform, false);
                    effect.Overlay = hit.AddComponent<SpriteRenderer>();
                }
                effect.Overlay.enabled = false; effect.Overlay.sortingLayerID = target.Body.sortingLayerID;
                effect.Overlay.sortingOrder = target.Body.sortingOrder + 2;
                if (!thunderCasters.TryGetValue(casterId, out var charge))
                {
                    var go = new GameObject("Native Thunder Caster " + casterId); go.transform.SetParent(transform, false);
                    charge = new FireballVisual { TargetId = casterId, Body = go.AddComponent<SpriteRenderer>() };
                    thunderCasters.Add(casterId, charge);
                }
                charge.TargetBody = caster.Body; charge.Thunder = thunderCatalog; charge.Age = 0; charge.Playing = true;
                charge.Body.enabled = false; charge.Body.sortingLayerID = caster.Body.sortingLayerID; charge.Body.sortingOrder = caster.Body.sortingOrder + 1;
            }
            return true;
        }
        public void AdvanceMagicEffects(float seconds)
        {
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            if (GameConstants.BattleMagicEffectCounterSeconds <= 0) return;
            AdvanceTornadoEffects(seconds);
            AdvanceNativeTargetEffects(seconds);
            AdvanceCrossTrust(seconds);
            AdvanceLevelUpCircles(seconds);
            AdvanceSalamanders(seconds);
            AdvanceThrustBeams(seconds);
            AdvanceGroundBursts(seconds);
            AdvanceExpandingCasters(seconds);
            AdvanceCoordinateAreas(seconds);
            AdvanceMagicCasterAudio(seconds);
            foreach (var effect in fireballs.Values)
            {
                if (!effect.Playing) continue;
                effect.Age += seconds; var counter = effect.Thunder != null ? effect.ThunderCounter : effect.Frames.CounterForAge(effect.Age);
                if (!IsBound || battle.IsBattleFinished || counter < 0 || effect.TargetBody == null || effect.Body == null
                    || !battle.TryGetUnit(effect.TargetId, out var unit) || unit.LifeState == UnitLifeState.Removed)
                { StopMagicVisual(effect); continue; }
                if (effect.Thunder != null)
                {
                    int elapsedSteps = (int)System.Math.Min(effect.Counter + 256d,
                        System.Math.Floor(((double)effect.Age + .000001) / GameConstants.BattleMagicEffectCounterSeconds));
                    for (int step = effect.Counter + 1; step <= elapsedSteps; step++)
                    {
                        counter = effect.ThunderCounter;
                        if (counter >= 20) { StopMagicVisual(effect); break; }
                        // 10009080 draws the overhead bolt and ground impact simultaneously.
                        effect.Body.sprite = effect.Thunder.Bolt(counter);
                        AdvanceMagicTargetAudio(effect.TargetId,counter,step);
                        effect.Overlay.sprite = effect.Thunder.Impact(counter);
                        effect.ThunderCounter = NativeThunderEffects.NextCounter(counter, effectRandom.Next(5), effectRandom.Next(3));
                        effect.Counter = step;
                    }
                    if (!effect.Playing) continue;
                    var targetPosition = effect.TargetBody.transform.position;
                    // Native bolt bottom aligns with the bottom of the target's 32-pixel cell.
                    effect.Body.transform.position = targetPosition + ground.transform.TransformVector(new Vector3(0, ground.layoutGrid.cellSize.y * 159f / 32f, 0));
                    effect.Overlay.transform.position = targetPosition;
                    effect.Body.enabled = effect.Body.sprite != null; effect.Overlay.enabled = effect.Overlay.sprite != null; continue;
                }
                AdvanceMagicTargetAudio(effect.TargetId,counter);
                if (!effect.Frames.DrawsAtCounter(counter))
                { effect.Body.enabled = false; effect.Counter = counter; continue; }
                var target = effect.TargetBody.transform.position;
                if (counter < 10)
                {
                    if (effect.Frames.DirectionalTravel)
                    {
                        var localSource = ground.transform.InverseTransformPoint(effect.Source);
                        var localTarget = ground.transform.InverseTransformPoint(target);
                        var position = effect.Frames.LinearTravel
                            ? ground.transform.TransformPoint(NativeFireballEffects.LinearPosition(localSource, localTarget, counter, ground.layoutGrid.cellSize))
                            : NativeFireballEffects.TravelPosition(effect.Source, target, counter * .1f,
                                ground.transform.TransformVector(new Vector3(0, ground.layoutGrid.cellSize.y * 50f / 32f, 0)));
                        if (counter != effect.Counter)
                        {
                            int direction = NativeFireballEffects.DirectionFrame(ground.transform.InverseTransformVector(position - effect.PreviousTravelPoint));
                            effect.Body.sprite = effect.Frames.Projectile(direction); effect.PreviousTravelPoint = position;
                        }
                        effect.Body.transform.position = position; effect.Counter = counter;
                        effect.Body.enabled = effect.Body.sprite != null; continue;
                    }
                    if (counter != effect.Counter) effect.Body.sprite = effect.Frames.Projectile(effectRandom.Next(effect.Frames.ProjectileCount));
                    var lift = ground.transform.TransformVector(new Vector3(0, ground.layoutGrid.cellSize.y * 50f / 32f, 0));
                    effect.Body.transform.position = NativeFireballEffects.TravelPosition(effect.Source, target, counter * .1f, lift);
                    // Native projectile rectangles use their top-left path point; compensate larger frames.
                    var rect = effect.Body.sprite.rect;
                    effect.Body.transform.position += ground.transform.TransformVector(new Vector3(
                        (rect.width - 32) / 64f * ground.layoutGrid.cellSize.x,
                        -(rect.height - 32) / 64f * ground.layoutGrid.cellSize.y, 0));
                }
                else
                {
                    effect.Body.sprite = effect.Frames.Impact(counter - 10);
                    effect.Body.transform.position = GetUnitMenuAnchor(effect.TargetBody);
                }
                effect.Counter = counter; effect.Body.enabled = effect.Body.sprite != null;
            }
            foreach (var charge in thunderCasters.Values)
            {
                if (!charge.Playing) continue;
                charge.Age += seconds; int counter = NativeFireballEffects.CounterAt(charge.Age, 4);
                if (!IsBound || battle.IsBattleFinished || charge.TargetBody == null || counter < 0 || counter >= 10
                    || !battle.TryGetUnit(charge.TargetId, out var unit) || unit.LifeState == UnitLifeState.Removed)
                { charge.Playing = false; if (charge.Body != null) charge.Body.enabled = false; continue; }
                charge.Body.sprite = charge.Thunder.Caster(counter); charge.Body.transform.position = charge.TargetBody.transform.position; charge.Body.enabled = true;
            }
        }
        void SuspendMagicEffects()
        {
            SuspendMagicAudio();
            SuspendCrossTrust();
            SuspendSalamanders();
            SuspendThrustBeams();
            SuspendGroundBursts();
            SuspendExpandingCasters();
            SuspendCoordinateAreas();
            SuspendLevelUpCircles();
            SuspendNativeTargetEffects();
            SuspendTornadoEffects();
            if (battle != null && listening) battle.MagicPresentationStarted -= OnLocalMagicEffect;
            if (battle != null && listening) battle.MagicCastPresentationStarted -= OnLocalMagicCast;
            if (magicEffectReceiver != null && listening) magicEffectReceiver.MagicEffectReceived -= OnNativeMagicEffect;
            foreach (var effect in fireballs.Values) StopMagicVisual(effect);
            foreach (var effect in thunderCasters.Values) { effect.Playing = false; if (effect.Body != null) effect.Body.enabled = false; }
        }
        public void ReleaseMagicEffectResources()
        {
            ReleaseMagicAudio();
            ReleaseNativeTargetEffects();
            SuspendMagicEffects();
            foreach (var effect in fireballs.Values)
            {
                if (effect.Body != null) ReleasePreview(effect.Body.gameObject);
                if (effect.Overlay != null) ReleasePreview(effect.Overlay.gameObject);
            }
            fireballs.Clear();
            foreach (var effect in thunderCasters.Values) if (effect.Body != null) ReleasePreview(effect.Body.gameObject);
            thunderCasters.Clear();
            ReleaseTornadoEffects();
            ReleaseCrossTrust();
            ReleaseSalamanders();
            ReleaseThrustBeams();
            ReleaseGroundBursts();
            ReleaseExpandingCasters();
            ReleaseCoordinateAreas();
            ReleaseLevelUpCircles();
        }
        static void StopMagicVisual(FireballVisual effect)
        {
            effect.Playing = false;
            if (effect.Body != null) effect.Body.enabled = false;
            if (effect.Overlay != null) effect.Overlay.enabled = false;
        }
    }
}
