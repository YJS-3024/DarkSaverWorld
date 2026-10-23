using System;
using DarkSaver.Core;
using DarkSaver.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class ScenarioTravelTests
    {
        [Test]
        public void DestinationsAreExplicitAndUnknownTargetsDoNotDefaultToTown()
        {
            foreach (var target in new[] { "Kaosia", "WorldField1010", "Brugos01" })
            {
                Assert.That(WorldSceneCoordinator.TryResolveDestination(target, out var map, out var runtime, out var size), Is.True);
                Assert.That(NativeSaveStore.IsSupportedMap(map, runtime), Is.True);
                Assert.That(size.x, Is.GreaterThan(0f));
                if (target == "Brugos01")
                {
                    Assert.That(map, Is.EqualTo("Brugos01_Map"));
                    Assert.That(runtime, Is.EqualTo("Brugos01_Runtime"));
                    Assert.That(size, Is.EqualTo(new Vector2(40f, 40f)));
                }
            }
            foreach (var target in new[] { null, "", "Brugos02", "kaosia" })
            {
                Assert.That(WorldSceneCoordinator.TryResolveDestination(target, out var map, out var runtime, out var size), Is.False);
                Assert.That(map, Is.Null); Assert.That(runtime, Is.Null); Assert.That(size, Is.EqualTo(Vector2.zero));
            }
        }

        [Test]
        public void ScenarioSaveKeepsCharacterAndRejectsMismatchedRuntime()
        {
            var data = new NativeSaveData
            {
                format = NativeSaveStore.Format, version = NativeSaveStore.Version,
                mapScene = "Brugos01_Map", runtimeScene = "Brugos01_Runtime", worldX = 14.5f, worldY = 7.5f,
                clearedScenarios = Array.Empty<string>(), hasCharacter = true,
                character = new NativeCharacterProfile { name = "시나리오", jobId = 100 }
            };
            try
            {
                Assert.That(NativeSaveStore.IsValid(data), Is.True);
                Assert.That(NativeSaveStore.QueueResume(data, NativeSaveStore.DefaultPath), Is.True);
                data.character.name = "수정";
                Assert.That(NativeSaveStore.TakeResume(out var restored, out _), Is.True);
                Assert.That(restored.mapScene, Is.EqualTo("Brugos01_Map"));
                Assert.That(restored.character.name, Is.EqualTo("시나리오"));
                Assert.That(restored.worldX, Is.EqualTo(14.5f));
                data.runtimeScene = "Town_Runtime";
                Assert.That(NativeSaveStore.IsValid(data), Is.False);
                Assert.That(NativeSaveStore.QueueResume(data, NativeSaveStore.DefaultPath), Is.False);
                Assert.That(NativeSaveStore.TakeResume(out _, out _), Is.False);
            }
            finally { NativeSaveStore.ClearResume(); }
        }

        [Test]
        public void RejectedTravelDoesNotChangeSpawnOrStartCoroutine()
        {
            var oldSpawn = ScenePortal2D.RequestedSpawnId;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Travel check");
                SceneManager.MoveGameObjectToScene(root, preview);
                var coordinator = root.AddComponent<WorldSceneCoordinator>();
                ScenePortal2D.SetRequestedSpawn("kept");
                Assert.That(coordinator.TryTravelTo("Unknown", "DemoEntry"), Is.False);
                Assert.That(coordinator.TryTravelTo("Brugos01", ""), Is.False);
                Assert.That(coordinator.IsTravelling, Is.False);
                Assert.That(ScenePortal2D.RequestedSpawnId, Is.EqualTo("kept"));
            }
            finally
            {
                ScenePortal2D.SetRequestedSpawn(oldSpawn);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
