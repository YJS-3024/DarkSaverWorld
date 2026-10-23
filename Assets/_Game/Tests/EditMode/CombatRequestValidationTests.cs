using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DarkSaver.Tests
{
    public sealed class CombatRequestValidationTests
    {
        [TestCase(CombatActionType.Skill, 0)]
        [TestCase((CombatActionType)99, 0)]
        [TestCase(CombatActionType.Magic, 0)]
        [TestCase(CombatActionType.Magic, 102)]
        [TestCase(CombatActionType.Attack, 101)]
        public void UnsupportedRequestsNeverBecomeOrdinaryAttacks(CombatActionType action, int magicId)
        {
            CheckRejected(World(), new CombatRequest(1, 2, action, magicId));
        }

        [TestCase((short)99, (short)2)]
        [TestCase((short)1, (short)99)]
        [TestCase((short)1, (short)1)]
        public void MissingOrSelfTargetsAreRejected(short actor, short target)
        {
            CheckRejected(World(), new CombatRequest(actor, target, CombatActionType.Attack));
        }

        [TestCase(true, UnitLifeState.Active)]
        [TestCase(false, UnitLifeState.Active)]
        [TestCase(true, UnitLifeState.Corpse)]
        [TestCase(false, UnitLifeState.Removed)]
        public void InactiveUnitsDoNotStartActions(bool actor, UnitLifeState state)
        {
            var world = World(); var unit = world.Get(actor ? (short)1 : (short)2);
            if (state == UnitLifeState.Active) unit.Hp = 0;
            else unit.LifeState = state;
            CheckRejected(world, new CombatRequest(1, 2, CombatActionType.Attack));
        }

        [TestCase(0)]
        [TestCase(9)]
        public void InsufficientMpDoesNotNotifyOrSpend(int mp)
        {
            var world = World(); world.Get(1).Mp = mp;
            CheckRejected(world, new CombatRequest(1, 2, CombatActionType.Magic, LocalCombatAuthority.PrototypeMagicId));
        }

        [Test]
        public void MissingWorldIsRejected()
        {
            var authority = new LocalCombatAuthority();
            var request = new CombatRequest(1, 2, CombatActionType.Attack);
            Assert.That(authority.TryValidateRequest(null, request, out var reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
            Assert.Throws<InvalidOperationException>(() => authority.Resolve(null, request));
        }

        [TestCase(CombatActionType.Attack)]
        [TestCase(CombatActionType.Magic)]
        public void AcceptedActionStartsBeforeAbsoluteResultNotification(CombatActionType action)
        {
            var world = World(); var actor = world.Get(1); var target = world.Get(2);
            var flow = new CombatFlow(world, new LocalCombatAuthority());
            var order = new List<string>();
            flow.ActionStarted += _ =>
            {
                Assert.That((actor.Mp, target.Hp), Is.EqualTo((10, 100)));
                order.Add("start");
            };
            flow.ResultApplied += result =>
            {
                Assert.That(actor.Mp, Is.EqualTo(result.AbsoluteMp));
                Assert.That(target.Hp, Is.EqualTo(result.AbsoluteHp));
                order.Add("result");
            };
            var request = new CombatRequest(1, 2, action, action == CombatActionType.Magic ? LocalCombatAuthority.PrototypeMagicId : 0);
            Assert.That(flow.TryExecute(request, out var applied, out var reason), Is.True);
            Assert.That(reason, Is.Empty);
            Assert.That(order, Is.EqualTo(new[] { "start", "result" }));
            Assert.That((applied.Damage, actor.Mp, target.Hp),
                Is.EqualTo(action == CombatActionType.Magic ? (41, 0, 59) : (16, 10, 84)));
        }

        [TestCase(CombatActionType.Attack)]
        [TestCase(CombatActionType.Magic)]
        public void LargeNativeAttackDoesNotOverflow(CombatActionType action)
        {
            var world = World(); world.Get(1).Attack = int.MaxValue; world.Get(2).Defense = 0;
            var flow = new CombatFlow(world, new LocalCombatAuthority());
            var result = flow.Execute(new CombatRequest(1, 2, action,
                action == CombatActionType.Magic ? LocalCombatAuthority.PrototypeMagicId : 0));
            Assert.That(result.Damage, Is.EqualTo(int.MaxValue));
            Assert.That(world.Get(2).Hp, Is.Zero);
            Assert.That(world.Get(2).LifeState, Is.EqualTo(UnitLifeState.Corpse));
        }

        [Test]
        public void BattleRejectsFourthMagicWithoutCounterattack()
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty; var count = SceneManager.sceneCount;
            var preview = EditorSceneManager.NewPreviewScene();
            var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>("Assets/_Game/Runtime/Combat/Brugos01Kisra.asset"));
            try
            {
                var so = new SerializedObject(enemy);
                so.FindProperty("useJobCatalogBaseStats").boolValue = false;
                so.FindProperty("baseHp").intValue = 1000;
                so.FindProperty("baseAttack").intValue = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                var go = new GameObject("Rejected magic test"); SceneManager.MoveGameObjectToScene(go, preview);
                var battle = go.AddComponent<BattleSceneRoot>();
                Assert.That(battle.ConfigureEnemy(enemy), Is.True);
                battle.ConfigureActionPointCosts(2, 6); // Fixture policy only.
                battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerMagic();
                battle.AdvanceActionPoints(6f); battle.PlayerMagic();
                battle.AdvanceActionPoints(6f); battle.PlayerMagic();
                Assert.That(battle.PlayerMp, Is.Zero);
                var hp = battle.PlayerHp; var enemyHp = battle.EnemyHp;
                Assert.DoesNotThrow(() => battle.PlayerMagic());
                Assert.That((battle.PlayerHp, battle.EnemyHp, battle.PlayerMp), Is.EqualTo((hp, enemyHp, 0)));
                Assert.That(battle.Status, Does.Contain("MP가 부족"));
                battle.AdvanceActionPoints(6f); battle.PlayerAttack();
                Assert.That(battle.EnemyHp, Is.LessThan(enemyHp));
                Assert.That(battle.PlayerHp, Is.LessThan(hp));
            }
            finally { Object.DestroyImmediate(enemy); EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.EqualTo(dirty));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(count));
        }

        private static CombatWorld World()
        {
            var world = new CombatWorld();
            world.Add(new CombatUnit { Id = 1, Hp = 100, MaxHp = 100, Mp = 10, MaxMp = 10, Attack = 26, Defense = 7 });
            world.Add(new CombatUnit { Id = 2, Enemy = true, Hp = 100, MaxHp = 100, Attack = 18, Defense = 10 });
            return world;
        }

        private static void CheckRejected(CombatWorld world, CombatRequest request)
        {
            var actor = world.Get(1); var target = world.Get(2);
            var before = (actor.Hp, actor.Mp, actor.LifeState, target.Hp, target.Mp, target.LifeState);
            var flow = new CombatFlow(world, new LocalCombatAuthority()); var notifications = 0;
            flow.ActionStarted += _ => notifications++;
            flow.ResultApplied += _ => notifications++;
            Assert.That(flow.TryExecute(request, out var result, out var reason), Is.False);
            Assert.That(result, Is.EqualTo(default(CombatResult)));
            Assert.That(reason, Is.Not.Empty);
            Assert.Throws<InvalidOperationException>(() => flow.Execute(request));
            Assert.Throws<InvalidOperationException>(() => new LocalCombatAuthority().Resolve(world, request));
            Assert.That(notifications, Is.Zero);
            Assert.That((actor.Hp, actor.Mp, actor.LifeState, target.Hp, target.Mp, target.LifeState), Is.EqualTo(before));
        }
    }
}
