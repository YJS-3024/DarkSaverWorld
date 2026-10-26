using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using DarkSaver.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DarkSaver.EditorTools.CanvasTesting;

namespace DarkSaver.EditorTools
{
    public static partial class BattlePlayVerification
    {
        [MenuItem("DarkSaver/Verification/Run class change to training102 round trip (isolated)")]
        public static void BeginClassChange()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Editor must be idle");
            foreach(string flag in new[]{"characterInfo","townItems","townHiring"})SessionState.SetBool(Key+flag,false);
            SessionState.SetBool(Key + "classChange", true);
            SessionState.SetInt(Key+"classMagicEditStep",0);
            SessionState.SetInt(Key+"classToolStep",0);
            SessionState.SetBool(Key+"equipmentPrep",false);
            SessionState.SetBool(Key + "mercenaryDeployment", false);
            try { BeginTownTraining(); }
            catch { SessionState.SetBool(Key + "classChange", false); throw; }
        }

        static void PrepareClassChange(BattleTransitionController session, TopDownPlayerController player)
        {
            // Explicit isolated eligibility/wounded fixture, never applied to a user save.
            var saved = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", player.transform.position);
            saved.character.hasLevels = true; saved.character.baseLevel = 12; saved.character.jobLevel = 5;
            saved.character.hasClassAbilities=true;saved.character.learnedMagicIds=new[]{101,102};
            saved.character.usableSkillIds=new[]{10101,10301,21101}; // Explicit old knowledge fixture, not original starting spells.
            saved.hasVitals = true; saved.hp = 17; saved.maxHp = 200; saved.mp = 3; saved.maxMp = 45; saved.lifeState = 0; saved.experience = 1234;
            var itemCatalog=AssetDatabase.LoadAssetAtPath<DarkSaver.Items.ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
            int itemId=0;for(int i=0;i<itemCatalog.Count;i++)if(itemCatalog.GetAt(i).Id>0 && itemCatalog.GetAt(i).GetNumericValue(3)==7 && !string.IsNullOrEmpty(itemCatalog.GetAt(i).Name)){itemId=itemCatalog.GetAt(i).Id;break;}
            int weaponId=0;for(int i=0;i<itemCatalog.Count;i++)if(itemCatalog.GetAt(i).Id>0 && itemCatalog.GetAt(i).GetNumericValue(3)!=7){weaponId=itemCatalog.GetAt(i).Id;break;}
            var toolStacks=new System.Collections.Generic.List<NativeItemStack> {new NativeItemStack {itemId=weaponId,quantity=2},new NativeItemStack {itemId=itemId,quantity=3}};
            for(int i=0;i<itemCatalog.Count && toolStacks.Count<8;i++)
                if(itemCatalog.GetAt(i).GetNumericValue(3)==7 && itemCatalog.GetAt(i).Id!=itemId)
                    toolStacks.Add(new NativeItemStack {itemId=itemCatalog.GetAt(i).Id,quantity=1});
            RequireMercenary(toolStacks.Count==8,"Seven native consumables required for paging fixture");
            toolStacks[3]=new NativeItemStack {itemId=307,quantity=1};
            toolStacks[4]=new NativeItemStack {itemId=313,quantity=1};
            toolStacks[5]=new NativeItemStack {itemId=315,quantity=1};
            toolStacks[toolStacks.Count-1]=new NativeItemStack {itemId=310,quantity=1};
            saved.inventory.items=toolStacks.ToArray();
            SessionState.SetInt(Key+"classToolWeapon",weaponId);
            SessionState.SetInt(Key+"classToolItem",itemId);
            RequireMercenary(session.RestoreSaveState(saved), "Class fixture restore failed");
            var panel = session.GetComponent<TownServicesPanel>();
            RequireMercenary(panel != null && panel.Open(TownService.Inventory,player),"Equipment preparation unavailable");
            SessionState.SetBool(Key+"equipmentPrep",true);SessionState.SetInt(Key+"equipmentStep",0);
            SessionState.SetInt(Key + "stage", 40); nextAction = EditorApplication.timeSinceStartup + .3;
            Note("Isolated base12/job5, wounded, pre-existing learned IDs101/102; preparing actual equipment before class change");
        }

