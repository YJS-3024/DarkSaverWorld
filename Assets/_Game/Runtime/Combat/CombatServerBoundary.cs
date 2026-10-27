using System;
using System.Collections.Generic;
namespace DarkSaver.Combat {
// Client guard and unresolved server contracts only. No damage, hit, effect or reward simulator.
public enum TemporaryStatusEffect { None, Poison, CurePoison, MovementReduction, Silence, Sleep, Wake,AttackDown,MagicDefenseDown,MagicDefenseUp,MagicAttackUp,Slow,Regeneration,PoisonWeapon,Accuracy,Berserk,SleepWard }
public sealed class CombatServerBoundary {
public const int PrototypeMagicId=0;public const int PrototypeMagicMpCost=0;
public bool TemporaryRulesEnabled { get=>false;set{} }
public CombatServerBoundary(Func<double,double,double> hitRule=null,Func<double> sample=null){}
public bool ConfigureTemporaryBuffMagic(int id, int mpCost, int range, int attack, int defense, float seconds)
{
    return false;
}
public bool IsTemporarySupportMagic(int id)
{
    return false;
}
public int EffectiveAttack(CombatUnit unit)
{
    return unit?.Attack ?? 0;
}
public int EffectiveDefense(CombatUnit unit)
{
    return unit?.Defense ?? 0;
}
public bool TryGetTemporaryBuff(CombatUnit unit, bool attack, out int bonus, out double seconds)
{
    bonus = default;
    seconds = default;
    return false;
}
public void ApplyTemporaryBuff(CombatResult result)
{
}
public void RemoveTemporaryBuffs(short id)
{
}
public void AdvanceTemporaryBuffs(CombatWorld world, float seconds)
{
}
public bool ConfigureTemporaryDamageMagic(int id, int mpCost, int range, int powerBonus, int defenseDivisor)
{
    return false;
}
public bool ConfigureTemporaryHealingMagic(int id, int mpCost, int range, int healing)
{
    return false;
}
public bool IsTemporaryHealingMagic(int id)
{
    return false;
}
public bool TryGetTemporaryMagicRange(int id, out int range)
{
    range = default;
    return false;
}
public bool TryGetTemporaryMagicMpCost(int id, out int cost)
{
    cost = default;
    return false;
}
public bool ConfigurePhysicalAccuracy(short unitId, double accuracyPercent, double evasionPercent)
{
    return false;
}
public bool TryValidateRequest(CombatWorld world, CombatRequest request, out string reason)
{
    reason = string.Empty;
    if (world == null || request.Action != CombatActionType.Attack || !world.TryGet(request.ActorId, out var actor) || !world.TryGet(request.TargetId, out var target) || actor.Id == target.Id || !actor.Alive || !target.Alive)
    {
        reason = "유효한 원작 공격 요청 상태가 필요합니다.";
        return false;
    }

    return true;
}
internal bool TryValidateItemRequest(CombatWorld world, CombatRequest request, out string reason)
{
    reason = "원작 아이템 승인 응답이 필요합니다.";
    return false;
}
public CombatResult Resolve(CombatWorld world, CombatRequest request)
{
    throw new NotSupportedException("Original server rule is not available.");
}
internal CombatResult ResolveItem(CombatWorld world, CombatRequest request)
{
    throw new NotSupportedException("Original server rule is not available.");
}
public bool ConfigureTemporaryStatusMagic(int id, int mp, int range, TemporaryStatusEffect effect, int power, float seconds)
{
    return false;
}
public bool TryGetTemporaryStatus(CombatUnit unit, TemporaryStatusEffect effect, out int power, out double seconds)
{
    power = default;
    seconds = default;
    return false;
}
public bool CanAct(CombatUnit unit)
{
    return unit != null && unit.Alive;
}
public bool CanCast(CombatUnit unit)
{
    return unit != null && unit.Alive;
}
internal List<(short Target, short Source, int Damage)> AdvanceStatuses(CombatWorld world, float seconds)
{
    return new List<(short Target, short Source, int Damage)>();
}
public int EffectiveMovementBudget(CombatUnit unit, int budget)
{
    return budget;
}
public bool ConfigureTemporaryQuickMagic(int id, int mp, int range, int percent, float seconds)
{
    return false;
}
public bool TryGetTemporaryQuick(CombatUnit unit, out int percent, out double seconds)
{
    percent = default;
    seconds = default;
    return false;
}
public int EffectiveActionCost(CombatUnit unit, int cost)
{
    return cost;
}
public bool ConfigureAbility(UnresolvedAbilityRule rule)
{
    return false;
}
public bool IsRevivalMagic(int id)
{
    return false;
}
public int EffectiveMagicAttack(CombatUnit unit)
{
    return unit?.MagicAttack ?? 0;
}
public int EffectiveMagicDefense(CombatUnit unit)
{
    return unit?.MagicDefense ?? 0;
}
}}
