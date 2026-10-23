using System;
using DarkSaver.Jobs;
using UnityEngine;

namespace DarkSaver.Combat
{
    [CreateAssetMenu(menuName = "DarkSaver/Battle Enemy")]
    public sealed class BattleEnemyDefinition : ScriptableObject
    {
        [SerializeField] private short instanceId;
        [SerializeField] private int jobId;
        [SerializeField] private string displayName;
        [SerializeField] private Vector2Int scriptPosition;
        [Tooltip("Use native job columns 2/4/6/8 only. Missing or invalid data rejects creation; no level/reward formula is applied.")]
        [SerializeField] private bool useJobCatalogBaseStats;
        [SerializeField] private JobCatalog jobCatalog;
        [Tooltip("Opt-in single-player rule: grant native job column 32 to the sole player on defeat. No server level/distribution formula.")]
        [SerializeField] private bool useNativeBaseDefeatExperience;
        [Tooltip("Explicit stats used only when Use Job Catalog Base Stats is disabled.")]
        [SerializeField] private int baseHp;
        [SerializeField] private int baseMp;
        [SerializeField] private int baseAttack;
        [SerializeField] private int baseDefense;
        [SerializeField] private BattleObjectiveDefinition victoryObjective;

        public int JobId => jobId;
        public bool UsesJobCatalogBaseStats => useJobCatalogBaseStats;
        public BattleObjectiveDefinition VictoryObjective => victoryObjective;
        public bool UsesNativeBaseDefeatExperience => useNativeBaseDefeatExperience;

        public bool TryGetDefeatExperience(out uint experience)
        {
            experience = 0;
            if (!useNativeBaseDefeatExperience) return true;
            if (jobCatalog == null || !jobCatalog.TryGet(jobId, out var entry)) return false;
            var value = entry.GetNumericValue(32); // 획득경; preserve invalid sentinels rather than clamp.
            if (value < 0 || value > uint.MaxValue) return false;
            experience = (uint)value;
            return true;
        }

        // Base ATR values only. Server-side level/stat derivation is not reconstructed.
        public bool TryCreateUnit(out CombatUnit unit)
        {
            unit = null;
            long hp = baseHp, mp = baseMp, attack = baseAttack, defense = baseDefense,magicAttack=0,magicDefense=0;
            if (useJobCatalogBaseStats)
            {
                if (jobCatalog == null || !jobCatalog.TryGet(jobId, out var entry)) return false;
                // 체력수, 마력수, 공력수, 방력수; the corresponding 한 columns are not derived stats.
                hp = entry.GetNumericValue(2);
                mp = entry.GetNumericValue(4);
                attack = entry.GetNumericValue(6);
                defense = entry.GetNumericValue(8);
                magicAttack=entry.GetNumericValue(10);magicDefense=entry.GetNumericValue(12);
            }
            // Preserve sentinels in the catalog, but never clamp them into usable combat stats.
            if (hp <= 0 || hp > int.MaxValue || !IsNonNegativeInt(mp)
                || !IsNonNegativeInt(attack) || !IsNonNegativeInt(defense)) return false;
            unit = new CombatUnit
            {
                Id = instanceId, Name = displayName, Enemy = true,
                X = scriptPosition.x, Y = scriptPosition.y,
                Hp = (int)hp, MaxHp = (int)hp, Mp = (int)mp, MaxMp = (int)mp,
                Attack = (int)attack, Defense = (int)defense,HasMagicAttack=useJobCatalogBaseStats && IsNonNegativeInt(magicAttack),HasMagicDefense=useJobCatalogBaseStats && IsNonNegativeInt(magicDefense),
                MagicAttack=IsNonNegativeInt(magicAttack)?(int)magicAttack:0,MagicDefense=IsNonNegativeInt(magicDefense)?(int)magicDefense:0
            };
            return true;
        }

        public CombatUnit CreateUnit()
        {
            if (TryCreateUnit(out var unit)) return unit;
            throw new InvalidOperationException("Invalid or missing battle enemy base stats.");
        }

        private static bool IsNonNegativeInt(long value) => value >= 0 && value <= int.MaxValue;
    }
}
