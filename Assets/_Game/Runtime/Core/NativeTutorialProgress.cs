using System;

namespace DarkSaver.Core
{
    // Authored single-player onboarding. Null on older profiles means no forced replay.
    [Serializable]
    public sealed class NativeTutorialProgress
    {
        public const int Complete = 4;
        public int version = 1;
        public int step;
        public bool IsValid => version == 1 && step >= 0 && step <= Complete;
        public bool IsComplete => IsValid && step == Complete;
        public NativeTutorialProgress Clone() => new NativeTutorialProgress { version = version, step = step };
        public bool TryAdvance(int expectedStep, bool movementPracticed, bool skip, out NativeTutorialProgress next)
        {
            next = null;
            if (!IsValid || step != expectedStep || IsComplete || (!skip && step == 1 && !movementPracticed)) return false;
            next = Clone(); next.step = skip ? Complete : step + 1; return true;
        }
    }
}
