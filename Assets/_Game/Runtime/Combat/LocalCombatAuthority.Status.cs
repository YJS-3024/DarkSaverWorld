using System;
using System.Collections.Generic;

namespace DarkSaver.Combat
{
    public enum TemporaryStatusEffect { None, Poison, CurePoison, MovementReduction, Silence, Sleep, Wake }

    public sealed partial class LocalCombatAuthority
    {
        private readonly Dictionary<int, (int Mp, int Range, TemporaryStatusEffect Effect, int Power, float Seconds)> statusRules = new();
        private readonly Dictionary<(short Id, TemporaryStatusEffect Effect), (short Source, int Power, double Remaining, double Elapsed)> statuses = new();

        public bool ConfigureTemporaryStatusMagic(int id, int mp, int range, TemporaryStatusEffect effect, int power, float seconds)
        {
            if (id <= 0 || id > short.MaxValue || mp < 0 || range < 0 || range > 255 || power < 1
                || (effect != TemporaryStatusEffect.Poison && effect != TemporaryStatusEffect.CurePoison && effect != TemporaryStatusEffect.MovementReduction
                    && effect != TemporaryStatusEffect.Silence && effect != TemporaryStatusEffect.Sleep && effect != TemporaryStatusEffect.Wake)
                || (effect == TemporaryStatusEffect.MovementReduction && power > 99)
                || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return false;
            statusRules[id] = (mp, range, effect, power, seconds);
            magicRules.Remove(id); buffRules.Remove(id); quickRules.Remove(id); return true;
        }

        public bool TryGetTemporaryStatus(CombatUnit unit, TemporaryStatusEffect effect, out int power, out double seconds)
        {
            power = 0; seconds = 0;
            if (!TemporaryRulesEnabled || unit == null || !unit.Alive
                || !statuses.TryGetValue((unit.Id, effect), out var state) || state.Remaining <= 0) return false;
            power = state.Power; seconds = state.Remaining; return true;
        }

        private bool IsSupportStatusMagic(int id) => statusRules.TryGetValue(id, out var rule)
            && (rule.Effect == TemporaryStatusEffect.CurePoison || rule.Effect == TemporaryStatusEffect.Wake);
        public bool CanAct(CombatUnit unit) => unit != null && unit.Alive
            && !TryGetTemporaryStatus(unit, TemporaryStatusEffect.Sleep, out _, out _);
        public bool CanCast(CombatUnit unit) => CanAct(unit)
            && !TryGetTemporaryStatus(unit, TemporaryStatusEffect.Silence, out _, out _);

        private void ApplyTemporaryStatus(CombatResult result)
        {
            if (!TemporaryRulesEnabled) return;
            if (result.Damage > 0) statuses.Remove((result.TargetId, TemporaryStatusEffect.Sleep));
            if (result.StatusEffect == TemporaryStatusEffect.Wake) statuses.Remove((result.TargetId, TemporaryStatusEffect.Sleep));
            else if (result.StatusEffect == TemporaryStatusEffect.CurePoison)
                statuses.Remove((result.TargetId, TemporaryStatusEffect.Poison));
            else if (result.StatusEffect != TemporaryStatusEffect.None && result.StatusSeconds > 0)
                statuses[(result.TargetId, result.StatusEffect)] = (result.ActorId, result.StatusPower, result.StatusSeconds, 0);
        }

        private void RemoveStatuses(short id)
        {
            statuses.Remove((id, TemporaryStatusEffect.Poison));
            statuses.Remove((id, TemporaryStatusEffect.MovementReduction));
            statuses.Remove((id, TemporaryStatusEffect.Silence));
            statuses.Remove((id, TemporaryStatusEffect.Sleep));
        }

        // Local timing policy: poison ticks every two seconds, including the final expiry tick.
        internal List<(short Target, short Source, int Damage)> AdvanceStatuses(CombatWorld world, float seconds)
        {
            var ticks = new List<(short, short, int)>();
            foreach (var key in new List<(short Id, TemporaryStatusEffect Effect)>(statuses.Keys))
            {
                var state = statuses[key];
                if (!world.TryGet(key.Id, out var unit) || !unit.Alive) { statuses.Remove(key); continue; }
                double active = Math.Min(seconds, state.Remaining), elapsed = state.Elapsed + active;
                if (key.Effect == TemporaryStatusEffect.Poison)
                {
                    double count = Math.Floor(elapsed / 2);
                    if (count > 0) ticks.Add((key.Id, state.Source, (int)Math.Min(int.MaxValue, count * state.Power)));
                    elapsed %= 2;
                }
                double remaining = state.Remaining - seconds;
                if (remaining <= 0) statuses.Remove(key);
                else statuses[key] = (state.Source, state.Power, remaining, elapsed);
            }
            return ticks;
        }

        public int EffectiveMovementBudget(CombatUnit unit, int budget)
        {
            if (budget < 0 || !TryGetTemporaryStatus(unit, TemporaryStatusEffect.MovementReduction, out int percent, out _)) return budget;
            return (int)((long)budget * (100 - percent) / 100);
        }
    }
}
