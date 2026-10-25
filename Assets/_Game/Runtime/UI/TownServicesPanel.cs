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
        public bool CanOpen => !FirstPlayTutorial.BlocksWorldActions && isActiveAndEnabled && (DarkSaver.World.TownSceneContext.IsReady || BattleTransitionController.IsTempleReady)
            && !SceneManager.GetSceneByName("Battle").isLoaded
            && !(GetComponent<BattleTransitionController>()?.IsTransitioning ?? false);

        public bool Open(TownService service, TopDownPlayerController controller)
        {
            if(DarkSaver.World.ShopInteriorContext.Current!=null)
            {
                if(service==TownService.Overview)service=TownService.Shop;
                if(service!=TownService.Shop && service!=TownService.Inventory && service!=TownService.CharacterInfo)return false;
            }
            if(BattleTransitionController.IsTempleReady && service!=TownService.Temple)return false;
            if(service==TownService.Temple && !BattleTransitionController.IsTempleReady)return false;
            if (service == TownService.Quests && DarkSaver.World.TownSceneContext.Current?.Key != "Kaosia") return false;
            if (!CanOpen || controller == null || !controller.isActiveAndEnabled
                || !System.Enum.IsDefined(typeof(TownService), service)) return false;
            if (!IsOpen)
            {
                GetComponent<ItemCatalogPanel>()?.Close(); GetComponent<MagicCatalogPanel>()?.Close();
                GetComponent<BattleFormationPanel>()?.Cancel();
                if (controller.UiInputBlocked) return false;
            }
            else if (player != controller) return false;
            EnsureAnalyzedTownBindings();
            player = controller; Service = service; IsOpen = true;
            ResetServiceInput();
            if(service==TownService.CharacterInfo) BindSessionCharacterInfoValues();
            speaker = null; lines = System.Array.Empty<string>(); lineIndex = 0; afterDialogue = TownService.Overview;
            player.SetUiInputBlocked(true); return true;
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
            Open(previous == TownService.Dialogue ? TownService.Overview : previous, controller);
            return false;
        }

        private void LateUpdate()
        {
            if (!IsOpen) { ui?.Hide(); return; }
            if (!CanOpen || player == null || !player.isActiveAndEnabled) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.Escape)) { if (!CancelPendingTrade()) Close(); return; }
            InGameUi.Ensure(ref ui, this, 60); ui.Begin();
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
            else ui.Panel("town-service", panel, Service == TownService.Dialogue ? speaker : (DarkSaver.World.TownSceneContext.Current?.Title ?? "마을") + " 마을 시설");
            if (Service == TownService.Overview)
            {
                var services = new[] {TownService.Inventory, TownService.Shop, TownService.ClassChange, TownService.Mercenary, TownService.Quests};
                var labels = new[] {"소지품", "상점", "클래스 체인지", "용병 고용", "시나리오1 의뢰"};
                for (int i = 0; i < services.Length; i++)
                { var selected = services[i]; ui.Button("service-" + i, new Rect(panel.x + 20 + (i % 2) * ((width - 48) / 2 + 8), 104 + (i / 2) * 38, (width - 48) / 2, 32), labels[i], () => Open(selected, player), selected != TownService.Quests || DarkSaver.World.TownSceneContext.Current?.Key == "Kaosia"); }
                ui.Button("town-formation", new Rect(panel.x + 20, 256, (width - 48) / 2, 32), "주인공 부대 편성", () => OpenFormation());
                ui.Button("town-character-info", new Rect(panel.x + 24 + (width - 48) / 2, 256, (width - 48) / 2, 32), "자기 정보", () => Open(TownService.CharacterInfo, player));
                var campaignSession=GetComponent<BattleTransitionController>();
                int unlocked=campaignSession?.CaptureUnlockedGenerals().Length ?? 0;
                if(unlocked>0)ui.Label("general-unlocks",new Rect(panel.x+20,294,width-40,18),"해금된 지휘관 기록: "+unlocked+"명",12);
            }
            else if (Service == TownService.Temple)
            {
                ui.Label("temple-description", new Rect(panel.x+20,110,width-40,80), "주인공을 부활시키고 생명력과 마력을 모두 회복합니다.\n비용은 무료입니다.");
                ui.Button("temple-recover", new Rect(panel.x+20,210,width-40,32), "부활 · 전회복", () => {
                    GetComponent<BattleTransitionController>().TryTempleRecovery(out var message); SaveMessage=message;
                });
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
                else ui.Button("back", new Rect(panel.x + 20, 270, width - 40, 32), "시설 목록", () => Open(TownService.Overview, player));
            }
            float saveY = questLayout ? 410 : nativeLayout ? 382 : tallLayout ? 388 : 316;
            ui.Button("town-save", new Rect(panel.x + (questLayout ? 300 : nativeLayout ? 220 : 20), saveY, questLayout ? width - 320 : nativeLayout ? width - 232 : (width - 48) / 2, 32), "저장", () => SaveTown());
            if (!nativeLayout) ui.NativeLibraryButton("close", new Vector2(panel.xMax - 24, panel.y + 6), 38, Close, true, "닫기");
            if (!nativeShopLayout) ui.Label("town-save-message", new Rect(panel.x + 20, nativeInventoryLayout ? 458 : saveY + 38, width - 40, nativeInventoryLayout ? 18 : 30), SaveMessage ?? "", nativeInventoryLayout ? 10 : 14);
            if (nativeLayout) ui.Raise("town-save");
            ui.End();
        }
        private void OnDisable() => Close();
    }
}
