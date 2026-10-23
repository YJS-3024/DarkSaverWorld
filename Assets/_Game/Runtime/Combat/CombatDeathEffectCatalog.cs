using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed class CombatDeathEffectCatalog : ScriptableObject
    {
        [SerializeField] private Sprite[] frames;

        public bool Configure(Sprite[] values)
        {
            if (values == null || values.Length != 7) return false;
            foreach (var sprite in values)
                if (sprite == null || sprite.rect.width != 31 || sprite.rect.height != 31
                    || sprite.pixelsPerUnit != 32) return false;
            frames = (Sprite[])values.Clone(); return true;
        }

        // Explicit counter query only. No automatic corpse timer or original-file access.
        public bool TryGetEffect(int counter, out Sprite effect)
        {
            effect = null;
            if (!CombatDeathPresentation.TryGetFrame(counter, out var frame)
                || frames == null || frames.Length != 7 || frames[frame.EffectColumn] == null) return false;
            effect = frames[frame.EffectColumn]; return true;
        }
    }
}
