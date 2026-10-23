using System;
using System.IO;
using DarkSaver.Core;
using DarkSaver.Combat;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class NativeCharacterTests
    {
        private static NativeSaveData Sample() => new NativeSaveData
        {
            format = NativeSaveStore.Format, version = NativeSaveStore.Version,
            mapScene = "Kaosia_Map", runtimeScene = "Town_Runtime", clearedScenarios = Array.Empty<string>()
        };

        [Test]
        public void BattleMagicSelectionIsOwnedBoundedAndIndependent()
        {
            var source=new NativeCharacterProfile {name="검증",jobId=100,hasClassAbilities=true,
                learnedMagicIds=new[]{101,102,1101},usableSkillIds=Array.Empty<int>()};
            var ids=new[]{102,101};Assert.IsTrue(source.TryBuildBattleMagicSelection(ids,out var selected));
            ids[0]=999;CollectionAssert.AreEqual(new[]{102,101},selected.battleMagicIds);
            Assert.IsFalse(source.hasBattleMagicSelection);CollectionAssert.AreEqual(source.learnedMagicIds,selected.learnedMagicIds);
            var copy=selected.Clone();copy.battleMagicIds[0]=1101;Assert.AreEqual(102,selected.battleMagicIds[0]);
            Assert.IsTrue(selected.TryBuildBattleMagicSelection(Array.Empty<int>(),out var empty));Assert.IsTrue(empty.hasBattleMagicSelection);
            foreach(var bad in new[]{new[]{999},new[]{101,101},new[]{0}})Assert.IsFalse(source.TryBuildBattleMagicSelection(bad,out _));
            Assert.IsFalse(source.TryBuildBattleMagicSelection(null,out _));
            source.learnedMagicIds=new[]{1,2,3,4,5,6,7,8,9};Assert.IsFalse(source.TryBuildBattleMagicSelection(source.learnedMagicIds,out _));
        }

        [Test]
        public void NameAndJobValidationRejectsUnimplementedProfiles()
        {
            foreach (var name in new[] { "카오스", "Hero 1", "전사" })
                Assert.That(new NativeCharacterProfile { name = name, jobId = 100 }.IsValid, Is.True);
            foreach (var name in new[] { null, "", " ", " 앞", "뒤 ", "줄\n바꿈", new string('가', GameConstants.CharacterNameMaxLength + 1) })
                Assert.That(new NativeCharacterProfile { name = name, jobId = 100 }.IsValid, Is.False);
            Assert.That(new NativeCharacterProfile { name = "전사", jobId = 200 }.IsValid, Is.False);
        }

        [Test]
        public void PendingCreationIsConsumedOnceAndExclusiveWithResume()
        {
            try
            {
                Assert.That(NativeCharacterProfile.TryQueue("  카오스  ", 100, "Tools/SaveChecks/character-fixture.json", out _), Is.True);
                Assert.That(NativeSaveStore.TakeResume(out _, out _), Is.False);
                Assert.That(NativeCharacterProfile.TakePending(out var profile, out var path), Is.True);
                Assert.That(profile.name, Is.EqualTo("카오스"));
                Assert.That(Path.IsPathRooted(path), Is.True);
                Assert.That(NativeCharacterProfile.TakePending(out _, out _), Is.False);
                Assert.That(NativeCharacterProfile.TryQueue("전사", 100, path, out _), Is.True);
                Assert.That(NativeSaveStore.QueueResume(Sample(), path), Is.True);
                Assert.That(NativeCharacterProfile.TakePending(out _, out _), Is.False);
                Assert.That(NativeSaveStore.TakeResume(out _, out _), Is.True);
                NativeCharacterProfile.TryQueue("전사", 100, path, out _);
                Assert.That(NativeCharacterProfile.TryQueue("", 100, path, out _), Is.False);
                Assert.That(NativeCharacterProfile.TakePending(out _, out _), Is.False);
            }
            finally { NativeSaveStore.ClearResume(); NativeCharacterProfile.ClearPending(); }
        }

        [Test]
        public void ProfileSessionCopiesAndRestoresWithoutChangingLegacyState()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("character session fixture"); SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var session = root.AddComponent<BattleTransitionController>();
                var profile = new NativeCharacterProfile { name = "카오스", jobId = 100 };
                Assert.That(session.InitializeCharacter(profile), Is.True);
                profile.name = "변경";
                var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                Assert.That(save.character.name, Is.EqualTo("카오스"));
                save.character.name = "별도";
                Assert.That(session.PlayerName, Is.EqualTo("카오스"));
                Assert.That(session.InitializeCharacter(profile), Is.False);
                Assert.That(session.RestoreSaveState(save), Is.True);
                save.character.name = "다시 변경";
                Assert.That(session.PlayerName, Is.EqualTo("별도"));
                var invalid = Sample(); invalid.character = new NativeCharacterProfile { name = "", jobId = 100 };
                Assert.That(session.RestoreSaveState(invalid), Is.False);
                Assert.That(session.PlayerName, Is.EqualTo("별도"));
                Assert.That(session.RestoreSaveState(Sample()), Is.True);
                Assert.That(session.HasCreatedCharacter, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void LegacyJsonAndProfileSaveBothRemainReadable()
        {
            var legacy = JsonUtility.FromJson<NativeSaveData>("{\"format\":\"DarkSaverWorld.NativeSave\",\"version\":1,\"mapScene\":\"Kaosia_Map\",\"runtimeScene\":\"Town_Runtime\",\"clearedScenarios\":[]}");
            Assert.That(legacy.hasCharacter, Is.False);
            Assert.That(NativeSaveStore.IsValid(legacy), Is.True);
            var folder = Path.Combine(Application.temporaryCachePath, "CharacterChecks-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new NativeSaveStore(Path.Combine(folder, "save.json"));
                Assert.That(store.TrySave(legacy, out _), Is.True);
                var source = Sample(); source.hasCharacter = true; source.character = new NativeCharacterProfile { name = "카오스", jobId = 100 };
                Assert.That(store.TrySave(source, out _), Is.True);
                Assert.That(store.TryLoad(out var loaded, out _), Is.True);
                Assert.That(loaded.character.name, Is.EqualTo("카오스"));
                Assert.That(loaded.character.jobId, Is.EqualTo(100));
                Assert.That(new NativeSaveStore(store.FilePath + ".bak").TryLoad(out var backup, out _), Is.True);
                Assert.That(backup.hasCharacter, Is.False);
                source.character.jobId = 200;
                Assert.That(store.TrySave(source, out _), Is.False);
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    File.Delete(Path.Combine(folder, "save.json"));
                    File.Delete(Path.Combine(folder, "save.json.bak"));
                    Directory.Delete(folder);
                }
            }
        }

        [Test]
        public void StartButtonOpensCreationAndRejectsInvalidSubmissionWithoutLoading()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("creation menu fixture"); SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var menu = root.AddComponent<StartMenuController>();
                Assert.That(menu.CreateCharacter("전사"), Is.False);
                menu.StartGame();
                Assert.That(menu.IsCreatingCharacter, Is.True);
                Assert.That(menu.CreateCharacter(""), Is.False);
                Assert.That(menu.CreateCharacter("전사", 200), Is.False);
                Assert.That(NativeCharacterProfile.TakePending(out _, out _), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void BattleNameChangeDoesNotResetVitalsExperienceOrAp()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("battle name fixture"); SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var battle = root.AddComponent<BattleSceneRoot>();
                var hp = battle.PlayerHp; var mp = battle.PlayerMp; var ap = battle.PlayerAp;
                battle.RestorePlayerExperience(3477000000u);
                Assert.That(battle.TrySetPlayerName("카오스"), Is.True);
                Assert.That(battle.PlayerName, Is.EqualTo("카오스"));
                Assert.That((battle.PlayerHp, battle.PlayerMp, battle.PlayerAp), Is.EqualTo((hp, mp, ap)));
                Assert.That(battle.PlayerExperience, Is.EqualTo(3477000000u));
                Assert.That(battle.TrySetPlayerName(""), Is.False);
                Assert.That(battle.PlayerName, Is.EqualTo("카오스"));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
