using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class BattleEncounterTests
    {
        [Test]
        public void RejectedEncounterDoesNotConsumeArmedState()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Encounter check");
                SceneManager.MoveGameObjectToScene(root, scene);
                var encounter = root.AddComponent<BattleEncounter2D>();
                Assert.That(encounter.IsArmed, Is.True);
                Assert.That(encounter.TryEncounter(null), Is.False);
                Assert.That(encounter.IsArmed, Is.True);
                encounter.enabled = false;
                Assert.That(encounter.TryEncounter(null), Is.False);
                Assert.That(encounter.IsArmed, Is.True);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void InactiveSessionRejectsEntryWithoutChangingState()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Entry check");
                SceneManager.MoveGameObjectToScene(root, scene);
                var session = root.AddComponent<BattleTransitionController>();
                session.enabled = false;
                Assert.That(session.TryEnterBattle(), Is.False);
                Assert.That(session.IsTransitioning, Is.False);
                Assert.That(session.PlayerExperience, Is.EqualTo(0u));
                Assert.That(session.HasPlayerVitals, Is.False);
                Assert.That(session.ClearedScenarioCount, Is.EqualTo(0));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
