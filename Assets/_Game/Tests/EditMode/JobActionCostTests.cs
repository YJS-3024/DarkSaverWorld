using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class JobActionCostTests
    {
        [Test]
        public void ExplicitJobAndMagicLookupsDoNotUseUnknownIds()
        {
            Assert.That(GameConstants.GetBattleAttackApCost(100), Is.EqualTo(GameConstants.BattleJob100AttackApCost));
            Assert.That(GameConstants.GetBattleAttackApCost(701), Is.EqualTo(GameConstants.BattleJob701AttackApCost));
            Assert.That(GameConstants.GetBattleMagicApCost(101), Is.EqualTo(GameConstants.BattleMagic101ApCost));
            foreach (var id in new[] { -1, 0, 700, 99999 })
            {
                Assert.That(GameConstants.GetBattleAttackApCost(id), Is.EqualTo(0));
                Assert.That(GameConstants.GetBattleMagicApCost(id), Is.EqualTo(0));
            }
        }

        [Test]
        public void JobBindingIsPerInstanceAndUnknownJobClearsPreviousCost()
        {
            var world = new CombatWorld();
            world.Add(NativeCombatStateFixture.Received(new CombatUnit { Id = 100 })); world.Add(NativeCombatStateFixture.Received(new CombatUnit { Id = 700 }));
            var flow = new CombatFlow(world, new CombatServerBoundary(), true);
            // Fixture values only; no constants or native assets are modified.
            Assert.That(flow.ConfigureActionPointCosts(2, 6), Is.True);
            Assert.IsTrue(flow.ConfigureMagicApCost(101,6)); // Explicit received fixture cost; no prototype-ID inference.
            Assert.That(flow.ConfigureAttackApCostForJob(100, 100), Is.True);
            Assert.That(flow.GetAttackApCost(100), Is.EqualTo(GameConstants.BattleJob100AttackApCost));
            Assert.That(flow.GetAttackApCost(700), Is.EqualTo(2));
            Assert.That(flow.ConfigureAttackApCostForJob(700, 701), Is.True);
            Assert.That(flow.GetAttackApCost(700), Is.EqualTo(GameConstants.BattleJob701AttackApCost));
            Assert.That(flow.ConfigureAttackApCostForJob(100, 99999), Is.True);
            Assert.That(flow.GetAttackApCost(100), Is.EqualTo(0));
            Assert.That(flow.ConfigureAttackApCostForJob(700, -1), Is.False);
            Assert.That(flow.ConfigureAttackApCostForJob(999, 100), Is.False);
            Assert.That(flow.GetMagicApCost(101), Is.EqualTo(6));
            Assert.That(flow.GetMagicApCost(99999), Is.EqualTo(0));
        }

        [Test]
        public void KisraUsesJob701NotInstance700AndPlayerBindPreservesVitals()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Job binding check"); SceneManager.MoveGameObjectToScene(root, scene);
                var battle = root.AddComponent<BattleSceneRoot>(); NativeCombatStateFixture.Initialize(battle,6);
                var hp = battle.PlayerHp; var mp = battle.PlayerMp; var ap = battle.PlayerAp;
                Assert.That(battle.ConfigurePlayerJob(100), Is.True);
                Assert.That(battle.ConfigurePlayerJob(0), Is.False);
                Assert.That(battle.PlayerJobId, Is.EqualTo(100));
                var enemy = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>("Assets/_Game/Runtime/Combat/Brugos01Kisra.asset");
                Assert.That(battle.ConfigureEnemy(enemy), Is.True);
                Assert.That(battle.EnemyJobId, Is.EqualTo(701));
                Assert.That(battle.EnemyHp, Is.EqualTo(450));
                Assert.That(battle.PlayerAttackApCost, Is.EqualTo(GameConstants.BattleJob100AttackApCost));
                Assert.That(battle.EnemyAttackApCost, Is.EqualTo(GameConstants.BattleJob701AttackApCost));
                Assert.That(battle.SelectedMagicApCost, Is.EqualTo(GameConstants.BattleMagic101ApCost));
                Assert.That(battle.PlayerHp, Is.EqualTo(hp)); Assert.That(battle.PlayerMp, Is.EqualTo(mp));
                Assert.That(battle.PlayerAp, Is.EqualTo(ap)); Assert.That(battle.PlayerExperience, Is.EqualTo(0u));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
