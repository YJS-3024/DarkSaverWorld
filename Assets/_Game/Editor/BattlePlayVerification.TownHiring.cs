using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkSaver.EditorTools
{
    public static partial class BattlePlayVerification
    {
        [MenuItem("DarkSaver/Verification/Run town hire availability UI (isolated)")]
        public static void BeginTownHiring()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Editor must be idle");
            SessionState.SetBool(Key+"hireNoticeOnly",false);
            SessionState.SetBool(Key + "characterInfo", false);
            SessionState.SetBool(Key + "townHiring", true);
            SessionState.SetBool(Key + "townItems", false);
            SessionState.SetBool(Key + "classChange", false);
            SessionState.SetBool(Key + "mercenaryDeployment", false);
            try { BeginTownTraining(); }
            catch { SessionState.SetBool(Key + "townHiring", false); throw; }
        }

        static void PrepareTownHiring(BattleTransitionController session, TopDownPlayerController player)
        {
            var state = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", player.transform.position);
            state.inventory.gold = 600; // Isolated funding fixture, never original starting money.
            state.character.hasClassAbilities=true;state.character.learnedMagicIds=Array.Empty<int>();
            state.character.usableSkillIds=new[]{21101,21701}; // Available choices, never mixed ownership permission.
            RequireMercenary(session.RestoreSaveState(state), "Hire funding fixture rejected");
            RequireMercenary(session.GetComponent<TownServicesPanel>().Open(TownService.Mercenary, player), "Hire UI unavailable");
            SessionState.SetInt(Key + "stage", 70);
            Note("Native hire availability and type lock UI; isolated gold600/two available skill fixture, native prices/type limits unchanged");
        }

        static void TickTownHiring(int stage)
        {
            if (EditorApplication.timeSinceStartup < nextAction) return;
            nextAction = EditorApplication.timeSinceStartup + .3;
            var session = UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>();
            var player = UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
            if (session == null || player == null || session.IsTransitioning) return;
            var panel = session.GetComponent<TownServicesPanel>();
            if(stage>=81 && stage<=87) {
                if(stage==81) {
                    var hire=TownItemElement<Button>(panel,"hire-one-0");
                    var feedback=TownItemElement<Label>(panel,"hire-pending");
                    var policy=TownItemElement<Label>(panel,"hire-type-policy");
                    var save=TownItemElement<Button>(panel,"town-save");
                    Note("Other-type UI: enabled="+(hire==null?"missing":hire.enabledInHierarchy.ToString())+" tooltip="+hire?.tooltip
                        +" feedback="+feedback?.text+" policyBottom="+policy?.worldBound.yMax+" saveTop="+save?.worldBound.yMin);
                    RequireMercenary(hire!=null && !hire.enabledInHierarchy && hire.tooltip.Contains("먼저 고용한"),"Other type hire not locked");
                    RequireMercenary(feedback!=null && feedback.text.Contains("먼저 고용한"),"Other type feedback missing");
                    RequireMercenary(feedback.worldBound.yMax<=TownItemElement<Button>(panel,"hire-skill").worldBound.yMin,"Blocked feedback overlaps action buttons");
                    RequireMercenary(policy!=null && save!=null && policy.text.Contains("보유 0명") && policy.worldBound.yMax<=save.worldBound.yMin,"Type policy overlaps save button");
                    RequireMercenary(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",player.transform.position))==SessionState.GetString(Key+"hireFlowState",""),"Blocked type inspection mutated session");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-hire-other-type-blocked.png"));
                    SessionState.SetInt(Key+"stage",82);return;
                }
                if(stage==82) {
                    if(SessionState.GetBool(Key+"hireNoticeOnly",false)) {
                        SessionState.SetBool(Key+"hireNoticeOnly",false);
                        Finish("PASS: focused actual hire/type-lock notices, pointer save/native readback, feedback/action and policy/save layout");return;
                    }
                    ClickTownItem(panel,"hire-back");SessionState.SetInt(Key+"stage",83);return;
                }
                if(stage==83) {ClickTownItem(panel,"town-character-info");SessionState.SetInt(Key+"stage",84);return;}
                if(stage==84) {
                    var icon=TownItemElement<VisualElement>(panel,"character-info-auxiliary-icon");
                    RequireMercenary(panel.Service==TownService.CharacterInfo && panel.CharacterInfoValuesAreLocal
                        && panel.CharacterInfoAuxiliaryClassId==200 && panel.CharacterInfoAuxiliaryCountText=="X 2"
                        && panel.CharacterInfoMercenaryIcon(200)!=null && icon?.style.backgroundImage.value.sprite==panel.CharacterInfoMercenaryIcon(200)
                        && TownItemElement<VisualElement>(panel,"character-info-auxiliary-count")?.tooltip=="X 2"
                        && panel.CharacterInfoHiresText.StartsWith("보유 고용 용병 2명"),"Actual hired units missing in character info");
                    RequireMercenary(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",player.transform.position))==SessionState.GetString(Key+"hireFlowState",""),"Character info changed actual hires/funds");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-hire-character-info.png"));
                    SessionState.SetInt(Key+"stage",85);return;
                }
                if(stage==85) {ClickTownItem(panel,"character-info-back");SessionState.SetInt(Key+"stage",86);return;}
                if(stage==86) {ClickTownItem(panel,"service-3");SessionState.SetInt(Key+"stage",87);return;}
                RequireMercenary(panel.Service==TownService.Mercenary && session.OwnedUnitCount==2 && session.CurrentGold==200,"Facility round trip lost actual hires");
                ClickTownItem(panel,"hire-formation");SessionState.SetInt(Key+"stage",77);return;
            }
            if (stage >= 77) {
                var formation = session.GetComponent<BattleFormationPanel>();
                var unit = session.CaptureOwnedRoster().units[0];
                RequireMercenary(formation.IsOpen && !panel.IsOpen && player.UiInputBlocked, "Hire-to-formation modal handoff failed");
                if (stage == 80) {
                    var confirmed = formation.ConfirmedSnapshot;
                    RequireMercenary(confirmed != null && formation.CanReturnToTown, "Confirmed formation cannot return to town");
                    ClickTownItem(panel, "town-return");
                    RequireMercenary(!formation.IsOpen && panel.IsOpen && panel.Service == TownService.Overview
                        && player.UiInputBlocked && formation.ConfirmedSnapshot == confirmed, "Return lost confirmed formation or input lock");
                    RequireMercenary(panel.SaveTown() && session.OwnedUnitCount == 2 && session.CaptureInventory().gold == 200,
                        "Return-to-town save changed hired roster or funds");
                    panel.Close();
                    var state = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", player.transform.position);
                    state.inventory.gold = 0; state.ownedRoster.units = Array.Empty<NativeOwnedUnitData>();
                    RequireMercenary(session.RestoreSaveState(state) && panel.Open(TownService.Mercenary, player), "Zero-funds isolated fixture failed");
                    SessionState.SetInt(Key + "stage", 75); return;
                }
                if (stage == 77) {
                    RequireMercenary(formation.OwnedCount == 2 && formation.MemberStatus(unit.persistentId).Contains("HP "+unit.hp+"/"+unit.maxHp), "Hired roster/vitals missing");
                    ClickTownItem(panel, "assign-primary-" + unit.persistentId);
                    UnityEngine.Tilemaps.Tilemap ground = null;
                    foreach (var map in UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>())
                        if (map.name == "Ground" && map.gameObject.scene.name == "Kaosia_Map") ground = map;
                    RequireMercenary(ground != null, "Town placement map missing");
                    var origin = ground.WorldToCell(player.transform.position);
                    bool placed = false;
                    for (int dy = -5; dy <= 5 && !placed; dy++)
                    for (int dx = -5; dx <= 5 && !placed; dx++) {
                        var cell = origin + new Vector3Int(dx, dy, 0);
                        if (cell == origin) continue;
                        placed = formation.TrySetPosition(unit.persistentId, new CombatGridCell(cell.x, ground.cellBounds.yMax - 1 - cell.y));
                    }
                    RequireMercenary(placed, "No native traversable town placement cell");
                    SessionState.SetInt(Key + "stage", 78); return;
                }
                RequireMercenary(formation.AssignedCount == 1 && formation.MemberStatus(unit.persistentId).EndsWith("출전 배정"), "Hire assignment status missing");
                var status = TownItemElement<Label>(panel, "status-" + unit.persistentId);
                RequireMercenary(status != null && status.text == formation.MemberStatus(unit.persistentId), "Formation status label mismatch");
                if (stage == 78) {
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-hire-formation.png"));
                    SessionState.SetInt(Key + "stage", 79); return;
                }
                ClickTownItem(panel, "confirm");
                RequireMercenary(!formation.IsOpen && formation.ConfirmedSnapshot != null && !player.UiInputBlocked, "Formation confirmation/input release failed: " + formation.Message);
                RequireMercenary(session.OpenFormationPreparation(), "Confirmed formation reopen failed");
                SessionState.SetInt(Key + "stage", 80); return;
            }
            var button = TownItemElement<Button>(panel, "hire-one-0");
            if (button == null) return;
            void Step() => SessionState.SetInt(Key + "stage", stage + 1);
            if (stage == 70) {
                RequireMercenary(button.enabledInHierarchy && panel.CanHireMercenary(21101, 200, out _), "Affordable native hire disabled");
                RequireMercenary(session.CaptureInventory().gold == 600 && session.OwnedUnitCount == 0, "Hire inspection mutated session");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-hire-available.png")); Step(); return;
            }
            if (stage == 71 || stage == 72) {
                ClickTownItem(panel, "hire-one-0");
                RequireMercenary(session.OwnedUnitCount == stage - 70, "Hire click did not create exactly one unit"); Step(); return;
            }
            if (stage == 73) {
                RequireMercenary(!button.enabledInHierarchy && !panel.CanHireMercenary(21101, 200, out var reason), "Full type not disabled");
                RequireMercenary(session.CaptureInventory().gold == 200 && session.OwnedUnitCount == 2, "Native hire price/type totals mismatch");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-hire-type-full.png")); Step(); return;
            }
            if (stage == 74) {
                ClickTownItem(panel,"town-save");
                var store = new NativeSaveStore(Path.Combine(output, "verification-save.json"));
                RequireMercenary(store.TryLoad(out var saved, out _) && saved.inventory.gold == 200 && saved.ownedRoster.units.Length == 2,
                    "Native hires disk persistence mismatch");
                foreach(var unit in saved.ownedRoster.units)RequireMercenary(unit.jobId==200 && unit.hireOwnerId==100 && unit.hireType=="보병용병고용","Saved hire metadata mismatch");
                SessionState.SetString(Key+"hireFlowState",JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",player.transform.position)));
                ClickTownItem(panel,"hire-skill");SessionState.SetInt(Key+"stage",81);return;
            }
            if (stage == 75) {
                RequireMercenary(!button.enabledInHierarchy && !panel.CanHireMercenary(21101, 200, out _), "Insufficient funds hire enabled");
                RequireMercenary(session.OwnedUnitCount == 0 && session.CaptureInventory().gold == 0, "Disabled hire changed resources");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-hire-no-funds.png")); Step(); return;
            }
            if (stage == 76) {
                panel.Close(); RequireMercenary(!player.UiInputBlocked, "Hire modal input lock leaked");
                Finish("PASS: read-only hire availability -> two pointer single hires -> native type cap/buttons -> pointer save and native roster/type/funds readback -> other type disabled/feedback -> actual hires in character info picture/X2 -> facility round trip -> hire-to-formation pointer handoff -> vitals/status/primary assignment/confirmation -> reopen/return to town/confirmed snapshot preserved/save -> separate no-funds fixture disabled -> close/input release");
            }
        }
    }
}
