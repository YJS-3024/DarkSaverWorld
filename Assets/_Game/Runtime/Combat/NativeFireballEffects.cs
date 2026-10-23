using System;
using UnityEngine;
namespace DarkSaver.Combat
{
    public sealed class NativeFireballEffects : ScriptableObject
    {
        [SerializeField] int[] magicIds = Array.Empty<int>();
        [SerializeField] Sprite[] projectile = Array.Empty<Sprite>(), impact = Array.Empty<Sprite>();
        [SerializeField] bool lavaTiming;
        [SerializeField] bool linearTravel;
        [SerializeField] bool curvedDirectionalTravel;
        public bool LinearTravel => linearTravel;
        public bool DirectionalTravel => linearTravel || curvedDirectionalTravel;
        // Legacy type name retained to preserve the existing fireball asset reference.
        public int ProjectileCount => projectile?.Length ?? 0;
        public int ImpactCount => impact?.Length ?? 0;
        public bool HasValidFrames => ProjectileCount > 0 && ProjectileCount <= 64 && ImpactCount > 0 && ImpactCount <= 64
            && !Array.Exists(projectile, x => x == null) && !Array.Exists(impact, x => x == null);
        public System.Collections.Generic.IReadOnlyList<int> MagicIds => Array.AsReadOnly(magicIds ?? Array.Empty<int>());
        public bool Supports(int id) => magicIds != null && Array.IndexOf(magicIds, id) >= 0;
        public Sprite Projectile(int index) => index >= 0 && index < ProjectileCount ? projectile[index] : null;
        public Sprite Impact(int index) => index >= 0 && index < ImpactCount ? impact[index] : null;
        public bool DrawsAtCounter(int counter) => counter >= 0 && counter < 10 + ImpactCount
            && (lavaTiming ? counter != 10 : counter != 0);
        public void Initialize(int[] ids, Sprite[] travel, Sprite[] hit, bool useLavaTiming = false, bool useLinearTravel = false, bool useCurvedDirectionalTravel = false)
        {
            if (ids == null || ids.Length == 0 || travel == null || travel.Length == 0 || travel.Length > 64
                || hit == null || hit.Length == 0 || hit.Length > 64
                || Array.Exists(travel, x => x == null) || Array.Exists(hit, x => x == null)) throw new ArgumentException("Invalid native fireball frames.");
            magicIds = (int[])ids.Clone(); projectile = (Sprite[])travel.Clone(); impact = (Sprite[])hit.Clone();
            lavaTiming = useLavaTiming;
            linearTravel = useLinearTravel;
            curvedDirectionalTravel = useCurvedDirectionalTravel;
        }
        public int CounterForAge(float seconds)
        {
            if (!linearTravel) return CounterAt(seconds, ImpactCount);
            int step = NativeTargetEffects.CounterAt(seconds);
            if (step < 0 || step > 10 + ImpactCount) return -1;
            int counter = step < 5 ? step * 2 : step + 5;
            return counter >= 10 + ImpactCount ? -1 : counter;
        }
        public static int DirectionFrame(Vector3 delta)
        {
            int x = Math.Sign(delta.x), y = Math.Sign(delta.y);
            return x == 0 ? y > 0 ? 0 : y < 0 ? 4 : -1
                : x > 0 ? y > 0 ? 1 : y < 0 ? 3 : 2 : y > 0 ? 7 : y < 0 ? 5 : 6;
        }
        // Native MulDiv rounds to the nearest integer, including negative half-pixels.
        public static Vector3 LinearPosition(Vector3 source, Vector3 target, int counter, Vector2 cellSize)
        {
            counter = Mathf.Clamp(counter, 0, 10);
            return source + new Vector3(
                (float)Math.Round((target.x - source.x) / cellSize.x * 32 * counter / 10d, MidpointRounding.AwayFromZero) * cellSize.x / 32,
                (float)Math.Round((target.y - source.y) / cellSize.y * 32 * counter / 10d, MidpointRounding.AwayFromZero) * cellSize.y / 32, 0);
        }
        public static int CounterAt(float seconds, int impactCount = 5)
        {
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)
                || GameConstants.BattleMagicEffectCounterSeconds <= 0 || impactCount < 1 || impactCount > 64) return -1;
            var count = Math.Floor(((double)seconds + 0.000001) / GameConstants.BattleMagicEffectCounterSeconds);
            return count >= 10 + impactCount ? -1 : (int)count;
        }
        // 100057DD: cubic control points share the midpoint, lifted 50 native pixels.
        public static Vector3 TravelPosition(Vector3 source, Vector3 target, float t, Vector3 lift)
        {
            t = Mathf.Clamp01(t); var a = 1 - t; var control = (source + target) * .5f + lift;
            return source * (a * a * a) + control * (3 * a * a * t + 3 * a * t * t) + target * (t * t * t);
        }
    }
}
