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
        public bool CanEnterScenario(string key) => OriginalRestorationPolicy.AllowSinglePlayerRules && HasCreatedCharacter && !HasPendingTutorial && ScenarioCatalog.CanEnter(key,IsScenarioCleared);
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
            if(!OriginalRestorationPolicy.AllowSinglePlayerRules){reason=string.Empty;return true;}
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
{reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}    }
}
