using System.Linq;
using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;
using DarkSaver.EditorTools.CanvasTesting;

namespace DarkSaver.EditorTools
{
    public static partial class BattlePlayVerification
    {
        [MenuItem("DarkSaver/Verification/Run session character info and allocation (isolated)")]
        public static void BeginCharacterInfo()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Editor must be idle");
            SessionState.SetBool(Key+"characterInfo",true);
            foreach(string flag in new[]{"townItems","townHiring","classChange","mercenaryDeployment"}) SessionState.SetBool(Key+flag,false);
            try {BeginTownTraining();} catch {SessionState.SetBool(Key+"characterInfo",false);throw;}
        }
        static void PrepareCharacterInfo(BattleTransitionController session,TopDownPlayerController player)
        {
            var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",player.transform.position);
            save.character.hasLevels=true; save.character.baseLevel=12; save.character.jobLevel=5;
            save.character.allocatedAttack=2; save.character.allocatedDefense=3; save.character.unspentClassChangeStatPoints=20;
            save.hasVitals=true; save.hp=17; save.maxHp=200; save.mp=3; save.maxMp=45; save.lifeState=0; save.experience=1234;
            NativeOwnedUnitData Hire(string id,int instance,int job)=>new NativeOwnedUnitData {
                persistentId=id,instanceId=instance,jobId=job,name="검증 용병",hp=1,maxHp=1,teamId=1,
                finalStateConfirmed=true,learnedMagicIds=Array.Empty<short>(),hireType="보병용병고용",hireOwnerId=100
            };
            save.version=NativeSaveStore.RosterVersion; save.hasOwnedRoster=true;
            save.ownedRoster=new NativeOwnedRoster {units=new[]{Hire("a",20000,200),Hire("b",20001,200),Hire("c",20002,201)}};
            save.ownedRoster.units[1].hp=0; save.ownedRoster.units[1].lifeState=1;
            RequireMercenary(session.RestoreSaveState(save),"Character info fixture rejected");
            RequireMercenary(session.GetComponent<TownServicesPanel>().Open(TownService.CharacterInfo,player),"Cannot open character info");
            SessionState.SetInt(Key+"stage",1000);
            Note("Isolated character info fixture: job5/base12, wounded, allocations2/3, mixed hires including dead; not user save");
        }
        static void TickCharacterInfo(int stage)
        {
            if(EditorApplication.timeSinceStartup<nextAction)return;
            nextAction=EditorApplication.timeSinceStartup+.4;
            var session=UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>();
            var player=UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
            if(session==null || player==null || session.IsTransitioning)return;
            var panel=session.GetComponent<TownServicesPanel>();
            void Step()=>SessionState.SetInt(Key+"stage",stage+1);
            string Snapshot()=>JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",player.transform.position));
            void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
            if(stage==1000) {
                RequireMercenary(panel.CharacterInfoLevelText=="5(12)" && panel.CharacterInfoVitalsText=="HP 17/200 · MP 3/45"
                    && panel.CharacterInfoAuxiliaryCountText==null && panel.CharacterInfoAuxiliaryClassId==-1 && panel.CharacterInfoHiresText.StartsWith("보유 고용 용병 3명"),"Session values mismatch");
                foreach(string key in new[]{"character-info-level","character-info-experience","character-info-value-0-0","character-info-value-0-2","character-info-weapon-name"})
                    RequireMercenary(TownItemElement<VisualElement>(panel,key)!=null,"Missing rendered value "+key);
                var summary=TownItemElement<Button>(panel,"character-info-hires");var back=TownItemElement<Button>(panel,"character-info-back");
                RequireMercenary(summary!=null && summary.tooltip.Contains(panel.CharacterInfoHiresText) && summary.worldBound.yMax<=back.worldBound.yMin
                    && back.style.top.value.value+back.style.height.value.value<=DarkSaver.Rendering.GameScreen.Height,"Summary tooltip/layout mismatch");
                Capture("session-character-info-mixed");Step();return;
            }
            if(stage==1001) {
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",player.transform.position);
                save.ownedRoster.units=new[]{save.ownedRoster.units[0]};save.experience=4321;
                RequireMercenary(session.RestoreSaveState(save) && panel.CharacterInfoHiresText.StartsWith("보유 고용 용병 3명"),"Cache unexpectedly polled");
                SessionState.SetString(Key+"infoReadOnly",Snapshot());ClickTownItem(panel,"character-info-hires");Step();return;
            }
            if(stage==1002) {
                RequireMercenary(panel.CharacterInfoAuxiliaryCountText=="X 1" && panel.CharacterInfoAuxiliaryClassId==200 && panel.CharacterInfoHiresText.StartsWith("보유 고용 용병 1명")
                    && panel.CharacterInfoExperienceText.StartsWith("4321/") && Snapshot()==SessionState.GetString(Key+"infoReadOnly",""),"Refresh mismatch or mutation");
                RequireMercenary(TownItemElement<VisualElement>(panel,"character-info-auxiliary-icon")?.style.backgroundImage.value.sprite==panel.CharacterInfoMercenaryIcon(200)
                    && panel.CharacterInfoMercenaryIcon(200)!=null && TownItemElement<VisualElement>(panel,"character-info-auxiliary-count")?.tooltip=="X 1","Actual local mercenary slot missing");
                RequireMercenary(TownItemElement<VisualElement>(panel,"character-info-weapon-name")?.tooltip=="미확인"
                    && TownItemElement<VisualElement>(panel,"character-info-weapon-unknown")!=null
                    && TownItemElement<VisualElement>(panel,"character-info-weapon-label")?.tooltip=="무기","Unknown weapon placeholder missing");
                Capture("session-character-info-single");Step();return;
            }
            if(stage==1003) {ClickTownItem(panel,"character-info-minimize");Step();return;}
            if(stage==1004) {RequireMercenary(panel.IsCharacterInfoMinimized,"Minimize failed");ClickTownItem(panel,"character-info-restore");Step();return;}
            if(stage==1005) {RequireMercenary(!panel.IsCharacterInfoMinimized && panel.CharacterInfoHiresText.StartsWith("보유 고용 용병 1명"),"Restore lost values");ClickTownItem(panel,"character-info-extra");Step();return;}
            if(stage==1006) {
                foreach(int i in new[]{4,5,10,11})RequireMercenary(!TownItemElement<Button>(panel,"character-adjustment-control-"+i).enabledInHierarchy,"HP/MP enabled");
                ClickTownItem(panel,"character-adjustment-control-12");Step();return;
            }
            if(stage==1007) {
                RequireMercenary(session.CaptureCharacterProfile().allocatedAttack==2 && panel.CharacterAdjustmentPointsText=="18","Preview committed or wrong cost");
                Capture("session-character-allocation-preview");Step();return;
            }
            if(stage==1008) {ClickTownItem(panel,"character-adjustment-cancel");Step();return;}
            if(stage==1009) {RequireMercenary(!panel.IsCharacterAdjustmentOpen && session.CaptureCharacterProfile().allocatedAttack==2,"Cancel committed preview");ClickTownItem(panel,"character-info-extra");Step();return;}
            if(stage==1010) {ClickTownItem(panel,"character-adjustment-control-12");Step();return;}
            if(stage==1011) {ClickTownItem(panel,"character-adjustment-apply");Step();return;}
            if(stage==1012) {
                var profile=session.CaptureCharacterProfile();
                RequireMercenary(!panel.IsCharacterAdjustmentOpen && profile.allocatedAttack==3 && profile.unspentClassChangeStatPoints==18
                    && session.TryGetPlayerPhysicalStats(out _,out _,out int attack,out _) && panel.CharacterInfoValueText(0,2)==attack.ToString(),"Confirm/info refresh failed");
                RequireMercenary(session.SaveWorld(),"Isolated save failed");
                var store=new NativeSaveStore(Path.Combine(output,"verification-save.json"));
                RequireMercenary(store.TryLoad(out var saved,out _) && saved.character.allocatedAttack==3
                    && saved.character.unspentClassChangeStatPoints==18 && saved.ownedRoster.units.Length==1,"Save readback mismatch");
                Capture("session-character-info-confirmed");Step();return;
            }
            if(stage==1013) {
                var payload=new byte[724];payload[0x96]=200;payload[0x98]=7;
                RequireMercenary(panel.BindCharacterInfoValues(payload) && panel.BindCharacterInfoWeaponName("검증 무기"),"Explicit original display fixture rejected");
                Step();return;
            }
            if(stage==1014) {
                RequireMercenary(panel.CharacterInfoMercenaryIcon(200)!=null && !panel.CharacterInfoValuesAreLocal && panel.CharacterInfoHiresText==null
                    && TownItemElement<VisualElement>(panel,"character-info-auxiliary-count")?.tooltip=="X 7"
                    && TownItemElement<VisualElement>(panel,"character-info-auxiliary-icon")?.style.backgroundImage.value.sprite==panel.CharacterInfoMercenaryIcon(200)
                    && TownItemElement<VisualElement>(panel,"character-info-weapon-name")?.tooltip=="검증 무기"
                    && TownItemElement<VisualElement>(panel,"character-info-weapon-label")==null
                    && TownItemElement<VisualElement>(panel,"character-info-weapon-unknown")==null
                    && TownItemElement<Button>(panel,"character-info-hires")==null,"Original display retained local state");
                Capture("session-character-info-original-fixture");Step();return;
            }
            if(stage==1015) {RequireMercenary(panel.BindSessionCharacterInfoValues(),"Return to local info failed");Step();return;}
            if(stage==1016) {
                RequireMercenary(panel.CharacterInfoAuxiliaryCountText=="X 1" && panel.CharacterInfoAuxiliaryClassId==200
                    && TownItemElement<VisualElement>(panel,"character-info-auxiliary-count")?.tooltip=="X 1"
                    && TownItemElement<VisualElement>(panel,"character-info-auxiliary-icon")?.style.backgroundImage.value.sprite==panel.CharacterInfoMercenaryIcon(200)
                    && TownItemElement<VisualElement>(panel,"character-info-weapon-name")?.tooltip=="미확인","Local return retained original record values");
                ClickTownItem(panel,"character-info-back");Step();return;
            }
            if(stage==1017) {RequireMercenary(panel.Service==TownService.Overview && panel.CharacterInfoHiresText==null,"Facility retained snapshot");ClickTownItem(panel,"town-character-info");Step();return;}
            if(stage==1018) {ClickTownItem(panel,"character-info-close");Step();return;}
            if(stage==1019) {
                RequireMercenary(!panel.IsOpen && !player.UiInputBlocked && panel.CharacterInfoHiresText==null,"Close did not clear/unlock");
                Finish("PASS: session info rendered, mixed/single hires, local/original source separation, unknown weapon, pointer refresh/minimize/restore/allocation cancel+confirm/facility return/close, isolated save readback");
            }
        }
    }
}
