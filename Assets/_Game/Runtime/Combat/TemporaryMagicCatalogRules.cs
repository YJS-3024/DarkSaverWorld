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
            int count=0;
            for(int i=0;i<catalog.Count;i++)if(battle.ConfigureSinglePlayerAbility(catalog.GetAt(i)))count++;
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
