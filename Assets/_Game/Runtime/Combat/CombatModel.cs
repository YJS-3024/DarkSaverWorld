using System;
using System.Collections.Generic;

namespace DarkSaver.Combat
{
    public enum UnitLifeState { Active, Corpse, Removed }
    public enum CombatActionType { Attack, Skill, Magic }

    [Serializable]
    public sealed class CombatUnit
    {
        public short Id;
        public int JobId;
        public string Name;
        public bool Enemy;
        public int X;
        public int Y;
        public int Hp;
        public int MaxHp;
        public int Mp;
        public int MaxMp;
        public int Attack;
        public int Defense;
        public bool HasMagicAttack,HasMagicDefense;
        public bool HasMagicStats { get=>HasMagicAttack && HasMagicDefense; set {HasMagicAttack=value;HasMagicDefense=value;} }
        public int MagicAttack, MagicDefense;
        public uint Experience;
        public CombatUnit() { ActionPoints = new CombatActionPoints(() => GameConstants.GetBattleMaximumAp(Enemy)); }
        public CombatActionPoints ActionPoints { get; }
        public UnitLifeState LifeState = UnitLifeState.Active;
        public bool Alive => LifeState == UnitLifeState.Active && Hp > 0;
    }

    public readonly struct CombatExperienceResult
    {
        public CombatExperienceResult(short unitId, uint absoluteExperience)
        {
            UnitId = unitId;
            AbsoluteExperience = absoluteExperience;
        }

        public short UnitId { get; }
        public uint AbsoluteExperience { get; }
    }

    public readonly struct CombatRequest
    {
        public CombatRequest(short actorId, short targetId, CombatActionType action, int magicId = 0)
        {
            ActorId = actorId;
            TargetId = targetId;
            Action = action;
            MagicId = magicId;
        }

        public short ActorId { get; }
        public short TargetId { get; }
        public CombatActionType Action { get; }
        public int MagicId { get; }
    }

    public readonly struct CombatResult
    {
        public CombatResult(short actorId, short targetId, int absoluteHp, int absoluteMp,
            int damage, bool leaveCorpse, bool hit = true, int healing = 0, int attackBonus = 0, int defenseBonus = 0, float buffSeconds = 0, int actionCostReduction = 0, TemporaryStatusEffect statusEffect = TemporaryStatusEffect.None, int statusPower = 0, float statusSeconds = 0,int actorHp=-1,int targetMp=-1,bool revive=false)
        {
            ActorId = actorId;
            TargetId = targetId;
            AbsoluteHp = absoluteHp;
            AbsoluteMp = absoluteMp;
            Damage = damage;
            LeaveCorpse = leaveCorpse;
            Hit = hit;
            Healing = healing;
            AttackBonus = attackBonus; DefenseBonus = defenseBonus; BuffSeconds = buffSeconds; ActionCostReduction = actionCostReduction;
            StatusEffect = statusEffect; StatusPower = statusPower; StatusSeconds = statusSeconds;ActorHp=actorHp;TargetMp=targetMp;Revive=revive;
        }

        public short ActorId { get; }
        public short TargetId { get; }
        public int AbsoluteHp { get; }
        public int AbsoluteMp { get; }
        public int Damage { get; }
        public bool LeaveCorpse { get; }
        public bool Hit { get; }
        public int Healing { get; }
        public int AttackBonus { get; }
        public int DefenseBonus { get; }
        public float BuffSeconds { get; }
        public int ActionCostReduction { get; }
        public TemporaryStatusEffect StatusEffect { get; }
        public int StatusPower { get; }
        public float StatusSeconds { get; }
        public int ActorHp {get;} public int TargetMp {get;} public bool Revive {get;}
        public CombatResult WithCasterTotals(int hp,int mp)=>new CombatResult(ActorId,TargetId,AbsoluteHp,mp,Damage,LeaveCorpse,Hit,Healing,AttackBonus,DefenseBonus,BuffSeconds,ActionCostReduction,StatusEffect,StatusPower,StatusSeconds,hp,TargetId==ActorId && TargetMp>=0?mp:TargetMp,Revive);
    }

    public sealed class CombatWorld
    {
        private readonly Dictionary<short, CombatUnit> units = new();
        public IEnumerable<CombatUnit> Units => units.Values;
        public void Add(CombatUnit unit) => units.Add(unit.Id, unit);
        public bool Remove(short id) => units.Remove(id);
        public CombatUnit Get(short id) => units[id];
        public bool TryGet(short id, out CombatUnit unit) => units.TryGetValue(id, out unit);

