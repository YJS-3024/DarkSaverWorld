using System;

namespace DarkSaver.Combat
{
    // 원작 서버가 담당했던 미확정 공식을 싱글게임에서 대체하는 경계다.
    public sealed partial class LocalCombatAuthority
    {
        // Existing demo spell, not a reconstructed original magic table or MP formula.
        public const int PrototypeMagicId = 101;
        public const int PrototypeMagicMpCost = GameConstants.BattleTemporaryMagic101MpCost;

        private readonly System.Collections.Generic.Dictionary<short, (double Accuracy, double Evasion)> physicalRates = new();
        private readonly Func<double, double, double> physicalHitRule;
        private readonly Func<double> randomSample;
        private readonly System.Collections.Generic.Dictionary<int, (int Mp, int Range, int Bonus, int Divisor, int Healing)> magicRules = new();
        private readonly System.Collections.Generic.Dictionary<int, (int Mp, int Range, int Attack, int Defense, float Seconds)> buffRules = new();
        private readonly System.Collections.Generic.Dictionary<(short Id, bool Attack), (int Amount, double Remaining)> buffs = new();
        private bool temporaryRulesEnabled;
        public bool TemporaryRulesEnabled { get => temporaryRulesEnabled; set { temporaryRulesEnabled = value; if (!value) { buffs.Clear(); quickBuffs.Clear(); statuses.Clear(); } } }

