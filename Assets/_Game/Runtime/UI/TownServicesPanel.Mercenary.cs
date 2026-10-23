using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Jobs;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        [SerializeField] private MercenaryHireCatalog hireSkills;
        [SerializeField] private JobCatalog mercenaryJobs;
        private int hireSkillIndex, hirePage;
        private string hireMessage;
        public string HireMessage => hireMessage;
        public bool CanHireMercenary(int skillId, int jobId, out string reason)
        {
            reason = "마을의 용병 고용창을 먼저 여세요.";
            if (!IsOpen || Service != TownService.Mercenary || !CanOpen) return false;
            var session = GetComponent<BattleTransitionController>();
            return session != null && session.CanHireMercenary(skillId, jobId, progression, hireSkills, mercenaryJobs, out reason);
        }
        public bool HireMercenary(int skillId, int jobId)
        {
            if (!IsOpen || Service != TownService.Mercenary || !CanOpen) return false;
            var session = GetComponent<BattleTransitionController>();
            return session != null && session.TryHireMercenary(skillId, jobId, progression, hireSkills, mercenaryJobs, out hireMessage);
        }
        public void ConfigureHiring(MercenaryHireCatalog skills, JobCatalog jobs)
        { hireSkills = skills; mercenaryJobs = jobs; hireSkillIndex = hirePage = 0; hireMessage = null; }
        public int[] CaptureCurrentHireSkills()
        {
            var profile = GetComponent<BattleTransitionController>()?.CaptureCharacterProfile();
            if (profile == null || hireSkills == null) return System.Array.Empty<int>();
            int[] ids = profile.usableSkillIds;
            if (!profile.hasClassAbilities && (progression == null || !progression.TryGetClassEntryAbilities(profile.jobId, out _, out ids))) return System.Array.Empty<int>();
            var result = new List<int>();
            foreach (int id in ids) if (hireSkills.TryGet(id, out _) && !result.Contains(id)) result.Add(id);
            return result.ToArray();
        }
        public bool TryGetHireCandidates(int skillId, out MercenaryHireEntry skill, out JobCatalogEntry[] jobs)
            => TryGetHireCandidates(skillId, CaptureCurrentHireSkills(), out skill, out jobs);

        private bool TryGetHireCandidates(int skillId, int[] allowedSkills, out MercenaryHireEntry skill, out JobCatalogEntry[] jobs)
        {
            skill = null; jobs = System.Array.Empty<JobCatalogEntry>();
            if (mercenaryJobs == null || System.Array.IndexOf(allowedSkills, skillId) < 0
                || !hireSkills.TryGet(skillId, out var row)) return false;
            var result = new List<JobCatalogEntry>();
            var seen = new HashSet<int>();
            foreach (int id in row.CopyJobIds()) { if (!mercenaryJobs.TryGet(id, out var job)) return false; if (seen.Add(id)) result.Add(job); }
            skill = row; jobs = result.ToArray(); return true;
        }
        private void DrawMercenaries(Rect panel)
        {
            var skills = CaptureCurrentHireSkills(); string text; string blockedReason = null;
            if (skills.Length == 0) text = "현재 클래스의 확인된 고용 기술이 없습니다.\n미확인 기술을 낮은 단계로 대체하지 않습니다.";
            else
            {
                hireSkillIndex = Mathf.Clamp(hireSkillIndex, 0, skills.Length - 1);
                if (!TryGetHireCandidates(skills[hireSkillIndex], skills, out var skill, out var jobs)) text = "고용 기술의 병종 카탈로그가 미연결입니다.";
                else
                {
                    hirePage = Mathf.Clamp(hirePage, 0, (jobs.Length - 1) / 3);
                    var session = GetComponent<BattleTransitionController>();
                    int hired = session?.CountPrimaryHires(skill.Name) ?? 0;
                    text = skill.Name + " Lv" + skill.Level + " / " + hired + "/" + skill.Maximum
                        + " / 소지금 " + (session?.CurrentGold?.ToString() ?? "미확인");
                    for (int i = hirePage * 3; i < Mathf.Min(hirePage * 3 + 3, jobs.Length); i++)
                    {
                        float y = 138 + (i % 3) * 34;
                        string mpText = jobs[i].GetNumericValue(4) == -1 ? "MP 미사용" : "MP " + jobs[i].GetNumericValue(4);
                        ui.Label("hire-job-" + (i % 3), new Rect(panel.x + 20, y, panel.width - 130, 32),
                            (jobs[i].Name ?? "미확인 용병").Replace('_', ' ') + " / 가격 " + jobs[i].GetNumericValue(33)
                            + "\nHP " + jobs[i].GetNumericValue(2) + " / " + mpText, 12);
                        int id = jobs[i].Id; int skillId = skill.SkillId;
                        bool canHire = CanHireMercenary(skillId, id, out var reason);
                        if (!canHire && blockedReason == null) blockedReason = reason;
                        ui.Button("hire-one-" + (i % 3), new Rect(panel.xMax - 100, y, 80, 30), "1명 고용",
                            () => HireMercenary(skillId, id), enabled: canHire, tooltip: canHire ? "원본 가격으로 1명 고용" : reason);
                    }
                    ui.Button("hire-page", new Rect(panel.x + 20, 266, 80, 30), "다음 병종", () => hirePage = (hirePage + 1) % ((jobs.Length + 2) / 3));
                }
                ui.Button("hire-skill", new Rect(panel.x + 106, 266, 80, 30), "다음 기술", () => { hireSkillIndex = (hireSkillIndex + 1) % skills.Length; hirePage = 0; });
            }
            ui.Label("hire-data", new Rect(panel.x + 20, 104, panel.width - 40, 32), text, 12);
            ui.Label("hire-pending", new Rect(panel.x + 20, 238, panel.width - 40, 22),
                blockedReason != null ? "고용 불가 항목: " + blockedReason : hireMessage ?? "한 번에 1명씩 고용 / 원본 가격 차감", 11);
            ui.Label("hire-type-policy",new Rect(panel.x+20,294,panel.width-40,16),"첫 고용 타입만 추가 고용 · 보유 0명일 때 타입 변경",11);
            ui.Button("hire-formation", new Rect(panel.x + 192, 266, 90, 30), "부대 편성", () => OpenFormation());
            ui.Button("hire-back", new Rect(panel.xMax - 100, 266, 80, 30), "시설 목록", () => Open(TownService.Overview, player));
        }
    }
}
