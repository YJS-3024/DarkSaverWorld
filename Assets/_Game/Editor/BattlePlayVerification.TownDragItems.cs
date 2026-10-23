using System.IO;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Items;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkSaver.EditorTools
{
    public static partial class BattlePlayVerification
    {
        static Button townDragSource;
        static Vector2 townDragDestination;
        static void BeginTownDrag(TownServicesPanel panel, string sourceKey, string targetKey)
        {
            townDragSource = TownItemElement<Button>(panel, sourceKey);
            var target = TownItemElement<Button>(panel, targetKey);
            RequireMercenary(townDragSource != null && target != null && townDragSource.enabledInHierarchy, "Drag source/target missing");
            var point = townDragSource.worldBound.center;
            var picked = townDragSource.panel.Pick(point);
            RequireMercenary(picked == townDragSource || townDragSource.Contains(picked), "Drag source obstructed");
            townDragDestination = target.worldBound.center;
            using (var e = PointerDownEvent.GetPooled(new Event {type=EventType.MouseDown,button=0,mousePosition=point})) { e.target=townDragSource; townDragSource.SendEvent(e); }
            using (var e = PointerMoveEvent.GetPooled(new Event {type=EventType.MouseDrag,button=0,mousePosition=townDragDestination})) { e.target=townDragSource; townDragSource.SendEvent(e); }
        }
        static void EndTownDrag()
        {
            var source = townDragSource; townDragSource = null;
            RequireMercenary(source != null, "No drag to finish");
            using var e = PointerUpEvent.GetPooled(new Event {type=EventType.MouseUp,button=0,mousePosition=townDragDestination});
            e.target=source; source.SendEvent(e);
        }
        static void TickTownDragItems(int stage)
        {
            if (EditorApplication.timeSinceStartup < nextAction) return;
            nextAction = EditorApplication.timeSinceStartup + .3;
            var session = Object.FindAnyObjectByType<BattleTransitionController>();
            var player = Object.FindAnyObjectByType<TopDownPlayerController>();
            if (session == null || player == null || session.IsTransitioning) return;
            var panel = session.GetComponent<TownServicesPanel>();
            int id = SessionState.GetInt(Key + "itemId", -1);
            void Step() => SessionState.SetInt(Key + "stage", stage + 1);
            void Check(long gold, int quantity) => RequireMercenary(session.CurrentGold == gold && session.OwnedItemCount(id) == quantity, "Drag trade resource mismatch");
            if (stage == 50) {
                RequireMercenary(TownItemElement<VisualElement>(panel, "town-service-native")?.style.backgroundImage.value.texture != null, "Native shop missing");
                RequireMercenary(TownItemElement<Button>(panel, "shop-detail-buy") == null && TownItemElement<TextField>(panel, "shop-quantity") == null
                    && TownItemElement<VisualElement>(panel, "trade-confirm-window") == null, "Legacy trade controls still visible");
                ClickTownItem(panel, "shop-item-0"); Check(1000,0);
                BeginTownDrag(panel, "shop-item-0", "shop-item-1"); EndTownDrag(); Check(1000,0); // Same-side cancellation.
                BeginTownDrag(panel, "shop-item-0", "close"); EndTownDrag(); Check(1000,0); // Outside either inventory.
                BeginTownDrag(panel, "shop-item-0", "shop-owned-0"); Step(); return;
            }
            if (stage == 51) {
                RequireMercenary(TownItemElement<VisualElement>(panel, "item-drag-preview")?.style.backgroundImage.value.sprite != null, "Drag preview missing");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "town-shop-drag-buy.png")); Step(); return;
            }
            if (stage == 52) {
                EndTownDrag(); Check(970,1); RequireMercenary(!panel.HasPendingTrade, "House confirmation entered ordinary item shop");
                ClickTownItem(panel, "shop-item-0"); Check(970,1); Step(); return;
            }
            if (stage == 53) {
                BeginTownDrag(panel, "shop-owned-0", "shop-item-0"); EndTownDrag(); Check(980,0);
                RequireMercenary(panel.SaveTown(), "Drag trade save failed");
                var store = new NativeSaveStore(Path.Combine(output, "verification-save.json"));
                RequireMercenary(store.TryLoad(out var saved, out _) && saved.inventory.gold == 980 && saved.inventory.Count(id) == 0, "Drag disk save mismatch");
                var state = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", player.transform.position);
                state.inventory.gold = 0;
                RequireMercenary(session.RestoreSaveState(state), "Zero-funds fixture rejected");
                BeginTownDrag(panel, "shop-item-0", "shop-owned-0"); EndTownDrag(); Check(0,0);
                RequireMercenary(session.RestoreSaveState(saved), "Restore saved drag state failed");
                var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemCatalog>(DarkSaver.Editor.ItemCatalogConverter.Output);
                var stacks = new System.Collections.Generic.List<NativeItemStack>();
                for (int i=0; i<catalog.Count && stacks.Count<29; i++)
                    if (catalog.GetAt(i).Id>0) stacks.Add(new NativeItemStack {itemId=catalog.GetAt(i).Id,quantity=stacks.Count+1});
                state = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", player.transform.position); state.inventory.items=stacks.ToArray();
                panel.Close(); RequireMercenary(session.RestoreSaveState(state) && panel.Open(TownService.Inventory,player), "Inventory fixture rejected");
                Step(); return;
            }
            if (stage == 54) {
                var background = TownItemElement<VisualElement>(panel,"inventory-native");
                var first = TownItemElement<Button>(panel,"owned-0");
                RequireMercenary(background?.style.backgroundImage.value.texture != null && background.style.width.value.value==409
                    && background.style.height.value.value==227,"Native inventory background missing");
                RequireMercenary(first.style.left.value.value-background.style.left.value.value==146
                    && first.style.top.value.value-background.style.top.value.value==42,"Inventory slot does not match original origin");
                RequireMercenary(TownItemElement<Button>(panel,"inventory-minimize").enabledInHierarchy,"Minimize unavailable");
                var gold = TownItemElement<VisualElement>(panel,"inventory-gold");
                RequireMercenary(gold != null && gold.tooltip==session.CurrentGold?.ToString()
                    && gold.style.left.value.value-background.style.left.value.value==50
                    && gold.style.top.value.value-background.style.top.value.value==193,"Inventory gold text/anchor mismatch");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-inventory-native.png"));
                SessionState.SetInt(Key+"inventoryMinimizeStep",0);
                ClickTownItem(panel,"inventory-next"); Step(); return;
            }
            if (stage == 55) {
                int minimizeStep = SessionState.GetInt(Key+"inventoryMinimizeStep",0);
                if (minimizeStep==1) {
                    var restore = TownItemElement<Button>(panel,"inventory-restore");
                    RequireMercenary(panel.IsInventoryMinimized && restore!=null && restore.style.width.value.value==29
                        && restore.style.height.value.value==30 && TownItemElement<Button>(panel,"owned-0")==null,"Minimized inventory did not hide full controls");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-inventory-minimized.png"));
                    SessionState.SetInt(Key+"inventoryMinimizeStep",2); return;
                }
                if (minimizeStep==2) {
                    ClickTownItem(panel,"inventory-restore"); SessionState.SetInt(Key+"inventoryMinimizeStep",3); return;
                }
                if (minimizeStep==3) {
                    RequireMercenary(!panel.IsInventoryMinimized && panel.InventoryPageIndex==1 && session.TryGetOwnedItemAt(28,out int selected,out _)
                        && panel.SelectedInventoryItem?.Id==selected && session.CurrentGold==980,"Restore lost page/selection/resources");
                    var details = TownItemElement<Label>(panel,"inventory-detail");
                    RequireMercenary(details != null && details.text.Contains("원본 슬롯 후보(0~6): "+ItemUsageConditions.EquipmentSlotsSummary(panel.SelectedInventoryItem)),
                        "Confirmed equipment position summary missing from inventory details");
                    ClickTownItem(panel,"inventory-confirm");
                    RequireMercenary(!panel.IsOpen && !player.UiInputBlocked,"Inventory close after restore failed");
                    Step(); return;
                }
                RequireMercenary(panel.InventoryPageIndex==1 && !TownItemElement<Button>(panel,"owned-1").enabledInHierarchy, "Inventory page/empty cell mismatch");
                ClickTownItem(panel,"owned-0"); ClickTownItem(panel,"inventory-minimize");
                SessionState.SetInt(Key+"inventoryMinimizeStep",1); return;
            }
            if (stage == 56) {
                var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemCatalog>(DarkSaver.Editor.ItemCatalogConverter.Output);
                var offers = new NativeShopOffer[13];
                for(int i=0;i<13;i++) { RequireMercenary(session.TryGetOwnedItemAt(i,out int owned,out _),"Offer fixture missing"); offers[i]=new NativeShopOffer{itemId=owned,buyPrice=30,sellPrice=10}; }
                RequireMercenary(panel.ConfigureShop(catalog,offers) && panel.Open(TownService.Shop,player),"Shop fixture rejected");
                SessionState.SetInt(Key+"dragLastId",offers[12].itemId); Step(); return;
            }
            if (stage == 57) { ClickTownItem(panel,"shop-next"); Step(); return; }
            if (stage == 58) {
                RequireMercenary(panel.ShopPageIndex==1 && !TownItemElement<Button>(panel,"shop-item-10").enabledInHierarchy,"Shop row/empty cells mismatch");
                ClickTownItem(panel,"shop-item-9"); Step(); return;
            }
            if (stage == 59) {
                RequireMercenary(panel.SelectedShopItem?.Id==SessionState.GetInt(Key+"dragLastId",-1),"Reused cell selected stale item");
                BeginTownDrag(panel,"shop-item-9","shop-owned-0"); EndTownDrag();
                RequireMercenary(session.CurrentGold==950 && session.OwnedItemCount(SessionState.GetInt(Key+"dragLastId",-1))==14,"Reused cell drag used stale item");
                ClickTownItem(panel,"shop-owned-13"); Step(); return;
            }
            if (stage == 60) {
                RequireMercenary(session.TryGetOwnedItemAt(13,out int unlisted,out _) && panel.SelectedShopInspectionItem?.Id==unlisted,"Unlisted inspection missing");
                BeginTownDrag(panel,"shop-owned-13","shop-item-0"); EndTownDrag();
                RequireMercenary(session.CurrentGold==950 && session.OwnedItemCount(unlisted)==14,"Unlisted item sold without quote");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-shop-drag-final.png")); Step(); return;
            }
            if (stage == 61) {
                BeginTownDrag(panel,"shop-item-0","shop-owned-0"); panel.Close(); EndTownDrag();
                RequireMercenary(session.CurrentGold==950 && !player.UiInputBlocked,"Closing mid-drag traded or retained lock");
                RequireMercenary(panel.Open(TownService.Shop,player),"Reopen shop failed"); Step(); return;
            }
            if (stage == 62) {
                // GameCore hosts several UI owners; address the document containing this shop,
                // not the first sibling HUD returned by GetComponentInChildren.
                InGameUi ui = null;
                var shopSource = TownItemElement<Button>(panel,"shop-item-0");
                foreach (var candidate in panel.GetComponentsInChildren<InGameUi>())
                    if (candidate.GetComponent<UIDocument>().rootVisualElement.Contains(shopSource)) ui = candidate;
                RequireMercenary(ui != null, "Shop UI missing for input cancellation");
                BeginTownDrag(panel,"shop-item-0","shop-owned-0");
                townDragSource.SetEnabled(false); EndTownDrag();
                RequireMercenary(session.CurrentGold==950,"Disabled source completed a trade");
                townDragSource = null;
                TownItemElement<Button>(panel,"shop-item-0").SetEnabled(true);
                BeginTownDrag(panel,"shop-item-0","shop-owned-0"); ui.SendMessage("OnApplicationFocus",false); EndTownDrag();
                RequireMercenary(session.CurrentGold==950,"Focus loss completed a trade");
                BeginTownDrag(panel,"shop-item-0","shop-owned-0"); ui.SendMessage("OnApplicationPause",true); EndTownDrag();
                RequireMercenary(session.CurrentGold==950,"Pause completed a trade");
                BeginTownDrag(panel,"shop-item-0","shop-owned-0");
                RequireMercenary(panel.SetShopPage(0),"Shop page change rejected"); EndTownDrag();
                RequireMercenary(session.CurrentGold==950,"Page change completed a stale trade");
                RequireMercenary(panel.SaveTown(),"Feedback fixture save failed");
                RequireMercenary(session.TryGetOwnedItemAt(13,out int unlisted,out _) && !panel.TradeDroppedItem(unlisted,false),"Unlisted feedback fixture unexpectedly sold");
                Step(); return;
            }
            if (stage == 63) {
                var trade = TownItemElement<Label>(panel,"shop-trade-status");
                var saved = TownItemElement<Label>(panel,"town-save-message");
                RequireMercenary(trade != null && saved != null && !string.IsNullOrEmpty(trade.text) && !string.IsNullOrEmpty(saved.text),"Trade/save feedback missing");
                RequireMercenary(trade.worldBound.yMax <= saved.worldBound.yMin,"Trade/save feedback overlaps");
                RequireMercenary(saved.style.top.value.value+saved.style.height.value.value <= DarkSaver.Rendering.GameScreen.Height,"Shop feedback exceeds logical screen");
                RequireMercenary(TownItemElement<Button>(panel,"shop-back").text.Contains("임시"),"Local facility navigation is not marked temporary");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-shop-feedback.png")); Step(); return;
            }
            if (stage == 64) {
                ClickTownItem(panel,"close"); RequireMercenary(!panel.IsOpen && !player.UiInputBlocked,"Native close failed");
                Step(); return;
            }
            if (stage == 65) {
                SessionState.SetString(Key+"characterInfoInventory",JsonUtility.ToJson(session.CaptureInventory()));
                ClickTownItem(panel,"town-character-info"); Step(); return;
            }
            if (stage == 66) {
                var window = TownItemElement<VisualElement>(panel,"character-info-native");
                RequireMercenary(panel.Service==TownService.CharacterInfo && player.UiInputBlocked && window?.style.backgroundImage.value.texture!=null
                    && window.style.width.value.value==335 && window.style.height.value.value==274,"Character info shell missing");
                RequireMercenary(TownItemElement<Button>(panel,"character-info-extra").enabledInHierarchy,"Character adjustment shell entry disabled");
                var face=TownItemElement<VisualElement>(panel,"character-info-portrait");
                RequireMercenary(face?.style.backgroundImage.value.texture==panel.CharacterInfoPortrait && panel.CharacterInfoPortrait!=null
                    && face.style.width.value.value==64 && face.style.height.value.value==64
                    && face.style.left.value.value-window.style.left.value.value==29 && face.style.top.value.value-window.style.top.value.value==46,"Original job portrait missing or misaligned");
                var name=TownItemElement<VisualElement>(panel,"character-info-name");
                var job=TownItemElement<VisualElement>(panel,"character-info-job");
                var gold=TownItemElement<VisualElement>(panel,"character-info-gold");
                RequireMercenary(name?.tooltip==session.PlayerName && job?.tooltip==panel.CharacterInfoJobName && panel.CharacterInfoJobName!="미확인"
                    && gold?.tooltip=="950","Character info values do not match session");
                RequireMercenary(name.style.left.value.value-window.style.left.value.value==146 && name.style.top.value.value-window.style.top.value.value==41
                    && job.style.top.value.value-window.style.top.value.value==58 && gold.style.top.value.value-window.style.top.value.value==124,"Character info value origins differ from original");
                var valuesPayload=new byte[724];
                valuesPayload[8]=7; valuesPayload[10]=12;
                for(int i=12;i<16;i++) valuesPayload[i]=0xff;
                valuesPayload[16]=0xd2; valuesPayload[17]=4;
                valuesPayload[0x96]=105; valuesPayload[0x98]=3;
                for(int c=0;c<2;c++) for(int row=0;row<8;row++) {
                    DarkSaver.Network.OriginalCharacterInfoValues.TryGetPayloadOffset(c,row,out int offset);
                    short value=(short)(c==0?-10-row:20+row);
                    valuesPayload[offset]=(byte)value; valuesPayload[offset+1]=(byte)(value>>8);
                }
                RequireMercenary(panel.BindCharacterInfoValues(valuesPayload),"Info display fixture binding failed");
                RequireMercenary(panel.BindCharacterInfoDemographics(25,2),"Info demographic fixture binding failed");
                RequireMercenary(panel.BindCharacterInfoWeaponName("검증 무기"),"Info weapon-name fixture binding failed");
                Step(); return;
            }
            if(stage==67) {
                var info=TownItemElement<VisualElement>(panel,"character-info-native");
                var weapon=TownItemElement<VisualElement>(panel,"character-info-weapon-name");
                var auxiliary=TownItemElement<VisualElement>(panel,"character-info-auxiliary-icon");
                var count=TownItemElement<VisualElement>(panel,"character-info-auxiliary-count");
                RequireMercenary(weapon?.tooltip=="검증 무기" && weapon.style.left.value.value-info.style.left.value.value==14
                    && weapon.style.top.value.value-info.style.top.value.value==145
                    && auxiliary?.style.backgroundImage.value.sprite==panel.CharacterAdjustmentClassIcon(105)
                    && auxiliary.style.left.value.value-info.style.left.value.value==29 && auxiliary.style.top.value.value-info.style.top.value.value==178
                    && count?.tooltip=="X 3" && count.style.left.value.value-info.style.left.value.value==73 && count.style.top.value.value-info.style.top.value.value==186,"Original weapon text or auxiliary class/count differs");
                var age=TownItemElement<VisualElement>(panel,"character-info-age");
                var gender=TownItemElement<VisualElement>(panel,"character-info-gender");
                RequireMercenary(age?.tooltip=="25" && gender?.tooltip=="남"
                    && age.style.left.value.value-info.style.left.value.value==146 && age.style.top.value.value-info.style.top.value.value==107
                    && gender.style.left.value.value-info.style.left.value.value==248 && gender.style.top.value.value-info.style.top.value.value==107,"Original age/gender values or origins differ");
                var level=TownItemElement<VisualElement>(panel,"character-info-level");
                var experience=TownItemElement<VisualElement>(panel,"character-info-experience");
                RequireMercenary(level?.tooltip=="12(7)" && experience?.tooltip=="4294967295/1234"
                    && level.style.left.value.value-info.style.left.value.value==146 && level.style.top.value.value-info.style.top.value.value==73
                    && experience.style.top.value.value-info.style.top.value.value==90,"Original two-level or unsigned experience display differs");
                for(int c=0;c<2;c++) for(int row=0;row<8;row++) {
                    var value=TownItemElement<VisualElement>(panel,"character-info-value-"+c+"-"+row);
                    RequireMercenary(value?.tooltip==panel.CharacterInfoValueText(c,row)
                        && value.style.left.value.value-info.style.left.value.value==(c==0?157:260)
                        && value.style.top.value.value-info.style.top.value.value==146+row*14,"Original info value text/position differs");
                }
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-character-info.png")); SessionState.SetInt(Key+"stage",85); return;
            }
            if(stage==85) { ClickTownItem(panel,"character-info-minimize"); SessionState.SetInt(Key+"stage",68); return; }
            if (stage == 68) {
                var restore = TownItemElement<Button>(panel,"character-info-restore");
                RequireMercenary(panel.IsCharacterInfoMinimized && restore!=null && restore.style.width.value.value==34 && restore.style.height.value.value==22
                    && TownItemElement<VisualElement>(panel,"character-info-native")==null,"Character info minimization failed");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-character-info-minimized.png")); Step(); return;
            }
            if (stage == 69) { ClickTownItem(panel,"character-info-restore"); Step(); return; }
            if (stage == 70) {
                RequireMercenary(panel.HasCharacterInfoDemographics && TownItemElement<VisualElement>(panel,"character-info-gender")?.tooltip=="남","Demographics lost on minimize/restore");
                RequireMercenary(panel.HasCharacterInfoValues && TownItemElement<VisualElement>(panel,"character-info-value-0-0")?.tooltip=="-10","Info values lost on minimize/restore");
                ClickTownItem(panel,"character-info-extra"); Step(); return;
            }
            if(stage==71) {
                var adjustment=TownItemElement<VisualElement>(panel,"character-adjustment-native");
                RequireMercenary(panel.IsCharacterAdjustmentOpen && player.UiInputBlocked && adjustment?.style.backgroundImage.value.texture!=null
                    && adjustment.style.width.value.value==540 && adjustment.style.height.value.value==435,"Character adjustment scene23 shell missing");
                var adjustmentFace=TownItemElement<VisualElement>(panel,"character-adjustment-portrait");
                RequireMercenary(adjustmentFace?.style.backgroundImage.value.texture==panel.CharacterInfoPortrait
                    && adjustmentFace.style.width.value.value==64 && adjustmentFace.style.height.value.value==64
                    && adjustmentFace.style.left.value.value-adjustment.style.left.value.value==20
                    && adjustmentFace.style.top.value.value-adjustment.style.top.value.value==47,"Adjustment portrait missing or misaligned");
                RequireMercenary(!TownItemElement<Button>(panel,"character-adjustment-apply").enabledInHierarchy,"Unconfirmed adjustment became executable");
                var adjustmentName=TownItemElement<VisualElement>(panel,"character-adjustment-name");
                var adjustmentJob=TownItemElement<VisualElement>(panel,"character-adjustment-job");
                RequireMercenary(adjustmentName?.tooltip==session.PlayerName && adjustmentJob?.tooltip==panel.CharacterInfoJobName
                    && adjustmentName.style.left.value.value-adjustment.style.left.value.value==50
                    && adjustmentName.style.top.value.value-adjustment.style.top.value.value==128
                    && adjustmentJob.style.left.value.value-adjustment.style.left.value.value==50
                    && adjustmentJob.style.top.value.value-adjustment.style.top.value.value==145,"Adjustment identity values or original origins differ");
                RequireMercenary(JsonUtility.ToJson(session.CaptureInventory())==SessionState.GetString(Key+"characterInfoInventory",""),"Adjustment entry mutated inventory");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-character-adjustment.png"));
                RequireMercenary(!panel.HasCharacterAdjustmentMagicSnapshot,"Unexpected production magic snapshot");
                var magicPanel=panel.GetComponent<MagicCatalogPanel>();
                if(magicPanel==null) magicPanel=panel.gameObject.AddComponent<MagicCatalogPanel>();
                if(magicPanel.Catalog==null) {
                    magicPanel.Configure(AssetDatabase.LoadAssetAtPath<DarkSaver.Magic.MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset"),player);
                    player.SetUiInputBlocked(true);
                }
                int fixtureMagic=0;
                for(int i=0;i<magicPanel.Catalog.Count;i++) {
                    var candidate=magicPanel.Catalog.GetAt(i);
                    if(candidate.Id>0 && candidate.Id<=short.MaxValue && BattleIconFrames.FromMagicCoordinates(candidate.IconX,candidate.IconY,0)!=null) {fixtureMagic=candidate.Id; break;}
                }
                RequireMercenary(fixtureMagic>0,"No converted magic icon for isolated visualization");
                var payload=new byte[724]; payload[0x40]=2; payload[0x6a]=1;
                payload[0x42]=(byte)fixtureMagic; payload[0x43]=(byte)(fixtureMagic>>8);
                payload[0x44]=payload[0x45]=0xff;
                payload[0x6c]=(byte)fixtureMagic; payload[0x6d]=(byte)(fixtureMagic>>8);
                SessionState.SetString(Key+"adjustmentProfile",JsonUtility.ToJson(session.CaptureCharacterProfile()));
                RequireMercenary(panel.BindCharacterAdjustmentMagicSnapshot(payload),"Magic display fixture binding failed");
                for(int i=0;i<6;i++) { payload[0x14+i*6]=(byte)(10+i); payload[0x16+i*6]=(byte)(12+i); payload[0x18+i*6]=(byte)(20+i); }
                payload[0x38]=9;
                RequireMercenary(panel.BindCharacterAdjustmentStatsSnapshot(payload),"Equipment-stat display fixture binding failed");
                var initialStats=(byte[])payload.Clone(); for(int i=0;i<6;i++) initialStats[0x14+i*6]=(byte)(8+i);
                RequireMercenary(panel.BindCharacterAdjustmentStatBars(payload,initialStats),"Equipment-stat bar comparison fixture binding failed");
                var costBytes=new byte[]{0x8b,2,0,2,0,2,0,2,0,2,0,2,0};
                RequireMercenary(DarkSaver.Network.CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(costBytes,out var costs,out _)
                    && panel.BindCharacterAdjustmentStatPreview(payload,initialStats,costs),"Stat preview fixture binding failed");
                var candidateBytes=new byte[]{0x87,0,0,105,0,0xff,0xff,2,0,110,0,115,0};
                RequireMercenary(DarkSaver.Network.CharacterAdjustmentOptionsNotification.TryReadOriginalPrefix(candidateBytes,out var candidateOptions,out _)
                    && panel.BindCharacterAdjustmentOptions(candidateOptions,AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset")),"Class option display fixture binding failed");
                Note("Isolated magic/stat/class visualization payload only; does not grant spells, stats or class eligibility");
                Step(); return;
            }
            if(stage==72) {
                var window=TownItemElement<VisualElement>(panel,"character-adjustment-native");
                var first=TownItemElement<Button>(panel,"character-adjustment-magic-0-0");
                var second=TownItemElement<Button>(panel,"character-adjustment-magic-1-0");
                RequireMercenary(first!=null && second!=null && first.style.width.value.value==32
                    && first.style.left.value.value-window.style.left.value.value==208 && first.style.top.value.value-window.style.top.value.value==47
                    && second.style.top.value.value-window.style.top.value.value==121,"Original magic grid origins differ");
                RequireMercenary(TownItemElement<VisualElement>(panel,"character-adjustment-magic-0-0-icon")!=null,"Known magic icon missing");
                for(int i=0;i<6;i++) {
                    var stat=TownItemElement<Label>(panel,"character-adjustment-stat-"+i);
                    RequireMercenary(stat?.text==panel.CharacterAdjustmentStatText(i) && stat.style.left.value.value-window.style.left.value.value==285
                        && stat.style.top.value.value-window.style.top.value.value==226+i*22,"Equipment stat values or row origins differ");
                }
                RequireMercenary(TownItemElement<VisualElement>(panel,"character-adjustment-points")?.tooltip=="9","Remaining stat points display differs");
                var bar=TownItemElement<VisualElement>(panel,"character-adjustment-bar-0");
                var gain=TownItemElement<VisualElement>(panel,"character-adjustment-gain-0");
                var statText=TownItemElement<Label>(panel,"character-adjustment-stat-0");
                RequireMercenary(bar!=null && gain!=null && bar.style.width.value.value==88 && gain.style.width.value.value==18
                    && bar.style.backgroundColor.value==Color.blue && gain.style.backgroundColor.value==Color.white
                    && bar.parent==statText.parent && bar.parent.IndexOf(bar)<bar.parent.IndexOf(statText),"Native stat fill widths/colors/text ordering differ");
                ClickTownItem(panel,"character-adjustment-magic-0-0"); Step(); return;
            }
            if(stage==73) {
                RequireMercenary(panel.CharacterAdjustmentSelectedMagicList==0 && panel.CharacterAdjustmentSelectedMagicIndex==0
                    && TownItemElement<VisualElement>(panel,"character-adjustment-magic-selection")!=null,"Magic list0 inspection failed");
                ClickTownItem(panel,"character-adjustment-magic-1-0"); Step(); return;
            }
            if(stage==74) {
                RequireMercenary(panel.CharacterAdjustmentSelectedMagicList==1 && panel.CharacterAdjustmentSelectedMagicIndex==0
                    && JsonUtility.ToJson(session.CaptureCharacterProfile())==SessionState.GetString(Key+"adjustmentProfile",""),"Magic inspection mutated learned abilities");
                RequireMercenary(panel.TryGetCharacterAdjustmentMagic(1,0,out short selectedMagic)
                    && panel.GetComponent<MagicCatalogPanel>().Catalog.TryGet(selectedMagic,out var detailEntry)
                    && panel.CharacterAdjustmentMagicDetail==TownServicesPanel.FormatCharacterAdjustmentMagicDetail(detailEntry)
                    && TownItemElement<VisualElement>(panel,"character-adjustment-magic-selection")?.tooltip==panel.CharacterAdjustmentMagicDetail
                    && TownItemElement<VisualElement>(panel,"character-adjustment-magic-selection-glyph-2-0")!=null,"Original magic detail fields or third line missing");
                var detailClip=TownItemElement<VisualElement>(panel,"character-adjustment-magic-selection");
                var finalLine=TownItemElement<VisualElement>(panel,"character-adjustment-magic-selection-glyph-2-0");
                RequireMercenary(detailClip.style.height.value.value==37 && finalLine.style.top.value.value+finalLine.style.height.value.value<=37,"Magic detail exceeds original 37px region");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-character-adjustment-magic.png"));
                Step(); return;
            }
            if(stage==75) {
                RequireMercenary(panel.CharacterAdjustmentClassSlot==1,"Original class response selection priority differs");
                RequireMercenary(TownItemElement<VisualElement>(panel,"character-adjustment-class-current")?.resolvedStyle.backgroundImage.sprite==panel.CharacterAdjustmentClassIcon(session.PlayerJobId)
                    && TownItemElement<VisualElement>(panel,"character-adjustment-class-right-icon")?.resolvedStyle.backgroundImage.sprite==panel.CharacterAdjustmentClassIcon(105),"Class menu current/right icon missing or incorrect");
                var currentIcon=TownItemElement<VisualElement>(panel,"character-adjustment-class-current");
                RequireMercenary(currentIcon.parent.IndexOf(currentIcon)>currentIcon.parent.IndexOf(TownItemElement<VisualElement>(panel,"character-adjustment-native")),"Current class icon hidden behind native window");
                ClickTownItem(panel,"character-adjustment-class-option"); Step(); return;
            }
            if(stage==76) {
                RequireMercenary(panel.CharacterAdjustmentClassSlot==2 && panel.CharacterAdjustmentOptionIndex==0,"Class candidate inspection failed");
                ClickTownItem(panel,"character-adjustment-option-next"); Step(); return;
            }
            if(stage==77) {
                var detail=TownItemElement<VisualElement>(panel,"character-adjustment-class-detail");
                RequireMercenary(TownItemElement<VisualElement>(panel,"character-adjustment-class-option-icon")?.resolvedStyle.backgroundImage.sprite==panel.CharacterAdjustmentClassIcon(115),"Class candidate icon did not follow paging");
                RequireMercenary(panel.CharacterAdjustmentOptionIndex==1 && panel.TryGetCharacterAdjustmentClass(2,out short choice) && choice==115
                    && detail?.tooltip==panel.CharacterAdjustmentClassDetail
                    && !TownItemElement<Button>(panel,"character-adjustment-option-next").enabledInHierarchy
                    && JsonUtility.ToJson(session.CaptureCharacterProfile())==SessionState.GetString(Key+"adjustmentProfile",""),"Class candidate paging mutated profile or failed bounds");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-character-adjustment-classes.png")); Step(); return;
            }
            if(stage==78) {
                foreach(int control in new[]{4,5,10,11}) RequireMercenary(!TownItemElement<Button>(panel,"character-adjustment-control-"+control).enabledInHierarchy,"HP/MP adjustment enabled");
                RequireMercenary(!TownItemElement<Button>(panel,"character-adjustment-control-6").enabledInHierarchy,"Stat decrease below opening baseline enabled");
                ClickTownItem(panel,"character-adjustment-control-12"); Step(); return;
            }
            if(stage==79) {
                RequireMercenary(panel.CharacterAdjustmentPointsText=="7" && panel.CharacterAdjustmentStatText(2)=="장착전:12/장착후:15/한계치:22"
                    && JsonUtility.ToJson(session.CaptureCharacterProfile())==SessionState.GetString(Key+"adjustmentProfile",""),"Stat preview failed cost or mutated profile");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-character-adjustment-stat-preview.png")); Step(); return;
            }
            if(stage==80) { ClickTownItem(panel,"character-adjustment-control-6"); Step(); return; }
            if(stage==81) {
                RequireMercenary(panel.CharacterAdjustmentPointsText=="9" && panel.CharacterAdjustmentStatText(2)=="장착전:12/장착후:14/한계치:22","Stat preview refund failed");
                ClickTownItem(panel,"character-adjustment-cancel"); Step(); return;
            }
            if (stage == 82) {
                RequireMercenary(!panel.HasCharacterAdjustmentStatPreview,"Stat preview survived close");
                RequireMercenary(!panel.HasCharacterAdjustmentOptions,"Class candidates survived close");
                RequireMercenary(!panel.HasCharacterAdjustmentMagicSnapshot,"Magic display snapshot survived close");
                RequireMercenary(!panel.HasCharacterAdjustmentStatsSnapshot,"Stat display snapshot survived close");
                RequireMercenary(!panel.IsCharacterAdjustmentOpen && player.UiInputBlocked,"Adjustment close lost parent modal lock");
                RequireMercenary(!panel.IsCharacterInfoMinimized && TownItemElement<VisualElement>(panel,"character-info-native")!=null,"Character info restore failed");
                ClickTownItem(panel,"character-info-back"); RequireMercenary(panel.Service==TownService.Overview && player.UiInputBlocked,"Character info back lost modal ownership");
                Step(); return;
            }
            if (stage == 83) { ClickTownItem(panel,"town-character-info"); Step(); return; }
            if (stage == 84) {
                RequireMercenary(panel.CharacterInfoWeaponName==null && panel.CharacterInfoAuxiliaryCountText==null,"Weapon/auxiliary data leaked into new facility visit");
                RequireMercenary(!panel.HasCharacterInfoDemographics,"Demographics leaked into new facility visit");
                RequireMercenary(!panel.HasCharacterInfoValues,"Info values leaked into new facility visit");
                RequireMercenary(!panel.IsCharacterInfoMinimized,"Character info reopened minimized"); ClickTownItem(panel,"character-info-close");
                RequireMercenary(!panel.IsOpen && !player.UiInputBlocked && JsonUtility.ToJson(session.CaptureInventory())==SessionState.GetString(Key+"characterInfoInventory","")
                    && session.CurrentGold==950,"Character info close mutated inventory or kept modal lock");
                RequireMercenary(panel.Open(TownService.CharacterInfo,player),"Character info lifecycle reopen failed");
                RequireMercenary(panel.OpenCharacterAdjustment(),"Adjustment lifecycle reopen failed");
                panel.enabled=false;
                RequireMercenary(!panel.IsOpen && !panel.IsCharacterAdjustmentOpen && !player.UiInputBlocked,"Disabled character info retained modal lock");
                panel.enabled=true;
                Finish("PASS: native inventory/shop trades/save/cancellations -> character info/minimize/restore -> native adjustment shell/disabled apply/close -> facility navigation/native close/disable -> inventory unchanged/input released");
            }
        }
    }
}
