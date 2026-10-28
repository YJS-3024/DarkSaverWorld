using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class CombatGroupTests
    {
        [Test]
        public void GroupsUseInstanceIdsNotJobsAndReturnOwnedOrderedSnapshots()
        {
            var world = new CombatWorld();
            var soldier = Unit(709); var leader = Unit(700); leader.JobId = 701;
            Assert.That(world.TryRegisterGroup(new[] { soldier, leader, Unit(701) }), Is.True);
            Assert.That(world.TryGetGroup(709, out var group), Is.True);
            Assert.That(group.CommanderId, Is.EqualTo(700));
            Assert.That(group.CommanderPresent && group.CommanderAlive, Is.True);
            Assert.That(group.SoldierCount, Is.EqualTo(2)); Assert.That(group.AliveCount, Is.EqualTo(3));
            Assert.That(group.Members[0].Id, Is.EqualTo(700));
            Assert.That(group.Members[0].JobId, Is.EqualTo(701));
            Assert.That(group.Members[1].Id, Is.EqualTo(701));
            soldier.Hp = 1; soldier.Id = 100; leader.JobId = 1;
            Assert.That(world.Get(709).Hp, Is.EqualTo(50)); Assert.That(world.Get(700).JobId, Is.EqualTo(701));
            world.Get(701).Hp = 20;
            Assert.That(group.Members[1].Hp, Is.EqualTo(50));
            Assert.That(((IList<CombatUnitSnapshot>)group.Members).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => ((IList<CombatUnitSnapshot>)group.Members).Clear());
        }

        [Test]
        public void InvalidRegistrationIsAtomicAndCannotOverwriteExistingUnits()
        {
            var world = new CombatWorld(); world.Add(Unit(700));
            Assert.That(world.TryRegisterGroup(new[] { Unit(701), Unit(701) }), Is.False);
            Assert.That(world.TryGet(701, out _), Is.False);
            Assert.That(world.TryRegisterGroup(new[] { Unit(701), Unit(710) }), Is.False);
            var enemy = Unit(702); enemy.Enemy = true;
            Assert.That(world.TryRegisterGroup(new[] { enemy }), Is.False);
            Assert.That(world.TryRegisterGroup(new[] { Unit(700) }), Is.False);
            Assert.That(world.TryRegisterGroup(new[] { Unit(711) }), Is.False);
            var invalid = Unit(702); invalid.Mp = 11;
            Assert.That(world.TryRegisterGroup(new[] { Unit(701), invalid }), Is.False);
            Assert.That(world.TryRegisterGroup(null), Is.False);
            Assert.That(world.TryRegisterGroup(Array.Empty<CombatUnit>()), Is.False);
            Assert.That(world.TryRegisterGroup(new[] { Unit(-1) }), Is.False);
            Assert.That(world.TryGet(701, out _), Is.False);
            Assert.That(world.Get(700).Hp, Is.EqualTo(50));
        }

        [Test]
        public void CommanderDeathDoesNotKillSoldiersOrMutatePreviousSnapshot()
        {
            var world = new CombatWorld(); world.TryRegisterGroup(new[] { Unit(700), Unit(701) });
            var flow = new CombatFlow(world, new CombatServerBoundary(), true);
            world.TryGetGroup(701, out var before);
            Assert.That(flow.TryApplyDeath(700, true), Is.True);
            world.TryGetGroup(701, out var after);
            Assert.That(after.CommanderPresent, Is.True); Assert.That(after.CommanderAlive, Is.False);
            Assert.That(after.AliveCount, Is.EqualTo(1)); Assert.That(before.AliveCount, Is.EqualTo(2));
            Assert.That(flow.TryApplyDeath(700, false), Is.True);
            Assert.That(world.Get(701).Alive, Is.True);
            Assert.That(flow.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp), Is.False);
            Assert.That(world.Get(701).ActionPoints.Current, Is.EqualTo(6));
            Assert.That(world.Get(700).ActionPoints.Current, Is.EqualTo(6));
            world.Remove(700); world.TryGetGroup(701, out var orphan);
            Assert.That(orphan.CommanderPresent, Is.False); Assert.That(orphan.AliveCount, Is.EqualTo(1));
        }

        [Test]
        public void SourceGroupWidthDoesNotBecomePlayerFormationOrAiQueueLimit()
        {
            var world = new CombatWorld(); var group = new CombatUnit[10];
            for (var i = 0; i < 10; i++) group[i] = Unit((short)(700 + i));
            Assert.That(world.TryRegisterGroup(group), Is.True);
            world.TryGetGroup(709, out var snapshot); Assert.That(snapshot.SoldierCount, Is.EqualTo(9));
            var boundary = new[] { Unit(32767), Unit(32760) };
            Assert.That(world.TryRegisterGroup(boundary), Is.True);
            world.TryGetGroup(32767, out snapshot); Assert.That(snapshot.CommanderId, Is.EqualTo(32760));
            Assert.That(world.TryGetGroup(-1, out _), Is.False);
            Assert.That(world.TryGetGroup(800, out _), Is.False);
            for (var g = 1100; g <= 1200; g += 10)
            {
                var additional = new CombatUnit[10];
                for (var i = 0; i < 10; i++) additional[i] = Unit((short)(g + i));
                Assert.That(world.TryRegisterGroup(additional), Is.True);
            }
            Assert.That(world.TryGetGroup(1209, out _), Is.True); // More than 100 registered units.
            var mixed = Unit(702); mixed.Enemy = true;
            var legacy = new CombatWorld(); legacy.Add(Unit(700)); legacy.Add(mixed);
            Assert.That(legacy.TryGetGroup(702, out _), Is.False);
        }

        [Test]
        public void AdditionalMembersReceiveIndependentPacketsWithoutLeaderDeathCascade()
        {
            WithBattle((battle, receiver) => {
                var commander = Unit(710); commander.Enemy = true;
                var soldier = Unit(711); soldier.Enemy = true; soldier.JobId = 701;
                Assert.That(battle.TryRegisterGroup(new[] { soldier, commander }), Is.True);
                Assert.That(receiver.TryApplyPacket(new HpSetNotification(711, 30)), Is.True);
                Assert.That(receiver.TryApplyPacket(new MpSetNotification(711, 4)), Is.True);
                Assert.That(receiver.TryApplyPacket(new ExperienceSetNotification(711, 123)), Is.True);
                Assert.That(receiver.TryApplyPacket(new UnitCorpseNotification(710)), Is.True);
                Assert.That(receiver.TryApplyPacket(new AttackEffectNotification(711, 100, 7)), Is.True);
                battle.TryGetUnit(711, out var unit);
                Assert.That(unit.Hp, Is.EqualTo(30)); Assert.That(unit.Mp, Is.EqualTo(4));
                Assert.That(unit.Experience, Is.EqualTo(123)); Assert.That(unit.Alive, Is.True);
                Assert.That(unit.JobId, Is.EqualTo(701));
                battle.TryGetGroup(711, out var group);
                Assert.That(group.CommanderAlive, Is.False); Assert.That(group.AliveCount, Is.EqualTo(1));
                Assert.That(battle.VictoryAchieved, Is.False);
            });
        }

        [Test]
        public void LivingAdditionalUnitsRecoverApAfterPrimaryUnitsDieAndCostsStayUnknown()
        {
            WithBattle((battle, receiver) => {
                Assert.That(battle.TryRegisterGroup(new[] { Unit(101), Unit(102) }), Is.True);
                Assert.That(battle.TryRegisterGroup(new[] { Unit(200) }), Is.True);
                var enemy = Unit(810); enemy.Enemy = true;
                Assert.That(battle.TryRegisterGroup(new[] { enemy }), Is.True);
                Assert.That(battle.CanStartAttackRequest(810, 101, out _), Is.False);
                Assert.That(receiver.TryApplyPacket(new UnitCorpseNotification(100)), Is.True);
                Assert.That(receiver.TryApplyPacket(new UnitCorpseNotification(800)), Is.True);
                Assert.That(NativeCombatStateFixture.ReceiveFullTicks(battle), Is.True);
                battle.TryGetUnit(101, out var ally); battle.TryGetUnit(810, out var foe);
                Assert.That(ally.Ap, Is.EqualTo(ally.MaxAp)); Assert.That(foe.Ap, Is.EqualTo(foe.MaxAp));
                Assert.That(battle.PlayerAp, Is.EqualTo(6)); Assert.That(battle.EnemyAp, Is.EqualTo(6));
                Assert.That(battle.CanStartAttackRequest(810, 101, out _), Is.False);
                battle.enabled = false;
                Assert.That(battle.TryRegisterGroup(new[] { Unit(110) }), Is.False);
                Assert.That(battle.TryGetUnit(110, out _), Is.False);
            });
        }

        private static CombatUnit Unit(short id) => NativeCombatStateFixture.Received(new CombatUnit {
            Id = id, JobId = 100, Name = "fixture", Hp = 50, MaxHp = 50,
            Mp = 10, MaxMp = 10, Attack = 5, Defense = 2
        });

        private static void WithBattle(Action<BattleSceneRoot, BattlePacketReceiver> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try {
                var root = new GameObject("Group check"); SceneManager.MoveGameObjectToScene(root, scene);
                var receiver = root.AddComponent<BattlePacketReceiver>(); NativeCombatStateFixture.Initialize(root.GetComponent<BattleSceneRoot>(),6);
                check(root.GetComponent<BattleSceneRoot>(), receiver);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
