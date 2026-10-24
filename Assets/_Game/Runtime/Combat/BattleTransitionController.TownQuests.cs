using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        public bool TryCommitTownQuest(int questId, bool claim, out string reason)
        {
            if(!processingSessionRequest)return SubmitSession(new DarkSaver.Network.TownQuestRequest(questId,claim),out reason);
            reason = "마을 의뢰를 처리할 수 없습니다.";
            if (!isActiveAndEnabled || HasPendingTutorial || transitioning || !HasCreatedCharacter
                || SceneManager.GetSceneByName(BattleSceneName).isLoaded
                || !SceneManager.GetSceneByName("Kaosia_Map").isLoaded || !SceneManager.GetSceneByName("Town_Runtime").isLoaded
                || FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>()?.IsTravelling == true) return false;
            var content = DarkSaver.UI.TownServicesPanel.QuestContent;
            var definition = Scenario01Controller.Definition;
            var quest = content != null && content.IsValid ? System.Array.Find(content.rows, q => q.id == questId) : null;
            if (definition == null || !definition.IsValid || quest == null) return false;
            var state = scenario01 ?? Scenario01Progress.Create(definition, IsScenarioCleared("01"));
            if (!TownQuestRules.TryApply(definition, state, inventory, quest, claim, out var next, out var items)) return false;
            var player = FindAnyObjectByType<TopDownPlayerController>();
            if (player == null || !player.isActiveAndEnabled) return false;
            var save = CaptureSaveState("Kaosia_Map", "Town_Runtime", player.transform.position);
            save.version = campaign.Count > 0 ? NativeSaveStore.CampaignVersion : NativeSaveStore.ScenarioVersion; save.hasScenario01 = true; save.scenario01 = next;
            save.hasInventory = true; save.inventory = items;
            if (saveStore == null) saveStore = new NativeSaveStore(NativeSaveStore.DefaultPath);
            if (!saveStore.TrySave(save, out reason)) return false;
            scenario01 = next; inventory = items; Scenario01Revision++;
            lastSaveMessage = reason = claim ? "의뢰 보상과 완료 상태를 저장했습니다." : "의뢰 수락을 저장했습니다.";
            return true;
        }
    }
}
