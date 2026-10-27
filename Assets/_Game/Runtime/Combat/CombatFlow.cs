using System;
using System.Collections.Generic;

namespace DarkSaver.Combat
{
    public sealed class CombatFlow
    {
        private readonly CombatWorld world;
        private readonly CombatServerBoundary authority;
        private readonly bool useActionPoints;
        private readonly Dictionary<short, int> attackApCosts = new();
        private readonly Dictionary<int, int> magicApCosts = new();
        private readonly Dictionary<int, int> temporaryJobAttackCosts = new();
        public bool ConfigureTemporaryJobAttackCost(int jobId, int cost)
        {
            if (jobId < 0 || cost < 1 || cost > GameConstants.BattleMaximumAp) return false;
            temporaryJobAttackCosts[jobId] = cost; return true;
        }
        public bool TemporaryRulesEnabled
        {
            get => authority.TemporaryRulesEnabled;
            set => authority.TemporaryRulesEnabled = value;
        }

        public bool ConfigurePhysicalAccuracy(short unitId, double accuracyPercent, double evasionPercent) =>
            world.TryGet(unitId, out _) && authority.ConfigurePhysicalAccuracy(unitId, accuracyPercent, evasionPercent);

        public bool ConfigureAbility(UnresolvedAbilityRule rule)=>authority.ConfigureAbility(rule);
        public bool IsRevivalMagic(int id)=>authority.IsRevivalMagic(id);
        public bool ConfigureTemporaryDamageMagic(int id, int mpCost, int range, int powerBonus, int defenseDivisor) =>
            authority.ConfigureTemporaryDamageMagic(id, mpCost, range, powerBonus, defenseDivisor);
        public bool TryGetTemporaryMagicRange(int id, out int range) => authority.TryGetTemporaryMagicRange(id, out range);
        public bool TryGetTemporaryMagicMpCost(int id, out int cost) => authority.TryGetTemporaryMagicMpCost(id, out cost);
        public bool ConfigureTemporaryHealingMagic(int id, int mpCost, int range, int healing) =>
            authority.ConfigureTemporaryHealingMagic(id, mpCost, range, healing);
        public bool IsTemporaryHealingMagic(int id) => authority.IsTemporaryHealingMagic(id);
        public bool IsTemporarySupportMagic(int id) => authority.IsTemporarySupportMagic(id);
        public bool TryGetEffectiveStats(short id, out int attack, out int defense)
        {
            attack = defense = 0;
            if (!world.TryGet(id, out var unit)) return false;
            attack = authority.EffectiveAttack(unit); defense = authority.EffectiveDefense(unit); return true;
        }
        public bool TryGetTemporaryBuff(short id, bool attack, out int bonus, out double seconds)
        {
            bonus = 0; seconds = 0;
            return world.TryGet(id, out var unit) && authority.TryGetTemporaryBuff(unit, attack, out bonus, out seconds);
        }
        public bool ConfigureTemporaryBuffMagic(int id, int mpCost, int range, int attack, int defense, float seconds) =>
            authority.ConfigureTemporaryBuffMagic(id, mpCost, range, attack, defense, seconds);


        public bool CanUnitAct(short id) => world.TryGet(id, out var unit) && authority.CanAct(unit);
        public bool CanUnitCast(short id) => world.TryGet(id, out var unit) && authority.CanCast(unit);

        public bool ConfigureTemporaryStatusMagic(int id, int mp, int range, TemporaryStatusEffect effect, int power, float seconds) =>
            authority.ConfigureTemporaryStatusMagic(id, mp, range, effect, power, seconds);
        public bool TryGetTemporaryStatus(short id, TemporaryStatusEffect effect, out int power, out double seconds)
        {
            power = 0; seconds = 0;
            return world.TryGet(id, out var unit) && authority.TryGetTemporaryStatus(unit, effect, out power, out seconds);
        }
        public int GetEffectiveMovementBudget(short id, int budget) =>
            world.TryGet(id, out var unit) ? authority.EffectiveMovementBudget(unit, budget) : budget;

        public bool ConfigureTemporaryQuickMagic(int id, int mp, int range, int percent, float seconds) =>
            authority.ConfigureTemporaryQuickMagic(id, mp, range, percent, seconds);
        public int GetEffectiveActionCost(short id, int cost) =>
            world.TryGet(id, out var unit) ? authority.EffectiveActionCost(unit, cost) : cost;
        public bool TryGetTemporaryQuick(short id, out int percent, out double seconds)
        {
            percent = 0; seconds = 0;
            return world.TryGet(id, out var unit) && authority.TryGetTemporaryQuick(unit, out percent, out seconds);
        }

