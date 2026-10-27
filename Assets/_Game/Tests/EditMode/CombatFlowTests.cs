using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class CombatFlowTests
    {
        [Test]
        public void TemporaryBuffsAffectDamageRefreshAndExpireWithoutChangingBaseStats()
        {
            var world = new CombatWorld();
            var actor = new CombatUnit { Id = 100, Hp = 100, MaxHp = 100, Mp = 100, MaxMp = 100, Attack = 20, Defense = 5 };
            var enemy = new CombatUnit { Id = 800, Enemy = true, Hp = 100, MaxHp = 100, Attack = 30, Defense = 10 };
            world.Add(actor); world.Add(enemy);
            var authority = new CombatServerBoundary(sample: () => 0);
            var flow = new CombatFlow(world, authority);
            var buff = new CombatRequest(100, 100, CombatActionType.Magic, 2101);
            Assert.IsFalse(flow.TryExecute(buff, out _, out _));
            flow.TemporaryRulesEnabled = true;
            Assert.IsFalse(flow.ConfigureTemporaryBuffMagic(2101, 6, 3, 10, 0, float.NaN));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Magic, 2101), out _, out _));
            Assert.AreEqual(100, actor.Mp);
            Assert.IsTrue(flow.TryExecute(buff, out var result, out _));
            Assert.AreEqual(10, result.AttackBonus); Assert.AreEqual(0, result.Damage); Assert.AreEqual(100, actor.Hp);
            Assert.AreEqual(94, actor.Mp); Assert.AreEqual(20, actor.Attack); Assert.AreEqual(30, authority.EffectiveAttack(actor));
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Attack), out result, out _));
            Assert.AreEqual(20, result.Damage);
            flow.AdvanceActionPoints(10);
            Assert.IsTrue(flow.TryExecute(buff, out _, out _)); Assert.AreEqual(30, authority.EffectiveAttack(actor));
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100, 100, CombatActionType.Magic, 2001), out _, out _));
            Assert.AreEqual(15, authority.EffectiveDefense(actor)); Assert.AreEqual(5, actor.Defense);
            Assert.IsTrue(flow.TryExecute(new CombatRequest(800, 100, CombatActionType.Attack), out result, out _));
            Assert.AreEqual(15, result.Damage);
            Assert.IsFalse(flow.AdvanceActionPoints(float.PositiveInfinity));
            flow.AdvanceActionPoints(19); Assert.AreEqual(30, authority.EffectiveAttack(actor));
            Assert.IsTrue(flow.TryGetTemporaryBuff(100, true, out var bonus, out var remaining));
            Assert.AreEqual(10, bonus); Assert.AreEqual(1d, remaining);
            Assert.IsTrue(flow.TryGetTemporaryBuff(100, false, out _, out remaining)); Assert.AreEqual(1d, remaining);
            Assert.IsFalse(flow.TryGetTemporaryBuff(800, true, out _, out _));
            flow.AdvanceActionPoints(1); Assert.AreEqual(20, authority.EffectiveAttack(actor)); Assert.AreEqual(5, authority.EffectiveDefense(actor));
            Assert.IsFalse(flow.TryGetTemporaryBuff(100, true, out bonus, out remaining)); Assert.AreEqual(0, bonus); Assert.AreEqual(0d, remaining);
            flow.TryExecute(buff, out _, out _); flow.TryApplyDeath(100, false);
            actor.LifeState = UnitLifeState.Active; actor.Hp = 100;
            Assert.AreEqual(20, authority.EffectiveAttack(actor));
            flow.TryExecute(buff, out _, out _); flow.TemporaryRulesEnabled = false; flow.TemporaryRulesEnabled = true;
            Assert.AreEqual(20, authority.EffectiveAttack(actor));
        }

        [Test]
        public void TemporaryHealingCapsHpAndRejectsEnemyDeadFullAndInsufficientMpWithoutCost()
        {
            var world = new CombatWorld();
            var actor = new CombatUnit { Id = 100, Hp = 80, MaxHp = 100, Mp = 30, MaxMp = 30 };
            var ally = new CombatUnit { Id = 101, Hp = 90, MaxHp = 100 };
            var enemy = new CombatUnit { Id = 800, Enemy = true, Hp = 10, MaxHp = 100 };
            world.Add(actor); world.Add(ally); world.Add(enemy);
            var flow = new CombatFlow(world, new CombatServerBoundary(), true);
            Assert.IsTrue(flow.ConfigureTemporaryHealingMagic(2401, 7, 3, int.MaxValue));
            Assert.IsFalse(flow.ConfigureTemporaryHealingMagic(2401, 7, 3, 0));
            flow.ConfigureMagicApCost(2401, 4); flow.AdvanceActionPoints(20);
            var self = new CombatRequest(100, 100, CombatActionType.Magic, 2401);
            Assert.IsFalse(flow.TryExecute(self, out _, out _));
            flow.TemporaryRulesEnabled = true;
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Magic, 2401), out _, out _));
            Assert.AreEqual(30, actor.Mp); Assert.AreEqual(GameConstants.BattleMaximumAp, actor.ActionPoints.Current);
            Assert.IsTrue(flow.TryExecute(self, out var result, out _));
            Assert.AreEqual(100, actor.Hp); Assert.AreEqual(23, actor.Mp); Assert.AreEqual(20, result.Healing);
            Assert.AreEqual(0, result.Damage); Assert.IsTrue(result.Hit);
            Assert.AreEqual(GameConstants.BattleMaximumAp - 4, actor.ActionPoints.Current);
            flow.AdvanceActionPoints(20);
            Assert.IsFalse(flow.TryExecute(self, out _, out _));
            Assert.IsTrue(flow.TryExecute(new CombatRequest(100, 101, CombatActionType.Magic, 2401), out result, out _));
            Assert.AreEqual(100, ally.Hp); Assert.AreEqual(10, result.Healing); Assert.AreEqual(16, actor.Mp);
            flow.AdvanceActionPoints(20); ally.Hp = 1; ally.LifeState = UnitLifeState.Removed;
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100, 101, CombatActionType.Magic, 2401), out _, out _));
            ally.LifeState = UnitLifeState.Active;
            flow.ConfigureTemporaryHealingMagic(2401, 17, 3, 10);
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100, 101, CombatActionType.Magic, 2401), out _, out _));
            Assert.AreEqual(16, actor.Mp); Assert.AreEqual(1, ally.Hp);
            Assert.AreEqual(GameConstants.BattleMaximumAp, actor.ActionPoints.Current);
        }

        [Test]
        public void ExplicitTemporaryMagicHasIndependentCostAndFormulaWithoutChangingEvidenceMode()
        {
            var world = new CombatWorld();
            var actor = new CombatUnit { Id = 100, Hp = 100, MaxHp = 100, Mp = 30, MaxMp = 30, Attack = 20 };
            var target = new CombatUnit { Id = 800, Enemy = true, Hp = 100, MaxHp = 100, Defense = 12 };
            world.Add(actor); world.Add(target);
            var flow = new CombatFlow(world, new CombatServerBoundary(), true);
            Assert.IsTrue(flow.ConfigureTemporaryDamageMagic(201, 7, 3, 10, 3));
            Assert.IsTrue(flow.ConfigureMagicApCost(201, 4));
            Assert.IsFalse(flow.TryGetTemporaryMagicRange(201, out _));
            flow.AdvanceActionPoints(20);
            var request = new CombatRequest(100, 800, CombatActionType.Magic, 201);
            Assert.IsFalse(flow.TryExecute(request, out _, out _));
            Assert.AreEqual(30, actor.Mp); Assert.AreEqual(100, target.Hp);
            flow.TemporaryRulesEnabled = true;
            Assert.IsTrue(flow.TryGetTemporaryMagicRange(201, out var range)); Assert.AreEqual(3, range);
            Assert.IsFalse(flow.ConfigureTemporaryDamageMagic(201, -1, 3, 10, 3));
            Assert.IsFalse(flow.ConfigureTemporaryDamageMagic(201, 7, -1, 10, 3));
            Assert.IsFalse(flow.ConfigureTemporaryDamageMagic(201, 7, 3, 10, 0));
            Assert.IsTrue(flow.TryExecute(request, out var result, out _));
            Assert.AreEqual(26, result.Damage); Assert.AreEqual(74, target.Hp);
            Assert.AreEqual(23, actor.Mp); Assert.AreEqual(GameConstants.BattleMaximumAp - 4, actor.ActionPoints.Current);
            flow.AdvanceActionPoints(20);
            Assert.IsTrue(flow.ConfigureTemporaryDamageMagic(201, 24, 3, 10, 3));
            Assert.IsFalse(flow.TryExecute(request, out _, out _));
            Assert.AreEqual(23, actor.Mp); Assert.AreEqual(74, target.Hp);
            Assert.AreEqual(GameConstants.BattleMaximumAp, actor.ActionPoints.Current);
            Assert.IsFalse(flow.TryGetTemporaryMagicRange(202, out _));
            flow.TemporaryRulesEnabled = false;
            Assert.IsFalse(flow.TryExecute(request, out _, out _));
        }

        [Test]
        public void TemporaryProfileCostsDamageRestAndEvidenceModeRemainSeparate()
        {
            var world = new CombatWorld();
            var actor = new CombatUnit { Id = 100, JobId = 100, Hp = 50, MaxHp = 50, Attack = 20 };
            var target = new CombatUnit { Id = 800, Enemy = true, Hp = 50, MaxHp = 100, Defense = 5 };
            world.Add(actor); world.Add(target);
            var flow = new CombatFlow(world, new CombatServerBoundary(sample: () => 0), true);
            Assert.That(flow.GetAttackApCost(100), Is.Zero);
            Assert.That(flow.TryTemporaryRest(800), Is.False);
            flow.TemporaryRulesEnabled = true;
            Assert.That(flow.GetAttackApCost(100), Is.EqualTo(GameConstants.BattleTemporaryJob100AttackApCost));
            Assert.That(flow.GetAttackApCost(800), Is.EqualTo(GameConstants.BattleTemporaryEnemyAttackApCost));
            Assert.That(flow.GetMagicApCost(101), Is.EqualTo(GameConstants.BattleTemporaryMagic101ApCost));
            flow.AdvanceActionPoints(14);
            Assert.That(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Attack), out var result, out _), Is.True);
            Assert.That(result.Hit, Is.True); Assert.That(target.Hp, Is.EqualTo(35));
            Assert.That(actor.ActionPoints.Current, Is.EqualTo(10 - GameConstants.BattleTemporaryJob100AttackApCost));
            Assert.That(flow.TryTemporaryRest(800), Is.True);
            Assert.That(target.Hp, Is.EqualTo(55)); Assert.That(target.ActionPoints.Current, Is.EqualTo(20 - GameConstants.BattleTemporaryRestApCost));
            Assert.That(flow.TryTemporaryRest(800), Is.False);
            flow.TemporaryRulesEnabled = false;
            Assert.That(flow.GetAttackApCost(100), Is.Zero);
            Assert.That(flow.GetMagicApCost(101), Is.Zero);
        }

        [Test]
        public void TemporaryPhysicalHitFormulaMatchesPercentagesAndRejectsInvalidValues()
        {
            Assert.That(UnresolvedCombatAccuracy.PhysicalHitProbability(80, 15), Is.EqualTo(.68).Within(1e-12));
            Assert.That(UnresolvedCombatAccuracy.PhysicalHitProbability(100, 0), Is.EqualTo(1));
            Assert.That(UnresolvedCombatAccuracy.PhysicalHitProbability(0, 0), Is.Zero);
            Assert.That(UnresolvedCombatAccuracy.PhysicalHitProbability(100, 100), Is.Zero);
            foreach (var invalid in new[] { -1d, 101d, double.NaN, double.PositiveInfinity })
                Assert.Throws<System.ArgumentOutOfRangeException>(() => UnresolvedCombatAccuracy.PhysicalHitProbability(invalid, 15));
        }

        [Test]
        public void TemporaryMissConsumesActionApButDoesNotDamageAndRuleCanBeReplaced()
        {
            var world = new CombatWorld();
            var actor = new CombatUnit { Id = 100, Hp = 50, MaxHp = 50, Attack = 20 };
            var target = new CombatUnit { Id = 800, Hp = 50, MaxHp = 50, Defense = 5 };
            world.Add(actor); world.Add(target);
            var authority = new CombatServerBoundary(sample: () => .68);
            var flow = new CombatFlow(world, authority, true);
            Assert.That(flow.ConfigureAttackApCost(100, 2), Is.True);
            Assert.That(flow.ConfigurePhysicalAccuracy(100, 80, 0), Is.True);
            flow.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp);
            Assert.That(flow.CanStartAction(new CombatRequest(100, 800, CombatActionType.Attack), out _), Is.False);
            Assert.That(flow.ConfigurePhysicalAccuracy(800, 100, 15), Is.True);
            Assert.That(flow.TryExecute(new CombatRequest(100, 800, CombatActionType.Attack), out var result, out _), Is.True);
            Assert.That(result.Hit, Is.False); Assert.That(result.Damage, Is.Zero);
            Assert.That(target.Hp, Is.EqualTo(50)); Assert.That(actor.ActionPoints.Current, Is.EqualTo(GameConstants.BattleMaximumAp - 2));
            Assert.That(target.Alive, Is.True);
            var replacement = new CombatServerBoundary((accuracy, evasion) => 1);
            replacement.ConfigurePhysicalAccuracy(100, 80, 0); replacement.ConfigurePhysicalAccuracy(800, 100, 15);
            var changed = replacement.Resolve(world, new CombatRequest(100, 800, CombatActionType.Attack));
            Assert.That(changed.Hit, Is.True); Assert.That(changed.Damage, Is.EqualTo(15));
        }

        [Test]
        public void BattleRoot_AppliesExperienceWithoutEditingOpenScenes()
        {
            var active = SceneManager.GetActiveScene();
            var dirty = active.isDirty;
            var sceneCount = SceneManager.sceneCount;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Battle XP Integration Check");
                SceneManager.MoveGameObjectToScene(go, preview);
                var root = go.AddComponent<BattleSceneRoot>();
                // EditMode/preview objects do not need a forced Unity lifecycle call.
                Assert.That(root.ApplyExperience(new CombatExperienceResult(100, 3477000000u)), Is.True);
                Assert.That(root.PlayerExperience, Is.EqualTo(3477000000u));
                Assert.That(root.ApplyExperience(new CombatExperienceResult(100, 50)), Is.True);
                Assert.That(root.PlayerExperience, Is.EqualTo(50));
                Assert.That(root.ApplyExperience(new CombatExperienceResult(701, 999)), Is.False);
                Assert.That(root.PlayerExperience, Is.EqualTo(50));
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.EqualTo(dirty));
        }

        [TestCase(0u)]
        [TestCase(3477000000u)]
        [TestCase(uint.MaxValue)]
        public void Experience_AppliesFullUnsignedValueBeforeNotification(uint experience)
        {
            var world = new CombatWorld();
            var unit = new CombatUnit { Id = 700, Experience = 100 };
            world.Add(unit);
            var flow = new CombatFlow(world, new CombatServerBoundary());
            var notifications = 0;
            flow.ExperienceApplied += result =>
            {
                Assert.That(result.UnitId, Is.EqualTo(700));
                Assert.That(result.AbsoluteExperience, Is.EqualTo(experience));
                Assert.That(unit.Experience, Is.EqualTo(experience));
                notifications++;
            };
            Assert.That(flow.TryApplyExperience(new CombatExperienceResult(700, experience)), Is.True);
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void Experience_RepeatedAndLowerValuesReplaceRatherThanAdd()
        {
            var world = new CombatWorld();
            var unit = new CombatUnit { Id = 700, Experience = 100 };
            world.Add(unit);
            var flow = new CombatFlow(world, new CombatServerBoundary());
            flow.TryApplyExperience(new CombatExperienceResult(700, 500));
            flow.TryApplyExperience(new CombatExperienceResult(700, 500));
            Assert.That(unit.Experience, Is.EqualTo(500));
            flow.TryApplyExperience(new CombatExperienceResult(700, 50));
            Assert.That(unit.Experience, Is.EqualTo(50));
        }

        [Test]
        public void Experience_UnknownInstanceDoesNotNotifyOrChangeExistingUnit()
        {
            var world = new CombatWorld();
            var unit = new CombatUnit { Id = 700, Experience = 100 };
            world.Add(unit);
            var flow = new CombatFlow(world, new CombatServerBoundary());
            var notified = false;
            flow.ExperienceApplied += _ => notified = true;
            Assert.That(flow.TryApplyExperience(new CombatExperienceResult(701, 500)), Is.False);
            Assert.That(notified, Is.False);
            Assert.That(unit.Experience, Is.EqualTo(100));
        }

        [Test]
        public void Experience_DoesNotReviveOrAlterCombatStats()
        {
            var world = new CombatWorld();
            var unit = new CombatUnit { Id = 700, Hp = 0, Mp = 12, Attack = 26, Defense = 7,
                LifeState = UnitLifeState.Corpse };
            world.Add(unit);
            var flow = new CombatFlow(world, new CombatServerBoundary());
            Assert.That(flow.TryApplyExperience(new CombatExperienceResult(700, 500)), Is.True);
            Assert.That(unit.Hp, Is.Zero);
            Assert.That(unit.Mp, Is.EqualTo(12));
            Assert.That(unit.Attack, Is.EqualTo(26));
            Assert.That(unit.Defense, Is.EqualTo(7));
            Assert.That(unit.LifeState, Is.EqualTo(UnitLifeState.Corpse));
        }

        [Test]
        public void Execute_AppliesAbsoluteResultAndLeavesCorpse()
        {
            var world = new CombatWorld();
            world.Add(new CombatUnit { Id = 1, Hp = 50, MaxHp = 50, Mp = 10, Attack = 100, Defense = 0 });
            world.Add(new CombatUnit { Id = 2, Enemy = true, Hp = 20, MaxHp = 20, Attack = 1, Defense = 0 });
            var flow = new CombatFlow(world, new CombatServerBoundary());

            var result = flow.Execute(new CombatRequest(1, 2, CombatActionType.Magic, 101));

            Assert.That(result.AbsoluteHp, Is.Zero);
            Assert.That(world.Get(1).Mp, Is.Zero);
            Assert.That(world.Get(2).LifeState, Is.EqualTo(UnitLifeState.Corpse));
        }
    }
}
