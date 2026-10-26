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
        [TestCase(100)][TestCase(101)][TestCase(103)]
        public void NewCharacterInitializesConfirmedJobVitalsBeforeFirstBattleAndFailsAtomicallyWithoutCatalog(int startingJob)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("new character vitals"); SceneManager.MoveGameObjectToScene(root, scene);
                var session = root.AddComponent<BattleTransitionController>();
                var profile = new NativeCharacterProfile { name="신규", jobId=startingJob, hasLevels=true, baseLevel=1, jobLevel=1, tutorial=new NativeTutorialProgress() };
                Assert.IsFalse(session.InitializeCharacter(profile));
                Assert.IsFalse(session.HasCreatedCharacter); Assert.IsFalse(session.HasPlayerVitals);
                Assert.IsNull(session.CaptureInventory(), "Failed creation must not grant funds.");
                var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                Assert.IsTrue(session.ConfigureFormationSupport(new[] {catalog}, new System.Collections.Generic.Dictionary<int, DarkSaver.Player.DirectionalSpriteAnimator>()));
                Assert.IsTrue(catalog.TryGet(startingJob, out var job));
                Assert.IsFalse(session.InitializeCharacter(profile), "Missing starting-ability catalogs must not partially create a character.");
                Assert.IsFalse(session.HasPlayerVitals); Assert.IsNull(session.CaptureInventory());
                root.AddComponent<TownServicesPanel>().ConfigureProgression(UnityEditor.AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>("Assets/_Game/Runtime/Combat/ExperienceProgression.asset"));
                root.AddComponent<MagicCatalogPanel>().Configure(UnityEditor.AssetDatabase.LoadAssetAtPath<DarkSaver.Magic.MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset"), root.AddComponent<DarkSaver.Player.TopDownPlayerController>());
                Assert.IsTrue(session.InitializeCharacter(profile));
                Assert.IsFalse(profile.hasClassAbilities, "Creation must not mutate the caller's candidate.");
                var created = session.CaptureCharacterProfile();
                Assert.IsTrue(created.hasClassAbilities);
                CollectionAssert.AreEqual(startingJob==101?new[]{301}:startingJob==103?new[]{101,301}:new[]{101},created.learnedMagicIds);
                CollectionAssert.AreEqual(startingJob==101?new[]{10101,21201}:startingJob==103?new[]{12101,11101,21701}:new[]{10101,21101},created.usableSkillIds);
                Assert.AreEqual(30000L, session.CaptureInventory().gold);
                Assert.IsFalse(session.InitializeCharacter(profile));
                Assert.AreEqual(30000L, session.CaptureInventory().gold, "Repeated initialization must not grant funds twice.");
                Assert.IsTrue(session.HasPlayerVitals);
                Assert.AreEqual(job.GetNumericValue(2), session.PlayerHp);
                Assert.AreEqual(job.GetNumericValue(4), session.PlayerMp);
                var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                Assert.IsTrue(save.hasVitals); Assert.IsTrue(session.RestoreSaveState(save));
                Assert.AreEqual(save.maxHp, session.PlayerHp); Assert.AreEqual(save.maxMp, session.PlayerMp);
                Assert.AreEqual(30000L, session.CaptureInventory().gold);
                save.inventory.gold = 22000; // Spending 8,000 must survive restoration without a top-up.
                Assert.IsTrue(session.RestoreSaveState(save));
                Assert.IsTrue(session.RestoreSaveState(save));
                Assert.AreEqual(22000L, session.CaptureInventory().gold);
                save.inventory.gold = 0;
                Assert.IsTrue(session.RestoreSaveState(save));
                Assert.AreEqual(0L, session.CaptureInventory().gold, "An existing zero-gold save must stay unchanged.");
                save.character.hasClassAbilities = false;
                save.character.learnedMagicIds = null; save.character.usableSkillIds = null;
                Assert.IsTrue(session.RestoreSaveState(save));
                Assert.IsFalse(session.CaptureCharacterProfile().hasClassAbilities, "Do not backfill unknown ownership in existing saves.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void TempleRecoveryRequiresNpcAndSavesBeforePublishingWithoutGrantingGold()
        {
            var map=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);map.name="KaosiaTemple_Map";
            string mapFolder="Assets/_Game/Tests/EditMode/TempleFixture-"+Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(mapFolder);
            Assert.IsTrue(EditorSceneManager.SaveScene(map,mapFolder+"/KaosiaTemple_Map.unity"));
            var runtime=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);runtime.name="KaosiaTemple_Runtime";
            string directory=Path.Combine(Path.GetTempPath(),"ds-temple-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var root=new GameObject("temple recovery fixture");SceneManager.MoveGameObjectToScene(root,runtime);
                var session=root.AddComponent<BattleTransitionController>();
                Assert.IsTrue(session.InitializeCharacter(new NativeCharacterProfile {name="부활검증",jobId=100}));
                var data=session.CaptureSaveState("KaosiaTemple_Map","KaosiaTemple_Runtime",Vector2.one);
                data.hasVitals=true;data.hp=0;data.maxHp=200;data.mp=7;data.maxMp=45;data.lifeState=(int)UnitLifeState.Removed;
                Assert.IsTrue(session.RestoreSaveState(data));
                var actor=new GameObject("temple visitor");SceneManager.MoveGameObjectToScene(actor,runtime);
                var player=actor.AddComponent<DarkSaver.Player.TopDownPlayerController>();
                Assert.IsFalse(session.TryTempleRecovery(out _),"No NPC must reject recovery.");
                var staff=new GameObject("temple staff");SceneManager.MoveGameObjectToScene(staff,runtime);
                staff.AddComponent<DarkSaver.World.ShopCounter2D>();staff.transform.position=Vector3.right*10;
                Assert.IsFalse(session.TryTempleRecovery(out _),"A distant NPC must reject recovery.");
                staff.transform.position=actor.transform.position;
                string blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"retained");
                session.ConfigureSaveFile(Path.Combine(blocked,"save.json"));
                Assert.IsFalse(session.TryTempleRecovery(out _));
                Assert.AreEqual(0,session.PlayerHp);Assert.AreEqual(7,session.PlayerMp);Assert.IsFalse(session.CanEnterBattle);
                string path=Path.Combine(directory,"save.json");session.ConfigureSaveFile(path);
                var body=actor.GetComponent<Rigidbody2D>();body.linearVelocity=Vector2.right;
                Assert.IsFalse(session.TryTempleRecovery(out _),"Moving must reject recovery without publishing vitals.");
                Assert.AreEqual(0,session.PlayerHp);Assert.AreEqual(7,session.PlayerMp);
                body.linearVelocity=Vector2.zero;
                root.AddComponent<TownServicesPanel>();
                Assert.IsTrue(staff.GetComponent<DarkSaver.World.ShopCounter2D>().TryOpen(player));
                Assert.IsTrue(session.TryTempleRecovery(out var reason),reason);
                Assert.AreEqual(200,session.PlayerHp);Assert.AreEqual(45,session.PlayerMp);
                Assert.IsTrue(session.CanEnterBattle);Assert.AreEqual(0L,session.CurrentGold);
                Assert.IsTrue(new NativeSaveStore(path).TryLoad(out var restored,out reason),reason);
                Assert.AreEqual(200,restored.hp);Assert.AreEqual((int)UnitLifeState.Active,restored.lifeState);
                Assert.AreEqual(0L,restored.inventory.gold);
                Assert.IsFalse(session.TryTempleRecovery(out _),"Repeated full-health recovery is a no-op.");
            }
            finally
            {
                EditorSceneManager.CloseScene(runtime,true);EditorSceneManager.CloseScene(map,true);
                UnityEditor.AssetDatabase.DeleteAsset(mapFolder);
                Directory.Delete(directory,true);
            }
        }

        [Test]
        public void LevelUpGrantsExplicitSourceAbilitiesWithoutMutatingSavedSelection()
        {
            var table=ScriptableObject.CreateInstance<DarkSaver.Combat.ExperienceProgressionTable>();
            try
            {
                table.Initialize(new[]{
                    new DarkSaver.Combat.ExperienceLevelEntry {jobId=155,level=20,nextJobId=155,requiredXp=10,learnedMagic="0",usableSkills="10105"},
                    new DarkSaver.Combat.ExperienceLevelEntry {jobId=155,level=21,nextJobId=155,requiredXp=20,learnedMagic="901",usableSkills="10106"}});
                var profile=new NativeCharacterProfile {name="검증",jobId=155,hasLevels=true,baseLevel=60,jobLevel=20,hasClassAbilities=true,learnedMagicIds=new[]{701},usableSkillIds=new[]{10105},hasBattleMagicSelection=true,battleMagicIds=new[]{701}};
                Assert.That(profile.TryAdvanceLevels(table,10,out var changed,out var remaining),Is.True);
                Assert.That(changed.learnedMagicIds,Is.EqualTo(new[]{701,901}));Assert.That(changed.usableSkillIds,Is.EqualTo(new[]{10106}));
                Assert.That(changed.battleMagicIds,Is.EqualTo(new[]{701}));Assert.That(profile.learnedMagicIds,Is.EqualTo(new[]{701}));Assert.That(remaining,Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(table);}
        }

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
                Assert.AreEqual(0L, session.CaptureInventory().gold, "Legacy profiles do not receive new-character funds.");
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
        public void StartButtonOpensLoginThenFirstLoginOpensCharacterSelection()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("creation menu fixture"); SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var menu = root.AddComponent<StartMenuController>();
                Assert.That(menu.CreateCharacter("전사"), Is.False);
                var directory=Path.Combine(Path.GetTempPath(),"ds-menu-"+Guid.NewGuid().ToString("N"));
                Assert.That(menu.ConfigureLocalLogin(Path.Combine(directory,"login.json"),System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0,NativeCharacterSlots.Count),i=>Path.Combine(directory,"slot-"+i+".json")))),Is.True);
                menu.StartGame();
                Assert.That(menu.IsLoggingIn,Is.True);Assert.That(menu.IsSelectingCharacter,Is.False);
                Assert.That(menu.SelectSlot(0),Is.False);
                Assert.That(menu.Login(),Is.True);Assert.That(menu.IsLoggingIn,Is.False);
                Assert.That(menu.IsSelectingCharacter, Is.True);
                Assert.That(root.GetComponent<DarkSaver.Network.ServerManager>().TryRequest(new DarkSaver.Network.CharacterListRequest(),out var list),Is.True);
                Assert.That(((DarkSaver.Network.CharacterListResponse)list).SlotCount,Is.EqualTo(10));
                Assert.That(menu.SelectSlot(9),Is.True);
                Assert.That(menu.IsCreatingCharacter, Is.False);
                Assert.That(menu.SelectSlot(-1), Is.False);
                Assert.That(menu.SelectSlot(NativeCharacterSlots.Count), Is.False);
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
