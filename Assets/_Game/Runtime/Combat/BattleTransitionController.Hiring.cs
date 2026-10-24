using System;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Jobs;
using UnityEngine.SceneManagement;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        // Display queries return values only, never mutable ownership references.
        public int CountPrimaryHires(string type)
        {
            if (string.IsNullOrEmpty(type) || ownedRoster?.units == null) return 0;
            int count = 0;
            foreach (var unit in ownedRoster.units)
                if (unit != null && unit.hireOwnerId == 100 && unit.hireType == type) count++;
            return count;
        }

        public bool IsPrimaryHiredUnit(string persistentId)
        {
            if (string.IsNullOrWhiteSpace(persistentId) || ownedRoster?.units == null) return false;
            foreach (var unit in ownedRoster.units)
                if (unit != null && unit.persistentId == persistentId) return unit.hireOwnerId == 100;
            return false;
        }

        public bool TryHireMercenary(int skillId, int jobId, ExperienceProgressionTable progression,
            MercenaryHireCatalog skills, JobCatalog jobs, out string message)
        {
            if(!processingSessionRequest)
            {serverProgression=progression;serverHireSkills=skills;serverHireJobs=jobs;return SubmitSession(new DarkSaver.Network.LocalHireRequest(skillId,jobId),out message);}

            if (!PrepareMercenaryHire(skillId, jobId, progression, skills, jobs, out var job, out int instance, out message)) return false;
            var roster = ownedRoster?.Clone() ?? new NativeOwnedRoster {units = Array.Empty<NativeOwnedUnitData>()};
            skills.TryGet(skillId, out var skill);
            var unit = new NativeOwnedUnitData { persistentId = Guid.NewGuid().ToString("N"), instanceId = instance,
                jobId = jobId, name = job.Name, hp = (int)job.GetNumericValue(2), maxHp = (int)job.GetNumericValue(2), mp = 0, maxMp = 0,
                attack = (int)job.GetNumericValue(6), defense = (int)job.GetNumericValue(8),
                hasMagicAttack=job.GetNumericValue(10)>=0 && job.GetNumericValue(10)<=int.MaxValue,
                hasMagicDefense=job.GetNumericValue(12)>=0 && job.GetNumericValue(12)<=int.MaxValue,
                magicAttack=job.GetNumericValue(10)>=0 && job.GetNumericValue(10)<=int.MaxValue ? (int)job.GetNumericValue(10) : 0,
                magicDefense=job.GetNumericValue(12)>=0 && job.GetNumericValue(12)<=int.MaxValue ? (int)job.GetNumericValue(12) : 0, lifeState = (int)UnitLifeState.Active,
                teamId = 1, recordEligible = false, finalStateConfirmed = true, learnedMagicIds = Array.Empty<short>(),
                hireType = skill.Name, hireOwnerId = 100 };
            var members = new List<NativeOwnedUnitData>(roster.units) {unit}; roster.units = members.ToArray();
            var funds = inventory.Clone(); funds.gold -= job.GetNumericValue(33);
            if (!roster.IsValid || !funds.IsValid) return false;
            // One click creates exactly one unit. Publish money and ownership together.
            ownedRoster = roster; inventory = funds; OwnedRosterRevision++;
            preparedGround = null; preparedRosterRevision = -1;
            GetComponent<DarkSaver.UI.BattleFormationPanel>()?.UseSessionRoster();
            message = job.Name + " 1명을 고용했습니다. 저장 버튼으로 보관하세요."; return true;
        }

        public bool CanHireMercenary(int skillId, int jobId, ExperienceProgressionTable progression,
            MercenaryHireCatalog skills, JobCatalog jobs, out string message)
            => PrepareMercenaryHire(skillId, jobId, progression, skills, jobs, out _, out _, out message);

        private bool PrepareMercenaryHire(int skillId, int jobId, ExperienceProgressionTable progression,
            MercenaryHireCatalog skills, JobCatalog jobs, out JobCatalogEntry job, out int instance, out string message)
        {
            job = null; instance = 0;
            message = "고용 조건·기술·재화·용병 정보를 확인하세요.";
            if (character == null || inventory == null || transitioning || !DarkSaver.World.TownSceneContext.IsReady
                || SceneManager.GetSceneByName("Battle").isLoaded || GetComponent<DarkSaver.UI.BattleFormationPanel>()?.IsOpen == true)
            { message = "마을의 정상 캐릭터·재화 상태에서만 고용할 수 있습니다."; return false; }
            if (skills == null || jobs == null || !skills.TryGet(skillId, out var skill))
            { message = "고용 기술 또는 병종 정보가 연결되지 않았습니다."; return false; }
            if (!jobs.TryGet(jobId, out job) || job.CategoryLabel != "mercenary" || !skill.ContainsJob(jobId))
            { message = "선택한 고용 기술에 포함되지 않은 병종입니다."; return false; }
            int[] allowed = character.usableSkillIds;
            if (!character.hasClassAbilities && (progression == null
                || !progression.TryGetClassEntryAbilities(character.jobId, out _, out allowed)))
            { message = "현재 클래스의 고용 기술을 확인할 수 없습니다."; return false; }
            if (Array.IndexOf(allowed, skillId) < 0) { message = "현재 클래스가 보유하지 않은 고용 기술입니다."; return false; }
            if (ownedRoster != null && !ownedRoster.IsValid || !inventory.IsValid)
            { message = "보유 용병 또는 소지금 데이터가 올바르지 않습니다."; return false; }
            var currentMembers = ownedRoster?.units ?? Array.Empty<NativeOwnedUnitData>();
            int count = 0;
            foreach (var member in currentMembers)
            {
                if (member.hireOwnerId == 100 && !string.IsNullOrEmpty(member.hireType)) {
                    if (member.hireType != skill.Name) {
                        message = "먼저 고용한 용병 타입만 추가 고용할 수 있습니다."; return false;
                    }
                    count++;
                }
                else if (string.IsNullOrEmpty(member.hireType) && jobs.TryGet(member.jobId, out var legacyJob) && legacyJob.CategoryLabel == "mercenary")
                { message = "기존 보유 용병의 고용 타입이 미확인이라 제한 검사를 보류합니다."; return false; }
            }
            if (count >= skill.Maximum) { message = "이 타입의 고용 가능 인원이 가득 찼습니다."; return false; }
            long price = job.GetNumericValue(33), hp = job.GetNumericValue(2), mp = job.GetNumericValue(4);
            long attack = job.GetNumericValue(6), defense = job.GetNumericValue(8);
            // User-confirmed sentinel: preserve raw -1 in catalog, MP unused in local state.
            if (mp != -1) { message = "마법 용병의 초기 기술 목록은 아직 미확인입니다."; return false; }
            if (price < 0) { message = "고용 가격이 미확인입니다."; return false; }
            if (inventory.gold < price) { message = "소지금이 부족합니다."; return false; }
            if (hp <= 0 || hp > int.MaxValue || attack < 0 || attack > int.MaxValue || defense < 0 || defense > int.MaxValue)
            { message = "용병 기본 능력치가 유효하지 않습니다."; return false; }
            // Local IDs only; not original server allocation. Deployment revalidates all collisions.
            instance = 20000;
            while (instance <= short.MaxValue) {
                bool used = false;
                foreach (var member in currentMembers) if (member.instanceId == instance) { used = true; break; }
                if (!used) break;
                instance++;
            }
            if (instance > short.MaxValue) { message = "용병 식별자 할당 한도에 도달했습니다."; return false; }
            message = "1명 고용 가능"; return true;
        }
    }
}
