using System;
using System.IO;
using DarkSaver.Core;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class NativeSaveStoreTests
    {
        private static NativeSaveData Sample() => new NativeSaveData
        {
            format = NativeSaveStore.Format, version = NativeSaveStore.Version,
            mapScene = "Kaosia_Map", runtimeScene = "Town_Runtime", worldX = 20.5f, worldY = 18.5f,
            experience = uint.MaxValue, hasVitals = true, hp = 37, maxHp = 120, mp = 30, maxMp = 30,
            lifeState = (int)UnitLifeState.Active, clearedScenarios = new[] { "01", "검사" }
        };

        [Test]
        public void IoFailureCanRetryWithoutPublishingAndDiagnosticsDoNotExposePath()
        {
            var directory=Path.Combine(Path.GetTempPath(),"ds-save-retry-"+System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"preserve");
                var store=new NativeSaveStore(Path.Combine(blocked,"slot.json"));
                Assert.That(store.TrySave(Sample(),out _),Is.False);Assert.That(store.CanRetryLastWrite,Is.True);
                Assert.That(store.LastWriteDiagnostic,Does.Contain("IOException").And.Contain("0x").And.Not.Contain(directory));
                Assert.That(File.ReadAllText(blocked),Is.EqualTo("preserve"));Assert.That(Directory.GetFiles(directory).Length,Is.EqualTo(1));
                File.Delete(blocked);Directory.CreateDirectory(blocked);
                Assert.That(store.TrySave(Sample(),out var reason),Is.True,reason);
                Assert.That(store.CanRetryLastWrite,Is.False);Assert.That(store.LastWriteDiagnostic,Is.Empty);
                Assert.That(store.TryLoad(out _,out reason),Is.True,reason);
            }
            finally{Directory.Delete(directory,true);}
        }

        [Test]
        public void ChecksumFailureRecoversBackupAndPreservesItOnNextSave()
        {
            Files(store =>
            {
                var data = Sample(); Assert.That(store.TrySave(data, out _), Is.True);
                data.experience = 12; Assert.That(store.TrySave(data, out _), Is.True);
                var backup = File.ReadAllText(store.FilePath + ".bak");
                // Valid JSON and valid state values still fail the unchanged checksum.
                var text = File.ReadAllText(store.FilePath);
                Assert.That(text.Contains("sha256"), Is.True);
                File.WriteAllText(store.FilePath, text.Replace("experience\\\":12", "experience\\\":13"));
                Assert.That(File.ReadAllText(store.FilePath), Is.Not.EqualTo(text));
                Assert.That(store.TryLoad(out var recovered, out _), Is.True);
                Assert.That(recovered.experience, Is.EqualTo(uint.MaxValue));
                Assert.That(store.TrySave(recovered, out _), Is.True);
                Assert.That(File.ReadAllText(store.FilePath + ".bak"), Is.EqualTo(backup));
            });
        }

        [Test]
        public void FutureVersionDoesNotFallBackToOlderValidBackup()
        {
            Files(store =>
            {
                Assert.That(store.TrySave(Sample(), out _), Is.True);
                Assert.That(store.TrySave(Sample(), out _), Is.True);
                File.WriteAllText(store.FilePath, "{\"format\":\"DarkSaverWorld.NativeSave\",\"version\":99}");
                Assert.That(store.TryLoad(out _, out _), Is.False);
                Assert.That(store.TrySave(Sample(), out _), Is.False);
            });
        }

        [Test]
        public void LegacyPlainSaveLoadsAndUpgradesWithoutChangingItsState()
        {
            Files(store =>
            {
                var source = Sample(); File.WriteAllText(store.FilePath, JsonUtility.ToJson(source));
                Assert.That(store.TryLoad(out var legacy, out _), Is.True);
                Assert.That(store.TrySave(legacy, out _), Is.True);
                Assert.That(store.TryLoad(out var upgraded, out _), Is.True);
                Assert.That(upgraded.tutorialPresenceVersion, Is.EqualTo(1));
                source.tutorialPresenceVersion = 1;
                Assert.That(JsonUtility.ToJson(upgraded), Is.EqualTo(JsonUtility.ToJson(source)));
                Assert.That(File.ReadAllText(store.FilePath), Does.Contain("sha256"));
            });
        }

        [Test]
        public void BattleMagicSelectionRoundTripsWithoutDeletingLearnedLevels()
        {
            Files(store => {
                var data=Sample();data.hasCharacter=true;
                var profile=new NativeCharacterProfile {name="검증",jobId=100,hasClassAbilities=true,learnedMagicIds=new[]{101,102},usableSkillIds=Array.Empty<int>()};
                Assert.IsTrue(profile.TryBuildBattleMagicSelection(new[]{101},out data.character));
                Assert.IsTrue(store.TrySave(data,out _));Assert.IsTrue(store.TryLoad(out var loaded,out _));
                CollectionAssert.AreEqual(new[]{101,102},loaded.character.learnedMagicIds);CollectionAssert.AreEqual(new[]{101},loaded.character.battleMagicIds);
                Assert.IsTrue(loaded.character.TryBuildBattleMagicSelection(Array.Empty<int>(),out data.character));
                Assert.IsTrue(store.TrySave(data,out _));Assert.IsTrue(store.TryLoad(out loaded,out _));
                Assert.IsTrue(loaded.character.hasBattleMagicSelection);Assert.IsEmpty(loaded.character.battleMagicIds);
            });
        }

        [Test]
        public void RoundTripPreservesNativeStateAndUnsignedExperience()
        {
            Files(store =>
            {
                Assert.That(store.TryLoad(out var missing, out _), Is.False);
                Assert.That(missing, Is.Null);
                var source = Sample();
                Assert.That(store.TrySave(source, out _), Is.True);
                Assert.That(store.TryLoad(out var loaded, out _), Is.True);
                Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(JsonUtility.ToJson(source)));
                Assert.That(loaded.experience, Is.EqualTo(uint.MaxValue));
            });
        }

        [Test]
        public void ReplacementKeepsPreviousValidSnapshotAsBackup()
        {
            Files(store =>
            {
                var data = Sample(); Assert.That(store.TrySave(data, out _), Is.True);
                data.experience = 12; Assert.That(store.TrySave(data, out _), Is.True);
                var backup = new NativeSaveStore(store.FilePath + ".bak");
                Assert.That(backup.TryLoad(out var old, out _), Is.True);
                Assert.That(old.experience, Is.EqualTo(uint.MaxValue));
                Assert.That(store.TryLoad(out var latest, out _), Is.True);
                Assert.That(latest.experience, Is.EqualTo(12u));
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(store.FilePath), "*.tmp-*"), Is.Empty);
            });
        }

        [TestCase("broken JSON")]
        [TestCase("{\"format\":\"DarkSaverWorld.NativeSave\",\"version\":99}")]
        public void UnreadableOrFutureSlotIsNeverOverwritten(string contents)
        {
            Files(store =>
            {
                File.WriteAllText(store.FilePath, contents);
                Assert.That(store.TryLoad(out var loaded, out _), Is.False);
                Assert.That(loaded, Is.Null);
                Assert.That(store.TrySave(Sample(), out _), Is.False);
                Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(contents));
                Assert.That(File.Exists(store.FilePath + ".bak"), Is.False);
            });
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void InvalidSnapshotCannotReplaceValidSlot(int kind)
        {
            Files(store =>
            {
                Assert.That(store.TrySave(Sample(), out _), Is.True);
                var original = File.ReadAllText(store.FilePath); var bad = Sample();
                switch (kind)
                {
                    case 0: bad.version++; break;
                    case 1: bad.runtimeScene = "Field_Runtime"; break;
                    case 2: bad.worldX = float.NaN; break;
                    case 3: bad.worldY = float.PositiveInfinity; break;
                    case 4: bad.hp = 121; break;
                    case 5: bad.lifeState = (int)UnitLifeState.Corpse; break;
                    case 6: bad.clearedScenarios = new[] { "01", "01" }; break;
                    case 7: bad.clearedScenarios = new[] { " " }; break;
                }
                Assert.That(NativeSaveStore.IsValid(bad), Is.False);
                Assert.That(store.TrySave(bad, out _), Is.False);
                Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(original));
            });
        }

        [Test]
        public void OversizedSlotIsRejectedWithoutReadingOrOverwritingIt()
        {
            Files(store =>
            {
                File.WriteAllText(store.FilePath, new string('x', 1024 * 1024 + 1));
                Assert.That(store.TryLoad(out _, out _), Is.False);
                Assert.That(store.TrySave(Sample(), out _), Is.False);
                Assert.That(new FileInfo(store.FilePath).Length, Is.EqualTo(1024 * 1024 + 1));
            });
        }

        [Test]
        public void QueueCopiesSnapshotAndConsumesItOnce()
        {
            Files(store =>
            {
                try
                {
                    var data = Sample(); Assert.That(NativeSaveStore.QueueResume(data, store.FilePath), Is.True);
                    data.clearedScenarios[0] = "changed"; data.hp = 1;
                    Assert.That(NativeSaveStore.TakeResume(out var queued, out var path), Is.True);
                    Assert.That(queued.hp, Is.EqualTo(37)); Assert.That(queued.clearedScenarios[0], Is.EqualTo("01"));
                    Assert.That(path, Is.EqualTo(store.FilePath));
                    Assert.That(NativeSaveStore.TakeResume(out _, out _), Is.False);
                }
                finally { NativeSaveStore.ClearResume(); }
            });
        }

        [Test]
        public void RejectedQueueCannotLeaveStaleResume()
        {
            Files(store =>
            {
                try
                {
                    Assert.That(NativeSaveStore.QueueResume(Sample(), store.FilePath), Is.True);
                    Assert.That(NativeSaveStore.QueueResume(null, store.FilePath), Is.False);
                    Assert.That(NativeSaveStore.TakeResume(out _, out _), Is.False);
                    Assert.That(NativeSaveStore.QueueResume(Sample(), null), Is.False);
                    Assert.That(NativeSaveStore.TakeResume(out _, out _), Is.False);
                }
                finally { NativeSaveStore.ClearResume(); }
            });
        }

        [TestCase(false)] [TestCase(true)]
        public void RestoredSessionPreservesWoundsOrDefeatWithoutReviving(bool defeated)
        {
            Session(session =>
            {
                var data = Sample();
                data.mapScene = "World1010_Map"; data.runtimeScene = "Field_Runtime";
                if (defeated) { data.hp = 0; data.lifeState = (int)UnitLifeState.Corpse; }
                Assert.That(session.RestoreSaveState(data), Is.True);
                Assert.That(session.PlayerHp, Is.EqualTo(data.hp)); Assert.That(session.PlayerMp, Is.EqualTo(30));
                Assert.That(session.CanEnterBattle, Is.EqualTo(!defeated));
                Assert.That(session.PlayerExperience, Is.EqualTo(uint.MaxValue));
                Assert.That(session.IsScenarioCleared("01"), Is.True);
                data.clearedScenarios[0] = "changed";
                var copy = session.CaptureSaveState("World1010_Map", "Field_Runtime", new Vector2(40.5f, 60.5f));
                Assert.That(copy.clearedScenarios, Does.Contain("01"));
                Assert.That(copy.lifeState, Is.EqualTo(defeated ? 1 : 0));
                Assert.That(NativeSaveStore.IsValid(copy), Is.True);
            });
        }

        [Test]
        public void InvalidRestoreLeavesSessionUnchanged()
        {
            Session(session =>
            {
                Assert.That(session.RestoreSaveState(Sample()), Is.True);
                var before = JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.zero));
                var bad = Sample(); bad.hp = -1; bad.experience = 0;
                Assert.That(session.RestoreSaveState(bad), Is.False);
                Assert.That(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.zero)), Is.EqualTo(before));
            });
        }

        [Test]
        public void WorldSaveWithoutPlayerOrMapDoesNotCreateSlot()
        {
            Files(store => Session(session =>
            {
                session.ConfigureSaveFile(store.FilePath);
                Assert.That(session.SaveWorld(), Is.False);
                Assert.That(File.Exists(store.FilePath), Is.False);
            }));
        }

        private static void Files(Action<NativeSaveStore> check)
        {
            var directory = Path.Combine(Path.GetTempPath(), "DarkSaverNativeSaveChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var store = new NativeSaveStore(Path.Combine(directory, "slot.json"));
            try { check(store); }
            finally
            {
                // Only the two exact files owned by this fixture, never a recursive delete.
                if (File.Exists(store.FilePath)) File.Delete(store.FilePath);
                if (File.Exists(store.FilePath + ".bak")) File.Delete(store.FilePath + ".bak");
                Directory.Delete(directory, false);
            }
        }

        private static void Session(Action<BattleTransitionController> check)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty; var count = SceneManager.sceneCount;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Native save session test"); SceneManager.MoveGameObjectToScene(go, preview);
                check(go.AddComponent<BattleTransitionController>());
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.EqualTo(dirty)); Assert.That(SceneManager.sceneCount, Is.EqualTo(count));
        }
    }
}
