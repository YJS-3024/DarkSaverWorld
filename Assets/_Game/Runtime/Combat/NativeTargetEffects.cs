using System;
using UnityEngine;

namespace DarkSaver.Combat
{
    // userunit.dll 100096C0; target renderer shared by fxptn 5/7/9.
    public sealed class NativeTargetEffects : ScriptableObject
    {
        [Serializable]
        public sealed class Pattern
        {
            [SerializeField] int patternId;
            [SerializeField] int[] magicIds;
            [SerializeField] Sprite[] frames;
            [SerializeField] bool shrinking;
            [SerializeField] bool doubleCycle;
            [SerializeField] bool heldShrink;
            [SerializeField] bool delayedImpact;
            [SerializeField] bool randomGround;
            [SerializeField] bool fixedTen;
            public bool RandomGround => randomGround;
            public int Id => patternId;
            public bool CenterOnUnit => shrinking || delayedImpact || fixedTen;
            public int FrameCount => frames?.Length ?? 0;
            public int LifetimeCounters => FrameCount == 0 ? 0 : fixedTen?10:randomGround ? (FrameCount + 1 < 3 ? 8 : FrameCount + 1) * 4
                : delayedImpact ? 25 : heldShrink ? 10 : shrinking ? 5
                : doubleCycle ? (FrameCount * 2 < 3 ? 8 : FrameCount * 2) : FrameCount < 3 ? 8 : FrameCount;
            public int MagicCount => magicIds?.Length ?? 0;
            public Pattern(int id, int[] ids, Sprite[] source, bool shrink = false, bool repeatTwice = false, bool holdThenShrink = false, bool delayImpact = false, bool randomizedGround = false,bool useFixedTen=false)
            {
                if (id <= 0 || ids == null || ids.Length == 0 || source == null || source.Length == 0 || source.Length > 64
                    || Array.Exists(source, x => x == null)) throw new ArgumentException("Invalid target effect pattern.");
                patternId = id; magicIds = (int[])ids.Clone(); frames = (Sprite[])source.Clone();
                shrinking = shrink;
                doubleCycle = repeatTwice;
                heldShrink = holdThenShrink;
                delayedImpact = delayImpact;
                randomGround = randomizedGround;
                fixedTen=useFixedTen;
            }
            public Vector2 Scale(int counter, float width, float height)
            {
                float size = heldShrink ? (counter < 5 ? 112 : (12 - counter) * 16) : (7 - counter) * 16;
                return shrinking || heldShrink ? new Vector2(size / width, size / height) : Vector2.one;
            }
            public Vector2 Offset(float width, float height) => shrinking || heldShrink
                ? new Vector2(-(int)(width - 32) / 2, (int)(height - 32) / 2) : CenterAdjustment(width, height);
            public bool Supports(int id) => magicIds != null && Array.IndexOf(magicIds, id) >= 0;
            public Sprite Frame(int counter) => counter < 0 || counter >= LifetimeCounters || delayedImpact && counter < 11 ? null
                : frames[(randomGround ? counter / 2 : heldShrink && counter >= 5 ? 4 : counter) % FrameCount];
        }
        [SerializeField] Pattern[] patterns = Array.Empty<Pattern>();
        public int PatternCount => patterns.Length;
        public int MagicCount { get { int total = 0; foreach (var p in patterns) total += p.MagicCount; return total; } }
        public void Initialize(Pattern[] source)
        {
            if (source == null || source.Length == 0 || Array.Exists(source, x => x == null)) throw new ArgumentException("Missing target patterns.");
            patterns = (Pattern[])source.Clone();
        }
        public bool TryGet(int id, out Pattern pattern)
        {
            foreach (var p in patterns) if (p.Supports(id)) { pattern = p; return true; }
            pattern = null; return false;
        }
        public static int CounterAt(float seconds)
        {
            double tick = GameConstants.BattleMagicEffectCounterSeconds;
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds) || tick <= 0) return -1;
            double counter = Math.Floor(((double)seconds + .000001) / tick);
            return counter > int.MaxValue ? -1 : (int)counter;
        }
        public static int NextGroundCounter(int counter, int initialRandom, int pauseRandom)
        {
            if (counter < 0 || initialRandom < 0 || initialRandom >= 5 || pauseRandom < 0 || pauseRandom >= 3)
                throw new ArgumentOutOfRangeException();
            return (counter == 0 ? initialRandom : counter) + (pauseRandom == 0 ? 0 : 1);
        }
        public static Vector2 CenterAdjustment(float width, float height)
        {
            // x86 signed /2 truncates toward zero, including frames smaller than 32px.
            return new Vector2((width - 32) / 2 - (int)(width - 32) / 2,
                -((height - 32) / 2 - (int)(height - 32) / 2));
        }
    }
}
