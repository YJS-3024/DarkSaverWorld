using System;
using System.IO;
using DarkSaver.Core;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class NativeOnboardingTests
    {
        [Test]
        public void NewProfileQueuesTutorialButLegacyProfileDoesNot()
        {
            try
            {
                Assert.IsTrue(NativeCharacterProfile.TryQueue("새 모험", 100, Path.Combine(Path.GetTempPath(), "onboarding-fixture.json"), out _));
                Assert.IsTrue(NativeCharacterProfile.TakePending(out var profile, out _));
                Assert.AreEqual(0, profile.tutorial.step);
                var clone = profile.Clone(); clone.tutorial.step = 4;
                Assert.AreEqual(0, profile.tutorial.step);
                var legacy = JsonUtility.FromJson<NativeCharacterProfile>("{\"name\":\"기존\",\"jobId\":100}");
                Assert.IsTrue(legacy.IsValid);
                string path = Path.Combine(Path.GetTempPath(), "ds-legacy-tutorial-" + Guid.NewGuid().ToString("N") + ".json");
                try
                {
                    File.WriteAllText(path, "{\"format\":\"DarkSaverWorld.NativeSave\",\"version\":1,\"mapScene\":\"Kaosia_Map\",\"runtimeScene\":\"Town_Runtime\",\"clearedScenarios\":[],\"hasCharacter\":true,\"character\":{\"name\":\"기존\",\"jobId\":100}}");
                    var store = new NativeSaveStore(path);
                    Assert.IsTrue(store.TryLoad(out var loaded, out var reason), reason);
                    Assert.IsNull(loaded.character.tutorial);
                    Assert.IsTrue(store.TrySave(loaded, out reason), reason);
                    Assert.IsTrue(store.TryLoad(out loaded, out reason), reason);
                    Assert.IsNull(loaded.character.tutorial);
                    Assert.IsTrue(NativeSaveStore.QueueResume(loaded, path));
                    Assert.IsTrue(NativeSaveStore.TakeResume(out loaded, out _));
                    Assert.IsNull(loaded.character.tutorial);
                }
                finally { NativeSaveStore.ClearResume(); File.Delete(path); File.Delete(path + ".bak"); }
            }
            finally { NativeCharacterProfile.ClearPending(); }
        }

        [Test]
        public void TutorialRequiresMovementAndRejectsStaleOrInvalidTransitions()
        {
            var source = new NativeTutorialProgress();
            Assert.IsTrue(source.TryAdvance(0, false, false, out var practice));
            Assert.IsFalse(practice.TryAdvance(1, false, false, out _));
            Assert.IsFalse(practice.TryAdvance(0, true, false, out _));
            Assert.IsTrue(practice.TryAdvance(1, true, false, out var town));
            Assert.AreEqual(2, town.step); Assert.AreEqual(1, practice.step);
            Assert.IsTrue(practice.TryAdvance(1, false, true, out var skipped));
            Assert.IsTrue(skipped.IsComplete);
            Assert.IsFalse(skipped.TryAdvance(4, true, false, out _));
            Assert.IsFalse(new NativeTutorialProgress { step = 5 }.IsValid);
        }

        [Test]
        public void RealJsonAndDiskPreserveTutorialAndRejectInvalidCheckpoint()
        {
            var folder = Path.Combine(Path.GetTempPath(), "ds-onboarding-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var store = new NativeSaveStore(Path.Combine(folder, "hero.json"));
                var data = new NativeSaveData { format = NativeSaveStore.Format, version = NativeSaveStore.Version,
                    mapScene = "Kaosia_Map", runtimeScene = "Town_Runtime", clearedScenarios = Array.Empty<string>(),
                    hasCharacter = true, character = new NativeCharacterProfile { name = "모험", jobId = 100, tutorial = new NativeTutorialProgress { step = 2 } } };
                Assert.IsTrue(store.TrySave(data, out _));
                Assert.IsTrue(store.TryLoad(out var restored, out _));
                Assert.AreEqual(2, restored.character.tutorial.step);
                data.character.tutorial.step = 99;
                Assert.IsFalse(store.TrySave(data, out _));
                Assert.IsTrue(store.TryLoad(out restored, out _));
                Assert.AreEqual(2, restored.character.tutorial.step);
                Assert.IsTrue(NativeCharacterSlots.IsOccupied(store.FilePath));
                Assert.IsFalse(NativeCharacterSlots.IsOccupied(Path.Combine(folder, "empty.json")));
                File.WriteAllText(Path.Combine(folder, "backup.json.bak"), "unreadable");
                Assert.IsTrue(NativeCharacterSlots.IsOccupied(Path.Combine(folder, "backup.json")));
            }
            finally { Directory.Delete(folder, true); }
        }

        [Test]
        public void SlotsHaveDistinctPathsAndKeepLegacySlot()
        {
            Assert.AreEqual(10,NativeCharacterSlots.Count);
            Assert.AreEqual(10,new System.Collections.Generic.HashSet<string>(NativeCharacterSlots.AllPaths()).Count);
            Assert.AreEqual("native-character-10.json",Path.GetFileName(NativeCharacterSlots.PathFor(9)));
            Assert.AreEqual(NativeSaveStore.DefaultPath, NativeCharacterSlots.PathFor(0));
            Assert.AreNotEqual(NativeCharacterSlots.PathFor(1), NativeCharacterSlots.PathFor(0));
            Assert.AreNotEqual(NativeCharacterSlots.PathFor(2), NativeCharacterSlots.PathFor(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => NativeCharacterSlots.PathFor(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => NativeCharacterSlots.PathFor(NativeCharacterSlots.Count));
        }
    }
}