        public bool ConfigureTemporaryBuffMagic(int id, int mpCost, int range, int attack, int defense, float seconds)
        {
            if (id <= 0 || id > short.MaxValue || mpCost < 0 || range < 0 || attack < 0 || defense < 0
                || (attack == 0 && defense == 0) || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return false;
            buffRules[id] = (mpCost, range, attack, defense, seconds); magicRules.Remove(id); quickRules.Remove(id); statusRules.Remove(id); return true;
        }
        public bool IsTemporarySupportMagic(int id) => IsTemporaryHealingMagic(id)
            || TemporaryRulesEnabled && (buffRules.ContainsKey(id) || quickRules.ContainsKey(id) || IsSupportStatusMagic(id));
        public int EffectiveAttack(CombatUnit unit) => EffectiveStat(unit, true);
        public int EffectiveDefense(CombatUnit unit) => EffectiveStat(unit, false);
        public bool TryGetTemporaryBuff(CombatUnit unit, bool attack, out int bonus, out double seconds)
        {
            bonus = 0; seconds = 0;
            if (!TemporaryRulesEnabled || unit == null || !unit.Alive || !buffs.TryGetValue((unit.Id, attack), out var buff)
                || buff.Remaining <= 0) return false;
            bonus = buff.Amount; seconds = buff.Remaining; return true;
        }
        private int EffectiveStat(CombatUnit unit, bool attack)
        {
            long value = attack ? unit.Attack : unit.Defense;
            if (TemporaryRulesEnabled && unit.Alive && buffs.TryGetValue((unit.Id, attack), out var buff)) value += buff.Amount;
            return (int)Math.Min(int.MaxValue, Math.Max(0, value));
        }
        public void ApplyTemporaryBuff(CombatResult result)
        {
            ApplyTemporaryStatus(result);
            if (!TemporaryRulesEnabled || result.BuffSeconds <= 0) return;
            if (result.ActionCostReduction > 0) quickBuffs[result.TargetId] = (result.ActionCostReduction, result.BuffSeconds);
            if (result.AttackBonus > 0) buffs[(result.TargetId, true)] = (result.AttackBonus, result.BuffSeconds);
            if (result.DefenseBonus > 0) buffs[(result.TargetId, false)] = (result.DefenseBonus, result.BuffSeconds);
        }
        public void RemoveTemporaryBuffs(short id) { buffs.Remove((id, true)); buffs.Remove((id, false)); quickBuffs.Remove(id); RemoveStatuses(id); }
        public void AdvanceTemporaryBuffs(CombatWorld world, float seconds)
        {
            AdvanceQuick(world, seconds);
            foreach (var key in new System.Collections.Generic.List<(short Id, bool Attack)>(buffs.Keys))
            {
                var buff = buffs[key]; double left = buff.Remaining - seconds;
                if (left <= 0 || !world.TryGet(key.Id, out var unit) || !unit.Alive) buffs.Remove(key);
                else buffs[key] = (buff.Amount, left);
            }
        }

        // Explicit temporary single-enemy damage rules only; this does not grant a learned skill.
        public bool ConfigureTemporaryDamageMagic(int id, int mpCost, int range, int powerBonus, int defenseDivisor)
        {
            if (id <= 0 || id > short.MaxValue || mpCost < 0 || range < 0 || powerBonus < 0 || defenseDivisor <= 0) return false;
            magicRules[id] = (mpCost, range, powerBonus, defenseDivisor, 0); buffRules.Remove(id); quickRules.Remove(id); statusRules.Remove(id); return true;
        }

        public bool ConfigureTemporaryHealingMagic(int id, int mpCost, int range, int healing)
        {
            if (id <= 0 || id > short.MaxValue || mpCost < 0 || range < 0 || healing <= 0) return false;
            magicRules[id] = (mpCost, range, 0, 1, healing); buffRules.Remove(id); quickRules.Remove(id); statusRules.Remove(id); return true;
        }

        public bool IsTemporaryHealingMagic(int id) => TemporaryRulesEnabled
            && TryGetMagicRule(id, out var rule) && rule.Healing > 0;

        public bool TryGetTemporaryMagicRange(int id, out int range)
        {
            range = 0;
            if (!TemporaryRulesEnabled || !TryGetMagicRule(id, out var rule)) return false;
            range = rule.Range; return true;
        }

        public bool TryGetTemporaryMagicMpCost(int id, out int cost)
        {
            cost = 0;
            if (!TemporaryRulesEnabled || !TryGetMagicRule(id, out var rule)) return false;
            cost = rule.Mp; return true;
        }

        private bool TryGetMagicRule(int id, out (int Mp, int Range, int Bonus, int Divisor, int Healing) rule)
        {
            if (TemporaryRulesEnabled && magicRules.TryGetValue(id, out rule)) return true;
            if (TemporaryRulesEnabled && buffRules.TryGetValue(id, out var buff))
            { rule = (buff.Mp, buff.Range, 0, 1, 0); return true; }
            if (TemporaryRulesEnabled && quickRules.TryGetValue(id, out var quick))
            { rule = (quick.Mp, quick.Range, 0, 1, 0); return true; }
            if (TemporaryRulesEnabled && statusRules.TryGetValue(id, out var status))
            { rule = (status.Mp, status.Range, 0, 1, 0); return true; }
            if (TemporaryRulesEnabled && id == 2401)
            {
                rule = (GameConstants.BattleTemporaryHeal2401MpCost, GameConstants.BattleTemporaryHeal2401Range,
                    0, 1, GameConstants.BattleTemporaryHeal2401Amount);
                return true;
            }
            rule = (PrototypeMagicMpCost, GameConstants.BattleTemporaryMagic101Range,
                GameConstants.BattleTemporaryMagicPowerBonus, Math.Max(1, GameConstants.BattleTemporaryMagicDefenseDivisor), 0);
            return id == PrototypeMagicId;
        }

        public LocalCombatAuthority(Func<double, double, double> hitRule = null, Func<double> sample = null)
        {
            ConfigureTemporaryBuffMagic(2101, GameConstants.BattleTemporaryBuffMpCost, GameConstants.BattleTemporaryBuffRange,
                GameConstants.BattleTemporaryBuffAmount, 0, GameConstants.BattleTemporaryBuffSeconds);
            ConfigureTemporaryBuffMagic(2001, GameConstants.BattleTemporaryBuffMpCost, GameConstants.BattleTemporaryBuffRange,
                0, GameConstants.BattleTemporaryBuffAmount, GameConstants.BattleTemporaryBuffSeconds);
            physicalHitRule = hitRule ?? TemporaryCombatHitRules.PhysicalHitProbability;
            var random = new Random();
            randomSample = sample ?? random.NextDouble;
        }

        // Explicit resolved percentages only: do not invent defaults or reinterpret
        // raw job stats, equipment modifiers or magical evasion as physical rates.
        public bool ConfigurePhysicalAccuracy(short unitId, double accuracyPercent, double evasionPercent)
        {
            if (unitId < 0 || !TemporaryCombatHitRules.IsValidPercent(accuracyPercent)
                || !TemporaryCombatHitRules.IsValidPercent(evasionPercent)) return false;
            physicalRates[unitId] = (accuracyPercent, evasionPercent); return true;
        }

        public bool TryValidateRequest(CombatWorld world, CombatRequest request, out string reason) =>
            TryValidateRequest(world, request, false, out reason);
        internal bool TryValidateItemRequest(CombatWorld world, CombatRequest request, out string reason) =>
            TryValidateRequest(world, request, true, out reason);
        private bool TryValidateRequest(CombatWorld world, CombatRequest request, bool item, out string reason)
        {
            reason = string.Empty;
            if (world == null) reason = "전투 상태가 없습니다.";
            else if (request.Action != CombatActionType.Attack && request.Action != CombatActionType.Magic)
                reason = "아직 지원하지 않는 행동입니다.";
            else if ((request.Action == CombatActionType.Magic && !TryGetMagicRule(request.MagicId, out _))
                || (request.Action == CombatActionType.Attack && request.MagicId != 0))
                reason = "현재 시연에서 지원하지 않는 마법 요청입니다.";
            else if (!world.TryGet(request.ActorId, out var actor) || !world.TryGet(request.TargetId, out var target))
                reason = "행동자 또는 대상이 없습니다.";
            else if (request.ActorId == request.TargetId && !(request.Action == CombatActionType.Magic && IsTemporarySupportMagic(request.MagicId)))
                reason = "현재 공격 시연은 자신을 대상으로 할 수 없습니다.";
            else if (!actor.Alive || !target.Alive)
                reason = "전투 불능 개체는 공격에 참여할 수 없습니다.";
            else if (!CanAct(actor)) reason = "수면 중에는 행동할 수 없습니다.";
            else if (request.Action == CombatActionType.Magic && !item && !CanCast(actor)) reason = "뮤트 상태에서는 마법을 시전할 수 없습니다.";
            else if (request.Action == CombatActionType.Magic && IsTemporarySupportMagic(request.MagicId) && actor.Enemy != target.Enemy)
                reason = "생존 아군을 지정해야 합니다.";
            else if (request.Action == CombatActionType.Magic && IsTemporaryHealingMagic(request.MagicId)
                && (actor.Enemy != target.Enemy || target.Hp >= target.MaxHp))
                reason = "HP가 부족한 생존 아군을 지정해야 합니다.";
            else if (request.Action == CombatActionType.Magic && TemporaryRulesEnabled
                && statusRules.TryGetValue(request.MagicId, out var hostileStatus) && !IsSupportStatusMagic(request.MagicId)
                && actor.Enemy == target.Enemy)
                reason = "상태 이상을 적용할 적을 지정해야 합니다.";
            else if (request.Action == CombatActionType.Magic && TryGetMagicRule(request.MagicId, out var rule) && !item && actor.Mp < rule.Mp)
                reason = "MP가 부족합니다.";
            else if (request.Action == CombatActionType.Magic && TemporaryRulesEnabled
                && statusRules.TryGetValue(request.MagicId, out var status)
                && ((status.Effect == TemporaryStatusEffect.CurePoison && !TryGetTemporaryStatus(target, TemporaryStatusEffect.Poison, out _, out _))
                    || (status.Effect == TemporaryStatusEffect.Wake && !TryGetTemporaryStatus(target, TemporaryStatusEffect.Sleep, out _, out _))))
                reason = "해제할 상태가 없습니다.";
            else if (request.Action == CombatActionType.Attack && !TemporaryRulesEnabled && physicalRates.Count > 0
                && (!physicalRates.ContainsKey(actor.Id) || !physicalRates.ContainsKey(target.Id)))
                reason = "행동자와 대상의 명중·회피 확률이 모두 필요합니다.";
            return reason.Length == 0;
        }

        public CombatResult Resolve(CombatWorld world, CombatRequest request) => Resolve(world, request, false);
        internal CombatResult ResolveItem(CombatWorld world, CombatRequest request) => Resolve(world, request, true);
        private CombatResult Resolve(CombatWorld world, CombatRequest request, bool item)
        {
            if (!TryValidateRequest(world, request, item, out var reason))
                throw new InvalidOperationException(reason);
            var actor = world.Get(request.ActorId);
            var target = world.Get(request.TargetId);
            var magic = request.Action == CombatActionType.Magic;
            TryGetMagicRule(request.MagicId, out var magicRule);
            var manaCost = magic && !item ? magicRule.Mp : 0;
            if (magic && TemporaryRulesEnabled && statusRules.TryGetValue(request.MagicId, out var statusRule))
                return new CombatResult(actor.Id, target.Id, target.Hp, actor.Mp - manaCost, 0, false,
                    statusEffect: statusRule.Effect, statusPower: statusRule.Power, statusSeconds: statusRule.Seconds);
            if (magic && TemporaryRulesEnabled && quickRules.TryGetValue(request.MagicId, out var quickRule))
                return new CombatResult(actor.Id, target.Id, target.Hp, actor.Mp - manaCost, 0, false,
                    buffSeconds: quickRule.Seconds, actionCostReduction: quickRule.Percent);
            if (magic && TemporaryRulesEnabled && buffRules.TryGetValue(request.MagicId, out var buffRule))
                return new CombatResult(actor.Id, target.Id, target.Hp, actor.Mp - manaCost, 0, false,
                    attackBonus: buffRule.Attack, defenseBonus: buffRule.Defense, buffSeconds: buffRule.Seconds);
            if (magic && magicRule.Healing > 0)
            {
                int hp = (int)Math.Min(target.MaxHp, (long)target.Hp + magicRule.Healing);
                return new CombatResult(actor.Id, target.Id, hp, actor.Mp - manaCost, 0, false,
                    healing: hp - target.Hp);
            }
            if (!magic && (physicalRates.Count > 0 || TemporaryRulesEnabled))
            {
                var accuracy = physicalRates.TryGetValue(actor.Id, out var actorRates)
                    ? actorRates.Accuracy : GameConstants.BattleTemporaryAccuracyPercent;
                var evasion = physicalRates.TryGetValue(target.Id, out var targetRates)
                    ? targetRates.Evasion : GameConstants.BattleTemporaryEvasionPercent;
                var chance = physicalHitRule(accuracy, evasion);
                if (double.IsNaN(chance) || double.IsInfinity(chance) || chance < 0 || chance > 1)
                    throw new InvalidOperationException("Invalid physical hit probability.");
                var sample = chance > 0 && chance < 1 ? randomSample() : 0;
                if (double.IsNaN(sample) || double.IsInfinity(sample) || sample < 0 || sample >= 1)
                    throw new InvalidOperationException("Random sample must be in [0,1).");
                if (chance <= 0 || sample >= chance)
                    return new CombatResult(actor.Id, target.Id, target.Hp, actor.Mp, 0, false, hit: false);
            }
            var damage = magic
                ? (int)Math.Min(int.MaxValue, Math.Max((long)Math.Max(1, GameConstants.BattleTemporaryMinimumDamage),
                    (long)(actor.HasMagicAttack ? actor.MagicAttack : EffectiveAttack(actor)) + magicRule.Bonus
                    - Math.Max(0, (target.HasMagicDefense ? target.MagicDefense : EffectiveDefense(target)) / magicRule.Divisor)))
                : TemporaryCombatDamageRules.Calculate(EffectiveAttack(actor), EffectiveDefense(target), false);
            return new CombatResult(actor.Id, target.Id, Math.Max(0, target.Hp - damage),
                actor.Mp - manaCost, damage, !TemporaryRulesEnabled);
        }
    }
}