        // Native registration: all-or-nothing, with an existing or incoming commander.
        // The source ID convention permits nine soldiers; player formation limits are separate.
        public bool TryRegisterGroup(IReadOnlyList<CombatUnit> incoming)
        {
            if (incoming == null || incoming.Count == 0 || incoming.Count > 10) return false;
            var pending = new Dictionary<short, CombatUnit>();
            var first = incoming[0];
            if (first == null || first.Id < 0) return false;
            var commanderId = (short)(first.Id / 10 * 10);
            foreach (var unit in incoming)
            {
                if (unit == null || unit.Id < 0 || unit.Id / 10 * 10 != commanderId
                    || unit.Enemy != first.Enemy || units.ContainsKey(unit.Id) || pending.ContainsKey(unit.Id)
                    || ((unit.HasMagicAttack && unit.MagicAttack<0) || (unit.HasMagicDefense && unit.MagicDefense<0)) || unit.JobId < 0 || unit.LifeState != UnitLifeState.Active
                    || unit.Hp <= 0 || unit.MaxHp < unit.Hp || unit.Mp < 0 || unit.MaxMp < unit.Mp)
                    return false;
                // Placement input is copied; callers cannot subsequently rewrite authoritative state.
                var copied = new CombatUnit {
                    Id = unit.Id, JobId = unit.JobId, Name = unit.Name, Enemy = unit.Enemy,
                    X = unit.X, Y = unit.Y, Hp = unit.Hp, MaxHp = unit.MaxHp,
                    Mp = unit.Mp, MaxMp = unit.MaxMp, Attack = unit.Attack, Defense = unit.Defense,
                    HasMagicAttack=unit.HasMagicAttack,HasMagicDefense=unit.HasMagicDefense,MagicAttack=unit.MagicAttack,MagicDefense=unit.MagicDefense,
                    Experience = unit.Experience
                };
                if (unit.ActionPoints.HasConfirmedMaximum) copied.ActionPoints.ConfigureMaximum(unit.ActionPoints.Maximum);
                copied.ActionPoints.ApplyAbsolute(unit.ActionPoints.Current);
                pending.Add(unit.Id, copied);
            }
            if (!pending.ContainsKey(commanderId) && !units.ContainsKey(commanderId)) return false;
            foreach (var pair in units)
                if (pair.Key >= 0 && pair.Key / 10 * 10 == commanderId && pair.Value.Enemy != first.Enemy)
                    return false;
            foreach (var pair in pending) units.Add(pair.Key, pair.Value);
            return true;
        }

        // Stage every group before publishing any additions. Existing units/AP
        // stay untouched on failure; generic enemy registration remains separate.
        public bool TryRegisterAlliedFormation(IReadOnlyList<CombatUnit> incoming)
        {
            if (incoming == null || incoming.Count == 0 || incoming.Count >
                GameConstants.BattleMaximumAlliedGroups * (GameConstants.BattleMaximumAlliedSoldiersPerGroup + 1)) return false;
            var staged = new CombatWorld(); foreach (var existing in units) staged.units.Add(existing.Key, existing.Value);
            var pendingIds = new HashSet<short>(); var groups = new SortedDictionary<short, List<CombatUnit>>();
            foreach (var unit in incoming)
            {
                if (unit == null || unit.Enemy || unit.Id < 0 || unit.JobId <= 0 || string.IsNullOrWhiteSpace(unit.Name) ||
                    unit.Attack < 0 || unit.Defense < 0 || !pendingIds.Add(unit.Id) || units.ContainsKey(unit.Id)) return false;
                var commander = (short)(unit.Id / 10 * 10);
                if (!groups.TryGetValue(commander, out var group)) { group = new List<CombatUnit>(); groups.Add(commander, group); }
                group.Add(unit);
            }
            foreach (var group in groups.Values) if (!staged.TryRegisterGroup(group)) return false;
            var alliedGroups = new Dictionary<short, int>();
            foreach (var unit in staged.Units)
            {
                if (unit.Enemy) continue;
                var commander = (short)(unit.Id / 10 * 10); alliedGroups.TryGetValue(commander, out var soldiers);
                alliedGroups[commander] = soldiers + (unit.Id == commander ? 0 : 1);
            }
            if (alliedGroups.Count > GameConstants.BattleMaximumAlliedGroups) return false;
            foreach (var count in alliedGroups.Values) if (count > GameConstants.BattleMaximumAlliedSoldiersPerGroup) return false;
            foreach (var id in pendingIds) units.Add(id, staged.units[id]);
            return true;
        }

        public bool TryGetSnapshot(short id, out CombatUnitSnapshot snapshot)
        {
            snapshot = default;
            if (!units.TryGetValue(id, out var unit)) return false;
            snapshot = new CombatUnitSnapshot(id, unit);
            return true;
        }

        public bool TryGetGroup(short memberId, out CombatGroupSnapshot group)
        {
            group = null;
            if (memberId < 0 || !units.TryGetValue(memberId, out var member)) return false;
            var commanderId = (short)(memberId / 10 * 10);
            var members = new List<CombatUnitSnapshot>();
            foreach (var pair in units)
            {
                if (pair.Key < 0 || pair.Key / 10 * 10 != commanderId) continue;
                if (pair.Value.Enemy != member.Enemy) return false;
                members.Add(new CombatUnitSnapshot(pair.Key, pair.Value));
            }
            members.Sort((left, right) => left.Id.CompareTo(right.Id));
            group = new CombatGroupSnapshot(commanderId, member.Enemy, members);
            return true;
        }
    }
}
