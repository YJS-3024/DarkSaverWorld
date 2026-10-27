using System;
namespace DarkSaver.Core
{
    [Serializable] public sealed class TutorialActor
    {
        public int id, job, x, y, targetX, targetY, hp = -1, mp = -1, tick, mask, magic, skill;
        public int maxHp=-1,maxMp=-1;
        public int tickEpoch; // Distinguishes repeated CHARMENU assignments of the same literal.
        public bool moving, dead, removed;
        public string facing = "DOWN";
        public TutorialActor Clone() => (TutorialActor)MemberwiseClone();
    }
    // Script cursor and practice actors are separate from the character's permanent state.
    [Serializable] public sealed class NativeTutorialProgress
    {
        public const int Complete = 420;
        public int version = 2;
        public int step;
        public bool finished, initialized, mouseMenu, keyboard, mouseMove;
        public int topMenu=-1, menuEpoch;
        public string practiceMenu="", activeCommand="";
        public int map, cameraX, cameraY, highlightX = -1, highlightY = -1, ally;
        public string message = "", portrait = "", music = "", victory = "";
        public int messageMode, unappliedLiteralCount;
        public string lastUnappliedLiteral="";
        public TutorialActor[] actors = Array.Empty<TutorialActor>();
        public string[] dialogs = Array.Empty<string>();
        public bool IsValid => version == 1 ? step >= 0 && step <= 4 : version == 2 && step >= 0 && step <= Complete
            && actors != null && actors.Length <= 7 && dialogs != null && dialogs.Length <= 12
            && Array.TrueForAll(actors,a=>a!=null && a.x>=0 && a.y>=0 && a.x<256 && a.y<256)
            && (!finished || step == Complete);
        // The old four-page account guide never completed the original battle tutorial.
        public bool IsComplete => version == 2 && IsValid && finished;
        public NativeTutorialProgress Clone()
        {
            var copy=(NativeTutorialProgress)MemberwiseClone();
            copy.actors=actors==null?Array.Empty<TutorialActor>():Array.ConvertAll(actors,a=>a?.Clone());
            copy.dialogs=dialogs==null?Array.Empty<string>():(string[])dialogs.Clone();return copy;
        }
        public bool TryAdvance(int expectedStep,bool movementPracticed,bool skip,out NativeTutorialProgress next)
        {next=null;return false;} // A page increment cannot satisfy a script input condition.
    }
}
