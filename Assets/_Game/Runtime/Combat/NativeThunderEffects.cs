using System;
using UnityEngine;
namespace DarkSaver.Combat
{
    public sealed class NativeThunderEffects : ScriptableObject
    {
        [SerializeField] int[] magicIds = Array.Empty<int>();
        [SerializeField] Sprite[] frames = Array.Empty<Sprite>();
        public bool Supports(int id) => Array.IndexOf(magicIds, id) >= 0 && frames.Length == 14;
        public int MagicCount => magicIds.Length;
        public void Initialize(int[] ids, Sprite[] sprites)
        {
            if (ids == null || ids.Length == 0 || sprites == null || sprites.Length != 14 || Array.Exists(sprites, x => x == null))
                throw new ArgumentException("Invalid native thunder frames.");
            magicIds = (int[])ids.Clone(); frames = (Sprite[])sprites.Clone();
        }
        public Sprite Bolt(int counter) => counter >= 0 && counter < 20 ? frames[counter / 2 % 5] : null;
        public Sprite Impact(int counter) => counter >= 0 && counter < 20 ? frames[5 + counter % 4] : null;
        public Sprite Caster(int counter) => counter >= 0 && counter < 10 ? frames[9 + counter % 5] : null;
        // 100090D6..1000910B: initial rand%5, then +1 except when rand%3 == 0.
        public static int NextCounter(int counter, int initialRoll, int advanceRoll)
            => (counter == 0 ? initialRoll % 5 : counter) + (advanceRoll % 3 == 0 ? 0 : 1);
    }
}
