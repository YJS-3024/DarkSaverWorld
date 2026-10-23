using System;
using UnityEngine;

namespace DarkSaver.Combat
{
    // userunit.dll 10007FF0 and directional trail helpers 10007DB0..10007FE9.
    public sealed class NativeCrossTrustEffects : ScriptableObject
    {
        [SerializeField] int[] magicIds = Array.Empty<int>(), lifetimes = Array.Empty<int>();
        [SerializeField] Sprite[] frames = Array.Empty<Sprite>();
        public int MagicCount => magicIds.Length;
        public Sprite Frame(int index) => index >= 0 && index < frames.Length ? frames[index] : null;
        public void Initialize(int[] ids, int[] durations, Sprite[] source)
        {
            if (ids == null || durations == null || ids.Length == 0 || ids.Length != durations.Length
                || source == null || source.Length != 3 || Array.Exists(source, x => x == null)
                || Array.Exists(durations, x => x <= 0 || x > 1000)) throw new ArgumentException("Invalid cross-trust visual data.");
            magicIds = (int[])ids.Clone(); lifetimes = (int[])durations.Clone(); frames = (Sprite[])source.Clone();
        }
        public bool TryGetLifetime(int id, out int counters)
        {
            int index = Array.IndexOf(magicIds, id);
            counters = index >= 0 ? lifetimes[index] : 0; return index >= 0;
        }
        public bool TryGetSegment(int counter, int direction, int slot, out Sprite sprite, out Vector2 pixels)
        {
            sprite = null; pixels = default;
            if (counter < 0 || direction < 0 || direction > 3 || slot < 0 || slot > 3) return false;
            int trail = Math.Max(0, counter - 3) + slot;
            if (direction < 2 ? trail > counter : trail >= counter) return false;
            sprite = Frame(trail % 3); if (sprite == null) return false;
            float w = sprite.rect.width, h = sprite.rect.height;
            // Original blit uses trunc((dimension+1)/2), not a centered sprite pivot.
            pixels = new Vector2(w / 2 - (int)(w + 1) / 2, -(h / 2 - (int)(h + 1) / 2));
            pixels += direction == 0 ? Vector2.right * (trail * 7) : direction == 1 ? Vector2.left * (trail * 7)
                : direction == 2 ? Vector2.up * (trail * 7) : Vector2.down * (trail * 7);
            return true;
        }
    }
}