        // Final resolved AP costs, not raw ATR TICK or an assumed percentage formula.
        public int GetAttackApCost(short unitId)
        {
            var cost = attackApCosts.TryGetValue(unitId, out var configured) ? configured : GameConstants.BattleAttackApCost;
            if (cost == 0 && TemporaryRulesEnabled && world.TryGet(unitId, out var unit))
                cost = !unit.Enemy && temporaryJobAttackCosts.TryGetValue(unit.JobId, out var jobCost)
                    ? jobCost : GameConstants.GetTemporaryAttackApCost(unit.JobId, unit.Enemy);
            return GetEffectiveActionCost(unitId, cost);
        }
        public int GetMagicApCost(int magicId)
        {
            var cost = magicApCosts.TryGetValue(magicId, out var configured) ? configured : GameConstants.GetBattleMagicApCost(magicId);
            if (cost == 0 && TemporaryRulesEnabled && (magicId == 2101 || magicId == 2001))
                return GameConstants.BattleTemporaryBuffApCost;
            if (cost == 0 && TemporaryRulesEnabled && magicId == 2401)
                return GameConstants.BattleTemporaryHeal2401ApCost;
            return cost == 0 && TemporaryRulesEnabled && magicId == CombatServerBoundary.PrototypeMagicId
                ? GameConstants.BattleTemporaryMagic101ApCost : cost;
        }

        public bool TryTemporaryRest(short unitId)
{return false;}
        public bool CanApplyItemRecovery(short actorId, short targetId, int hp, int mp, int range, int apCost)
        {
            if(!TemporaryRulesEnabled || hp<0 || mp<0 || (hp==0 && mp==0) || range<0 || range>255
                || !world.TryGet(actorId,out var actor) || !world.TryGet(targetId,out var target)
                || !authority.CanAct(actor) || !target.Alive || actor.Enemy || target.Enemy
                || !actor.ActionPoints.CanStart(GetEffectiveActionCost(actorId, apCost)))return false;
            long distance=Math.Max(Math.Abs((long)actor.X-target.X),Math.Abs((long)actor.Y-target.Y));
            return distance<=range && ((hp>0 && target.Hp<target.MaxHp) || (mp>0 && target.Mp<target.MaxMp));
        }
        public bool TryApplyItemRecovery(short actorId, short targetId, int hp, int mp, int range, int apCost)
{return false;}
        public bool CanApplyItemMagic(short actorId, short targetId, int magicId, int range, int apCost)
        {
            if (!TemporaryRulesEnabled || range < 0 || range > 255 || !world.TryGet(actorId, out var actor)
                || actor.Enemy || !world.TryGet(targetId, out var target)
                || !authority.TryValidateItemRequest(world, new CombatRequest(actorId, targetId, CombatActionType.Magic, magicId), out _)
                || (authority.IsTemporarySupportMagic(magicId) ? actor.Enemy != target.Enemy : actor.Enemy == target.Enemy)
                || !actor.ActionPoints.CanStart(GetEffectiveActionCost(actorId, apCost))) return false;
            return Math.Max(Math.Abs((long)actor.X-target.X), Math.Abs((long)actor.Y-target.Y)) <= range;
        }
        public bool TryApplyItemMagic(short actorId, short targetId, int magicId, int range, int apCost)
{return false;}
        public bool ConfigureAttackApCostForJob(short unitId, int jobId)
        {
            var cost = GameConstants.GetBattleAttackApCost(jobId);
            if (jobId < 0 || !world.TryGet(unitId, out var unit) || cost < 0 || cost > unit.ActionPoints.Maximum) return false;
            // Explicit zero disables the action; never inherit another unit/job's cost.
            attackApCosts[unitId] = cost;
            return true;
        }

        public bool ConfigureAttackApCost(short unitId, int cost)
        {
            if (!world.TryGet(unitId, out var unit) || !IsValidCost(cost) || cost > unit.ActionPoints.Maximum) return false;
            attackApCosts[unitId] = cost; return true;
        }

        public bool ConfigureMagicApCost(int magicId, int cost)
        {
            if (magicId <= 0 || !IsValidCost(cost)) return false;
            magicApCosts[magicId] = cost; return true;
        }

        private static bool IsValidCost(int cost) => cost > 0 && cost <= GameConstants.BattleHighestMaximumAp;

