using System;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.EditorTools
{
    public static partial class BattlePlayVerification
    {
        static NativeOwnedRoster hiredBeforeDeployment;
        static CombatGridCell mercenaryMoveTarget;

        [MenuItem("DarkSaver/Verification/Run hired primary troop round trip (isolated)")]
        public static void BeginMercenaryDeployment()
        {
            SessionState.SetBool(Key + "mercenaryDeployment", true);
            SessionState.SetInt(Key + "troopOrderStep", 0);
            SessionState.SetBool(Key + "troopOrderCaptured", false);
            try { BeginTownTraining(); }
            catch { SessionState.SetBool(Key + "mercenaryDeployment", false); throw; }
        }

        static void RequireMercenary(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }

        static void HireVerificationMercenaries(BattleTransitionController session, TopDownPlayerController player)
        {
            // Funding is confined to this helper's separate verification profile/save.
            var save = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", player.transform.position);
            save.inventory.gold = 10000;
            RequireMercenary(session.RestoreSaveState(save), "Isolated funding profile restore failed");
            var shop = session.GetComponent<TownServicesPanel>();
            VerifyTownFacilities(session, player, shop);
            RequireMercenary(shop != null && shop.Open(TownService.Mercenary, player), "Hiring panel unavailable");
            RequireMercenary(shop.HireMercenary(21101, 200), "First native hire rejected");
            RequireMercenary(shop.HireMercenary(21101, 201), "Second native hire rejected");
            shop.Close(); hiredBeforeDeployment = session.CaptureOwnedRoster();
            RequireMercenary(hiredBeforeDeployment.units.Length == 2, "Expected two single hires");
            // A wounded starting fixture allows the normal rest action without injecting battle HP/AP.
            hiredBeforeDeployment.units[0].hp -= 10;
            RequireMercenary(session.TrySetOwnedRoster(hiredBeforeDeployment, out var reason), "Wounded isolated fixture rejected: " + reason);
            Note("PASS: native town panel hired two soldiers separately; isolated funding/wounded starting fixture only");
        }

        static void VerifyTownFacilities(BattleTransitionController session, TopDownPlayerController player, TownServicesPanel panel)
        {
            RequireMercenary(panel != null && panel.Open(TownService.Overview, player), "Town facilities unavailable");
            RequireMercenary(panel.SaveTown() && player.UiInputBlocked, "Town modal save/input lock failed");
            RequireMercenary(panel.OpenFormation(), "Town-to-formation handoff failed");
            RequireMercenary(!panel.IsOpen && session.GetComponent<BattleFormationPanel>().IsOpen && player.UiInputBlocked, "Formation handoff lock mismatch");
            session.GetComponent<BattleFormationPanel>().Cancel();
            RequireMercenary(!player.UiInputBlocked, "Formation cancel retained lock");
            RequireMercenary(panel.Open(TownService.ClassChange, player)
                && panel.TryPreviewClassChange(0, out var row, out var projected)
                && row.level == 5 && projected.jobId == 105 && session.PlayerJobId == 100, "Class preview mutated or misread current class");
            panel.Close();
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<DarkSaver.Items.ItemCatalog>(DarkSaver.Editor.ItemCatalogConverter.Output);
            int itemId = 0;
            if (catalog != null) for (int i = 0; i < catalog.Count && itemId == 0; i++) if (catalog.GetAt(i).Id > 0) itemId = catalog.GetAt(i).Id;
            RequireMercenary(itemId > 0, "Native item catalog unavailable");
            // Quotes are explicit isolated test fixtures, not recovered Kaosia stock or pricing.
            RequireMercenary(panel.ConfigureShop(catalog, new[] {new DarkSaver.Items.NativeShopOffer {itemId = itemId, buyPrice = 30, sellPrice = 10}}), "Fixture shop setup failed");
            RequireMercenary(panel.Open(TownService.Shop, player) && panel.Trade(itemId, 3, true) && panel.Trade(itemId, 2, false), "Quantity shop trade failed");
            RequireMercenary(session.CaptureInventory().gold == 9930 && session.CaptureInventory().Count(itemId) == 1, "Quantity shop totals mismatch");
            RequireMercenary(panel.Open(TownService.Inventory, player) && panel.SelectOwnedItem(itemId)
                && panel.SelectedInventoryItem != null && panel.SaveTown(), "Inventory inspection/modal save failed");
            var store = new NativeSaveStore(Path.Combine(output, "verification-save.json"));
            RequireMercenary(store.TryLoad(out var saved, out _) && saved.inventory.gold == 9930
                && saved.inventory.Count(itemId) == 1, "Town facility disk persistence mismatch");
            panel.Close(); panel.ConfigureShop(catalog, Array.Empty<DarkSaver.Items.NativeShopOffer>());
            Note("PASS: town overview/save/formation lock/class preview/quantity shop/inventory/disk persistence; shop quotes are isolated fixtures");
        }

        static void PrepareMercenaryDeployment(BattleTransitionController session, TopDownPlayerController player)
        {
            var ground = FindGround();
            RequireMercenary(ground != null && session.OpenFormationPreparation(), "Training preparation unavailable");
            var panel = session.GetComponent<BattleFormationPanel>();
            var playerCell = ground.WorldToCell(player.transform.position);
            var encounter = UnityEngine.Object.FindAnyObjectByType<BattleEncounter2D>();
            RequireMercenary(encounter != null, "Training encounter missing");
            var enemyCell = ground.WorldToCell(encounter.transform.position);
            foreach (var unit in hiredBeforeDeployment.units)
            {
                RequireMercenary(panel.AssignToPrimary(unit.persistentId), "Primary assignment rejected: " + panel.Message);
                bool placed = false;
                for (int radius = 1; radius <= 5 && !placed; radius++)
                for (int dy = -radius; dy <= radius && !placed; dy++)
                for (int dx = -radius; dx <= radius && !placed; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
                    var cell = playerCell + new Vector3Int(dx, dy, 0);
                    if (cell == enemyCell || cell == playerCell) continue;
                    placed = panel.TrySetPosition(unit.persistentId, new CombatGridCell(cell.x, ground.cellBounds.yMax - 1 - cell.y));
                }
                RequireMercenary(placed, "No native traversable soldier cell near player");
            }
            RequireMercenary(panel.Confirm(), "Formation confirm rejected: " + panel.Message);
            RequireMercenary(encounter.TryEncounter(player), "Prepared training encounter rejected: " + session.LastTransitionMessage);
            SessionState.SetInt(Key + "stage", 30);
            Note("Primary slots 101/102 confirmed; normal training encounter accepted");
        }

        static void TickMercenaryDeployment(int stage)
        {
            if (EditorApplication.timeSinceStartup < nextAction) return;
            nextAction = EditorApplication.timeSinceStartup + .25;
            var session = UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>();
            if (session == null || session.IsTransitioning) return;
            if (stage <= 34)
            {
                battle = UnityEngine.Object.FindAnyObjectByType<BattleSceneRoot>();
                presentation = UnityEngine.Object.FindAnyObjectByType<BattleMapPresentation>();
                RequireMercenary(battle != null && presentation != null && presentation.IsBound, "Normal scene entry did not bind hired bodies");
            }
            if (stage == 30)
            {
                RequireMercenary(presentation.CreatedFormationBodyCount == 2, "Expected two real formation bodies");
                foreach (short id in new short[] {101, 102})
                {
                    RequireMercenary(battle.TryGetUnit(id, out var unit) && unit.Alive, "Hired actor unavailable");
                    var body = presentation.transform.Find("Formation unit " + id);
                    RequireMercenary(body != null && body.GetComponent<SpriteRenderer>().enabled
                        && body.GetComponent<SpriteRenderer>().sprite != null
                        && body.GetComponent<DirectionalSpriteAnimator>().HasCompleteDirections, "Native soldier body/animation missing");
                }
                RequireMercenary(battle.TrySelectUnit(101) && presentation.TryOpenActionMenu(), "Hired actor action menu rejected");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "hired-soldier-menu.png"));
                SessionState.SetInt(Key + "stage", 31); Note("PASS: two native bodies deployed and soldier action menu opened"); return;
            }
            if (stage == 31)
            {
                RequireMercenary(battle.TryGetUnit(101, out var actor) && actor.Alive, "Selected soldier disappeared");
                if (actor.Ap < actor.MaxAp) return;
                RequireMercenary(presentation.TryChooseBasicCommand(BattleMapCommand.Move), "Soldier movement command rejected");
                RequireMercenary(presentation.TryGetReachableMovementDestinations(out var cells), "Soldier movement destinations unavailable");
                bool moved = false;
                foreach (var cell in cells)
                {
                    if (Math.Max(Math.Abs(cell.X - actor.X), Math.Abs(cell.Y - actor.Y)) != 1) continue;
                    var ground = FindGround();
                    if (!presentation.TryHandleWorldClick(ground.GetCellCenterWorld(new Vector3Int(cell.X, ground.cellBounds.yMax - 1 - cell.Y)))) continue;
                    mercenaryMoveTarget = cell; moved = true; break;
                }
                RequireMercenary(moved, "No accepted adjacent soldier movement"); moves++;
                SessionState.SetInt(Key + "stage", 32); return;
            }
            if (stage == 32)
            {
                if (battle.IsMovementInProgress(101)) return;
                RequireMercenary(battle.TryGetUnit(101, out var actor) && actor.X == mercenaryMoveTarget.X
                    && actor.Y == mercenaryMoveTarget.Y, "Soldier movement destination mismatch");
                if (actor.Ap < actor.MaxAp) return;
                RequireMercenary(presentation.TryChooseBasicCommand(BattleMapCommand.Rest), "Soldier rest rejected"); rests++;
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "hired-soldier-after-move-rest.png"));
                SessionState.SetInt(Key + "stage", 33); Note("PASS: hired soldier moved via command/world target and rested"); return;
            }
            if (stage == 33)
            {
                int step=SessionState.GetInt(Key+"troopOrderStep",0);
                if(step==0 || step==3 || step==6)
                    RequireMercenary(battle.TrySelectUnit(100) && presentation.TryOpenActionMenu(), "Troop action menu unavailable");
                else if(step==1 || step==4 || step==7) ClickBattleMenu("radial-6");
                else if(step==2)
                {
                    RequireMercenary(presentation.BattleWindowShellId==BattleMapPresentation.TroopShell, "Troop submenu missing");
                    foreach(int id in new[]{1,2,8})
                    {
                        var button=BattleMenuElement<UnityEngine.UIElements.Button>("troop-order-"+id);
                        RequireMercenary(button!=null && button.enabledInHierarchy && Math.Abs(button.style.width.value.value-32)<.1f,
                            "Native troop icon unavailable: "+id+" width="+(button==null ? -1 : button.style.width.value.value));
                    }
                    if(!SessionState.GetBool(Key+"troopOrderCaptured",false))
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"native-troop-orders.png"));
                        SessionState.SetBool(Key+"troopOrderCaptured",true);
                        return; // Capture at frame end while the menu remains open.
                    }
                    ClickBattleMenu("troop-order-2");
                    RequireMercenary(battle.TryGetTroopOrder(101,out var order) && order==BattleSceneRoot.TroopOrder.AutomaticAttack, "Attack order not applied");
                }
                else if(step==5)
                {
                    ClickBattleMenu("troop-order-8");
                    RequireMercenary(battle.TryGetTroopOrder(102,out var order) && order==BattleSceneRoot.TroopOrder.AutomaticDefense, "Defense order not shared by group");
                }
                else if(step==8)
                {
                    ClickBattleMenu("troop-order-1");
                    RequireMercenary(battle.TryGetTroopOrder(101,out var order) && order==BattleSceneRoot.TroopOrder.Direct, "Direct order not restored");
                    Note("PASS: native three-icon troop menu pointer selection attack/defense/direct; group state shared; commander manual");
                }
                else
                {
                    RequireMercenary(presentation.TryOpenSystemMenu(), "Retreat menu unavailable");
                    SessionState.SetInt(Key + "stage", 34); return;
                }
                SessionState.SetInt(Key+"troopOrderStep",step+1);return;
            }
            if (stage == 34)
            {
                RequireMercenary(presentation.TryRetreatImmediately(), "Immediate retreat rejected");
                SessionState.SetInt(Key + "stage", 35); return;
            }
            if (stage == 35)
            {
                if (SceneManager.GetSceneByName("Battle").isLoaded) return;
                var returned = session.CaptureOwnedRoster();
                RequireMercenary(returned != null && returned.units.Length == 2, "Returned roster missing");
                for (int i = 0; i < 2; i++)
                    RequireMercenary(returned.units[i].persistentId == hiredBeforeDeployment.units[i].persistentId
                        && returned.units[i].instanceId == hiredBeforeDeployment.units[i].instanceId
                        && returned.units[i].mp == returned.units[i].maxMp, "Permanent identity/MP return mismatch");
                RequireMercenary(session.SaveWorld(), "Returned roster save failed");
                var store = new NativeSaveStore(Path.Combine(output, "verification-save.json"));
                RequireMercenary(store.TryLoad(out var saved, out _) && saved.ownedRoster.units.Length == 2
                    && saved.ownedRoster.units[0].instanceId == hiredBeforeDeployment.units[0].instanceId, "Disk roster mismatch");
                RequireMercenary(UnityEngine.Object.FindAnyObjectByType<WorldSessionHud>().ReturnFromTraining(), "Town return rejected");
                SessionState.SetInt(Key + "stage", 36); return;
            }
            if (stage == 36 && SceneManager.GetSceneByName("Town_Runtime").isLoaded)
            {
                // Scene.isLoaded can become true before the travel coroutine restores input.
                var coordinator = UnityEngine.Object.FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>();
                if (coordinator != null && coordinator.IsTravelling) return;
                var player = UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
                RequireMercenary(player != null && player.isActiveAndEnabled && !player.UiInputBlocked, "Town input not restored");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "hired-troop-town-return.png"));
                Finish("PASS: town single hires -> training -> primary assignment -> real scene bodies/menu/move/rest -> retreat -> owned identity/save -> town return");
            }
        }
    }
}
