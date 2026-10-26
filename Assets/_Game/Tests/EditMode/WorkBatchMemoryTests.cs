using DarkSaver.EditorTools;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class WorkBatchMemoryTests
    {
        [Test]
        public void CleanupSkipsUnsavedSceneWithoutSavingOrClosingIt()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.CreateScene("Unsaved cleanup fixture");
            try {
                var marker = new GameObject("Unsaved cleanup guard fixture");
                SceneManager.MoveGameObjectToScene(marker, scene);
                EditorSceneManager.MarkSceneDirty(scene);
                string reason = WorkBatchMemoryMaintenance.BlockingReason();
                Assert.That(reason, Is.EqualTo("Unsaved scene").Or.EqualTo("Editor busy"));
                WorkBatchMemoryMaintenance.CollectIfIdle();
                Assert.That(WorkBatchMemoryMaintenance.LastReport, Is.EqualTo("Skipped: " + reason));
                Assert.That(scene.isLoaded && scene.isDirty && marker != null, Is.True);
                Assert.That(scene.path, Is.Empty);
            } finally {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