        static void TickEquipmentPreparation(BattleTransitionController session,TownServicesPanel panel,TopDownPlayerController player)
        {
            int step=SessionState.GetInt(Key+"equipmentStep",0),weapon=SessionState.GetInt(Key+"classToolWeapon",0);
            if(step==0)ClickTownItem(panel,"equipment-slot-3");
            else if(step==1)ClickTownItem(panel,"equipment-remove");
            else if(step==2)ClickTownItem(panel,"owned-0");
            else if(step==3)
            {
                ClickTownItem(panel,"equipment-slot-2");RequireMercenary(session.EquipmentItemAt(2)== -1,"Equipment selection committed early");
            }
            else if(step==4)ClickTownItem(panel,"equipment-confirm");
            else if(step==5)
            {
                RequireMercenary(session.EquipmentItemAt(2)==weapon && session.EquipmentItemAt(3)==0 && session.OwnedItemCount(weapon)==2,"Equipment confirm changed ownership quantity or wrong slot");
                RequireMercenary(session.TryGetPlayerPhysicalStats(out int a,out _,out int attack,out _) && attack==a+40,"Native dagger bonus not reflected in session");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-native-equipment-confirmed.png"));
                var save=TownItemElement<Button>(panel,"town-save");var equip=TownItemElement<Button>(panel,"equipment-confirm");
                RequireMercenary(save!=null && equip!=null && !save.worldBound.Overlaps(equip.worldBound),"Equipment and save buttons overlap");
            }
            else if(step==6)ClickTownItem(panel,"town-save");
            else if(step==7)RequireMercenary(panel.Open(TownService.CharacterInfo,player),"Equipped character info unavailable");
            else if(step==8)
            {
                RequireMercenary(panel.CharacterInfoWeaponDisplayText!=null && panel.CharacterInfoWeaponDisplayText.Contains("대거")
                    && panel.CharacterInfoPhysicalBreakdown.Contains("장비"),"Actual equipment name or stat source missing");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"town-native-equipped-self-info.png"));
                Note("PASS: pointer equipment selection/clear opposite slot/confirm; dagger +40; ownership unchanged; equipped info displayed; explicit save");
            }
            else
            {
                RequireMercenary(panel.Open(TownService.ClassChange,player) && panel.RequestClassChange(),"Class confirmation after equipment unavailable");
                SessionState.SetBool(Key+"equipmentPrep",false);return;
            }
            SessionState.SetInt(Key+"equipmentStep",step+1);
        }

        static void TickClassChange(int stage)
        {
            if (EditorApplication.timeSinceStartup < nextAction) return;
            nextAction = EditorApplication.timeSinceStartup + .3;
            var session = UnityEngine.Object.FindAnyObjectByType<BattleTransitionController>();
            var coordinator = UnityEngine.Object.FindAnyObjectByType<WorldSceneCoordinator>();
            var player = UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
            if (session == null || player == null || session.IsTransitioning || coordinator != null && coordinator.IsTravelling) return;
            var panel = session.GetComponent<TownServicesPanel>();
            if(SessionState.GetBool(Key+"equipmentPrep",false)){TickEquipmentPreparation(session,panel,player);return;}
            if (stage == 40)
            {
                Button button = null;
                foreach (var document in panel.GetComponentsInChildren<InGameUi>().Select(CanvasProbe.For))
                    if (document.rootVisualElement.Q<Button>("class-change") is Button found) button = found;
                if (button == null || button.panel == null || button.worldBound.width <= 0) return;
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "class-confirmation.png"));
                SessionState.SetInt(Key + "stage", 48); return;
            }
            if(stage==48) {ClickTownItem(panel,"class-detail");SessionState.SetInt(Key+"stage",49);return;}
            if(stage==49) {
                var label=TownItemElement<Label>(panel,"class-preview");
                var catalog=session.GetComponent<MagicCatalogPanel>().Catalog;
                foreach(int id in new[]{101,102,2401}) {
                    RequireMercenary(catalog.TryGet(id,out var entry) && label!=null && label.text.Contains(entry.Name+" (#"+id+")"),"Class preview lost explicit spell ID "+id);
                }
                RequireMercenary(session.CaptureCharacterProfile().learnedMagicIds.SequenceEqual(new[]{101,102}),"Preview granted or deleted learned spells");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"class-magic-retention-preview.png"));
                SessionState.SetInt(Key+"stage",41);return;
            }
            if (stage == 41)
            {
                ClickTownItem(panel,"class-change");
                var profile = session.CaptureCharacterProfile();
                RequireMercenary(profile.jobId == 105 && profile.baseLevel == 12 && profile.jobLevel == 1
                    && profile.learnedMagicIds.SequenceEqual(new[]{101,102,2401})
                    && profile.unspentClassChangeStatPoints==GameConstants.ClassChangeAdditionalStatPoints
                    && session.PlayerHp == 240 && session.PlayerMp == 48 && session.PlayerExperience == 1234,
                    "Class/vitals/base/job/XP change mismatch");
                RequireMercenary(player.GetComponent<SpriteRenderer>().sprite.name.StartsWith("Class105_") && panel.SaveTown(), "Class artwork/save failed");
                SessionState.SetInt(Key + "stage", 42); Note("PASS: class105, base12/job1, native240HP/48MP full, preserved old learned IDs101/102 and explicit class grant2401, explicit save"); return;
            }
            if (stage == 42)
            {
                int step=SessionState.GetInt(Key+"classMagicEditStep",0);
                SessionState.SetInt(Key+"classMagicEditStep",step+1);
                if(step==0) { panel.Close();RequireMercenary(panel.Open(TownService.CharacterInfo,player),"Information open failed");return; }
                if(step==1) { ClickTownItem(panel,"character-info-extra");return; }
                if(step==2 || step==4 || step==8) {ClickTownItem(panel,"character-adjustment-magic-1-1");return;}
                if(step==3 || step==5 || step==9) {ClickTownItem(panel,"character-adjustment-control-2");return;}
                if(step==6) {ClickTownItem(panel,"character-adjustment-magic-0-1");return;}
                if(step==7) {ClickTownItem(panel,"character-adjustment-control-3");return;}
                if(step==10) {
                    RequireMercenary(session.CaptureCharacterProfile().hasBattleMagicSelection==false,"Preview committed magic selection early");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"battle-magic-selection-preview.png"));return;
                }
                if(step==11) {ClickTownItem(panel,"character-adjustment-control-19");return;}
                if(step==12) {
                    var selected=session.CaptureCharacterProfile();
                    RequireMercenary(selected.hasBattleMagicSelection && selected.battleMagicIds.SequenceEqual(new[]{101})
                        && selected.learnedMagicIds.SequenceEqual(new[]{101,102,2401}),"Selected level or learned retention mismatch");
                    RequireMercenary(panel.SaveTown(),"Battle magic selection save failed");
                    Note("PASS: pointer selected learned Lv1 for battle, retained Lv2/class spell, explicit save");return;
                }
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "class105-town.png")); panel.Close();
                var roots = SceneManager.GetSceneByName("Town_Runtime").GetRootGameObjects();
                var returned = roots.Single(go => go.name == "Spawn FromTraining");
                player.transform.position = returned.transform.position; player.GetComponent<Rigidbody2D>().position = returned.transform.position;
                Physics2D.SyncTransforms();
                RequireMercenary(player.TryMoveToWorld(roots.Single(go => go.name == "Training Entrance").transform.position), "Class105 training approach failed");
                SessionState.SetInt(Key + "stage", 43); return;
            }
            if (stage == 43)
            {
                if (!SceneManager.GetSceneByName("Training102_Runtime").isLoaded) return;
                RequireMercenary(!SceneManager.GetSceneByName("Training101_Runtime").isLoaded && session.PlayerJobId == 105 && session.SaveWorld(), "Wrong training tier/save after class change");
                if (!SceneManager.GetSceneByName("Battle").isLoaded)
                    RequireMercenary(UnityEngine.Object.FindAnyObjectByType<BattleEncounter2D>().TryEncounter(player), "Class105 normal encounter rejected");
                SessionState.SetInt(Key + "stage", 44); Note("PASS: new class automatically entered Training102"); return;
            }
            if (stage == 44)
            {
                battle = UnityEngine.Object.FindAnyObjectByType<BattleSceneRoot>(); presentation = UnityEngine.Object.FindAnyObjectByType<BattleMapPresentation>();
                RequireMercenary(battle != null && presentation != null && presentation.IsBound && battle.PlayerJobId == 105
                    && battle.PlayerMaxHp == 240 && battle.PlayerMaxMp == 48, "New class battle binding/stats failed");
                battle.TryGetUnit(100,out var equippedActor);
                var jobs=AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");jobs.TryGet(105,out var equippedJob);
                RequireMercenary(equippedActor.Attack==equippedJob.GetNumericValue(6)+40 && session.EquipmentItemAt(2)==SessionState.GetInt(Key+"classToolWeapon",0),"Equipment lost or double-counted through class change/battle entry");
                RequireMercenary(battle.TrySelectUnit(100),"Primary selection for learned-spell display failed");
                RequireMercenary(presentation.DisplayedMagicCount==1,"Selected primary lost learned-spell entries");
                int[] expected={101};
                for(int i=0;i<expected.Length;i++)RequireMercenary(presentation.TryGetDisplayedMagic(i,out var magic) && magic.Id==expected[i],"Battle learned-spell ID mismatch");
                int toolStep=SessionState.GetInt(Key+"classToolStep",0);
                if(toolStep==0) {
                    battle.TryGetUnit(100,out var ready);if(ready.Ap<ready.MaxAp)return;
                    // Explicit isolated injury fixture; actual use must heal through the session gateway.
                    RequireMercenary(battle.ApplyHp(100,battle.PlayerMaxHp-60),"Item recovery injury fixture failed");
                    RequireMercenary(presentation.TryOpenActionMenu() && presentation.TryChooseCommand(BattleMapCommand.Tools),"Session tool window unavailable");
                    SessionState.SetInt(Key+"classToolStep",1);return;
                }
                if(toolStep==1) {
                    RequireMercenary(presentation.TryGetToolDisplay(100,out var rows) && rows.Count==7 && rows[0].ItemId==SessionState.GetInt(Key+"classToolItem",0) && rows[0].Count==3,"Actual saved inventory did not bind to battle tools");
                    RequireMercenary(BattleMenuElement<Button>("tool-row-0").enabledSelf,"Recovery item selection unavailable");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"battle-real-inventory-tools.png"));
                    SessionState.SetInt(Key+"classToolStep",2);return;
                }
                if(toolStep==2){ClickBattleMenu("tool-next");SessionState.SetInt(Key+"classToolStep",3);return;}
                if(toolStep==3)
                {
                    RequireMercenary(presentation.ToolDisplayOffset==1,"Native final full-page overlap differs");
                    RequireMercenary(BattleMenuElement<Button>("tool-next").resolvedStyle.display==DisplayStyle.None && BattleMenuElement<Button>("tool-prev").enabledSelf,"Native tool page button states differ");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"battle-real-inventory-tools-page2.png"));
                    SessionState.SetInt(Key+"classToolStep",4);return;
                }
                if(toolStep==4){ClickBattleMenu("tool-prev");SessionState.SetInt(Key+"classToolStep",5);return;}
                if(toolStep==5)
                {
                    RequireMercenary(presentation.ToolDisplayOffset==0,"Native first page was not restored");
                    ClickBattleMenu("tool-row-0");SessionState.SetInt(Key+"classToolStep",6);return;
                }
                if(toolStep==6)
                {
                    RequireMercenary(presentation.IsToolTargeting && session.OwnedItemCount(SessionState.GetInt(Key+"classToolItem",0))==3,"Selection consumed an item early");
                    RequireMercenary(presentation.TryHandleCancelInput(false) && !presentation.IsToolTargeting,"Item targeting cancel failed");
                    RequireMercenary(session.OwnedItemCount(SessionState.GetInt(Key+"classToolItem",0))==3,"Cancel consumed an item");
                    RequireMercenary(presentation.TryOpenActionMenu() && presentation.TryChooseCommand(BattleMapCommand.Tools),"Tool menu reopen failed");
                    SessionState.SetInt(Key+"classToolStep",7);return;
                }
                if(toolStep==7){ClickBattleMenu("tool-row-0");SessionState.SetInt(Key+"classToolStep",8);return;}
                if(toolStep==8)
                {
                    int item=SessionState.GetInt(Key+"classToolItem",0);int hp=battle.PlayerHp,mp=battle.PlayerMp,ap=battle.PlayerAp;
                    RequireMercenary(!presentation.TryUseSelectedRecoveryTool(battle.PrimaryEnemyId) && session.OwnedItemCount(item)==3,"Invalid item target changed inventory");
                    battle.TryGetUnit(100,out var actor);
                    var ground=UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>()
                        .FirstOrDefault(map=>map.name=="Ground" && map.gameObject.scene.name=="Map_102_;");
                    RequireMercenary(ground!=null,"Current Training102 ground is unavailable");
                    var point=ground.GetCellCenterWorld(new Vector3Int(actor.X,ground.cellBounds.yMax-1-actor.Y));
                    RequireMercenary(presentation.TryHandleWorldClick(point),"Recovery item map target rejected: "+presentation.LastInputMessage);
                    RequireMercenary(battle.PlayerHp==Math.Min(battle.PlayerMaxHp,hp+15) && battle.PlayerMp==mp && battle.PlayerAp==ap-6
                        && session.OwnedItemCount(item)==2 && !presentation.IsToolTargeting,"Recovery item AP/vitals/quantity transaction mismatch");
                    RequireMercenary(session.CaptureCharacterProfile().learnedMagicIds.SequenceEqual(new[]{101,102,2401}),"Item use granted a learned magic");
                    SessionState.SetInt(Key+"classToolStep",9);return;
                }
                if(toolStep==9)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"battle-real-item-use.png"));
                    Note("PASS: native tools paging; pointer recovery selection/cancel; invalid target no-op; actual item use native EA HP+15/AP-6/count3->2; learned spells unchanged");
                    SessionState.SetInt(Key+"classToolStep",10);return;
                }
                if(toolStep==10)
                {
                    battle.TryGetUnit(100,out var actor);if(actor.Ap<actor.MaxAp)return;
                    RequireMercenary(presentation.TryOpenActionMenu() && presentation.TryChooseCommand(BattleMapCommand.Tools),"Quick tool window unavailable");
                    SessionState.SetInt(Key+"classToolStep",11);return;
                }
                if(toolStep==11){ClickBattleMenu("tool-next");SessionState.SetInt(Key+"classToolStep",12);return;}
                if(toolStep==12){ClickBattleMenu("tool-row-5");SessionState.SetInt(Key+"classToolStep",13);return;}
                if(toolStep==13)
                {
                    int ap=battle.PlayerAp,mp=battle.PlayerMp,cost=battle.PlayerAttackApCost;
                    RequireMercenary(presentation.IsToolTargeting && session.OwnedItemCount(310)==1,"Quick selected wrong item or spent early");
                    battle.TryGetUnit(100,out var actor);
                    var ground=UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>().FirstOrDefault(map=>map.name=="Ground" && map.gameObject.scene.name=="Map_102_;");
                    RequireMercenary(ground!=null,"Quick target ground unavailable");
                    RequireMercenary(presentation.TryHandleWorldClick(ground.GetCellCenterWorld(new Vector3Int(actor.X,ground.cellBounds.yMax-1-actor.Y))),"Quick item target rejected");
                    RequireMercenary(battle.TryGetTemporaryQuick(100,out int percent,out _) && percent==30
                        && battle.PlayerAttackApCost<cost && battle.PlayerAp==ap-6 && battle.PlayerMp==mp
                        && session.OwnedItemCount(310)==0,"Quick cost reduction/quantity/MP mismatch");
                    RequireMercenary(session.CaptureCharacterProfile().learnedMagicIds.SequenceEqual(new[]{101,102,2401}),"Quick item granted knowledge");
                    SessionState.SetInt(Key+"classToolStep",14);return;
                }
                if(toolStep==14)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"battle-real-quick-item.png"));
                    Note("PASS: pointer Quick item selection + self map target; native linked2203; no MP/knowledge charge; count1->0; action expenditure reduced30percent, recovery speed unchanged");
                    SessionState.SetInt(Key+"classToolStep",15);return;
                }
                if(TickStatusItems(toolStep,battle,presentation,session))return;
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "class105-training-battle.png"));
                RequireMercenary(presentation.TryOpenSystemMenu(), "Class105 retreat UI failed");
                SessionState.SetInt(Key + "stage", 45); return;
            }
            if (stage == 45)
            {
                ClickBattleMenu("system-retreat");
                SessionState.SetInt(Key + "stage", 46); return;
            }
            if (stage == 46)
            {
                if (SceneManager.GetSceneByName("Battle").isLoaded) return;
                RequireMercenary(session.SaveWorld(), "Class105 battle return save failed");
                var store = new NativeSaveStore(Path.Combine(output, "verification-save.json"));
                RequireMercenary(store.TryLoad(out var saved, out _) && saved.character.jobId == 105
                    && saved.character.learnedMagicIds.SequenceEqual(new[]{101,102,2401})
                    && saved.character.hasBattleMagicSelection && saved.character.battleMagicIds.SequenceEqual(new[]{101})
                    && saved.inventory.Count(SessionState.GetInt(Key+"classToolItem",0))==2 && saved.inventory.Count(310)==0 && saved.inventory.Count(307)==0 && saved.inventory.Count(313)==0 && saved.inventory.Count(315)==0
                    && saved.inventory.Count(SessionState.GetInt(Key+"classToolWeapon",0))==2
                    && saved.character.EquipmentItemAt(2)==SessionState.GetInt(Key+"classToolWeapon",0) && saved.character.EquipmentItemAt(3)==0
                    && saved.character.unspentClassChangeStatPoints==GameConstants.ClassChangeAdditionalStatPoints
                    && saved.character.baseLevel == 12 && saved.character.jobLevel == 1 && saved.experience == 1234,
                    "Class105 saved progression mismatch");
                RequireMercenary(session.RestoreSaveState(saved) && session.TryRestoreCharacterArtwork(player), "Class105 restore/artwork failed");
                RequireMercenary(UnityEngine.Object.FindAnyObjectByType<WorldSessionHud>().ReturnFromTraining(), "Class105 town return rejected");
                SessionState.SetInt(Key + "stage", 47); return;
            }
            if (stage == 47 && SceneManager.GetSceneByName("Town_Runtime").isLoaded)
            {
                RequireMercenary(player.isActiveAndEnabled && !player.UiInputBlocked && player.GetComponent<SpriteRenderer>().sprite.name.StartsWith("Class105_"), "Class105 world input/artwork not restored");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "class105-town-return.png"));
                Finish("PASS: confirmed class100->105, base12/job1, full native HP/MP, retained learned IDs101/102 plus class grant2401 in preview/pointer confirmation/save -> pointer-edited battle-use list101 (learned101/102/2401 retained) -> auto Training102 -> real battle -> retreat -> disk restore -> town");
            }
        }
    }
}
