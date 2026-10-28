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
        public void MissingBuffResponseCannotAlterBaseStatsOrConsumeResources()
        {
            var world=new CombatWorld();
            var actor=NativeCombatStateFixture.Received(new CombatUnit{Id=100,Hp=50,MaxHp=100,Mp=17,MaxMp=30,Attack=20,Defense=4},20);
            var target=NativeCombatStateFixture.Received(new CombatUnit{Id=800,Enemy=true,Hp=75,MaxHp=100,Mp=11,MaxMp=30,Attack=12,Defense=7},20);
            world.Add(actor);world.Add(target);var authority=new CombatServerBoundary();var flow=new CombatFlow(world,authority,true);
            Assert.IsFalse(flow.ConfigureTemporaryBuffMagic(10101,3,5,10,20,60));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,100,CombatActionType.Magic,10101),out _,out _));
            Assert.IsFalse(flow.TryGetTemporaryBuff(100,true,out _,out _));
            Assert.AreEqual(20,authority.EffectiveAttack(actor));Assert.AreEqual(4,authority.EffectiveDefense(actor));
            Assert.IsFalse(flow.AdvanceActionPoints(1000));Assert.AreEqual(17,actor.Mp);Assert.AreEqual(20,actor.ActionPoints.Current);
        }

        [Test]
        public void HealingRequiresSeparateAbsoluteHpResponse()
        {
            var world=new CombatWorld();
            var actor=NativeCombatStateFixture.Received(new CombatUnit{Id=100,Hp=50,MaxHp=100,Mp=17,MaxMp=30,Attack=20,Defense=4},20);
            var target=NativeCombatStateFixture.Received(new CombatUnit{Id=800,Enemy=true,Hp=75,MaxHp=100,Mp=11,MaxMp=30,Attack=12,Defense=7},20);
            world.Add(actor);world.Add(target);var authority=new CombatServerBoundary();var flow=new CombatFlow(world,authority,true);
            Assert.IsFalse(flow.ConfigureTemporaryHealingMagic(301,3,5,25));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,100,CombatActionType.Magic,301),out _,out _));
            Assert.AreEqual(50,actor.Hp);Assert.AreEqual(17,actor.Mp);
            Assert.IsTrue(flow.TryApplyHp(100,90));Assert.AreEqual(90,actor.Hp);Assert.AreEqual(17,actor.Mp);
            Assert.IsFalse(flow.TryApplyHp(100,101));Assert.AreEqual(90,actor.Hp);
        }

        [Test]
        public void ExplicitMagicCostDoesNotSupplyMissingServerDamageFormula()
        {
            var world=new CombatWorld();
            var actor=NativeCombatStateFixture.Received(new CombatUnit{Id=100,Hp=50,MaxHp=100,Mp=17,MaxMp=30,Attack=20,Defense=4},20);
            var target=NativeCombatStateFixture.Received(new CombatUnit{Id=800,Enemy=true,Hp=75,MaxHp=100,Mp=11,MaxMp=30,Attack=12,Defense=7},20);
            world.Add(actor);world.Add(target);var authority=new CombatServerBoundary();var flow=new CombatFlow(world,authority,true);
            Assert.IsFalse(flow.ConfigureTemporaryDamageMagic(201,7,3,10,3));
            Assert.IsTrue(flow.ConfigureMagicApCost(201,3));
            Assert.IsFalse(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,201),out _,out _));
            Assert.AreEqual(75,target.Hp);Assert.AreEqual(17,actor.Mp);Assert.AreEqual(20,actor.ActionPoints.Current);
        }

        [Test]
        public void ExplicitRequestCostsCannotReenableRemovedRestOrDamageSimulator()
        {
            var world=new CombatWorld();
            var actor=NativeCombatStateFixture.Received(new CombatUnit{Id=100,Hp=50,MaxHp=100,Mp=17,MaxMp=30,Attack=20,Defense=4},20);
            var target=NativeCombatStateFixture.Received(new CombatUnit{Id=800,Enemy=true,Hp=75,MaxHp=100,Mp=11,MaxMp=30,Attack=12,Defense=7},20);
            world.Add(actor);world.Add(target);var authority=new CombatServerBoundary();var flow=new CombatFlow(world,authority,true);
            flow.TemporaryRulesEnabled=true;Assert.IsFalse(flow.TemporaryRulesEnabled);
            Assert.IsTrue(flow.ConfigureActionPointCosts(3,6));Assert.IsTrue(flow.ConfigureMagicApCost(101,6));
            Assert.AreEqual(3,flow.GetAttackApCost(100));Assert.AreEqual(6,flow.GetMagicApCost(101));
            Assert.IsFalse(flow.TryTemporaryRest(100));Assert.IsFalse(flow.TryExecute(new CombatRequest(100,800,CombatActionType.Attack),out _,out _));
            Assert.AreEqual(50,actor.Hp);Assert.AreEqual(17,actor.Mp);Assert.AreEqual(75,target.Hp);
        }

        [Test]
        public void MissingHitVerdictNeverInventsPhysicalHitProbability()
        {
            foreach(var input in new[]{(80d,15d),(0d,0d),(100d,100d),(-1d,15d),(double.NaN,15d)})
                Assert.Throws<System.NotSupportedException>(()=>UnresolvedCombatAccuracy.PhysicalHitProbability(input.Item1,input.Item2));
        }

        [Test]
        public void RequestAnticipationDoesNotTreatMissingVerdictAsMissOrDamage()
        {
            var world=new CombatWorld();
            var actor=NativeCombatStateFixture.Received(new CombatUnit{Id=100,Hp=50,MaxHp=100,Mp=17,MaxMp=30,Attack=20,Defense=4},20);
            var target=NativeCombatStateFixture.Received(new CombatUnit{Id=800,Enemy=true,Hp=75,MaxHp=100,Mp=11,MaxMp=30,Attack=12,Defense=7},20);
            world.Add(actor);world.Add(target);var authority=new CombatServerBoundary();var flow=new CombatFlow(world,authority,true);
            Assert.IsFalse(flow.ConfigurePhysicalAccuracy(100,100,0));Assert.IsTrue(flow.ConfigureActionPointCosts(3,6));
            Assert.IsTrue(flow.TryStartAction(new CombatRequest(100,800,CombatActionType.Attack),out _));
            Assert.AreEqual(17,actor.ActionPoints.Current);Assert.AreEqual(75,target.Hp);
            Assert.Throws<System.NotSupportedException>(()=>authority.Resolve(world,new CombatRequest(100,800,CombatActionType.Attack)));
            Assert.AreEqual(17,actor.ActionPoints.Current);Assert.AreEqual(75,target.Hp);
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
                var root = go.AddComponent<BattleSceneRoot>(); NativeCombatStateFixture.Initialize(root,6);
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
            var unit = NativeCombatStateFixture.Received(new CombatUnit { Id = 700, Experience = 100 });
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
            var unit = NativeCombatStateFixture.Received(new CombatUnit { Id = 700, Experience = 100 });
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
            var unit = NativeCombatStateFixture.Received(new CombatUnit { Id = 700, Experience = 100 });
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
            var unit = NativeCombatStateFixture.Received(new CombatUnit { Id = 700, Hp = 0, Mp = 12, Attack = 26, Defense = 7,
                LifeState = UnitLifeState.Corpse });
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
        public void MissingVerdictCannotReplaceIndependentHpAndCorpseResponses()
        {
            var world=new CombatWorld();
            var actor=NativeCombatStateFixture.Received(new CombatUnit{Id=100,Hp=50,MaxHp=100,Mp=17,MaxMp=30,Attack=20,Defense=4},20);
            var target=NativeCombatStateFixture.Received(new CombatUnit{Id=800,Enemy=true,Hp=75,MaxHp=100,Mp=11,MaxMp=30,Attack=12,Defense=7},20);
            world.Add(actor);world.Add(target);var authority=new CombatServerBoundary();var flow=new CombatFlow(world,authority,true);
            Assert.Throws<System.InvalidOperationException>(()=>flow.Execute(new CombatRequest(100,800,CombatActionType.Magic,101)));
            Assert.AreEqual(75,target.Hp);Assert.AreEqual(17,actor.Mp);Assert.AreEqual(UnitLifeState.Active,target.LifeState);
            Assert.IsTrue(flow.TryApplyHp(800,0));Assert.AreEqual(UnitLifeState.Active,target.LifeState);
            Assert.IsTrue(flow.TryApplyDeath(800,true,preserveReceivedHp:true));Assert.AreEqual(UnitLifeState.Corpse,target.LifeState);
        }
    }
}
