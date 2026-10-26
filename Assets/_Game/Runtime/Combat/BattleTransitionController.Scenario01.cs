using System;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        private Scenario01Progress scenario01;
        public bool CanRetryScenarioSave { get; private set; }
        public string ScenarioSaveDiagnostic { get; private set; } = string.Empty;
        public int Scenario01Revision { get; private set; }
        public Scenario01Progress CaptureScenario01() => scenario01?.Clone();

        // Disk and live economy/progress are committed together. Visual effects
        // are applied only after this returns true, and retries cannot regrant.
        public bool TryCommitScenario01(int expectedRevision, Scenario01Progress progress, NativeInventory items,
            out string reason)
        {
            CanRetryScenarioSave=false; ScenarioSaveDiagnostic=string.Empty;
            reason = "시나리오 진행을 저장할 수 없습니다.";
            if (expectedRevision != Scenario01Revision || transitioning || !HasCreatedCharacter
                || progress == null || Scenario01Controller.Definition == null || !progress.Matches(Scenario01Controller.Definition)
                || items == null || !items.IsValid
                || !SceneManager.GetSceneByName("Brugos01_Map").isLoaded
                || !SceneManager.GetSceneByName("Brugos01_Runtime").isLoaded
                || SceneManager.GetSceneByName(BattleSceneName).isLoaded) return false;
            var player = FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>();
            if (player == null || !player.isActiveAndEnabled) return false;
            var snapshot = CaptureSaveState("Brugos01_Map", "Brugos01_Runtime", player.transform.position);
            snapshot.hasScenario01 = true; snapshot.scenario01 = progress.Clone();
            snapshot.version = campaign.Count > 0 ? NativeSaveStore.CampaignVersion : NativeSaveStore.ScenarioVersion;
            snapshot.inventory = items.Clone(); snapshot.hasInventory = true;
            if (progress.phase == Scenario01Progress.Complete)
            {
                var keys = new SortedSet<string>(snapshot.clearedScenarios, StringComparer.Ordinal) { "01" };
                snapshot.clearedScenarios = new List<string>(keys).ToArray();
            }
            if (!StageScenarioGeneralRewards(snapshot, out reason)) return false;
            if (saveStore == null) saveStore = new NativeSaveStore(NativeSaveStore.DefaultPath);
            if (!saveStore.TrySave(snapshot, out reason))
            { CanRetryScenarioSave=saveStore.CanRetryLastWrite; ScenarioSaveDiagnostic=saveStore.LastWriteDiagnostic; lastSaveMessage=reason; return false; }
            PublishScenarioGeneralRewards(snapshot);
            scenario01 = snapshot.scenario01; inventory = snapshot.inventory;
            if (progress.phase == Scenario01Progress.Complete) clearedScenarioKeys.Add("01");
            Scenario01Revision++; lastSaveMessage = "시나리오 진행을 저장했습니다."; return true;
        }
    }
}
