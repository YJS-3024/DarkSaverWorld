using System;
using DarkSaver.Magic;
namespace DarkSaver.Combat
{
    public enum AbilityEffect {Damage,Heal,HpDrain,MpDrain,RestoreMp,Revive,AttackBuff,DefenseBuff,Quick,Poison,CurePoison,MovementReduction,Silence,Sleep,Wake,AttackDown,MagicDefenseDown,MagicDefenseUp,MagicAttackUp,Slow,Regeneration,PoisonWeapon,Accuracy,Berserk,SleepWard,TurnUndead,Hiring,Emote}


    public sealed class UnresolvedAbilityRule
    {
        public int Id,Mp,Range,Power,Ap;public float Seconds;public AbilityEffect Effect;public bool Physical;
        public bool Combat=>Effect!=AbilityEffect.Hiring && Effect!=AbilityEffect.Emote;
        public bool Support=>Effect==AbilityEffect.Heal || Effect==AbilityEffect.RestoreMp || Effect==AbilityEffect.Revive || Effect==AbilityEffect.AttackBuff || Effect==AbilityEffect.DefenseBuff || Effect==AbilityEffect.Quick || Effect==AbilityEffect.CurePoison || Effect==AbilityEffect.Wake || Effect==AbilityEffect.MagicDefenseUp || Effect==AbilityEffect.MagicAttackUp || Effect==AbilityEffect.Regeneration || Effect==AbilityEffect.PoisonWeapon || Effect==AbilityEffect.Accuracy || Effect==AbilityEffect.Berserk || Effect==AbilityEffect.SleepWard;
    }
    public static class UnresolvedAbilityRules
    {
        public static bool TryCreate(MagicCatalogEntry entry,out UnresolvedAbilityRule rule)
{rule=default;return false;}        public static bool IsUndead(CombatUnit unit)
{return false;}    }
}
