using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class ScenarioCompletionTests
    {
        [TestCase(false, "01", false)]
        [TestCase(true, "", false)]
        [TestCase(true, " ", false)]
        [TestCase(true, "01", true)]
        public void CompletionRequiresConfirmedFlagAndNonEmptyKey(bool flag, string key, bool expected)
        {
            CheckSession((session, battle, objective) =>
            {
                var serialized = new SerializedObject(objective);
                serialized.FindProperty("clearsScenarioOnVictory").boolValue = flag;
                serialized.FindProperty("mapKey").stringValue = key;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(battle.ConfigureObjective(objective), Is.True);
                Assert.That(session.CaptureScenarioCompletion(battle), Is.False);
                battle.ConfigureActionPointCosts(2, 6); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerAttack(); // Fixture costs.
                Assert.That(battle.VictoryAchieved, Is.True);
                Assert.That(session.CaptureScenarioCompletion(battle), Is.EqualTo(expected));
                Assert.That(session.IsScenarioCleared(key), Is.EqualTo(expected));
                Assert.That(session.PlayerExperience, Is.Zero);
            });
        }

        [Test]
        public void ClearSurvivesBattleRemovalAndDoesNotRepeatOrReward()
        {
            CheckSession((session, battle, objective) =>
            {
                Assert.That(battle.ConfigureObjective(objective), Is.True);
                Assert.That(battle.RestorePlayerExperience(3477000000u), Is.True);
                Assert.That(session.CapturePlayerExperience(battle), Is.True);
                Assert.That(session.IsScenarioCleared("01"), Is.False);
                battle.ConfigureActionPointCosts(2, 6); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerAttack(); // Fixture costs.
                Assert.That(session.CaptureScenarioCompletion(battle), Is.True);
                Assert.That(session.ClearedScenarioCount, Is.EqualTo(1));
                Assert.That(session.CaptureScenarioCompletion(battle), Is.False);
                Assert.That(session.ClearedScenarioCount, Is.EqualTo(1));
                var hud = session.gameObject.AddComponent<DarkSaver.UI.WorldSessionHud>();
                Assert.That(hud.SummaryText, Does.Contain("시나리오 01: 완료"));
                Assert.That(hud.SummaryText, Does.Contain("완료 시나리오 1개"));
                Assert.That(session.PlayerExperience, Is.EqualTo(3477000000u));
                Object.DestroyImmediate(battle.gameObject);
                Assert.That(session.CaptureScenarioCompletion(battle), Is.False);
                Assert.That(session.CaptureScenarioCompletion(null), Is.False);
                Assert.That(session.IsScenarioCleared("01"), Is.True);
                Assert.That(session.IsScenarioCleared("02"), Is.False);
                var fresh = CreateInScene("Fresh session", session.gameObject.scene).AddComponent<BattleTransitionController>();
                Assert.That(fresh.IsScenarioCleared("01"), Is.False);
                Assert.That(fresh.ClearedScenarioCount, Is.Zero);
            });
        }

        [Test]
        public void OrdinaryVictoryDoesNotClearScenario()
        {
            CheckSession((session, battle, objective) =>
            {
                Assert.That(battle.ConfigureObjective(null), Is.True);
                battle.ConfigureActionPointCosts(2, 6); battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerAttack(); // Fixture costs.
                Assert.That(battle.VictoryAchieved, Is.True);
                Assert.That(battle.ClearedScenarioKey, Is.Empty);
                Assert.That(session.CaptureScenarioCompletion(battle), Is.False);
                Assert.That(session.IsScenarioCleared("01"), Is.False);
            });
        }

        private static void CheckSession(System.Action<BattleTransitionController, BattleSceneRoot, BattleObjectiveDefinition> check)
        {
            var active = SceneManager.GetActiveScene();
            var dirty = active.isDirty;
            var preview = EditorSceneManager.NewPreviewScene();
            var source = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>("Assets/_Game/Runtime/Combat/Brugos01Kisra.asset");
            var enemy = Object.Instantiate(source);
            var objective = Object.Instantiate(source.VictoryObjective);
            try
            {
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("useJobCatalogBaseStats").boolValue = false;
                serialized.FindProperty("baseHp").intValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var session = CreateInScene("Completion session", preview).AddComponent<BattleTransitionController>();
                var battle = CreateInScene("Completion battle", preview).AddComponent<BattleSceneRoot>();
                Assert.That(battle.ConfigureEnemy(enemy), Is.True);
                check(session, battle, objective);
                Assert.That(source.CreateUnit().Hp, Is.EqualTo(450));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(objective);
                EditorSceneManager.ClosePreviewScene(preview);
            }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.EqualTo(dirty));
        }

        private static GameObject CreateInScene(string name, Scene scene)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }
    }
}
