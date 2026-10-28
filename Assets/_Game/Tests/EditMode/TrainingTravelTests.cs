using System;
using DarkSaver.Core;
using DarkSaver.Jobs;
using DarkSaver.Player;
using DarkSaver.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class TrainingTravelTests
    {
        [Test]
        public void ClassTierCannotInventUnreceivedTrainingMapAssignment()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
            for(int id=100;id<=172;id++)
            {
                Assert.IsFalse(TrainingEntrance2D.TryResolveTrainingMap(catalog,id,out var map),"job="+id);
                Assert.That(map,Is.Zero);
            }
        }

        [TestCase(165)] [TestCase(166)] [TestCase(167)] [TestCase(168)] [TestCase(169)]
        [TestCase(99)] [TestCase(170)] [TestCase(302)] [TestCase(-1)]
        public void FinalAndNonPlayerJobsCannotEnterTraining(int id)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
            Assert.That(TrainingEntrance2D.TryResolveTrainingMap(catalog, id, out var map), Is.False);
            Assert.That(map, Is.Zero);
        }

        [Test]
        public void MissingOrWrongCatalogDoesNotFallBackToFirstTier()
        {
            Assert.That(TrainingEntrance2D.TryResolveTrainingMap(null, 100, out _), Is.False);
            var catalog = ScriptableObject.CreateInstance<JobCatalog>();
            try {
                var values = new long[JobCatalogEntry.NumericColumnCount]; values[0] = 100;
                catalog.Initialize(new[] { new JobCatalogEntry(values, "", "fixture", "", "monster_job") });
                Assert.That(TrainingEntrance2D.TryResolveTrainingMap(catalog, 100, out var map), Is.False);
                Assert.That(map, Is.Zero);
            } finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [TestCase(101)] [TestCase(102)] [TestCase(103)] [TestCase(104)]
        [TestCase(105)] [TestCase(106)] [TestCase(107)]
        public void TrainingDestinationAndSaveKeepExactMapRuntimePair(int id)
        {
            Assert.That(WorldSceneCoordinator.TryResolveDestination("Training" + id, out var map, out var runtime, out var size), Is.True);
            Assert.That(map, Is.EqualTo("Map_" + id + "_;"));
            Assert.That(runtime, Is.EqualTo("Training" + id + "_Runtime")); Assert.That(size, Is.EqualTo(new Vector2(20, 15)));
            Assert.That(NativeSaveStore.IsSupportedMap(map, runtime), Is.True);
            Assert.That(NativeSaveStore.IsSupportedMap(map, "Town_Runtime"), Is.False);
            Assert.That(NativeSaveStore.IsSupportedMap(map, "Training" + (id == 107 ? 101 : id + 1) + "_Runtime"), Is.False);
            var save = new NativeSaveData { format = NativeSaveStore.Format, version = NativeSaveStore.Version,
                mapScene = map, runtimeScene = runtime, worldX = 3.5f, worldY = 4.5f,
                clearedScenarios = Array.Empty<string>(), hasCharacter = true,
                character = new NativeCharacterProfile { name = "훈련장", jobId = 100 } };
            try {
                Assert.That(NativeSaveStore.QueueResume(save, NativeSaveStore.DefaultPath), Is.True);
                Assert.That(NativeSaveStore.TakeResume(out var restored, out _), Is.True);
                Assert.That(restored.mapScene, Is.EqualTo(map)); Assert.That(restored.runtimeScene, Is.EqualTo(runtime));
                Assert.That(restored.character.name, Is.EqualTo("훈련장")); Assert.That(restored.worldX, Is.EqualTo(3.5f));
            } finally { NativeSaveStore.ClearResume(); }
        }

        [TestCase("Training100")] [TestCase("Training108")] [TestCase("Training0101")]
        [TestCase("training101")] [TestCase("Training101_Runtime")]
        public void InvalidTrainingTargetsAreRejected(string target)
        {
            Assert.That(WorldSceneCoordinator.TryResolveDestination(target, out _, out _, out _), Is.False);
        }
    }
}
