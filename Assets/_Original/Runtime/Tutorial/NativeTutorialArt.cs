using System;
using UnityEngine;

namespace DarkSaver.Original.Tutorial
{
    [Serializable] public sealed class TutorialNativeButton
    { public int id, x, y, sx, sy, width, height; }
    [Serializable] public sealed class TutorialNativeWindow
    { public int key, scene, x, y, width, height; public TutorialNativeButton[] buttons; }
    [Serializable] public sealed class TutorialDecoration
    { public int x, y, width, height; }
    [Serializable] public sealed class TutorialDefinition
    {
        public TutorialMap map;
        public TutorialNativeWindow[] windows;
        public TutorialDecoration[] decorations;
        public int magicTravelX, magicTravelY, magicTravelFrames, magicImpactX, magicImpactY, magicImpactFrames, magicSound1, magicSound2;
    }
    public sealed class NativeTutorialArt : ScriptableObject
    {
        public TextAsset script, definition, help;
        public TextAsset waitingDefinition;
        public Texture2D waitingGround, waitingButtons, waitingDialogue, waitingItems, waitingChatExpanded, waitingCursors;
        public Texture2D waitingFighterFace, waitingKnightFace, waitingMagicianFace;
        public Texture2D[] waitingWindows, waitingMinimizedWindows;
        public Texture2D ground, dialogue, face, icons, board, effects, attackEffects, buttons, minimap;
        public Texture2D[] actors, windows, minimizedWindows, decorations, ranges;
        public int[] actorJobs;
        public AudioClip music;
        public TutorialDefinition Definition => definition == null ? null : JsonUtility.FromJson<TutorialDefinition>(definition.text);
        public WaitingDefinition WaitingDefinition => waitingDefinition == null ? null : JsonUtility.FromJson<WaitingDefinition>(waitingDefinition.text);
        public bool WaitingConfigured => waitingDefinition != null && waitingGround != null && waitingButtons != null
            && waitingDialogue != null && waitingItems != null && waitingChatExpanded != null && waitingCursors != null
            && waitingFighterFace != null && waitingKnightFace != null && waitingMagicianFace != null
            && waitingWindows?.Length == 4 && waitingMinimizedWindows?.Length == 4;
        public bool IsConfigured => script != null && definition != null && help != null && ground != null && dialogue != null && face != null
            && icons != null && board != null && effects != null && attackEffects != null && buttons != null && minimap != null && actors?.Length == 9
            && actorJobs?.Length == 7 && minimizedWindows?.Length == 9 && windows?.Length == 9 && decorations?.Length == 31 && ranges?.Length == 4 && music != null;
        public Texture2D Actor(int job, int allyMode)
        {
            if (job == 125 && allyMode >= 0) return actors[allyMode == 0 ? 7 : 8];
            int index = Array.IndexOf(actorJobs, job); return index >= 0 && index < actors.Length ? actors[index] : null;
        }
    }
}
