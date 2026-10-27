using System;
using System.Linq;
using DarkSaver.Core;

namespace DarkSaver.Events
{
    [Serializable] public sealed class TownQuestDefinition
    {
        public int id, npcId, itemId;
        public long gold;
        public string npcName, dialogue, prompt, yes, no, ending, result;
        public string[] sources;
        public bool IsValid => (id == 1 || id == 2) && npcId > 0 && !string.IsNullOrEmpty(dialogue)
            && !string.IsNullOrEmpty(prompt) && !string.IsNullOrEmpty(ending) && gold >= 0 && itemId >= 0
            && (gold > 0 ^ itemId > 0);
    }
    [Serializable] public sealed class TownQuestContent
    {
        public int version;
        public string sourceHash;
        public TownQuestDefinition[] rows;
        public bool IsValid => version == 1 && Scenario01Definition.IsHash(sourceHash) && rows != null && rows.Length == 2
            && rows.All(r => r != null && r.IsValid) && rows.Select(r => r.id).Distinct().Count() == rows.Length;
    }
    public static class TownQuestRules
    {
        public static int Stage(Scenario01Progress state, int id)
        {
            if (state == null) return 0;
            for (int step = 3; step >= 1; step--) if (state.dutySteps.Contains(id + ":" + step)) return step;
            return 0;
        }
        // Local claim step3 is a persistent receipt. Source EVT owns objective step2;
        // its narrative-only necklace/orb is never fabricated as an inventory item.
        public static bool TryApply(Scenario01Definition scenario, Scenario01Progress state, NativeInventory items,
            TownQuestDefinition quest, bool claim, out Scenario01Progress next, out NativeInventory inventory)
{next=default;inventory=default;return false;}    }
}
