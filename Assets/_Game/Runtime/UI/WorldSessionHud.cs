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
        public bool IsStatusMinimized { get; private set; }
        public bool IsStatusClosed { get; private set; }
        public void MinimizeStatus(){IsStatusMinimized=true;IsStatusClosed=false;}
        public void CloseStatus(){IsStatusClosed=true;IsStatusMinimized=false;}
        public void RestoreStatus(){IsStatusClosed=IsStatusMinimized=false;}
        // World scenes have no combat status window to restore by clicking the player.
        public bool TryRestoreStatusAt(Vector2 world) => false;
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
            if (GetComponent<ScenarioRoomPanel>() == null) gameObject.AddComponent<ScenarioRoomPanel>();
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
            pointerBounds=default;
            var controller=FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>();
            if(controller!=null && !controller.UiInputBlocked)
            {
                if(Input.GetKeyDown(KeyCode.I))OpenPersonalWindow(TownService.Inventory);
                else if(Input.GetKeyDown(KeyCode.C))OpenPersonalWindow(TownService.CharacterInfo);
                if(DarkSaver.Core.OriginalRestorationPolicy.AllowSinglePlayerRules && Input.GetKeyDown(KeyCode.F5) && FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling!=true)session.SaveWorld();
            }
            // Character combat status belongs to BattleSceneRoot only. World scenes keep
            // personal-window shortcuts and notices without drawing a combat panel.
            var message=!string.IsNullOrEmpty(trainingEntryMessage)?trainingEntryMessage
                : string.IsNullOrEmpty(session.LastSaveMessage)?session.LastTransitionMessage:session.LastSaveMessage;
            if(!string.IsNullOrEmpty(message))ui.NativeBitmapText("world-notice",new Rect(144,12,480,32),message,Color.white,2);
            ui.End();
        }
    }
}
