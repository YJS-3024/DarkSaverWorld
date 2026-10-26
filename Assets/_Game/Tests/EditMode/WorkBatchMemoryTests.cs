using System.IO;
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
            var scene = EditModeSceneFixture.Create("Unsaved cleanup fixture");
            var originalBytes=File.ReadAllBytes(scene.path);
            try {
                var marker = new GameObject("Unsaved cleanup guard fixture");
                SceneManager.MoveGameObjectToScene(marker, scene);
                EditorSceneManager.MarkSceneDirty(scene);
                string reason = WorkBatchMemoryMaintenance.BlockingReason();
                Assert.That(reason, Is.EqualTo("Unsaved scene").Or.EqualTo("Editor busy"));
                WorkBatchMemoryMaintenance.CollectIfIdle();
                Assert.That(WorkBatchMemoryMaintenance.LastReport, Is.EqualTo("Skipped: " + reason));
                Assert.That(scene.isLoaded && scene.isDirty && marker != null, Is.True);
                Assert.That(scene.path,Does.Contain("SceneFixture-"));
                Assert.That(File.ReadAllBytes(scene.path),Is.EqualTo(originalBytes),"cleanup must not save the dirty fixture");
            } finally {
                EditModeSceneFixture.Close(scene);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
