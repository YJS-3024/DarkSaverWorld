using System;
using System.Collections.Generic;

namespace DarkSaver.Combat
{
    public sealed partial class LocalCombatAuthority
    {
        private readonly Dictionary<int, (int Mp, int Range, int Percent, float Seconds)> quickRules = new();
        private readonly Dictionary<short, (int Percent, double Remaining)> quickBuffs = new();

        // User-confirmed effect: lower action expenditure, never faster AP recovery.
        // Percentage, duration and affected actions are explicit single-player policy.
        public bool ConfigureTemporaryQuickMagic(int id, int mp, int range, int percent, float seconds)
        {
            if (id <= 0 || id > short.MaxValue || mp < 0 || range < 0 || range > 255
                || percent < 1 || percent > 99 || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return false;
            abilityRules.Remove(id);quickRules[id] = (mp, range, percent, seconds);
            magicRules.Remove(id); buffRules.Remove(id); statusRules.Remove(id); return true;
        }

        public bool TryGetTemporaryQuick(CombatUnit unit, out int percent, out double seconds)
        {
            percent = 0; seconds = 0;
            if (!TemporaryRulesEnabled || unit == null || !unit.Alive
                || !quickBuffs.TryGetValue(unit.Id, out var buff) || buff.Remaining <= 0) return false;
            percent = buff.Percent; seconds = buff.Remaining; return true;
        }

        public int EffectiveActionCost(CombatUnit unit, int cost)
        {
            if(unit==null || cost<=0 || cost>unit.ActionPoints.Maximum)return cost;
            long result=cost;
            // Discrete AP must still benefit from a positive Quick/Rapid percentage.
            if(TryGetTemporaryQuick(unit,out int percent,out _))
                result=cost<=1?1:Math.Max(1,Math.Min(cost-1,(result*(100-percent)+99)/100));
            if(TryGetTemporaryStatus(unit,TemporaryStatusEffect.Slow,out int slow,out _))result=Math.Min(unit.ActionPoints.Maximum,(result*(100+Math.Min(90,slow))+99)/100);
            return (int)result;
        }

        private void AdvanceQuick(CombatWorld world, float seconds)
        {
            foreach (short id in new List<short>(quickBuffs.Keys))
            {
                var buff = quickBuffs[id]; double remaining = buff.Remaining - seconds;
                if (remaining <= 0 || !world.TryGet(id, out var unit) || !unit.Alive) quickBuffs.Remove(id);
                else quickBuffs[id] = (buff.Percent, remaining);
            }
        }
    }
}
