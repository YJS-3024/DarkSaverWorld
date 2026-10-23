using System;
using UnityEngine;

namespace DarkSaver.UI
{
    // Converted assets only. No original library/layout files are read in the player.
    public sealed class NativeBattleWindows : ScriptableObject
    {
        [Serializable] public sealed class Window
        {
            public int sceneId;
            public string title;
            public Texture2D background;
            public Vector2Int size;
            public Texture2D minimizedBackground;
            public Vector2Int minimizedSize;
            public Control[] controls;
        }
        [Serializable] public sealed class Control
        {
            public int nativeId;
            public Rect rect;
            public Texture2D[] frames;
        }
        public Window[] windows = Array.Empty<Window>();
        public Window Find(int id) => Array.Find(windows, window => window.sceneId == id);
    }
}