        public bool ConfigureActionPointCosts(int attack, int magic)
        {
            if (!IsValidCost(attack) || !IsValidCost(magic)) return false;
            foreach (var unit in world.Units) if (attack > unit.ActionPoints.Maximum) return false;
            // Compatibility for explicit encounter/fixture setup; class-specific setup uses ConfigureAttackApCost.
            foreach (var unit in world.Units) attackApCosts[unit.Id] = attack;
            magicApCosts[CombatServerBoundary.PrototypeMagicId] = magic; return true;
        }

        public CombatFlow(CombatWorld world, CombatServerBoundary authority, bool useActionPoints = false)
        {
            this.world = world;
            this.authority = authority;
            this.useActionPoints = useActionPoints;
        }

        public bool AdvanceActionPoints(float seconds)
{return false;}
        public event Action<CombatRequest> ActionStarted;
        // Presentation only: additional targets are not additional AP/MP-spending actions.
        public event Action<CombatRequest> AdditionalMagicTargetStarted;
        public event Action<CombatResult> ResultApplied;
        // A value display, not evidence of attack, critical hit or death.
        public event Action<short, int, bool> VitalsDisplayChanged;
        private void NotifyVitals(short unitId, int before, int after, bool mp)
        {
            if (before != after) VitalsDisplayChanged?.Invoke(unitId, after - before, mp);
        }
        public event Action<CombatExperienceResult> ExperienceApplied;

        // Original SetExp applies an authoritative uint32 value, not an XP delta.
        // Leveling and reward distribution are separate, unimplemented rules.
        public bool TryApplyExperience(CombatExperienceResult result)
        {
            if (!world.TryGet(result.UnitId, out var unit)) return false;
            unit.Experience = result.AbsoluteExperience;
            ExperienceApplied?.Invoke(result);
            return true;
        }

        // Native bounds policy, not a recovered server formula. HP assignment does not
        // confirm death or resurrect a corpse; those are separate protocol operations.
        public bool TryApplyHp(short unitId, int absoluteHp)
        {
            if (!world.TryGet(unitId, out var unit) || absoluteHp < 0 || absoluteHp > unit.MaxHp
                || (unit.LifeState != UnitLifeState.Active && absoluteHp != 0)) return false;
            var before = unit.Hp; unit.Hp = absoluteHp;
            NotifyVitals(unitId, before, unit.Hp, false);
            return true;
        }

        public bool TryApplyMp(short unitId, int absoluteMp)
        {
            if (!world.TryGet(unitId, out var unit) || absoluteMp < 0 || absoluteMp > unit.MaxMp) return false;
            var before = unit.Mp; unit.Mp = absoluteMp;
            NotifyVitals(unitId, before, unit.Mp, true);
            return true;
        }

        // Explicit death notification, not inferred damage/reward. Keep the record so
        // objective instance IDs remain verifiable after removal. Removed is terminal
        // until a separate spawn operation exists (native ordering policy).
        public bool TryApplyDeath(short unitId, bool leaveCorpse, bool preserveReceivedHp = false)
        {
            if (!world.TryGet(unitId, out var unit)) return false;
            if (leaveCorpse && unit.LifeState == UnitLifeState.Removed) return false;
            unit.LifeState = leaveCorpse ? UnitLifeState.Corpse : UnitLifeState.Removed;
            // The original removal/corpse handlers do not supply HP. Native clients
            // preserve SetHp ordering; only legacy fixtures normalize death to HP0.
            if (!preserveReceivedHp) unit.Hp = 0;
            authority.RemoveTemporaryBuffs(unitId);
            return true;
        }

        public CombatResult Execute(CombatRequest request)
        {
            if (TryExecute(request, out var result, out var reason)) return result;
            throw new InvalidOperationException(reason);
        }

        public bool CanStartAction(CombatRequest request, out string reason)
        {
            if (!authority.TryValidateRequest(world, request, out reason)) return false;
            if (useActionPoints)
            {
                var cost = request.Action == CombatActionType.Attack
                    ? GetAttackApCost(request.ActorId) : GetEffectiveActionCost(request.ActorId, GetMagicApCost(request.MagicId));
                if (cost <= 0 || cost > world.Get(request.ActorId).ActionPoints.Maximum)
                { reason = "행동별 AP 비용이 아직 설정되지 않았습니다."; return false; }
                if (!world.Get(request.ActorId).ActionPoints.CanStart(cost))
                { reason = "AP가 최대치에 도달해야 행동할 수 있습니다."; return false; }
            }
            return true;
        }

