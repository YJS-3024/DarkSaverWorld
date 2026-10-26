using System;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Events;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        // Source NPC identities/dialogue; each user-authorized local NPC selects its own quest.
        private static TownQuestContent questContent;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetQuestContent() => questContent = null;
        public static TownQuestContent QuestContent
        {
            get
            {
                if (questContent != null) return questContent;
                var text = Resources.Load<TextAsset>("Town/Quests");
                if (text == null) return null;
                var value = JsonUtility.FromJson<TownQuestContent>(text.text);
                questContent = value != null && value.IsValid ? value : null;
                return questContent;
            }
        }
        private int selectedQuest = 1, questPage;
        private string questReply;
        private void DrawQuests(Rect panel)
        {
            var content = QuestContent;
            if (content == null) { ui.Label("quest-unavailable", new Rect(panel.x + 20, 110, panel.width - 40, 80), "의뢰 자료를 불러올 수 없습니다."); return; }
            var session = GetComponent<BattleTransitionController>();
            var state = session?.CaptureScenario01();
            var quest = Array.Find(content.rows, q => q.id == selectedQuest);
            if (quest == null) return;
            int stage = TownQuestRules.Stage(state, quest.id);
            string status = stage == 0 ? "수락 전" : stage == 1 ? "진행 중" : stage == 2 ? "보고 가능" : "보상 수령 완료";
            ui.Label("quest-status", new Rect(panel.x + 20, 142, panel.width - 40, 24), quest.npcName + " · " + status, 16);
            string text = questReply ?? (stage == 3 ? quest.ending + "\n" + quest.result : stage == 2 ? quest.ending
                : stage == 1 ? quest.yes + "\n시나리오1에서 의뢰의 단서를 확인하고 돌아오세요." : quest.dialogue);
            var lines = NativeStatusFont.WrapLines(text, Mathf.FloorToInt(panel.width - 40));
            int pages = Math.Max(1, (lines.Length + 7) / 8); questPage = Math.Min(questPage, pages - 1);
            ui.NativeBitmapText("quest-text", new Rect(panel.x + 20, 172, panel.width - 40, 128),
                string.Join("\n", lines.Skip(questPage * 8).Take(8)), Color.white, 8);
            ui.Button("quest-prev", new Rect(panel.x + 20, 304, 86, 28), "이전", () => questPage--, questPage > 0);
            ui.Label("quest-page", new Rect(panel.x + 112, 304, 90, 28), (questPage + 1) + " / " + pages, 14);
            ui.Button("quest-next", new Rect(panel.xMax - 106, 304, 86, 28), "다음", () => questPage++, questPage + 1 < pages);
            bool read = questPage == pages - 1;
            if (stage == 0)
            {
                bool available = state == null || state.phase < Scenario01Progress.Outro;
                ui.Label("quest-prompt", new Rect(panel.x + 20, 338, panel.width - 40, 24), available ? quest.prompt : "이미 완료한 시나리오의 의뢰는 새로 수락할 수 없습니다.", 14);
                ui.Button("quest-accept", new Rect(panel.x + 20, 368, 150, 28), "예 · 수락", () => ApplyTownQuest(false), read && available);
                ui.Button("quest-decline", new Rect(panel.x + 180, 368, 150, 28), "아니오", () => { questReply = quest.no; questPage = 0; }, read);
            }
            else if (stage == 2) ui.Button("quest-claim", new Rect(panel.x + 20, 368, 250, 28), "보고하고 보상 받기", () => ApplyTownQuest(true), read);
        }
        private void ApplyTownQuest(bool claim)
        {
            if (!IsOpen || Service != TownService.Quests || !CanOpen) return;
            var session = GetComponent<BattleTransitionController>();
            if (session == null) return;
            bool committed = session.TryCommitTownQuest(selectedQuest, claim, out var reason);
            SaveMessage = reason;
            if (!committed) return;
            questPage = 0;
            var quest = Array.Find(QuestContent.rows, q => q.id == selectedQuest);
            questReply = claim ? quest.ending + "\n" + quest.result : quest.yes;
        }
    }
}
