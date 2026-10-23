using System;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class BattleFormationDeploymentTests
    {
        private static CombatUnit Unit(short id) => new() { Id = id, JobId = 100, Name = "fixture", X = id % 10,
            Y = id / 10, Hp = 40, MaxHp = 50, Mp = 2, MaxMp = 10, Attack = 0, Defense = 0 };
        private static BattleFormationSnapshot Formation(params short[] ids)
        {
            var draft = new BattleFormationMemberDraft[ids.Length];
            for (int i = 0; i < ids.Length; i++) { var unit = Unit(ids[i]);
                draft[i] = new BattleFormationMemberDraft { PersistentId = "owned-" + ids[i],
                    CommanderPersistentId = "owned-" + (ids[i] / 10 * 10), CommanderId = (short)(ids[i] / 10 * 10),
                    Unit = unit, Position = new CombatGridCell(unit.X, unit.Y), IsOwned = true, FinalStateConfirmed = true,
                    TeamId = 1, RecordEligible = true, LearnedMagicIds = Array.Empty<short>() }; }
            Assert.That(BattleFormation.TryCreateSnapshot(draft, 50, 50, Array.Empty<short>(), out var formation, out _), Is.True);
            return formation;
        }

        [Test]
        public void AllGroupsRegisterTogetherAndInputsCannotRewriteRegisteredUnits()
        {
            var world = new CombatWorld(); world.Add(Unit(100)); var one = Unit(200); var two = Unit(209); var three = Unit(300);
            Assert.That(world.TryRegisterAlliedFormation(new[] { three, two, one }), Is.True);
            one.Hp = 1; two.Mp = 9;
            Assert.That(world.Get(200).Hp, Is.EqualTo(40)); Assert.That(world.Get(209).Mp, Is.EqualTo(2));
            Assert.That(world.Get(200).ActionPoints.Current, Is.EqualTo(6)); Assert.That(world.Get(100).ActionPoints.Current, Is.EqualTo(6));
            Assert.That(world.TryRegisterAlliedFormation(new[] { Unit(200) }), Is.False);
        }

        [Test]
        public void LaterGroupFailureNeverLeavesEarlierGroupRegistered()
        {
            var world = new CombatWorld(); var existing = Unit(100); world.Add(existing);
            Assert.That(world.TryRegisterAlliedFormation(new[] { Unit(200), Unit(209), Unit(301) }), Is.False);
            Assert.That(world.TryGet(200, out _), Is.False); Assert.That(world.TryGet(209, out _), Is.False);
            Assert.That(world.Get(100), Is.SameAs(existing)); Assert.That(existing.Hp, Is.EqualTo(40));
            Assert.That(world.TryRegisterAlliedFormation(new[] { Unit(200), Unit(300), Unit(400) }), Is.False);
            Assert.That(world.TryGet(200, out _), Is.False);
        }

        [Test]
        public void GlobalEnemyCollisionAndCombinedSoldierLimitAreAtomic()
        {
            var world = new CombatWorld(); world.Add(Unit(100)); var enemy = Unit(300); enemy.Enemy = true; world.Add(enemy);
            Assert.That(world.TryRegisterAlliedFormation(new[] { Unit(200), Unit(300) }), Is.False);
            Assert.That(world.TryGet(200, out _), Is.False); Assert.That(world.Get(300), Is.SameAs(enemy));
            Assert.That(world.TryRegisterAlliedFormation(new[] { Unit(101), Unit(102), Unit(103), Unit(104) }), Is.True);
            Assert.That(world.TryRegisterAlliedFormation(new[] { Unit(200), Unit(109) }), Is.False);
            Assert.That(world.TryGet(200, out _), Is.False); Assert.That(world.TryGet(109, out _), Is.False);
        }

        [Test]
        public void BattleRootRegistersAndCapturesRemovedUnitsWithoutUpdatingSource()
        {
            WithBattle(battle => {
                var formation = Formation(200, 209, 300);
                Assert.That(battle.TryRegisterAlliedFormation(formation, out _), Is.True);
                Assert.That(battle.ApplyDeath(209, false), Is.True);
                Assert.That(battle.ApplyExperience(new CombatExperienceResult(200, uint.MaxValue)), Is.True);
                Assert.That(battle.TryCaptureFormationReturn(formation, out var returned, out _), Is.True);
                Assert.That(returned.Members[1].Unit.LifeState, Is.EqualTo(UnitLifeState.Removed));
                Assert.That(returned.Members[0].Unit.Mp, Is.EqualTo(2)); Assert.That(returned.Members[0].Unit.Experience, Is.EqualTo(uint.MaxValue));
                Assert.That(formation.Members[1].Unit.LifeState, Is.EqualTo(UnitLifeState.Active));
                Assert.That(battle.TryCaptureFormationReturn(Formation(400), out var missing, out _), Is.False);
                Assert.That(missing, Is.Null);
            });
        }

        [Test]
        public void RootRejectsCombinedGroupOverflowWithoutPartialRegistration()
        {
            WithBattle(battle => {
                var hp = battle.PlayerHp; var ap = battle.PlayerAp;
                Assert.That(battle.TryRegisterAlliedFormation(Formation(200, 300, 400), out _), Is.False);
                Assert.That(battle.TryGetUnit(200, out _), Is.False); Assert.That(battle.TryGetUnit(300, out _), Is.False);
                Assert.That(battle.PlayerHp, Is.EqualTo(hp)); Assert.That(battle.PlayerAp, Is.EqualTo(ap));
                Assert.That(battle.TryRegisterAlliedFormation(null, out _), Is.False);
            });
        }

        private static void WithBattle(Action<BattleSceneRoot> action)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty; var preview = EditorSceneManager.NewPreviewScene();
            try { var go = new GameObject("Atomic formation test"); SceneManager.MoveGameObjectToScene(go, preview);
                action(go.AddComponent<BattleSceneRoot>()); }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
