using System;
using System.Collections.Generic;
using UnityEngine;
namespace DarkSaver.Combat
{
    public sealed class NativeProjectileEffectsCatalog : ScriptableObject
    {
        [SerializeField] NativeFireballEffects[] patterns = Array.Empty<NativeFireballEffects>();
        public int PatternCount => patterns.Length;
        public int MagicCount { get { int count = 0; foreach (var pattern in patterns) count += pattern.MagicIds.Count; return count; } }
        public void Initialize(NativeFireballEffects[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Missing effect patterns.");
            var ids = new HashSet<int>();
            foreach (var pattern in source)
            {
                if (pattern == null || !pattern.HasValidFrames || pattern.MagicIds.Count == 0) throw new ArgumentException("Invalid effect pattern.");
                foreach (var id in pattern.MagicIds) if (!ids.Add(id)) throw new ArgumentException("Duplicate magic effect mapping.");
            }
            patterns = (NativeFireballEffects[])source.Clone();
        }
        public bool TryGet(int magicId, out NativeFireballEffects effect)
        {
            foreach (var pattern in patterns) if (pattern != null && pattern.Supports(magicId)) { effect = pattern; return true; }
            effect = null; return false;
        }
    }
}
