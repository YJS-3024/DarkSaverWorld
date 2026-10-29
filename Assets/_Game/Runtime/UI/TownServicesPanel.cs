using DarkSaver.Combat;
using DarkSaver.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.UI
{
    public enum TownService { Overview, Shop, ClassChange, Mercenary, Dialogue, Inventory, CharacterInfo, Quests, Temple }

    [DisallowMultipleComponent]
    public sealed partial class TownServicesPanel : MonoBehaviour
    {
        private TopDownPlayerController player;
        private InGameUi ui;
        private string speaker;
        private string[] lines = System.Array.Empty<string>();
        private int lineIndex;
        private TownService afterDialogue = TownService.Overview;
        public string SaveMessage { get; private set; }
        public bool IsOpen { get; private set; }
        public TownService Service { get; private set; }
        public string Speaker => speaker;
        public string CurrentLine => lineIndex < lines.Length ? lines[lineIndex] : string.Empty;
        public bool CanOpen => CanOpenService(IsOpen?Service:TownService.Inventory);
        public bool CanOpenService(TownService service)
        {
            if(FirstPlayTutorial.BlocksWorldActions || !isActiveAndEnabled || SceneManager.GetSceneByName("Battle").isLoaded
                || DarkSaver.Events.Scenario01Controller.BlocksWorldActions
                || (GetComponent<BattleTransitionController>()?.IsTransitioning ?? false)
                || FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>()?.IsTravelling==true)return false;
            return service==TownService.Inventory || service==TownService.CharacterInfo
                ? DarkSaver.World.PersonalEquipmentContext.IsReady
                : DarkSaver.World.TownSceneContext.IsReady || service==TownService.Mercenary && SceneManager.GetSceneByName("Interior2900_Runtime").isLoaded || service==TownService.Temple && BattleTransitionController.IsTempleReady;
        }

        public bool OpenFromNpc(TownService service,TopDownPlayerController controller,string name,int questId=0)
        {
            if(service==TownService.Quests && (QuestContent==null || System.Array.Find(QuestContent.rows,q=>q.id==questId)==null))return false;
            if(!Open(service,controller))return false;
            speaker=name;
            if(service==TownService.Quests)selectedQuest=questId;
            return true;
        }

        public bool Open(TownService service, TopDownPlayerController controller)
        {
            if(!DarkSaver.Core.OriginalRestorationPolicy.AllowSinglePlayerRules
                && service!=TownService.Shop && service!=TownService.Inventory && service!=TownService.CharacterInfo && !(service==TownService.Mercenary && SceneManager.GetSceneByName("Interior2900_Runtime").isLoaded))return false;
            if(service==TownService.Overview)return false;
            if(DarkSaver.World.ShopInteriorContext.Current!=null)
            {
                if(service!=TownService.Shop && service!=TownService.Inventory && service!=TownService.CharacterInfo)return false;
            }
            if(BattleTransitionController.IsTempleReady && service!=TownService.Temple && service!=TownService.Inventory && service!=TownService.CharacterInfo)return false;
            if(service==TownService.Temple && !BattleTransitionController.IsTempleReady)return false;
            if (service == TownService.Quests && DarkSaver.World.TownSceneContext.Current?.Key != "Kaosia") return false;
            if (!CanOpenService(service) || controller == null || !controller.isActiveAndEnabled
                || !System.Enum.IsDefined(typeof(TownService), service)) return false;
            if (!IsOpen)
            {
                GetComponent<ItemCatalogPanel>()?.Close(); GetComponent<MagicCatalogPanel>()?.Close();
                GetComponent<BattleFormationPanel>()?.Cancel();
                if (controller.UiInputBlocked) return false;
            }
            else if (player != controller) return false;
            EnsureAnalyzedTownBindings();
            player = controller; Service = service; IsOpen = true;inspectedWorldUnit=null;originalWorldInventory=null;
            ResetServiceInput();
            if(service==TownService.CharacterInfo) BindSessionCharacterInfoValues();
            speaker = null; lines = System.Array.Empty<string>(); lineIndex = 0; afterDialogue = TownService.Overview;
            player.SetUiInputBlocked(true);
            if((service==TownService.CharacterInfo || service==TownService.Inventory) && GetComponent<NativeWorldWindows>()?.IsAvailable==true)GetComponent<NativeWorldWindows>().BindPersonalWindow(this);
            return true;
        }

        public bool OpenDialogue(string name, string[] text, TopDownPlayerController controller, TownService destination = TownService.Overview)
        {
            if (string.IsNullOrWhiteSpace(name) || text == null || text.Length == 0) return false;
            foreach (string line in text) if (string.IsNullOrWhiteSpace(line)) return false;
            if (!System.Enum.IsDefined(typeof(TownService), destination) || destination == TownService.Dialogue) return false;
            var copy = (string[])text.Clone();
            if (!Open(TownService.Dialogue, controller)) return false;
            speaker = name; lines = copy; afterDialogue = destination; return true;
        }

        public bool NextLine()
        {
            if (!IsOpen || Service != TownService.Dialogue || lines.Length == 0) return false;
            if (++lineIndex >= lines.Length)
            {
                if (afterDialogue == TownService.Overview) Close();
                else { var next = afterDialogue; if (!Open(next, player)) Close(); }
            }
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false; ResetServiceInput(); if (player != null) player.SetUiInputBlocked(false);
            player = null; speaker = null; lines = System.Array.Empty<string>(); lineIndex = 0; ui?.Hide();
        }

        private void ResetServiceInput()
        {
            ui?.CancelItemDrags();
            nativeMercenaryNpc=null;
            CancelPendingTrade();
            pendingClassId = 0; classChangeMessage = null;
            shopPage = 0; shopQuantity = "1"; shopMessage = null; ClearShopInspection();
            inventoryPage = 0; selectedInventoryId = -1; selectedEquipmentSlot=-1; equipmentBaseline=null;
            IsInventoryMinimized = false;
            IsCharacterInfoMinimized = false;
            IsCharacterAdjustmentOpen = false;
            ClearCharacterAdjustmentMagic();
            ClearCharacterAdjustmentStats();
            ClearCharacterAdjustmentOptions();
            ClearCharacterInfoValues();
            detailSource = null; detailText = null;
            hireSkillIndex = hirePage = 0; hireMessage = null;
            questPage = 0; questReply = null;
            SaveMessage = null;
        }

        public bool SaveTown()
        {
            if (!IsOpen || !CanOpen) return false;
            var session = GetComponent<BattleTransitionController>();
            if (session == null) { SaveMessage = "저장 세션이 없습니다."; return false; }
            bool saved = session.SaveWorld();
            SaveMessage = saved ? "마을 상태를 저장했습니다." : session.LastSaveMessage;
            return saved;
        }

        public bool OpenFormation()
        {
            if (!IsOpen || !CanOpen || GetComponent<BattleFormationPanel>() == null) return false;
            var session = GetComponent<BattleTransitionController>();
            if (session == null) return false;
            var controller = player; var previous = Service;
            Close();
            if (session.OpenFormationPreparation()) return true;
            if(previous!=TownService.Dialogue && previous!=TownService.Overview)Open(previous,controller);
            return false;
        }

        private void LateUpdate()
        {
            if (!IsOpen) { ui?.Hide(); return; }
            if (!CanOpen || player == null || !player.isActiveAndEnabled) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.Escape)) { if (!CancelPendingTrade()) Close(); return; }
            InGameUi.Ensure(ref ui, this, 60); ui.Begin();
            if(Service==TownService.Mercenary){DrawOriginalMercenaries();ui.End();return;}
            if (Service == TownService.CharacterInfo) { DrawCharacterInfo(); ui.End(); return; }
            if (Service == TownService.Inventory && IsInventoryMinimized) { DrawMinimizedInventory(); ui.End(); return; }
            bool inventoryLayout = Service == TownService.Inventory;
            bool nativeShopLayout = Service == TownService.Shop && NativeShopWindow != null;
            bool nativeInventoryLayout = inventoryLayout && NativeInventoryWindow != null;
            bool nativeLayout = nativeShopLayout || nativeInventoryLayout;
            bool questLayout = Service == TownService.Quests;
            bool tallLayout = inventoryLayout || Service == TownService.Shop || questLayout;
            float width = Mathf.Min(questLayout ? 560 : nativeShopLayout ? 419 : nativeInventoryLayout ? 409 : inventoryLayout ? 600 : 440, Rendering.GameScreen.Width - 24);
            var panel = new Rect((Rendering.GameScreen.Width - width) / 2, 64, width, tallLayout ? 400 : 328);
            if (nativeShopLayout) ui.NativeTexturePanel("town-service-native", new Rect(panel.position, new Vector2(419, 303)), NativeShopWindow.background);
            else if (nativeInventoryLayout) ui.NativeTexturePanel("inventory-native", new Rect(panel.position, new Vector2(409, 227)), NativeInventoryWindow.background);
            else ui.Panel("town-service", panel, speaker ?? "대화");
            if (Service == TownService.Temple)
            {
                ui.Label("temple-description", new Rect(panel.x+20,110,width-40,80), "주인공을 부활시키고 생명력과 마력을 모두 회복합니다.\n비용은 무료입니다.");
                ui.Button("temple-recover", new Rect(panel.x+20,210,width-40,32), "부활 · 전회복", () => {
                    GetComponent<BattleTransitionController>().TryTempleRecovery(out var message); SaveMessage=message;
                });
                ui.Label("temple-response",new Rect(panel.x+20,252,width-40,54),SaveMessage ?? "",12);
            }
            else if (Service == TownService.Quests) DrawQuests(panel);
            else if (Service == TownService.Shop) {
                if (nativeShopLayout) DrawNativeShopList(panel, NativeShopWindow);
                else ui.Label("shop-unavailable", new Rect(panel.x + 20, 110, panel.width - 40, 60), "원본 상점 UI 리소스를 확인하세요.");
            }
            else if (Service == TownService.Inventory) DrawInventory(panel);
            else if (Service == TownService.ClassChange) DrawClassChange(panel);
            else if (Service == TownService.Mercenary) DrawMercenaries(panel);
            else
            {
                string text = Service == TownService.Dialogue ? CurrentLine : string.Empty;
                ui.Label("content", new Rect(panel.x + 20, 110, width - 40, 150), text);
                if (Service == TownService.Dialogue)
                    ui.Button("next", new Rect(panel.x + 20, 270, width - 40, 32), lineIndex + 1 < lines.Length ? "다음" : "대화 종료", () => NextLine());
                else ui.NativeLibraryButton("back",new Vector2(panel.x+20,270),38,Close,true,"대화 종료");
            }
            if (!nativeLayout) ui.NativeLibraryButton("close", new Vector2(panel.xMax - 24, panel.y + 6), 38, Close, true, "닫기");
            ui.End();
        }
        private void OnDisable() => Close();
    }
}
