using DarkSaver.Combat;
using DarkSaver.Core;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        [SerializeField] private ExperienceProgressionTable progression;
        public ExperienceProgressionTable Progression => progression;
        private int classCandidateIndex, classDetailPage;
        private int pendingClassId;
        private string classChangeMessage;
        public bool RequestClassChange()
        {
            if (!IsOpen || Service != TownService.ClassChange || !CanOpen) return false;
            var session = GetComponent<BattleTransitionController>(); var magic = GetComponent<MagicCatalogPanel>()?.Catalog;
            pendingClassId = 0;
            if (session == null || !session.CanChangeClass(progression, magic, player, out classChangeMessage)) return false;
            if (!session.CaptureCharacterProfile().TryBuildEligibleClassChange(progression, magic, out var changed)) return false;
            pendingClassId = changed.jobId; classChangeMessage = "전직 확인을 누르면 클래스가 변경됩니다."; return true;
        }
        public bool ConfirmClassChange()
        {
            if (!IsOpen || Service != TownService.ClassChange || !CanOpen || pendingClassId == 0) return false;
            int expected = pendingClassId; pendingClassId = 0;
            var session = GetComponent<BattleTransitionController>(); var magic = GetComponent<MagicCatalogPanel>()?.Catalog;
            if (session == null || !session.CaptureCharacterProfile().TryBuildEligibleClassChange(progression, magic, out var projected)
                || projected.jobId != expected) { classChangeMessage = "캐릭터가 바뀌었습니다. 다시 전직을 선택하세요."; return false; }
            bool changed = session.TryChangeClass(progression, magic, player, out classChangeMessage);
            if (changed) classCandidateIndex = classDetailPage = 0;
            return changed;
        }
        public void ConfigureProgression(ExperienceProgressionTable table) { progression = table; classCandidateIndex = classDetailPage = 0; }
        public ExperienceLevelEntry[] CaptureClassChangeRows() => progression == null ? System.Array.Empty<ExperienceLevelEntry>()
            : progression.GetClassChangeRows(GetComponent<BattleTransitionController>()?.PlayerJobId ?? 0);

        // Projection only; never applies a class, grants spells, spends XP, or saves.
        public bool TryPreviewClassChange(int index, out ExperienceLevelEntry source, out NativeCharacterProfile projected)
        {
            source = default; projected = null;
            var rows = CaptureClassChangeRows();
            var current = GetComponent<BattleTransitionController>()?.CaptureCharacterProfile();
            var catalog = GetComponent<MagicCatalogPanel>()?.Catalog;
            if (index < 0 || index >= rows.Length || current == null
                || !current.TryBuildClassChange(progression, catalog, rows[index].level, out var candidate)) return false;
            source = rows[index]; projected = candidate; return true;
        }

        private void DrawClassChange(Rect panel)
        {
            var rows = CaptureClassChangeRows();
            int detailPages = 1;
            string text;
            if (rows.Length == 0) text = progression == null ? "성장표가 연결되지 않았습니다." : "현재 클래스의 다음 클래스 기록이 없습니다.";
            else
            {
                classCandidateIndex = Mathf.Clamp(classCandidateIndex, 0, rows.Length - 1);
                var row = rows[classCandidateIndex];
                string target = progression.TryGet(row.nextJobId, 1, out var next) ? next.jobName : "미확인 클래스 #" + row.nextJobId;
                text = row.jobName + " → " + target + "\n원본 기록 레벨 " + row.level + " / 행 경험치 " + row.requiredXp;
                if (TryPreviewClassChange(classCandidateIndex, out _, out var projected))
                {
                    int magicPages = (projected.learnedMagicIds.Length + 3) / 4;
                    detailPages = 1 + magicPages + (projected.usableSkillIds.Length + 3) / 4;
                    classDetailPage = Mathf.Clamp(classDetailPage, 0, detailPages - 1);
                    if (classDetailPage == 0)
                        text += "\n" + (GetComponent<BattleTransitionController>().CaptureCharacterProfile() is var profile && profile != null && profile.hasLevels
                            ? "베이스 " + profile.baseLevel + " / 직업 " + profile.jobLevel : "현재 레벨 미확인")
                            + " / 경험치 " + GetComponent<BattleTransitionController>().PlayerExperience
                            + "\n변경 후 마법 " + projected.learnedMagicIds.Length + "종 / 기술 " + projected.usableSkillIds.Length + "종"
                            + "\n추가 능력치 잔여 "+projected.unspentClassChangeStatPoints+" / 자기 정보의 능력조정에서 배분 · 마법 레벨 재확인 중";
                    else
                    {
                        bool showMagic = classDetailPage <= magicPages;
                        var ids = showMagic ? projected.learnedMagicIds : projected.usableSkillIds;
                        int start = (showMagic ? classDetailPage - 1 : classDetailPage - 1 - magicPages) * 4;
                        var catalog = GetComponent<MagicCatalogPanel>()?.Catalog;
                        text += showMagic ? "\n변경 후 마법:" : "\n변경 후 사용기술:";
                        for (int i = start; i < Mathf.Min(start + 4, ids.Length); i++)
                        { int id = ids[i]; text += "\n" + (catalog != null && catalog.TryGet(id, out var magic) ? magic.Name + " (#" + id + ")" : "미해석 기술 #" + id); }
                    }
                }
                else text += "\n후보 기술의 카탈로그 대응을 확인할 수 없습니다.";
                ui.Button("class-prev", new Rect(panel.x + 20, 268, 60, 28), "이전", () => classCandidateIndex = Mathf.Max(0, classCandidateIndex - 1));
                ui.Button("class-next", new Rect(panel.x + 84, 268, 60, 28), "다음", () => classCandidateIndex = Mathf.Min(rows.Length - 1, classCandidateIndex + 1));
                ui.Button("class-detail", new Rect(panel.x + 148, 268, 100, 28), "정보 전환", () => classDetailPage = (classDetailPage + 1) % detailPages);
            }
            ui.Label("class-preview", new Rect(panel.x + 20, 104, panel.width - 40, 140), text);
            ui.Label("class-change-status", new Rect(panel.x + 20, 238, panel.width - 150, 26), classChangeMessage ?? "확인된 직업 레벨·외형이 있어야 전직 가능", 12);
            ui.Button("class-change", new Rect(panel.xMax - 120, 236, 100, 28), pendingClassId == 0 ? "전직" : "전직 확인",
                () => { if (pendingClassId == 0) RequestClassChange(); else ConfirmClassChange(); });
            ui.Button("class-back", new Rect(panel.xMax - 100, 268, 80, 28), "시설 목록", () => Open(TownService.Overview, player));
        }
    }
}
