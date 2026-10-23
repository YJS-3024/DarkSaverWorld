using System;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class BattleFormationTests
    {
        [Test]
        public void AlliedThreeGroupLimitCountsExistingPlayerButNotEnemies()
        {
            WithBattle(battle => {
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(200) }, out _), Is.True);
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(300) }, out _), Is.True);
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(400) }, out _), Is.False);
                Assert.That(battle.TryGetUnit(400, out _), Is.False);
                Assert.That(battle.TryGetGroup(300, out var group), Is.True); Assert.That(group.CommanderAlive, Is.True);
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(301) }, out _), Is.True);
            });
        }

        [Test]
        public void FourSoldierLimitCountsExistingMembersWithoutForcingConsecutiveIds()
        {
            WithBattle(battle => {
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(101), Unit(109), Unit(102), Unit(108) }, out _), Is.True);
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(103) }, out _), Is.False);
                Assert.That(battle.TryGetUnit(103, out _), Is.False);
                battle.TryGetGroup(100, out var group); Assert.That(group.SoldierCount, Is.EqualTo(4));
                Assert.That(battle.PlayerAp, Is.EqualTo(6)); Assert.That(battle.PlayerHp, Is.EqualTo(120));
            });
        }

        [Test]
        public void InvalidOrOversizeFormationNeverPartiallyRegisters()
        {
            WithBattle(battle => {
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(200), Unit(201), Unit(202), Unit(203), Unit(204), Unit(205) }, out _), Is.False);
                Assert.That(battle.TryGetUnit(200, out _), Is.False);
                var enemy = Unit(200); enemy.Enemy = true;
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { enemy }, out _), Is.False);
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(200), Unit(301) }, out _), Is.False);
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(200), Unit(201), Unit(201) }, out _), Is.False);
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(201) }, out _), Is.False);
                Assert.That(battle.TryRegisterAlliedFormationGroup(null, out _), Is.False);
                Assert.That(battle.TryGetUnit(200, out _), Is.False);
            });
        }

        [Test]
        public void GenericRegistryStillAcceptsNineSoldiersWithoutFormationLimits()
        {
            WithBattle(battle => {
                var full = new CombatUnit[10];
                for (short i = 0; i < 10; i++) full[i] = Unit((short)(200 + i));
                Assert.That(battle.TryRegisterGroup(full), Is.True);
                battle.TryGetGroup(209, out var group); Assert.That(group.SoldierCount, Is.EqualTo(9));
                var enemy = new CombatUnit[10];
                for (short i = 0; i < 10; i++) { enemy[i] = Unit((short)(900 + i)); enemy[i].Enemy = true; }
                Assert.That(battle.TryRegisterGroup(enemy), Is.True);
            });
        }

        [Test]
        public void FormationCannotDesynchronizeAnAlreadyConfiguredGrid()
        {
            WithBattle(battle => {
                Assert.That(battle.TryConfigureGrid(3, 3, new[] {
                    new CombatGridPlacement(100, new CombatGridCell(0, 0)),
                    new CombatGridPlacement(800, new CombatGridCell(2, 2)) }, (_, _) => true, (_, _) => true), Is.True);
                Assert.That(battle.TryRegisterAlliedFormationGroup(new[] { Unit(200) }, out _), Is.False);
                Assert.That(battle.TryGetUnit(200, out _), Is.False);
            });
        }

        private static CombatUnit Unit(short id) => new CombatUnit {
            Id = id, JobId = 100, Name = "Formation member", Hp = 50, MaxHp = 50, Mp = 10, MaxMp = 10 };
        private static void WithBattle(Action<BattleSceneRoot> check)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty;
            var scene = EditorSceneManager.NewPreviewScene();
            try { var go = new GameObject("Allied formation test"); SceneManager.MoveGameObjectToScene(go, scene);
                check(go.AddComponent<BattleSceneRoot>()); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
