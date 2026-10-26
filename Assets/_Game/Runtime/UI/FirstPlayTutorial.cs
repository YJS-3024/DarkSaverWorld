using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using UnityEngine;

namespace DarkSaver.UI
{
    // Uses the loaded town as a safe practice area before unlocking town services.
    public sealed class FirstPlayTutorial : MonoBehaviour
    {
        private BattleTransitionController session;
        private TopDownPlayerController player;
        private InGameUi ui;
        private Vector3 moveOrigin;
        private float distance;
        private int shownStep = -1;
        private bool checkpointSaved;
        private string message;
        public static bool BlocksWorldActions
        {
            get { var session = FindAnyObjectByType<BattleTransitionController>(); return session != null && session.HasPendingTutorial; }
        }
        public static void Ensure(BattleTransitionController session, TopDownPlayerController player)
        {
            if (session == null || player == null || !session.HasPendingTutorial) return;
            var tutorial = session.GetComponent<FirstPlayTutorial>();
            if (tutorial == null) tutorial = session.gameObject.AddComponent<FirstPlayTutorial>();
            tutorial.session = session; tutorial.player = player; player.SetTutorialInputBlocked(true); tutorial.Checkpoint();
        }
        public static bool BlocksPointer(Vector2 screenPoint)
        {
            return BlocksWorldActions && Rendering.GameScreen.TryScreenToGui(screenPoint, out var point)
                && new Rect(220, 12, 408, 200).Contains(point);
        }
        private void Checkpoint()
        {
            checkpointSaved = session.TrySaveTutorial(session.CaptureCharacterProfile().tutorial.step, false, false, true, out message);
            if (checkpointSaved) message = string.Empty;
        }
        private void Update()
        {
            if (player == null || session == null) return;
            if (!session.HasPendingTutorial) { player.SetTutorialInputBlocked(false); ui?.Hide(); enabled = false; return; }
            int step = session.CaptureCharacterProfile().tutorial.step;
            if (shownStep != step) { shownStep = step; moveOrigin = player.transform.position; distance = 0; }
            if (step == 1) { distance += Vector3.Distance(moveOrigin, player.transform.position); moveOrigin = player.transform.position; }
            player.SetTutorialInputBlocked(!checkpointSaved || step != 1);
        }
        private void Advance(bool skip)
        {
            if (!checkpointSaved) return;
            if (session.TrySaveTutorial(session.CaptureCharacterProfile().tutorial.step, distance >= 2f, skip, false, out message)) message = string.Empty;
            if (!session.HasPendingTutorial) player.SetTutorialInputBlocked(false);
        }
        private void LateUpdate()
        {
            if (session == null || !session.HasPendingTutorial) { ui?.Hide(); return; }
            int step = session.CaptureCharacterProfile().tutorial.step;
            bool practice = checkpointSaved && step == 1;
            InGameUi.Ensure(ref ui, this, 140); ui.Begin();
            if (!practice) ui.ModalBlocker("block");
            var rect = practice ? new Rect(220, 12, 408, 200) : new Rect(100, 100, 440, 280);
            ui.Panel("tutorial", rect);
            string[] titles = { "모험을 시작하기 전에", "이동 연습", "마을 이용", "첫 시나리오" };
            string[] descriptions = {
                "싱글플레이 기본 조작을 안내합니다.\n안내를 마치면 카오시아 마을을 자유롭게 이용할 수 있습니다.",
                "WASD·방향키 또는 바닥 클릭으로 이동하세요.\n두 칸 이상 움직인 뒤 다음을 누르세요.",
                "상점 주인과 안내인을 클릭해 물품·소지품·자기 정보를 확인하세요.\n부대 관리인에게 편성을, 기록관에게 저장을 요청하세요. F5로도 저장할 수 있습니다.",
                "마을 출구를 지나 필드의 시나리오 입구로 이동하세요.\n몬스터와 접촉하면 전투가 시작되고, 대사는 다음·Space·Enter로 넘깁니다.\n보스 처치 후 완료 연출과 자동 저장이 이어집니다."
            };
            ui.Label("title", new Rect(rect.x + 16, rect.y + 12, rect.width - 32, 28), titles[step], 20);
            ui.Label("body", new Rect(rect.x + 16, rect.y + 46, rect.width - 32, practice ? 54 : 120), descriptions[step], 14);
            if (!string.IsNullOrEmpty(message)) ui.Label("error", new Rect(rect.x + 16, rect.yMax - 88, rect.width - 32, 40), message, 12);
            if (!checkpointSaved) ui.Button("retry", new Rect(rect.x + 16, rect.yMax - 42, 180, 28), "저장 다시 시도", Checkpoint);
            else
            {
                ui.Button("skip", new Rect(rect.x + 16, rect.yMax - 42, 130, 28), "안내 건너뛰기", () => Advance(true));
                ui.Button("next", new Rect(rect.xMax - 174, rect.yMax - 42, 158, 28), step == 3 ? "마을 시작" : "다음", () => Advance(false), !practice || distance >= 2f);
            }
            ui.End();
        }
        private void OnDisable() { if (player != null) player.SetTutorialInputBlocked(false); ui?.Hide(); }
    }
}
