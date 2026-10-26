using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Combat;
using DarkSaver.Events;
using DarkSaver.Jobs;
using DarkSaver.Magic;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class ScenarioGeneralRewardTests
    {
        [Test]
        public void RecruitClassArtworkUsesNative32PixelRowsIncludingSixRowSheets()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<ClassArtworkCatalog>("Assets/_Game/Runtime/Jobs/ClassArtworkCatalog.asset");
            Assert.IsNotNull(catalog);
            foreach(int job in new[]{100,101,102,103,104,106,107,109,111,113})
            {
                Assert.IsTrue(catalog.TryGet(job,out var entry));
                for(int i=0;i<12;i++)
                {
                    var frame=entry.GetFrame(i);Assert.IsNotNull(frame);
                    Assert.AreEqual(new Rect(i%3*32,frame.texture.height-(i/3+1)*32,32,32),frame.rect);
                    Assert.AreSame(frame,entry.GetFrame(i),"Corrected frames must be cached.");
                }
            }
        }

        [Test]
        public void LegacyUnlocksBecomeOwnedOnlyAfterSuccessfulSaveAndNeverDuplicateOnReload()
        {
            var active=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            scene.name="Town_Runtime";
            string assetFolder="Assets/_Game/Tests/EditMode/RewardFixture-"+Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(assetFolder);
            Assert.IsTrue(EditorSceneManager.SaveScene(scene,assetFolder+"/Town_Runtime.unity"));
            var map=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);map.name="Kaosia_Map";
            string folder=Path.Combine(Path.GetTempPath(),"ds-scenario-reward-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                var root=new GameObject("reward fixture");SceneManager.MoveGameObjectToScene(root,scene);
                var playerGo=new GameObject("reward visitor");SceneManager.MoveGameObjectToScene(playerGo,scene);
                var player=playerGo.AddComponent<TopDownPlayerController>();
                var session=root.AddComponent<BattleTransitionController>();
                Assert.IsTrue(session.InitializeCharacter(new NativeCharacterProfile {name="영입검증",jobId=100}));
                var jobs=AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                var table=AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>("Assets/_Game/Runtime/Combat/ExperienceProgression.asset");
                var magic=AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                root.AddComponent<TownServicesPanel>().ConfigureProgression(table);
                root.AddComponent<MagicCatalogPanel>().Configure(magic,player);
                Assert.IsTrue(session.ConfigureFormationSupport(new[]{jobs},new Dictionary<int,DirectionalSpriteAnimator>()));
                var definition=JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/02/Content").text);
                var progress=Scenario01Progress.Create(definition);progress.phase=Scenario01Progress.Complete;progress.unlockedGenerals.Add(102);
                var data=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                data.version=NativeSaveStore.CampaignVersion;data.campaign=new[]{progress};data.clearedScenarios=new[]{"02"};
                Assert.IsTrue(session.RestoreSaveState(data));
                Assert.AreEqual(0,session.OwnedUnitCount);
                string blocked=Path.Combine(folder,"blocked");File.WriteAllText(blocked,"retained");
                session.ConfigureSaveFile(Path.Combine(blocked,"save.json"));
                Assert.IsFalse(session.SaveWorld());Assert.AreEqual(0,session.OwnedUnitCount);
                Assert.IsEmpty(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one).scenarioGeneralReceipts);
                string path=Path.Combine(folder,"save.json");session.ConfigureSaveFile(path);
                Assert.IsTrue(session.SaveWorld(),session.LastSaveMessage);
                Assert.AreEqual(1,session.OwnedUnitCount);
                Assert.IsTrue(new NativeSaveStore(path).TryLoad(out var saved,out var reason),reason);
                Assert.AreEqual(102,saved.ownedRoster.units[0].jobId);
                CollectionAssert.AreEqual(new short[]{2401},saved.ownedRoster.units[0].learnedMagicIds);
                CollectionAssert.AreEqual(new short[]{10101,21701},saved.ownedRoster.units[0].usableSkillIds);
                CollectionAssert.AreEqual(new[]{102},saved.scenarioGeneralReceipts);
                Assert.IsTrue(session.RestoreSaveState(saved));Assert.IsTrue(session.SaveWorld());
                Assert.AreEqual(1,session.OwnedUnitCount);
            }
            finally
            {
                EditorSceneManager.CloseScene(map,true);EditorSceneManager.CloseScene(scene,true);
                if(active.IsValid() && active.isLoaded)SceneManager.SetActiveScene(active);
                Directory.Delete(folder,true);
                AssetDatabase.DeleteAsset(assetFolder);
            }
        }

        [Test]
        public void UnderscoredScenario10GrantIsInertAndReceiptValidationRejectsDuplicates()
        {
            var definition=JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/10/Content").text);
            var state=Scenario01Progress.Create(definition);state.phase=Scenario01Progress.Outro;
            // The original spelling is processed through an isolated command fixture to avoid combat bypass.
            var command=new Scenario01Command {name="GET_GENERAL",args=new[]{"109"}};
            var copy=JsonUtility.FromJson<Scenario01Definition>(JsonUtility.ToJson(definition));copy.outro=new[]{command,new Scenario01Command{name="END"}};
            Assert.AreEqual(Scenario01Rules.StepResult.Advanced,Scenario01Rules.Step(copy,state,new NativeInventory(),0,out var next,out _));
            Assert.IsEmpty(next.unlockedGenerals);
            Assert.IsTrue(ScenarioGeneralRewards.ValidReceipts(null));
            Assert.IsFalse(ScenarioGeneralRewards.ValidReceipts(new[]{102,102}));
            Assert.IsFalse(ScenarioGeneralRewards.ValidReceipts(new[]{999}));
        }
    }
}
