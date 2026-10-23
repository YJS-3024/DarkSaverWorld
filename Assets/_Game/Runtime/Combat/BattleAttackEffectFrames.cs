using UnityEngine;

namespace DarkSaver.Combat
{
    public static class BattleAttackEffectFrames
    {
        // userunit.dll 1000FFD7: (counter / 2) % 4. State expires above 48.
        public static int FrameAt(float seconds)
        {
            if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)
                || GameConstants.BattleAttackCounterSeconds <= 0) return -1;
            var tick = System.Math.Floor((double)seconds / GameConstants.BattleAttackCounterSeconds);
            return tick > 48 ? -1 : ((int)tick / 2) % 4;
        }
        public static Sprite[] Load()
        {
            var input = Resources.LoadAll<Sprite>("Effects/BattleAttackFx2");
            var output = new Sprite[4];
            foreach (var sprite in input)
                for (var i = 0; i < 4; i++) if (sprite.name == "NativeAttack_" + i) output[i] = sprite;
            return output;
        }
    }
}
