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
{result=default;claimed=default;reason=default;return false;}
        public static bool ValidReceipts(int[] ids) => ids==null || ids.Length<=70
            && ids.All(id=>id>=100 && id<=169) && ids.Distinct().Count()==ids.Length;
    }
}
