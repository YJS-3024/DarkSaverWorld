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
        public bool CanEnterScenario(string key) => HasCreatedCharacter && !HasPendingTutorial && ScenarioCatalog.CanEnter(key,IsScenarioCleared);
        public int[] CaptureUnlockedGenerals()=>campaign.Values
            .Where(s=>ScenarioGeneralRewards.JobForScenario(s.scenarioKey)>0
                && s.unlockedGenerals?.Contains(ScenarioGeneralRewards.JobForScenario(s.scenarioKey))==true)
            .Select(s=>ScenarioGeneralRewards.JobForScenario(s.scenarioKey)).Distinct().OrderBy(id=>id).ToArray();
        private readonly Dictionary<int,DarkSaver.Player.DirectionalSpriteAnimator> scenarioGeneralArtwork=new();
        private bool AddScenarioGeneralArtwork(BattleFormationSnapshot formation,Dictionary<int,DarkSaver.Player.DirectionalSpriteAnimator> artwork)
        {
            foreach(var member in formation.Members)
            {
                int job=member.Unit.JobId;
                if(artwork.ContainsKey(job))continue;
                if(classArtwork==null || !classArtwork.TryGet(job,out _))return false;
                if(!scenarioGeneralArtwork.TryGetValue(job,out var template) || template==null)
                {
                    var go=new UnityEngine.GameObject("Scenario general artwork "+job);
                    go.SetActive(false);go.transform.SetParent(transform,false);
                    go.AddComponent<UnityEngine.SpriteRenderer>();
                    template=go.AddComponent<DarkSaver.Player.DirectionalSpriteAnimator>();
                    if(!classArtwork.TryApply(job,template))
                    {if(UnityEngine.Application.isPlaying)UnityEngine.Object.Destroy(go);else UnityEngine.Object.DestroyImmediate(go);return false;}
                    scenarioGeneralArtwork[job]=template;
                }
                artwork[job]=template;
            }
            return true;
        }

        private bool StageScenarioGeneralRewards(NativeSaveData snapshot,out string reason)
        {
            var grants=new List<int>();
            foreach(var state in snapshot.campaign ?? Array.Empty<Scenario01Progress>())
            {
                int job=ScenarioGeneralRewards.JobForScenario(state.scenarioKey);
                if(job>0 && state.unlockedGenerals?.Contains(job)==true)grants.Add(job);
            }
            if(grants.All(id=>Array.IndexOf(scenarioGeneralReceipts,id)>=0)){reason=string.Empty;return true;}
            var progression=GetComponent<DarkSaver.UI.TownServicesPanel>()?.Progression;
            var magic=GetComponent<DarkSaver.UI.MagicCatalogPanel>()?.Catalog;
            if(!ScenarioGeneralRewards.TryBuild(ownedRoster,character.jobId,scenarioGeneralReceipts,grants,FindEquipmentJob,
                progression==null ? null : progression.TryGetClassEntryAbilities,id=>magic!=null && magic.TryGet(id,out _),
                out var roster,out var receipts,out reason))return false;
            snapshot.ownedRoster=roster;snapshot.hasOwnedRoster=roster!=null;snapshot.scenarioGeneralReceipts=receipts;
            return true;
        }
        private void PublishScenarioGeneralRewards(NativeSaveData snapshot)
        {
            var receipts=snapshot.scenarioGeneralReceipts ?? Array.Empty<int>();
            if(scenarioGeneralReceipts.SequenceEqual(receipts))return;
            scenarioGeneralReceipts=(int[])receipts.Clone();ownedRoster=snapshot.ownedRoster?.Clone();OwnedRosterRevision++;
            preparedGround=null;preparedRosterRevision=-1;
            GetComponent<DarkSaver.UI.BattleFormationPanel>()?.UseSessionRoster();
        }

        public bool TryCommitCampaign(string key,int revision,Scenario01Progress state,NativeInventory items,out string reason)
        {
            CanRetryScenarioSave=false; ScenarioSaveDiagnostic=string.Empty;
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
            if(!StageScenarioGeneralRewards(snapshot,out reason))return false;
            if(saveStore==null)saveStore=new NativeSaveStore(NativeSaveStore.DefaultPath);
            if(!saveStore.TrySave(snapshot,out reason))
            {CanRetryScenarioSave=saveStore.CanRetryLastWrite;ScenarioSaveDiagnostic=saveStore.LastWriteDiagnostic;return false;}
            PublishScenarioGeneralRewards(snapshot);
            campaign[key]=state.Clone();inventory=items.Clone();CampaignRevision++;
            if(state.phase==Scenario01Progress.Complete)clearedScenarioKeys.Add(key);
            lastSaveMessage="시나리오 "+key+" 진행을 저장했습니다.";return true;
        }
    }
}
