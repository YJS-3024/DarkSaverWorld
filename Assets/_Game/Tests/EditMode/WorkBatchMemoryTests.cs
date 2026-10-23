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
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try {
                var marker = new GameObject("Unsaved cleanup guard fixture");
                SceneManager.MoveGameObjectToScene(marker, scene);
                EditorSceneManager.MarkSceneDirty(scene);
                Assert.That(WorkBatchMemoryMaintenance.BlockingReason(), Is.EqualTo("Unsaved scene"));
                WorkBatchMemoryMaintenance.CollectIfIdle();
                Assert.That(WorkBatchMemoryMaintenance.LastReport, Is.EqualTo("Skipped: Unsaved scene"));
                Assert.That(scene.isLoaded && scene.isDirty && marker != null, Is.True);
                Assert.That(scene.path, Is.Empty);
            } finally {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
