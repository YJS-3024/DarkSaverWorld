using System;

namespace DarkSaver.Combat
{
    // Per-unit AP only. Does not decide AI, paths, animation, or damage.
    public sealed class CombatActionPoints
    {
        private double elapsed;
        private readonly Func<int> maximum;
        public CombatActionPoints(bool enemy = false) : this(() => GameConstants.GetBattleMaximumAp(enemy)) { }
        internal CombatActionPoints(Func<int> maximum) { this.maximum = maximum ?? throw new ArgumentNullException(nameof(maximum)); }
        public int Current { get; private set; } = GameConstants.BattleStartingAp;
        public int Maximum => maximum();

        public bool CanStart(int cost) => cost > 0 && cost <= Maximum && Current == Maximum;

        // SetTickMenu assigns a signed incoming tick clamped to [0, maximum].
        // It does not deduct a cost or alter the recovery clock.
        public bool ApplyAbsolute(int value)
        {
            if (Maximum <= 0) return false;
            Current = Math.Max(0, Math.Min(Maximum, value)); return true;
        }

        public bool TrySpend(int cost)
        {
            if (!CanStart(cost)) return false;
            Current -= cost; elapsed = 0;
            return true;
        }

        public bool Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) return false;
            if (GameConstants.BattleApRecoverySeconds <= 0 || GameConstants.BattleApRecoveryAmount <= 0 ||
                Maximum <= 0 || Current < 0 || Current > Maximum)
                throw new InvalidOperationException("Invalid AP constants.");
            if (Current == Maximum) { elapsed = 0; return true; }
            elapsed += seconds;
            var ticks = Math.Floor(elapsed / GameConstants.BattleApRecoverySeconds);
            var recovered = (int)Math.Min(Maximum - Current, ticks * GameConstants.BattleApRecoveryAmount);
            Current += recovered;
            elapsed = Current == Maximum ? 0 : elapsed % GameConstants.BattleApRecoverySeconds;
            return true;
        }
    }
}
