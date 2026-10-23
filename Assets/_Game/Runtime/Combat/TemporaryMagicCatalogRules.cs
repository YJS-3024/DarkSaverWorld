using System;
using DarkSaver.Magic;

namespace DarkSaver.Combat
{
    public enum TemporaryMagicEffect { Unresolved, Damage, Healing, AttackBuff, DefenseBuff, Quick, Poison, CurePoison, MovementReduction, Silence, Sleep, Wake }

    // Explicit family names only. This is not a reconstruction of the server effect table.
    public static class TemporaryMagicCatalogRules
    {
        public static int Configure(BattleSceneRoot battle, MagicCatalog catalog)
        {
            if (battle == null || catalog == null || !battle.TemporaryLocalRulesEnabled) return 0;
            int count = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                var entry = catalog.GetAt(i);
                if (battle.TryGetTemporaryMagicMpCost(entry.Id, out _) || !TryClassify(entry, out var effect, out var level)) continue;
                int extra = level - 1;
                if (effect == TemporaryMagicEffect.Poison || effect == TemporaryMagicEffect.CurePoison || effect == TemporaryMagicEffect.MovementReduction
                    || effect == TemporaryMagicEffect.Silence || effect == TemporaryMagicEffect.Sleep || effect == TemporaryMagicEffect.Wake)
                {
                    if (entry.Mp < 0 || entry.Mp > int.MaxValue || entry.Range < 0 || entry.Range > 255) continue;
                    var status = effect == TemporaryMagicEffect.Poison ? TemporaryStatusEffect.Poison
                        : effect == TemporaryMagicEffect.CurePoison ? TemporaryStatusEffect.CurePoison : effect == TemporaryMagicEffect.Silence ? TemporaryStatusEffect.Silence
                        : effect == TemporaryMagicEffect.Sleep ? TemporaryStatusEffect.Sleep
                        : effect == TemporaryMagicEffect.Wake ? TemporaryStatusEffect.Wake : TemporaryStatusEffect.MovementReduction;
                    // Data-derived poison magnitude; fixed local timing/movement policy, not server formulas.
                    int power = status == TemporaryStatusEffect.Poison ? (int)Math.Max(1, Math.Min(1000, entry.GetNumericValue(9)))
                        : status == TemporaryStatusEffect.MovementReduction ? Math.Min(60, 25 + extra * 5) : 1;
                    if (battle.ConfigureTemporaryStatusMagic(entry.Id, (int)entry.Mp, (int)entry.Range, status, power, 20))
                    { battle.ConfigureMagicApCost(entry.Id, GameConstants.BattleTemporaryBuffApCost); count++; }
                    continue;
                }
                if (effect == TemporaryMagicEffect.Quick)
                {
                    // Native MP/range; local percentage/duration/cost, not decoded EA/EB semantics.
                    if (entry.Mp < 0 || entry.Mp > int.MaxValue || entry.Range < 0 || entry.Range > 255) continue;
                    if (battle.ConfigureTemporaryQuickMagic(entry.Id, (int)entry.Mp, (int)entry.Range,
                        Math.Min(45, 20 + extra * 5), GameConstants.BattleTemporaryBuffSeconds))
                    { battle.ConfigureMagicApCost(entry.Id, GameConstants.BattleTemporaryBuffApCost); count++; }
                    continue;
                }
                int mp = (effect == TemporaryMagicEffect.Damage ? GameConstants.BattleTemporaryMagic101MpCost
                    : effect == TemporaryMagicEffect.Healing ? GameConstants.BattleTemporaryHeal2401MpCost : GameConstants.BattleTemporaryBuffMpCost)
                    + extra * GameConstants.BattleTemporaryMagicMpPerLevel;
                bool success;
                if (effect == TemporaryMagicEffect.Damage)
                    success = battle.ConfigureTemporaryDamageMagic(entry.Id, mp, GameConstants.BattleTemporaryMagic101Range,
                        GameConstants.BattleTemporaryMagicPowerBonus + extra * GameConstants.BattleTemporaryMagicPowerPerLevel,
                        GameConstants.BattleTemporaryMagicDefenseDivisor);
                else if (effect == TemporaryMagicEffect.Healing)
                    success = battle.ConfigureTemporaryHealingMagic(entry.Id, mp, GameConstants.BattleTemporaryHeal2401Range,
                        GameConstants.BattleTemporaryHeal2401Amount + extra * GameConstants.BattleTemporaryHealPerLevel);
                else
                {
                    int bonus = GameConstants.BattleTemporaryBuffAmount + extra * GameConstants.BattleTemporaryBuffPerLevel;
                    success = battle.ConfigureTemporaryBuffMagic(entry.Id, mp, GameConstants.BattleTemporaryBuffRange,
                        effect == TemporaryMagicEffect.AttackBuff ? bonus : 0, effect == TemporaryMagicEffect.DefenseBuff ? bonus : 0,
                        GameConstants.BattleTemporaryBuffSeconds);
                }
                if (!success) continue;
                battle.ConfigureMagicApCost(entry.Id, effect == TemporaryMagicEffect.Damage
                    ? GameConstants.BattleTemporaryMagic101ApCost : GameConstants.BattleTemporaryBuffApCost);
                count++;
            }
            return count;
        }

        public static bool TryClassify(MagicCatalogEntry entry, out TemporaryMagicEffect effect, out int level)
        {
            effect = TemporaryMagicEffect.Unresolved; level = 1;
            if (entry == null || entry.Id <= 0 || entry.Id > short.MaxValue || string.IsNullOrWhiteSpace(entry.Name)) return false;
            string name = entry.Name.Trim(); int open = name.IndexOf('(');
            if (open >= 0)
            {
                string suffix = name.Substring(open).ToUpperInvariant();
                if (!suffix.StartsWith("(LV", StringComparison.Ordinal) || !suffix.EndsWith(")", StringComparison.Ordinal)
                    || !int.TryParse(suffix.Substring(3, suffix.Length - 4), out level) || level < 1 || level > 100) return false;
                name = name.Substring(0, open).Trim();
            }
            effect = name switch
            {
                "파이어볼" or "파이어" or "프리즈" or "블리자드" or "썬더" or "썬더스톰"
                    or "윈드커터" or "토네이도" or "어스퀘이크" or "메테오" or "블래스트" => TemporaryMagicEffect.Damage,
                "힐" or "포스힐" => TemporaryMagicEffect.Healing,
                "어택" => TemporaryMagicEffect.AttackBuff,
                "프로텍션" => TemporaryMagicEffect.DefenseBuff,
                "퀵" => TemporaryMagicEffect.Quick,
                "포이즌" or "포이즌2" => TemporaryMagicEffect.Poison,
                "큐어" or "해독제" or "초급해독제_묶음" or "중급해독제_묶음" or "고급해독제_묶음" => TemporaryMagicEffect.CurePoison,
                "디무브" => TemporaryMagicEffect.MovementReduction,
                "뮤트" => TemporaryMagicEffect.Silence,
                "슬립" => TemporaryMagicEffect.Sleep,
                "각성제" => TemporaryMagicEffect.Wake,
                _ => TemporaryMagicEffect.Unresolved
            };
            return effect != TemporaryMagicEffect.Unresolved;
        }
    }
}
