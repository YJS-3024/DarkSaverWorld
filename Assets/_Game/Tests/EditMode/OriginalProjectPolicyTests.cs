using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class OriginalProjectPolicyTests
    {
        [Test]
        public void OriginalModeDoesNotQueueAuthoredCharacterOrTutorial()
        {
            Assert.IsFalse(OriginalRestorationPolicy.AllowSinglePlayerRules);
            string path=Path.Combine(Path.GetTempPath(),"original-create-"+Guid.NewGuid().ToString("N")+".json");
            Assert.IsFalse(NativeCharacterProfile.TryQueue("Original",100,path,out var reason));
            Assert.IsNotEmpty(reason);Assert.IsFalse(NativeCharacterProfile.TakePending(out _,out _));
            Assert.IsFalse(File.Exists(path));
        }

        [Test]
        public void UnconfirmedSessionRulesCannotCreateMoneyHealingOrOwnership()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("original session fixture");SceneManager.MoveGameObjectToScene(go,scene);
                var session=go.AddComponent<BattleTransitionController>();
                Assert.IsFalse(session.InitializeCharacter(new NativeCharacterProfile {name="Original",jobId=100}));
                Assert.IsFalse(session.TryTempleRecovery(out var reason));Assert.IsNotEmpty(reason);
                Assert.IsFalse(session.TrySetOwnedRoster(null,out _));
                Assert.IsFalse(session.HasCreatedCharacter);Assert.IsFalse(session.HasPendingTutorial);
                Assert.IsFalse(session.HasPlayerVitals);Assert.IsNull(session.CaptureInventory());
                Assert.AreEqual(0,session.ClearedScenarioCount);
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }

        [Test]
        public void AuthoredScenarioServerCannotReadOrAdvanceSaveState()
        {
            var local=new ScenarioServerBoundary(null,
                ()=>throw new Exception("must not read old progress"),
                ()=>throw new Exception("must not read or grant inventory"),
                ()=>throw new Exception("must not advance revision"),()=>100,()=>false,
                (Scenario01Progress state,NativeInventory inventory,out string reason)=>{reason="must not write";Assert.Fail(reason);return false;});
            var response=local.Handle(new ScenarioCommandRequest("01",0,ScenarioOperation.Initialize));
            Assert.IsFalse(response.Accepted);Assert.IsNotEmpty(response.Reason);
        }

        [Test]
        public void OnlyObservedOrConfirmedStaffRemainInOriginalPresentation()
        {
            Assert.IsTrue(OriginalRestorationPolicy.AllowsStaffEvidence("screenshot"));
            Assert.IsTrue(OriginalRestorationPolicy.AllowsStaffEvidence("user-confirmed"));
            Assert.IsFalse(OriginalRestorationPolicy.AllowsStaffEvidence("user-authored"));
            Assert.IsFalse(OriginalRestorationPolicy.AllowsStaffEvidence(null));
        }
    }
}
