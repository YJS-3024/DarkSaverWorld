using UnityEngine;
using UnityEngine.SceneManagement;
using DarkSaver.Core;

namespace DarkSaver.UI
{
    public sealed class StartMenuController : MonoBehaviour
    {
        private const string GameSceneName = "Kaosia_Play";
        private bool loading;
        private bool canContinue;
        private string loadMessage;
        private bool creating;
        private string characterName = string.Empty;
        public bool IsCreatingCharacter => creating;

        private void Start() => RefreshContinue();
        public void RefreshContinue() => canContinue = new NativeSaveStore(NativeSaveStore.DefaultPath).TryLoad(out _, out _);

        private InGameUi ui;
        private void LateUpdate()
        {
            InGameUi.Ensure(ref ui, this);
            ui.Begin();
            var width = Mathf.Min(420f, Rendering.GameScreen.Width - 40f);
            var height = creating ? 420f : 350f;
            var panel = new Rect((Rendering.GameScreen.Width - width) * 0.5f, (Rendering.GameScreen.Height - height) * 0.5f, width, height);

            ui.Panel("menu", panel);
            ui.Label("title", new Rect(panel.x + 30, panel.y + 35, panel.width - 60, 60), "DARK SAVER", 34);
            if (creating) { DrawCharacterCreation(panel); ui.End(); return; }
            ui.Button("new", new Rect(panel.x + 60, panel.y + 125, panel.width - 120, 50),
                loading ? "불러오는 중…" : "새 게임", StartGame, !loading);
            ui.Button("continue", new Rect(panel.x + 60, panel.y + 185, panel.width - 120, 40), "이어하기", () => ContinueGame(), !loading && canContinue);
            ui.Button("quit", new Rect(panel.x + 60, panel.y + 235, panel.width - 120, 40), "종료", () => {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }, !loading);
            if (!string.IsNullOrEmpty(loadMessage))
                ui.Label("message", new Rect(panel.x + 20, panel.y + 285, panel.width - 40, 50), loadMessage);
            ui.End();
        }

        public void StartGame()
        {
            if (loading) return;
            NativeSaveStore.ClearResume();
            NativeCharacterProfile.ClearPending();
            creating = true;
            loadMessage = string.Empty;
        }

        private void DrawCharacterCreation(Rect panel)
        {
            ui.Label("name-label", new Rect(panel.x + 30, panel.y + 110, panel.width - 60, 25), "캐릭터 이름");
            ui.TextInput("name", new Rect(panel.x + 30, panel.y + 140, panel.width - 60, 35), characterName,
                GameConstants.CharacterNameMaxLength, value => characterName = value);
            ui.Label("job", new Rect(panel.x + 30, panel.y + 190, panel.width - 60, 50), "용병대장으로 모험을 시작합니다.");
            ui.Button("create", new Rect(panel.x + 30, panel.y + 255, panel.width - 60, 40), "생성하고 마을 진입", () => CreateCharacter(characterName), !loading);
            ui.Button("back", new Rect(panel.x + 30, panel.y + 305, panel.width - 60, 35), "뒤로", () => creating = false, !loading);
            ui.Label("creation-message", new Rect(panel.x + 30, panel.y + 350, panel.width - 60, 60),
                string.IsNullOrEmpty(loadMessage) ? "진행 상황은 마을의 저장 버튼으로 기록할 수 있습니다." : loadMessage);
        }

        public bool CreateCharacter(string name, int jobId = NativeCharacterProfile.SupportedStartingJob, string filePath = null)
        {
            if (loading || !creating || !NativeCharacterProfile.TryQueue(name, jobId, filePath, out loadMessage)) return false;
            loading = true;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            return true;
        }

        public bool ContinueGame(string filePath = null)
        {
            if (loading) return false;
            var store = new NativeSaveStore(filePath ?? NativeSaveStore.DefaultPath);
            if (!store.TryLoad(out var data, out loadMessage) || !NativeSaveStore.QueueResume(data, store.FilePath)) return false;
            loading = true;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            return true;
        }
    }
}
