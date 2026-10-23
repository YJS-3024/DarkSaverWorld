using UnityEngine;
namespace DarkSaver.UI
{
    public static class BattleFeedbackMotion
    {
        // userunit.dll 10015260; shared by HP delta, MP delta and MISS.
        static readonly int[] nativeY = { 0, -4, -5, -4, 0, 1, 4, 10, 4, 1, 0, 1, 4, 10 };
        public static bool TryGetStepOffset(int step, out Vector2 offset)
        {
            offset = default;
            if (step <= 0 || step >= nativeY.Length) return false;
            offset = new Vector2(step, nativeY[step]); return true;
        }
        public static bool TryGetOffset(float seconds, out Vector2 offset)
        {
            offset = default;
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)
                || GameConstants.BattleFeedbackSeconds <= 0 || seconds >= GameConstants.BattleFeedbackSeconds) return false;
            // Counter-to-seconds conversion remains adjustable, not recovered timing.
            var step = (int)System.Math.Floor((double)seconds / GameConstants.BattleFeedbackSeconds * 14);
            return TryGetStepOffset(step, out offset);
        }
    }
}
