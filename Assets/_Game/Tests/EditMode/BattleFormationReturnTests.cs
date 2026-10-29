using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class BattleFormationReturnTests
    {
        private static BattleFormationMemberDraft Member(short id, int x) => new()
        {
            PersistentId = "owned-" + id, CommanderPersistentId = "owned-" + (id / 10 * 10), CommanderId = (short)(id / 10 * 10),
            Unit = NativeCombatStateFixture.Received(new CombatUnit { Id = id, JobId = 100, Name = "fixture", X = x, Y = 0,
                Hp = 40, MaxHp = 50, Mp = 5, MaxMp = 10, Attack = 0, Defense = 0 }),
            IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(x, 0), TeamId = 1,
            RecordEligible = true, LearnedMagicIds = Array.Empty<short>()
        };
        private static BattleFormationSnapshot Deployment(params BattleFormationMemberDraft[] members)
        {
            Assert.That(BattleFormation.TryCreateSnapshot(members, 30, 30, Array.Empty<short>(), out var snapshot, out _), Is.True);
            return snapshot;
        }
        private static CombatUnitSnapshot Observe(CombatUnit unit)
        {
            var world = new CombatWorld(); world.Add(unit);
            Assert.That(world.TryGetSnapshot(unit.Id, out var observed), Is.True); return observed;
        }

        [Test]
        public void ReorderedResultsMapByDeploymentIdentityAndRetainRawValues()
        {
            var leader = Member(100, 0); var soldier = Member(109, 1); var other = Member(200, 2);
            var deployment = Deployment(other, soldier, leader);
            leader.Unit.Hp = 17; leader.Unit.Mp = 2; leader.Unit.Experience = uint.MaxValue;
            leader.Unit.X = 12; leader.Unit.Y = 13; leader.Unit.ActionPoints.ApplyAbsolute(20); leader.Unit.ActionPoints.TrySpend(4);
            soldier.Unit.Hp = 0; soldier.Unit.LifeState = UnitLifeState.Removed;
            var observations = new List<CombatUnitSnapshot> { Observe(other.Unit), Observe(soldier.Unit), Observe(leader.Unit) };
            Assert.That(BattleFormationReturn.TryCapture(deployment, observations, out var returned, out _), Is.True);
            Assert.That(returned.Members[0].PersistentId, Is.EqualTo("owned-100"));
            Assert.That(returned.Members[1].CommanderPersistentId, Is.EqualTo("owned-100"));
            Assert.That(returned.Members[1].CommanderId, Is.EqualTo(100));
            var unit = returned.Members[0].Unit;
            Assert.That(unit.Hp, Is.EqualTo(17)); Assert.That(unit.Mp, Is.EqualTo(2)); Assert.That(unit.Ap, Is.EqualTo(GameConstants.BattleMaximumAp - 4));
            Assert.That(unit.Experience, Is.EqualTo(uint.MaxValue)); Assert.That(unit.X, Is.EqualTo(12)); Assert.That(unit.Y, Is.EqualTo(13));
            Assert.That(returned.Members[1].Unit.LifeState, Is.EqualTo(UnitLifeState.Removed));
            Assert.That(soldier.Unit.LifeState, Is.EqualTo(UnitLifeState.Removed)); Assert.That(leader.Unit.Mp, Is.EqualTo(2));
            Assert.That(deployment.Members[0].Unit.Hp, Is.EqualTo(40));
        }

        [TestCase(10,9)][TestCase(30,29)][TestCase(0,23)]
        public void ReturnPreservesReceivedPerRecordMaximumWithoutFactionCap(int maximum,int current)
        {
            var leader=Member(100,0);
            if(maximum>0) leader.Unit.ActionPoints.ConfigureMaximum(maximum);
            else leader.Unit=new CombatUnit {Id=100,JobId=100,Name="fixture",Hp=40,MaxHp=50,Mp=5,MaxMp=10};
            leader.Unit.ActionPoints.ApplyAbsolute(current);
            var deployment=Deployment(leader);
            Assert.That(BattleFormationReturn.TryCapture(deployment,new[]{Observe(leader.Unit)},out var returned,out _),Is.True);
            Assert.That(returned.Members[0].Unit.Ap,Is.EqualTo(current));
            Assert.That(returned.Members[0].Unit.MaxAp,Is.EqualTo(maximum));
        }

        [Test]
        public void MissingDuplicateForeignAndEnemyResultsNeverProducePartialReturn()
        {
            var leader = Member(100, 0); var soldier = Member(109, 1); var deployment = Deployment(leader, soldier);
            var one = Observe(leader.Unit); var two = Observe(soldier.Unit); var foreign = Member(200, 2);
            var cases = new[] { Array.Empty<CombatUnitSnapshot>(), new[] { one }, new[] { one, one },
                new[] { one, Observe(foreign.Unit) }, new[] { one, two, Observe(foreign.Unit) } };
            foreach (var observations in cases)
            { Assert.That(BattleFormationReturn.TryCapture(deployment, observations, out var returned, out var reason), Is.False);
              Assert.That(returned, Is.Null); Assert.That(reason, Is.Not.Empty); }
            soldier.Unit.Enemy = true;
            Assert.That(BattleFormationReturn.TryCapture(deployment, new[] { one, Observe(soldier.Unit) }, out _, out _), Is.False);
            Assert.That(BattleFormationReturn.TryCapture(null, new[] { one, two }, out _, out _), Is.False);
            Assert.That(BattleFormationReturn.TryCapture(deployment, null, out _, out _), Is.False);
            Assert.That(leader.Unit.Hp, Is.EqualTo(40)); Assert.That(leader.Unit.ActionPoints.Current, Is.EqualTo(6));
        }

        [Test]
        public void ZeroHpActiveCorpseAndRemovedAreRecordedWithoutRevivalOrInference()
        {
            var leader = Member(100, 0); var deployment = Deployment(leader); leader.Unit.Hp = 0; leader.Unit.Mp = 0;
            foreach (var state in new[] { UnitLifeState.Active, UnitLifeState.Corpse, UnitLifeState.Removed })
            {
                leader.Unit.LifeState = state;
                Assert.That(BattleFormationReturn.TryCapture(deployment, new[] { Observe(leader.Unit) }, out var returned, out _), Is.True);
                Assert.That(returned.Members[0].Unit.Hp, Is.Zero); Assert.That(returned.Members[0].Unit.Mp, Is.Zero);
                Assert.That(returned.Members[0].Unit.LifeState, Is.EqualTo(state));
                Assert.That(leader.Unit.LifeState, Is.EqualTo(state));
            }
        }

        [Test]
        public void InvalidVitalsLifeStateAndChangedJobAreRejectedWithoutClamping()
        {
            for (var kind = 0; kind < 10; kind++)
            {
                var leader = Member(100, 0); var deployment = Deployment(leader);
                switch (kind)
                {
                    case 0: leader.Unit.Hp = -1; break;
                    case 1: leader.Unit.Hp = 51; break;
                    case 2: leader.Unit.MaxHp = 0; break;
                    case 3: leader.Unit.Mp = -1; break;
                    case 4: leader.Unit.Mp = 11; break;
                    case 5: leader.Unit.Attack = -1; break;
                    case 6: leader.Unit.Defense = -1; break;
                    case 7: leader.Unit.LifeState = (UnitLifeState)99; break;
                    case 8: leader.Unit.LifeState = (UnitLifeState)(-1); break;
                    case 9: leader.Unit.JobId = 200; break;
                }
                Assert.That(BattleFormationReturn.TryCapture(deployment, new[] { Observe(leader.Unit) }, out var returned, out _), Is.False);
                Assert.That(returned, Is.Null); Assert.That(deployment.Members[0].Unit.Hp, Is.EqualTo(40));
            }
        }

        [Test]
        public void ReturnOwnsReadOnlyValuesAfterObservationAndSourceMutation()
        {
            var leader = Member(100, 0); var deployment = Deployment(leader);
            var observations = new List<CombatUnitSnapshot> { Observe(leader.Unit) };
            Assert.That(BattleFormationReturn.TryCapture(deployment, observations, out var returned, out _), Is.True);
            leader.PersistentId = "changed"; leader.Unit.Hp = 1; leader.Unit.Experience = 123; observations.Clear();
            Assert.That(returned.Members[0].PersistentId, Is.EqualTo("owned-100"));
            Assert.That(returned.Members[0].Unit.Hp, Is.EqualTo(40)); Assert.That(returned.Members[0].Unit.Experience, Is.Zero);
            Assert.Throws<NotSupportedException>(() => ((IList<BattleFormationReturnMember>)returned.Members).Clear());
        }
    }
}
