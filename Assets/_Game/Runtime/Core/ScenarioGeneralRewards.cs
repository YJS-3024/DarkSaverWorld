using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Jobs;

namespace DarkSaver.Core
{
    // Confirmed original GETGENERAL rewards. Scenario10's GET_GENERAL is not an alias.
    public static class ScenarioGeneralRewards
    {
        public delegate bool AbilityLookup(int jobId,out int[] spells,out int[] skills);
        public static int JobForScenario(string key) => key switch {
            "02"=>102,"03"=>100,"04"=>101,"05"=>103,"06"=>106,
            "07"=>111,"08"=>104,"09"=>107,"11"=>109,"14"=>113,_=>0 };

        // Stage ownership and receipts; the session saves both before publishing either.
        public static bool TryBuild(NativeOwnedRoster current, int primaryJob, int[] receipts, IEnumerable<int> grants,
            Func<int,JobCatalogEntry> findJob, AbilityLookup findAbilities, Func<int,bool> hasAbility,
            out NativeOwnedRoster result, out int[] claimed, out string reason)
        {
            result=current?.Clone();claimed=receipts==null?Array.Empty<int>():(int[])receipts.Clone();
            reason="시나리오 영입 보상의 원본 능력치·기술을 확인할 수 없습니다.";
            if(current!=null && !current.IsValid || !ValidReceipts(claimed) || grants==null)return false;
            var completed=new HashSet<int>(claimed);
            var members=new List<NativeOwnedUnitData>(result?.units ?? Array.Empty<NativeOwnedUnitData>());
            foreach(int id in grants.Distinct().OrderBy(id=>id))
            {
                if(id<100 || id>169)return false;
                if(completed.Contains(id))continue;
                // A reward unlock never creates another copy of an already owned class.
                if(primaryJob==id || members.Any(u=>u.jobId==id)){completed.Add(id);continue;}
                var job=findJob?.Invoke(id);
                if(job==null)return false;
                long family=job.GetNumericValue(22);
                if(family<0 || family>9)return false;
                bool alreadyOwned=false;
                if(primaryJob>0)
                {
                    var primary=findJob(primaryJob);if(primary==null)return false;
                    alreadyOwned=primary.GetNumericValue(22)==family;
                }
                foreach(var member in members.Where(u=>DarkSaver.Jobs.NativePlayerJobs.Contains(u.jobId) && u.hireOwnerId==0))
                {
                    var owned=findJob(member.jobId);if(owned==null)return false;
                    alreadyOwned|=owned.GetNumericValue(22)==family;
                }
                // A promoted character still occupies the same original troop-family slot.
                if(alreadyOwned){completed.Add(id);continue;}
                if(findAbilities==null || hasAbility==null
                    || !findAbilities(id,out var spells,out var skills) || spells==null || skills==null)return false;
                var abilities=spells.Concat(skills).Distinct().OrderBy(n=>n).ToArray();
                if(abilities.Any(n=>n<=0 || n>short.MaxValue || !hasAbility(n)))return false;
                long hp=job.GetNumericValue(2),mp=job.GetNumericValue(4),attack=job.GetNumericValue(6),defense=job.GetNumericValue(8),ma=job.GetNumericValue(10),md=job.GetNumericValue(12);
                if(hp<=0 || hp>int.MaxValue || mp<0 || mp>int.MaxValue || attack<0 || attack>int.MaxValue
                    || defense<0 || defense>int.MaxValue || ma<0 || ma>int.MaxValue || md<0 || md>int.MaxValue)return false;
                int instance=1000;
                while(instance<=19990 && members.Any(u=>u.instanceId/10==instance/10))instance+=10;
                if(instance>19990)return false;
                members.Add(new NativeOwnedUnitData {persistentId="scenario-general:"+id,instanceId=instance,jobId=id,name=job.Name,
                    hp=(int)hp,maxHp=(int)hp,mp=(int)mp,maxMp=(int)mp,attack=(int)attack,defense=(int)defense,
                    hasMagicAttack=true,hasMagicDefense=true,magicAttack=(int)ma,magicDefense=(int)md,
                    lifeState=(int)UnitLifeState.Active,teamId=1,recordEligible=true,finalStateConfirmed=true,
                    learnedMagicIds=spells.Select(n=>(short)n).ToArray(),usableSkillIds=skills.Select(n=>(short)n).ToArray()});
                completed.Add(id);
            }
            if(members.Count>0 || current!=null)result=new NativeOwnedRoster {units=members.ToArray()};
            claimed=completed.OrderBy(id=>id).ToArray();
            if(result!=null && !result.IsValid)return false;
            reason=string.Empty;return true;
        }

        public static bool ValidReceipts(int[] ids) => ids==null || ids.Length<=70
            && ids.All(id=>id>=100 && id<=169) && ids.Distinct().Count()==ids.Length;
    }
}
