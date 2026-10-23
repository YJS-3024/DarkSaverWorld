using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class BattleFormationSnapshotTests
    {
        private static BattleFormationMemberDraft Member(short id, int x)
        {
            var commander = (short)(id / 10 * 10);
            return new BattleFormationMemberDraft {
                PersistentId = "owned-" + id, CommanderPersistentId = "owned-" + commander, CommanderId = commander,
                Unit = new CombatUnit { Id = id, JobId = 100, Name = "Explicit fixture", X = x, Y = 0,
                    Hp = 40, MaxHp = 50, Mp = 0, MaxMp = 10, Attack = 0, Defense = 0, Experience = uint.MaxValue },
                IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(x, 0), TeamId = 1,
                RecordEligible = true, LearnedMagicIds = Array.Empty<short>()
            };
        }
        private static bool Create(IReadOnlyList<BattleFormationMemberDraft> draft, out BattleFormationSnapshot snapshot)
            => BattleFormation.TryCreateSnapshot(draft, 30, 30, Array.Empty<short>(), out snapshot, out _);

        [Test]
        public void SnapshotOwnsOrderedValuesAndKnownZeroStatsWithoutChangingInputs()
        {
            var spells = new List<short> { 701 }; var soldier = Member(109, 2); soldier.LearnedMagicIds = spells;
            var input = new List<BattleFormationMemberDraft> { Member(200, 3), soldier, Member(100, 1) };
            Assert.That(Create(input, out var snapshot), Is.True); Assert.That(snapshot.GroupCount, Is.EqualTo(2));
            Assert.That(snapshot.Members[0].Unit.Id, Is.EqualTo(100)); Assert.That(snapshot.Members[1].Unit.Id, Is.EqualTo(109));
            soldier.Unit.Hp = 1; soldier.Unit.Name = "changed"; soldier.PersistentId = "changed"; spells.Clear(); input.Clear();
            var kept = snapshot.Members[1]; Assert.That(kept.Unit.Hp, Is.EqualTo(40));
            Assert.That(kept.Unit.Attack, Is.Zero); Assert.That(kept.Unit.Defense, Is.Zero); Assert.That(kept.Unit.Mp, Is.Zero);
            Assert.That(kept.Unit.Experience, Is.EqualTo(uint.MaxValue)); Assert.That(kept.PersistentId, Is.EqualTo("owned-109"));
            Assert.That(kept.LearnedMagicIds, Is.EqualTo(new short[] { 701 }));
            Assert.Throws<NotSupportedException>(() => ((IList<short>)kept.LearnedMagicIds).Add(702));
            Assert.Throws<NotSupportedException>(() => ((IList<BattleFormationMemberSnapshot>)snapshot.Members).Clear());
        }

        [Test]
        public void MissingOwnershipFinalStateOrNativeContextNeverProducesPartialSnapshot()
        {
            var changes = new Action<BattleFormationMemberDraft>[] {
                m => m.IsOwned = false, m => m.FinalStateConfirmed = false, m => m.Unit = null,
                m => m.Position = null, m => m.TeamId = null, m => m.RecordEligible = null,
                m => m.LearnedMagicIds = null, m => m.CommanderId = null, m => m.PersistentId = " " };
            foreach (var change in changes) {
                var bad = Member(101, 2); change(bad); var leader = Member(100, 1);
                Assert.That(Create(new[] { leader, bad }, out var snapshot), Is.False); Assert.That(snapshot, Is.Null);
                Assert.That(leader.Unit.Hp, Is.EqualTo(40)); Assert.That(leader.Unit.ActionPoints.Current, Is.EqualTo(6));
            }
            Assert.That(Create(null, out _), Is.False); Assert.That(Create(Array.Empty<BattleFormationMemberDraft>(), out _), Is.False);
        }

        [Test]
        public void DuplicateIdentityWrongCommanderAndReservedInstancesAreRejected()
        {
            var leader = Member(100, 1); var soldier = Member(101, 2);
            soldier.PersistentId = leader.PersistentId; Assert.That(Create(new[] { leader, soldier }, out _), Is.False);
            soldier = Member(101, 2); soldier.CommanderPersistentId = "owned-200";
            Assert.That(Create(new[] { leader, soldier }, out _), Is.False);
            soldier = Member(101, 2); soldier.CommanderId = 200;
            Assert.That(Create(new[] { leader, soldier }, out _), Is.False);
            Assert.That(Create(new[] { Member(101, 2) }, out _), Is.False);
            Assert.That(Create(new[] { leader, Member(100, 2) }, out _), Is.False);
            Assert.That(BattleFormation.TryCreateSnapshot(new[] { leader }, 30, 30, new short[] { 100 }, out var snapshot, out _), Is.False);
            Assert.That(snapshot, Is.Null);
        }

        [Test]
        public void GroupAndSoldierLimitsAreSeparateFromNativeNineSlotWidth()
        {
            var all = new List<BattleFormationMemberDraft>();
            for (short group = 100; group <= 120; group += 10)
                for (short slot = 0; slot <= 4; slot++) all.Add(Member((short)(group + slot), all.Count));
            Assert.That(Create(all, out var snapshot), Is.True); Assert.That(snapshot.Members.Count, Is.EqualTo(15));
            Assert.That(Create(new[] { Member(100, 0), Member(110, 1), Member(120, 2), Member(130, 3) }, out _), Is.False);
            var tooMany = new List<BattleFormationMemberDraft>();
            for (short slot = 0; slot <= 5; slot++) tooMany.Add(Member((short)(100 + slot), slot));
            Assert.That(Create(tooMany, out _), Is.False);
        }

        [Test]
        public void InvalidCoordinatesVitalsAndSpellsAreRejectedWithoutClamping()
        {
            var changes = new Action<BattleFormationMemberDraft>[] {
                m => { m.Position = new CombatGridCell(-1, 0); m.Unit.X = -1; },
                m => { m.Position = new CombatGridCell(30, 0); m.Unit.X = 30; },
                m => m.Unit.X = 5, m => m.Unit.Hp = 0, m => m.Unit.Hp = 51, m => m.Unit.Mp = -1,
                m => m.Unit.Mp = 11, m => m.Unit.Attack = -1, m => m.Unit.Defense = -1,
                m => m.Unit.LifeState = UnitLifeState.Corpse, m => m.Unit.Enemy = true, m => m.Unit.JobId = 0,
                m => m.LearnedMagicIds = new short[] { 701, 701 }, m => m.LearnedMagicIds = new short[] { 0 } };
            foreach (var change in changes) { var member = Member(100, 1); change(member); Assert.That(Create(new[] { member }, out _), Is.False); }
            Assert.That(Create(new[] { Member(100, 1), Member(101, 1) }, out _), Is.False);
            Assert.That(BattleFormation.TryCreateSnapshot(new[] { Member(100, 1) }, 0, 30, Array.Empty<short>(), out _, out _), Is.False);
            Assert.That(BattleFormation.TryCreateSnapshot(new[] { Member(100, 1) }, 30, 30, null, out _, out _), Is.False);
        }
    }
}
