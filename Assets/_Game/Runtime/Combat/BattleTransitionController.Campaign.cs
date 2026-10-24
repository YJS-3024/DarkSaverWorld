using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        private readonly Dictionary<string,Scenario01Progress> campaign=new();
        public int CampaignRevision {get;private set;}
        public Scenario01Progress CaptureCampaign(string key)=>key!=null && campaign.TryGetValue(key,out var value)?value.Clone():null;
        private Scenario01Progress[] CaptureCampaignStates()=>campaign.OrderBy(r=>r.Key,StringComparer.Ordinal).Select(r=>r.Value.Clone()).ToArray();
        private void RestoreCampaignStates(Scenario01Progress[] values)
        {campaign.Clear();if(values!=null)foreach(var row in values)campaign.Add(row.scenarioKey,row.Clone());CampaignRevision++;}
        public bool CanEnterScenario(string key) => ScenarioCatalog.TryGet(key,out var definition) && HasCreatedCharacter && !HasPendingTutorial
            && (definition.Key=="01" || !IsScenarioCleared(definition.Key) && IsScenarioCleared((int.Parse(definition.Key)-1).ToString("00")));
        public int[] CaptureUnlockedGenerals()=>campaign.Values.SelectMany(s=>s.unlockedGenerals).Distinct().OrderBy(id=>id).ToArray();
        public bool TryCommitCampaign(string key,int revision,Scenario01Progress state,NativeInventory items,out string reason)
        {
            reason="시나리오 진행을 저장할 수 없습니다.";
            if(!ScenarioCatalog.TryGet(key,out var metadata) || metadata.Key=="01" || revision!=CampaignRevision || !HasCreatedCharacter || transitioning
                || items==null || !items.IsValid || state==null || !state.Matches(Scenario01Controller.LoadDefinition(key))
                || !SceneManager.GetSceneByName(metadata.MapScene).isLoaded || !SceneManager.GetSceneByName(metadata.RuntimeScene).isLoaded
                || SceneManager.GetSceneByName(BattleSceneName).isLoaded)return false;
            var player=FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>();if(player==null || !player.isActiveAndEnabled)return false;
            var snapshot=CaptureSaveState(metadata.MapScene,metadata.RuntimeScene,player.transform.position);
            snapshot.version=NativeSaveStore.CampaignVersion;
            var candidates=campaign.ToDictionary(r=>r.Key,r=>r.Value.Clone());candidates[key]=state.Clone();
            snapshot.campaign=candidates.OrderBy(r=>r.Key,StringComparer.Ordinal).Select(r=>r.Value).ToArray();
            snapshot.inventory=items.Clone();snapshot.hasInventory=true;
            if(state.phase==Scenario01Progress.Complete)snapshot.clearedScenarios=snapshot.clearedScenarios.Concat(new[]{key}).Distinct().OrderBy(k=>k,StringComparer.Ordinal).ToArray();
            if(saveStore==null)saveStore=new NativeSaveStore(NativeSaveStore.DefaultPath);
            if(!saveStore.TrySave(snapshot,out reason))return false;
            campaign[key]=state.Clone();inventory=items.Clone();CampaignRevision++;
            if(state.phase==Scenario01Progress.Complete)clearedScenarioKeys.Add(key);
            lastSaveMessage="시나리오 "+key+" 진행을 저장했습니다.";return true;
        }
    }
}
