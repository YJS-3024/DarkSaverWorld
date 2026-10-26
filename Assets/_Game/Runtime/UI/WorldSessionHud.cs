using DarkSaver.Combat;
using DarkSaver.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.UI
{
    // Original status artwork; local session commands remain a single-player extension.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BattleTransitionController))]
    [RequireComponent(typeof(BattleFormationPanel))]
    public sealed class WorldSessionHud : MonoBehaviour
    {
        [SerializeField] private string trackedScenarioKey = "01";
        private BattleTransitionController session;
        private InGameUi ui;
        private Rect pointerBounds;
        private string trainingEntryMessage;
        private bool emotesOpen;private Rect emoteBounds;private string emoteMessage;
        public void SetTrainingEntryMessage(string message) => trainingEntryMessage = message;
        public bool ReturnFromTraining()
        {
            if (!IsVisible) return false;
            for (int id = 101; id <= 107; id++)
                if (SceneManager.GetSceneByName("Training" + id + "_Runtime").isLoaded)
                { var coordinator = FindAnyObjectByType<WorldSceneCoordinator>(); return coordinator != null && coordinator.TryTravelTo("Kaosia", "FromTraining"); }
            return false;
        }

        public bool BlocksWorldPointer(Vector2 screenPoint) => isActiveAndEnabled && IsVisible &&
            ((GetComponent<TownServicesPanel>()?.IsOpen ?? false) || (GetComponent<BattleFormationPanel>()?.IsOpen ?? false) ||
             !Rendering.GameScreen.TryScreenToGui(screenPoint, out var point) || pointerBounds.Contains(point) || emotesOpen && emoteBounds.Contains(point));

        private void Awake()
        {
            // Existing scenes gain the preparation panel without rewriting scene YAML.
            if (GetComponent<BattleFormationPanel>() == null) gameObject.AddComponent<BattleFormationPanel>();
            if (GetComponent<TownServicesPanel>() == null) gameObject.AddComponent<TownServicesPanel>();
        }

        public bool IsVisible => !FirstPlayTutorial.BlocksWorldActions && !SceneManager.GetSceneByName("Battle").isLoaded && !DarkSaver.Events.Scenario01Controller.BlocksWorldActions;
        public bool TravelScenarioDemo()
        {
            var inDemo = SceneManager.GetSceneByName("Brugos01_Runtime").isLoaded;
            var inTown = SceneManager.GetSceneByName("Town_Runtime").isLoaded;
            if (!IsVisible || (!inDemo && !inTown)) return false;
            var coordinator = FindAnyObjectByType<WorldSceneCoordinator>();
            return coordinator != null && coordinator.TryTravelTo(inDemo ? "Kaosia" : "Brugos01", inDemo ? "FromField" : "DemoEntry");
        }
        public string SummaryText
        {
            get
            {
                if (session == null) session = GetComponent<BattleTransitionController>();
                var displayScenarioKey = DarkSaver.Events.Scenario01Controller.ActiveKey ?? trackedScenarioKey;
                var text = $"경험치 {session.PlayerExperience}\n완료 시나리오 {session.ClearedScenarioCount}개";
                if (session.HasCreatedCharacter) text = $"{session.PlayerName} / 직업 {session.PlayerJobId}\n" + text;
                if (session.OwnedUnitCount > 0) text += $"\n추가 보유 유닛 {session.OwnedUnitCount}명";
                if (!string.IsNullOrEmpty(session.LastTransitionMessage)) text += "\n" + session.LastTransitionMessage;
                if (session.HasPlayerVitals)
                {
                    text += $"\nHP {session.PlayerHp}/{session.PlayerMaxHp}  MP {session.PlayerMp}/{session.PlayerMaxMp}";
                    if (!session.CanEnterBattle) text += "\n전투 불능: 재조우 불가 (부활 미연결)";
                }
                if (!string.IsNullOrWhiteSpace(trackedScenarioKey))
                    text += $"\n시나리오 {displayScenarioKey}: {(session.IsScenarioCleared(displayScenarioKey) ? "완료" : "미완료")}";
                if (!string.IsNullOrEmpty(session.LastSaveMessage)) text += "\n" + session.LastSaveMessage;
                return text;
            }
        }

        private void LateUpdate()
        {
            if (!IsVisible) { emotesOpen=false;ui?.Hide(); return; }
            InGameUi.Ensure(ref ui, this);
            ui.Begin();
            if (!TownSceneContext.IsReady) trainingEntryMessage = null;
            if (session == null) session = GetComponent<BattleTransitionController>();
            var width = Mathf.Min(260f, Rendering.GameScreen.Width - 24f);
            if (width <= 0f) { ui.Hide(); return; }
            var panel = new Rect(12, 12, 120, 213);
            pointerBounds = panel;
            ui.NativeStatusPanel("world", panel);
            ui.NativeStatusName("world-name", new Rect(panel.x + 9, panel.y + 7, 68, 14),
                session.HasCreatedCharacter ? session.PlayerName : "용병대장");
            var playerBody = FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>()?.GetComponent<SpriteRenderer>();
            ui.Portrait("world-portrait", new Rect(panel.x + 44, panel.y + 79, 32, 32),
                NativeBattleStatus.UnitPicture(playerBody, session.PlayerJobId));
            // Original layout includes AP; it has no meaning outside combat, so do not fabricate a value.
            ui.NativeStatusGauge("world-hp", new Vector2(panel.x + 30, panel.y + 156), session.PlayerHp, session.PlayerMaxHp, 1);
            ui.NativeStatusGauge("world-mp", new Vector2(panel.x + 30, panel.y + 169), session.PlayerMp, session.PlayerMaxMp, 2);
            ui.NativeStatusNumber("world-exp", new Vector2(panel.x + 34, panel.y + 183),
                session.PlayerExperience.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var items = GetComponent<ItemCatalogPanel>();
            var magic = GetComponent<MagicCatalogPanel>();
            var formation = GetComponent<BattleFormationPanel>();
            var town = GetComponent<TownServicesPanel>();
            if ((town != null && town.IsOpen) || (items != null && items.IsOpen) || (magic != null && magic.IsOpen) || (formation != null && formation.IsOpen)) { ui.End(); return; }
            var buttonY = panel.yMax + 4f;
            width = 120;
            if (town != null && town.CanOpen)
            {
                ui.Button("town-services", new Rect(12, buttonY, width, 28), ShopInteriorContext.Current!=null?"상점 이용":"마을 시설", () => town.Open(ShopInteriorContext.Current!=null?TownService.Shop:TownService.Overview, FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>()));
                buttonY += 32f;
                ui.Button("town-inventory", new Rect(12, buttonY, width, 28), "소지품", () => town.Open(TownService.Inventory, FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>()));
                buttonY += 32f;
                ui.Button("town-character-info", new Rect(12, buttonY, width, 28), "자기 정보", () => town.Open(TownService.CharacterInfo, FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>()));
                buttonY += 32f;
            }
            if (formation != null && ShopInteriorContext.Current==null)
            {
                ui.Button("formation", new Rect(12, buttonY, width, 28), "부대 편성", () => session.OpenFormationPreparation());
                buttonY += 32f;
            }
            ui.Button("save", new Rect(12, buttonY, width, 28), "저장", () => session.SaveWorld());
            buttonY += 32f;
            ui.Button("emote-open",new Rect(12,buttonY,width,28),"감정표현",()=>{emotesOpen=!emotesOpen;emoteMessage=null;});buttonY+=32;
            if(emotesOpen)
            {
                emoteBounds=new Rect(Rendering.GameScreen.Width-330,16,318,280);ui.Panel("emotes",emoteBounds);
                for(int index=0;index<15;index++)
                {
                    int id=29001+index;
                    ui.Button("emote-"+id,new Rect(emoteBounds.x+8+(index%2)*152,emoteBounds.y+8+(index/2)*30,146,26),"표현 "+(index+1),()=>{session.TryEmote(id,out emoteMessage);emotesOpen=false;});
                }
                ui.Button("emote-close",new Rect(emoteBounds.x+8,emoteBounds.y+250,302,24),"닫기",()=>emotesOpen=false);
            }
            for (int id = 101; id <= 107; id++)
                if (SceneManager.GetSceneByName("Training" + id + "_Runtime").isLoaded)
                { ui.Button("training-return", new Rect(12, buttonY, width, 28), "카오시아로 복귀", () => ReturnFromTraining()); buttonY += 32f; break; }
            var demo = SceneManager.GetSceneByName("Brugos01_Runtime").isLoaded;
            if (demo || SceneManager.GetSceneByName("Town_Runtime").isLoaded)
            {
                ui.Button("travel", new Rect(12, buttonY, width, 28), demo ? "카오시아로 복귀" : "시나리오 1 입장", () => TravelScenarioDemo());
                buttonY += 32f;
            }
            pointerBounds.yMax = buttonY;
            var message = !string.IsNullOrEmpty(emoteMessage)?emoteMessage:!string.IsNullOrEmpty(trainingEntryMessage) ? trainingEntryMessage
                : string.IsNullOrEmpty(session.LastSaveMessage) ? session.LastTransitionMessage : session.LastSaveMessage;
            if (!string.IsNullOrEmpty(message))
            {
                ui.Panel("notice", new Rect(12, buttonY, width, 56));
                ui.Label("notice-text", new Rect(24, buttonY + 4, width - 24, 48), message);
                pointerBounds.yMax += 56;
            }
            ui.End();
        }
    }
}
