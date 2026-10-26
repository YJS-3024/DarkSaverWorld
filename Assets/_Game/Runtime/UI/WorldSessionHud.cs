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
             !Rendering.GameScreen.TryScreenToGui(screenPoint, out var point) || pointerBounds.Contains(point));

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
                    if (!session.CanEnterBattle) text += "\n전투 불능: 신전 관리인에게 부활을 요청하세요.";
                }
                if (!string.IsNullOrWhiteSpace(trackedScenarioKey))
                    text += $"\n시나리오 {displayScenarioKey}: {(session.IsScenarioCleared(displayScenarioKey) ? "완료" : "미완료")}";
                if (!string.IsNullOrEmpty(session.LastSaveMessage)) text += "\n" + session.LastSaveMessage;
                return text;
            }
        }

        public bool OpenPersonalWindow(TownService service)
        {
            if(!IsVisible || service!=TownService.Inventory && service!=TownService.CharacterInfo)return false;
            var controller=FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>();
            return controller!=null && !controller.UiInputBlocked && (GetComponent<TownServicesPanel>()?.Open(service,controller) ?? false);
        }

        private void LateUpdate()
        {
            if (!IsVisible) { ui?.Hide(); return; }
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
            var controller=FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>();
            if(controller!=null && !controller.UiInputBlocked)
            {
                if(Input.GetKeyDown(KeyCode.I))OpenPersonalWindow(TownService.Inventory);
                else if(Input.GetKeyDown(KeyCode.C))OpenPersonalWindow(TownService.CharacterInfo);
            }
            ui.NativeRosterSelection("world-inventory-open",new Rect(panel.x+44,panel.y+79,32,32),
                ()=>OpenPersonalWindow(TownService.Inventory),controller!=null && !controller.UiInputBlocked,"장비·소지품 (I)");
            if(Input.GetKeyDown(KeyCode.F5) && controller!=null && !controller.UiInputBlocked
                && FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling!=true)session.SaveWorld();
            var message=!string.IsNullOrEmpty(trainingEntryMessage)?trainingEntryMessage
                : string.IsNullOrEmpty(session.LastSaveMessage)?session.LastTransitionMessage:session.LastSaveMessage;
            if(!string.IsNullOrEmpty(message))ui.NativeBitmapText("world-notice",new Rect(144,12,480,32),message,Color.white,2);
            ui.End();
        }
    }
}