        // Client-side request start only. Does not call Resolve or apply a result.
        public bool TryStartAction(CombatRequest request, out string reason)
        {
            if (!CanStartAction(request, out reason)) return false;
            if (useActionPoints)
            {
                var cost = request.Action == CombatActionType.Attack
                    ? GetAttackApCost(request.ActorId) : GetEffectiveActionCost(request.ActorId, GetMagicApCost(request.MagicId));
                if (!world.Get(request.ActorId).ActionPoints.TrySpend(cost)) return false;
            }
            ActionStarted?.Invoke(request); // 원작 AttackChar/Magic 선행 연출 위치
            return true;
        }

        // Packet-oriented magic boundary: do not collapse a position/multi-target request
        // into the prototype's single target, ID101 or assumed MP cost10.
        public bool CanStartMagicRequest(int magicId, short casterId, IReadOnlyList<short> targets, out string reason)
        {
            reason = string.Empty;
            if (magicId <= 0 || targets == null) { reason = "마법 번호 또는 대상 목록이 없습니다."; return false; }
            if (!world.TryGet(casterId, out var caster) || !caster.Alive)
            { reason = "시전자가 없거나 전투 불능 상태입니다."; return false; }
            if (!authority.CanCast(caster)) { reason = "수면 또는 뮤트 상태에서는 시전할 수 없습니다."; return false; }
            foreach (var id in targets)
                if (!world.TryGet(id, out var target) || !target.Alive)
                { reason = "대상이 없거나 전투 불능 상태입니다."; return false; }
            if (useActionPoints)
            {
                var cost = GetEffectiveActionCost(casterId, GetMagicApCost(magicId));
                if (!IsValidCost(cost)) { reason = "마법별 AP 비용이 아직 설정되지 않았습니다."; return false; }
                if (!caster.ActionPoints.CanStart(cost)) { reason = "AP가 최대치에 도달해야 행동할 수 있습니다."; return false; }
            }
            return true;
        }

        public bool TryStartMagicRequest(int magicId, short casterId, IReadOnlyList<short> targets, out string reason)
        {
            if (!CanStartMagicRequest(magicId, casterId, targets, out reason)) return false;
            if (useActionPoints && !world.Get(casterId).ActionPoints.TrySpend(GetEffectiveActionCost(casterId, GetMagicApCost(magicId)))) return false;
            // No fabricated CombatRequest target, MP deduction or local result here.
            return true;
        }

        public bool TryExecute(CombatRequest request, out CombatResult result, out string reason)
{result=default;reason="원작 서버 판정이 필요합니다.";return false;}
        // Reuse only explicitly configured temporary effects. Validate the whole list before
        // spending, resolve from one snapshot, and charge the caster once (duplicates are not hits).
        public bool TryExecuteMagicTargets(int magicId, short casterId, IReadOnlyList<short> targets, out string reason)
{reason="원작 서버 판정이 필요합니다.";return false;}
        private void Apply(CombatResult result)
        {
            var previous = ApplyState(result);
            PublishResult(result, previous);
        }

        private (int Hp, int Mp,int ActorHp,int TargetMp) ApplyState(CombatResult result)
        {
            var actor = world.Get(result.ActorId);
            var target = world.Get(result.TargetId);
            var previousMp = actor.Mp; var previousHp = target.Hp;var previousActorHp=actor.Hp;var previousTargetMp=target.Mp;
            actor.Mp = result.AbsoluteMp;
            target.Hp = result.AbsoluteHp;
            if(result.ActorHp>=0)actor.Hp=Math.Min(actor.MaxHp,result.ActorHp);
            if(result.TargetMp>=0)target.Mp=Math.Min(target.MaxMp,result.TargetMp);
            if(result.Revive && target.Hp>0){target.LifeState=UnitLifeState.Active;authority.RemoveTemporaryBuffs(target.Id);}
            authority.ApplyTemporaryBuff(result);
            if (target.Hp == 0)
            {
                target.LifeState = result.LeaveCorpse ? UnitLifeState.Corpse : UnitLifeState.Removed;
                authority.RemoveTemporaryBuffs(target.Id);
            }
            return (previousHp, previousMp,previousActorHp,previousTargetMp);
        }

        private void PublishResult(CombatResult result, (int Hp, int Mp,int ActorHp,int TargetMp) previous)
        {
            NotifyVitals(result.TargetId, previous.Hp, result.AbsoluteHp, false);
            NotifyVitals(result.ActorId, previous.Mp, world.Get(result.ActorId).Mp, true);
            if(result.ActorHp>=0 && result.ActorId!=result.TargetId)NotifyVitals(result.ActorId,previous.ActorHp,result.ActorHp,false);
            if(result.TargetMp>=0 && result.TargetId!=result.ActorId)NotifyVitals(result.TargetId,previous.TargetMp,result.TargetMp,true);
            ResultApplied?.Invoke(result);
        }
    }
}
