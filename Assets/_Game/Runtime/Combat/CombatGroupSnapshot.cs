using System.Collections.Generic;

namespace DarkSaver.Combat
{
    // Value snapshots: observation never exposes mutable CombatUnit or AP objects.
    public readonly struct CombatUnitSnapshot
    {
        internal CombatUnitSnapshot(short id, CombatUnit unit)
        {
            Id = id; JobId = unit.JobId; Name = unit.Name; Enemy = unit.Enemy;
            X = unit.X; Y = unit.Y; Hp = unit.Hp; MaxHp = unit.MaxHp;
            Mp = unit.Mp; MaxMp = unit.MaxMp; Ap = unit.ActionPoints.Current; MaxAp = unit.ActionPoints.Maximum;
            Experience = unit.Experience; LifeState = unit.LifeState;
            Attack = unit.Attack; Defense = unit.Defense;
            HasMagicAttack=unit.HasMagicAttack;HasMagicDefense=unit.HasMagicDefense;MagicAttack=unit.MagicAttack;MagicDefense=unit.MagicDefense;
        }
        public short Id { get; }
        public int JobId { get; }
        public string Name { get; }
        public bool Enemy { get; }
        public int X { get; }
        public int Y { get; }
        public int Hp { get; }
        public int MaxHp { get; }
        public int Mp { get; }
        public int MaxMp { get; }
        public int Ap { get; }
        public int MaxAp { get; }
        public uint Experience { get; }
        public int Attack { get; }
        public int Defense { get; }
        public bool HasMagicAttack { get; }
        public bool HasMagicDefense { get; }
        public bool HasMagicStats => HasMagicAttack && HasMagicDefense;
        public int MagicAttack { get; }
        public int MagicDefense { get; }
        public UnitLifeState LifeState { get; }
        public bool Alive => LifeState == UnitLifeState.Active && Hp > 0;
    }

    public sealed class CombatGroupSnapshot
    {
        internal CombatGroupSnapshot(short commanderId, bool enemy, List<CombatUnitSnapshot> members)
        {
            CommanderId = commanderId; Enemy = enemy; Members = members.AsReadOnly();
            foreach (var member in members)
            {
                if (member.Id == commanderId) { CommanderPresent = true; CommanderAlive = member.Alive; }
                else SoldierCount++;
                if (member.Alive) AliveCount++;
            }
        }
        public short CommanderId { get; }
        public bool Enemy { get; }
        public IReadOnlyList<CombatUnitSnapshot> Members { get; }
        public bool CommanderPresent { get; }
        public bool CommanderAlive { get; }
        public int SoldierCount { get; }
        public int AliveCount { get; }
    }
}
