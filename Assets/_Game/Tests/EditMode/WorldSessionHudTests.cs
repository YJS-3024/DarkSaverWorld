using DarkSaver.Combat;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class WorldSessionHudTests
    {
        [TestCase(0u)]
        [TestCase(3477000000u)]
        [TestCase(uint.MaxValue)]
        public void HudReadsSessionWithoutChangingExperienceOrCompletion(uint experience)
        {
            var active = SceneManager.GetActiveScene();
            var dirty = active.isDirty;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("HUD test");
                SceneManager.MoveGameObjectToScene(go, preview);
                var hud = go.AddComponent<WorldSessionHud>();
                var session = go.GetComponent<BattleTransitionController>();
                Assert.That(session, Is.Not.Null);
                var battleGo = new GameObject("XP test source");
                SceneManager.MoveGameObjectToScene(battleGo, preview);
                var battle = battleGo.AddComponent<BattleSceneRoot>();
                Assert.That(battle.RestorePlayerExperience(experience), Is.True);
                Assert.That(session.CapturePlayerExperience(battle), Is.True);
                Assert.That(hud.SummaryText, Does.Contain("경험치 " + experience));
                Assert.That(hud.SummaryText, Does.Contain("완료 시나리오 0개"));
                Assert.That(hud.SummaryText, Does.Contain("시나리오 01: 미완료"));
                Assert.That(hud.IsVisible, Is.True);
                Assert.That(session.PlayerExperience, Is.EqualTo(experience));
                Assert.That(session.ClearedScenarioCount, Is.Zero);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
