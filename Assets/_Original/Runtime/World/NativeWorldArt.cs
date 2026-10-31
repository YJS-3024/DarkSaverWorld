using System;
using UnityEngine;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed class NativeWorldArt : ScriptableObject
    {
        public TextAsset definition;
        public Texture2D[] grounds, decorations, pictures, windows;
        public int[] pictureIds;
        public Texture2D buttons, items;
        public NativeTutorialArt battle;
        public WorldDefinition Definition => definition == null ? null : JsonUtility.FromJson<WorldDefinition>(definition.text);
        public bool IsConfigured => definition != null && grounds?.Length == 11 && decorations != null && pictures != null
            && pictures.Length == pictureIds?.Length && windows?.Length == 7 && buttons != null && items != null && battle != null && battle.WaitingConfigured;
        public Texture2D Picture(int id) { int i = Array.IndexOf(pictureIds, id); return i >= 0 ? pictures[i] : null; }
    }
}
