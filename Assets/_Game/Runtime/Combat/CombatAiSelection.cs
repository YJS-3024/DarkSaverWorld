using System;
using System.Collections.Generic;

namespace DarkSaver.Combat
{
    // Recovered scheduling/selection mechanics only. No invented clock, AP cost,
    // target resolver or server combat formula. Capacity is not a world unit limit.
    public sealed class CombatAiUnitQueue
    {
        public const int Capacity = 100;
        private short[] units = Array.Empty<short>();
        private int next;
        public int Count => units.Length;

        public bool TrySetUnits(IReadOnlyList<short> source)
        {
            if (source == null || source.Count > Capacity) return false;
            var copy = new short[source.Count];
            for (var i = 0; i < copy.Length; i++) copy[i] = source[i];
            units = copy; next = 0; return true;
        }

        // The owner checks existence/life/action readiness. Do not consume a second
        // queue entry in the same call merely because the first unit cannot act.
        public bool TryNext(out short unitId)
        {
            unitId = default;
            if (units.Length == 0) return false;
            unitId = units[next];
            next = (next + 1) % units.Length;
            return true;
        }
    }

    public static class CombatAiActionSelection
    {
        // Caller provides rand()%100 and the original file-order weights.
        // Uncovered rolls produce no choice; totals >100 clip later actions,
        // never normalize probabilities or substitute an attack/rest action.
        public static bool TrySelect(IReadOnlyList<int> weights, int roll, out int actionIndex)
        {
            actionIndex = -1;
            if (weights == null || roll < 0 || roll >= 100) return false;
            for (var i = 0; i < weights.Count; i++)
                if (weights[i] < 0) return false; // Native input validation, not original negative-weight behavior.
            long cumulative = 0;
            for (var i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative) { actionIndex = i; return true; }
            }
            return false;
        }
    }
}
