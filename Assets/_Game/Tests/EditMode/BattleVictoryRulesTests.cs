using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class BattleVictoryRulesTests
    {
        [Test]
        public void KisraAsset_CreatesIndependentInstanceWithMatchingObjective()
        {
            var definition = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>(
                "Assets/_Game/Runtime/Combat/Brugos01Kisra.asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.JobId, Is.EqualTo(701));
            var unit = definition.CreateUnit();
            Assert.That(unit.Id, Is.EqualTo(700));
            Assert.That((unit.X, unit.Y), Is.EqualTo((19, 10)));
            Assert.That(unit.Hp, Is.EqualTo(300));
            Assert.That(unit.Mp, Is.EqualTo(150));
            var world = new CombatWorld();
            world.Add(unit);
            Assert.That(definition.VictoryObjective.HasRequiredUnits(world), Is.True);
            unit.Hp = 0;
            Assert.That(definition.VictoryObjective.IsSatisfied(world), Is.False);
            unit.LifeState = UnitLifeState.Corpse;
            Assert.That(definition.VictoryObjective.IsSatisfied(world), Is.True);
            Assert.That(definition.CreateUnit().Hp, Is.EqualTo(300));
        }

        [Test]
        public void EmptyAuthoredObjective_DoesNotFallBackToElimination()
        {
            var objective = ScriptableObject.CreateInstance<BattleObjectiveDefinition>();
            try
            {
                var world = new CombatWorld();
                world.Add(new CombatUnit { Id = 800, Enemy = true, Hp = 0 });
                Assert.That(objective.IsSatisfied(world), Is.False);
                Assert.That(objective.HasRequiredUnits(world), Is.False);
            }
            finally { Object.DestroyImmediate(objective); }
        }

        [Test]
        public void BrugosAsset_RequiresInstance700_NotJob701()
        {
            var objective = AssetDatabase.LoadAssetAtPath<BattleObjectiveDefinition>(
                "Assets/_Game/Runtime/Combat/Brugos01Victory.asset");
            Assert.That(objective, Is.Not.Null);
            Assert.That(objective.MapKey, Is.EqualTo("01"));
            var world = new CombatWorld();
            world.Add(new CombatUnit { Id = 701, Enemy = true, Hp = 0 });
            Assert.That(objective.HasRequiredUnits(world), Is.False);
            Assert.That(objective.IsSatisfied(world), Is.False);
            var boss = new CombatUnit { Id = 700, Enemy = true, Hp = 10 };
            world.Add(boss);
            Assert.That(objective.HasRequiredUnits(world), Is.True);
            Assert.That(objective.IsSatisfied(world), Is.False);
            Assert.That(objective.GetCompletionDialogue(world), Is.Empty);
            boss.Hp = 0;
            Assert.That(objective.IsSatisfied(world), Is.False);
            boss.LifeState = UnitLifeState.Removed;
            Assert.That(objective.IsSatisfied(world), Is.True);
            Assert.That(objective.GetCompletionDialogue(world), Does.Contain("여기선 일단 물러나야겠군"));
        }

        [Test]
        public void SpecifiedTarget_AllowsOtherEnemiesToSurvive()
        {
            var world = new CombatWorld();
            world.Add(new CombatUnit { Id = 700, Enemy = true, Hp = 0, LifeState = UnitLifeState.Corpse });
            world.Add(new CombatUnit { Id = 710, Enemy = true, Hp = 10 });
            Assert.That(BattleVictoryRules.IsSatisfied(world, new short[] { 700 }), Is.True);
            Assert.That(BattleVictoryRules.IsSatisfied(world, null), Is.False);
        }

        [Test]
        public void MissingOrFriendlyTarget_DoesNotWin()
        {
            var world = new CombatWorld();
            world.Add(new CombatUnit { Id = 100, Hp = 0 });
            Assert.That(BattleVictoryRules.IsSatisfied(world, new short[] { 701 }), Is.False);
            Assert.That(BattleVictoryRules.IsSatisfied(world, new short[] { 100 }), Is.False);
        }

        [Test]
        public void EverySpecifiedTargetMustBeDefeated()
        {
            var world = new CombatWorld();
            world.Add(new CombatUnit { Id = 700, Enemy = true, Hp = 0, LifeState = UnitLifeState.Corpse });
            var enemy = new CombatUnit { Id = 710, Enemy = true, Hp = 10 };
            world.Add(enemy);
            var targets = new short[] { 700, 710 };
            Assert.That(BattleVictoryRules.IsSatisfied(world, targets), Is.False);
            enemy.Hp = 0;
            Assert.That(BattleVictoryRules.IsSatisfied(world, targets), Is.False);
            enemy.LifeState = UnitLifeState.Corpse;
            Assert.That(BattleVictoryRules.IsSatisfied(world, targets), Is.True);
            Assert.That(BattleVictoryRules.IsSatisfied(world, null), Is.True);
        }

        [Test]
        public void EmptyBattle_DoesNotWin()
        {
            Assert.That(BattleVictoryRules.IsSatisfied(new CombatWorld(), null), Is.False);
        }
    }
}
