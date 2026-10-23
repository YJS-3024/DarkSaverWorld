using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class BattleExperienceSessionTests
    {
        [TestCase(0u)]
        [TestCase(3477000000u)]
        [TestCase(uint.MaxValue)]
        public void ExperienceSurvivesBattleRootReplacement(uint experience)
        {
            var active = SceneManager.GetActiveScene();
            var dirty = active.isDirty;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject Create(string name)
                {
                    var go = new GameObject(name);
                    SceneManager.MoveGameObjectToScene(go, preview);
                    return go;
                }
                var session = Create("XP session").AddComponent<BattleTransitionController>();
                var first = Create("First battle").AddComponent<BattleSceneRoot>();
                Assert.That(first.RestorePlayerExperience(experience), Is.True);
                Assert.That(session.CapturePlayerExperience(first), Is.True);
                Object.DestroyImmediate(first.gameObject);
                Assert.That(session.PlayerExperience, Is.EqualTo(experience));
                Assert.That(session.CapturePlayerExperience(first), Is.False);
                Assert.That(session.PlayerExperience, Is.EqualTo(experience));
                var second = Create("Second battle").AddComponent<BattleSceneRoot>();
                Assert.That(session.RestorePlayerExperience(second), Is.True);
                Assert.That(second.PlayerExperience, Is.EqualTo(experience));
                Assert.That(session.RestorePlayerExperience(second), Is.True);
                Assert.That(second.PlayerExperience, Is.EqualTo(experience));
                Assert.That(second.PlayerHp, Is.EqualTo(120));
                Assert.That(session.CapturePlayerExperience(null), Is.False);
                Assert.That(session.RestorePlayerExperience(null), Is.False);
                var fresh = Create("New session").AddComponent<BattleTransitionController>();
                Assert.That(fresh.PlayerExperience, Is.Zero);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
