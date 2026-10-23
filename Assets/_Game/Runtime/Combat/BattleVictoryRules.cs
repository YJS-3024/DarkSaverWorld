using System.Collections.Generic;

namespace DarkSaver.Combat
{
    public static class BattleVictoryRules
    {
        // Null/empty targets use the demo's elimination rule. Explicit targets are instance IDs,
        // not job/template IDs from the original ability table.
        public static bool IsSatisfied(CombatWorld world, IReadOnlyList<short> targetIds)
        {
            if (targetIds != null && targetIds.Count > 0)
            {
                foreach (var id in targetIds)
                {
                    CombatUnit target = null;
                    foreach (var unit in world.Units)
                        if (unit.Id == id) { target = unit; break; }
                    // Missing targets must not be interpreted as already defeated.
                    if (target == null || !target.Enemy || target.LifeState == UnitLifeState.Active) return false;
                }
                return true;
            }

            var foundEnemy = false;
            foreach (var unit in world.Units)
            {
                if (!unit.Enemy) continue;
                foundEnemy = true;
                // HP 0 alone is not a death confirmation (HP and death packets are separate).
                if (unit.LifeState == UnitLifeState.Active) return false;
            }
            return foundEnemy;
        }
    }
}
