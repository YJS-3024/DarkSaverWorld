using System;
using DarkSaver.Magic;

namespace DarkSaver.Combat
{
    public enum TemporaryMagicEffect { Unresolved, Damage, Healing, AttackBuff, DefenseBuff, Quick, Poison, CurePoison, MovementReduction, Silence, Sleep, Wake }

    public static class UnresolvedMagicExecution
    {
        public static int Configure(BattleSceneRoot battle, MagicCatalog catalog)
{return 0;}
        public static bool TryClassify(MagicCatalogEntry entry, out TemporaryMagicEffect effect, out int level)
{effect=default;level=default;return false;}    }
}
