using System.Collections.Generic;
using UnityEngine;
namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class TornadoTarget { public short Id; public Vector2Int Cell; public SpriteRenderer Body, Target; public float Age; }
        sealed class TornadoArea { public Vector2Int Cell; public SpriteRenderer[] Bodies; public float Age; public int Counter = -1; public readonly HashSet<short> Owners = new(); }
        readonly Dictionary<short, TornadoTarget> tornadoTargets = new();
        readonly Dictionary<Vector2Int, TornadoArea> tornadoAreas = new();
        readonly Queue<Vector2Int> tornadoAreaOrder = new();
        NativeFireballEffects tornadoCatalog;
        bool SupportsTornado(int magicId)
        {
            tornadoCatalog ??= Resources.Load<NativeFireballEffects>("Effects/NativeTornadoEffects");
            presentationCatalog??=Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog");
            return tornadoCatalog != null && tornadoCatalog.HasValidFrames && (tornadoCatalog.Supports(magicId)
                || presentationCatalog!=null && presentationCatalog.TryGet(magicId,out var entry) && entry.Family==3);
        }
        SpriteRenderer CreateTornadoBody(string name, SpriteRenderer target)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); var body = go.AddComponent<SpriteRenderer>();
            body.sortingLayerID = target.sortingLayerID; body.sortingOrder = target.sortingOrder + 1; body.enabled = false; return body;
        }
        bool StartTornado(int magicId, short casterId, short targetId, Vector2Int cell, bool resetArea)
        {
            if (!IsBound || !isActiveAndEnabled || !SupportsTornado(magicId)) return false;
            StopNativeTargetEffect(targetId);
            var target = visuals.Find(v => v.UnitId == targetId); if (target.Body == null) return false;
            StopTornadoTarget(targetId);
            if (fireballs.TryGetValue(targetId, out var old)) StopMagicVisual(old);
            if (!tornadoTargets.TryGetValue(targetId, out var effect))
            { effect = new TornadoTarget { Id = targetId, Body = CreateTornadoBody("Native Tornado Target " + targetId, target.Body) }; tornadoTargets.Add(targetId, effect); }
            effect.Target = target.Body; effect.Cell = cell; effect.Age = 0; effect.Body.enabled = false;
            if (!tornadoAreas.TryGetValue(cell, out var area))
            {
                if (tornadoAreas.Count >= 15)
                {
                    var oldest = tornadoAreaOrder.Dequeue();
                    foreach (var body in tornadoAreas[oldest].Bodies) if (body != null) ReleasePreview(body.gameObject);
                    tornadoAreas.Remove(oldest);
                }
                area = new TornadoArea { Cell = cell, Bodies = new SpriteRenderer[3] };
                for (int i = 0; i < 3; i++) area.Bodies[i] = CreateTornadoBody("Native Tornado Area " + cell + " " + i, target.Body);
                tornadoAreas.Add(cell, area); tornadoAreaOrder.Enqueue(cell); resetArea = true;
            }
            if (resetArea) { area.Owners.Clear(); area.Age = 0; area.Counter = -1; foreach (var body in area.Bodies) body.enabled = false; }
            area.Owners.Add(targetId);
            // Family 3 shares the verified family-2 caster renderer (10006690).
            var caster = visuals.Find(v => v.UnitId == casterId);
            thunderCatalog ??= Resources.Load<NativeThunderEffects>("Effects/NativeThunderEffectsV2");
            if (caster.Body != null && thunderCatalog != null)
            {
                if (!thunderCasters.TryGetValue(casterId, out var charge))
                { charge = new FireballVisual { TargetId = casterId, Body = CreateTornadoBody("Native Thunder Caster " + casterId, caster.Body) }; thunderCasters.Add(casterId, charge); }
                charge.TargetBody = caster.Body; charge.Thunder = thunderCatalog; charge.Age = 0; charge.Playing = true; charge.Body.enabled = false;
            }
            return true;
        }
        void StopTornadoTarget(short id)
        {
            StopMagicTargetAudio(id);
            if (!tornadoTargets.TryGetValue(id, out var target)) return;
            target.Age = float.PositiveInfinity; if (target.Body != null) target.Body.enabled = false;
            if (tornadoAreas.TryGetValue(target.Cell, out var area))
            { area.Owners.Remove(id); if (area.Owners.Count == 0) { area.Age = float.PositiveInfinity; foreach (var body in area.Bodies) body.enabled = false; } }
        }
        void AdvanceTornadoEffects(float seconds)
        {
            foreach (var effect in tornadoTargets.Values)
            {
                effect.Age += seconds; int count = NativeFireballEffects.CounterAt(effect.Age, 4);
                bool active = IsBound && !battle.IsBattleFinished && count >= 0 && count < 10 && effect.Target != null
                    && effect.Body != null
                    && battle.TryGetUnit(effect.Id, out var unit) && unit.LifeState != UnitLifeState.Removed;
                if (effect.Body != null) effect.Body.enabled = active; if (!active) continue;
                effect.Body.sprite = tornadoCatalog.Impact(count % 4); effect.Body.transform.position = GetUnitMenuAnchor(effect.Target);
                AdvanceMagicTargetAudio(effect.Id,count);
            }
            foreach (var area in tornadoAreas.Values)
            {
                area.Age += seconds; int count = NativeFireballEffects.CounterAt(area.Age, 4);
                // Coordinate pass reads the target counter after its +1 update.
                bool active = IsBound && !battle.IsBattleFinished && count >= 0 && count < 9 && HasActiveTornadoOwner(area);
                foreach (var body in area.Bodies) body.enabled = active;
                if (!active || count == area.Counter) continue;
                var anchor = ground.GetCellCenterWorld(new Vector3Int(area.Cell.x, bounds.yMax - 1 - area.Cell.y, 0));
                foreach (var body in area.Bodies)
                {
                    int radius = effectRandom.Next(20, 70), degrees = effectRandom.Next(360);
                    double angle = degrees * 3.1415926535 / 180;
                    var offset = new Vector3((int)(radius * System.Math.Cos(angle) + .5) / 32f * ground.layoutGrid.cellSize.x,
                        (16 - (int)(radius * System.Math.Sin(angle) + .5)) / 32f * ground.layoutGrid.cellSize.y, 0);
                    body.sprite = tornadoCatalog.Projectile((count + 1) % 4); body.transform.position = anchor + ground.transform.TransformVector(offset);
                }
                area.Counter = count;
            }
        }
        void SuspendTornadoEffects()
        {
            foreach (var target in tornadoTargets.Values) { target.Age = float.PositiveInfinity; if (target.Body != null) target.Body.enabled = false; }
            foreach (var area in tornadoAreas.Values) { area.Age = float.PositiveInfinity; foreach (var body in area.Bodies) if (body != null) body.enabled = false; }
        }
        bool HasActiveTornadoOwner(TornadoArea area)
        {
            foreach (var id in area.Owners)
                if (battle.TryGetUnit(id, out var unit) && unit.LifeState != UnitLifeState.Removed
                    && tornadoTargets.TryGetValue(id, out var target) && !float.IsInfinity(target.Age)) return true;
            return false;
        }
        void ReleaseTornadoEffects()
        {
            foreach (var target in tornadoTargets.Values) if (target.Body != null) ReleasePreview(target.Body.gameObject);
            foreach (var area in tornadoAreas.Values) foreach (var body in area.Bodies) if (body != null) ReleasePreview(body.gameObject);
            tornadoTargets.Clear(); tornadoAreas.Clear(); tornadoAreaOrder.Clear();
        }
    }
}
