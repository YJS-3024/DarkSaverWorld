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
        {
            next = null; inventory = null;
            if (scenario == null || state == null || !state.Matches(scenario) || items == null || !items.IsValid
                || quest == null || !quest.IsValid || state.activeEvent != 0 || state.pendingBattle != -1) return false;
            int stage = Stage(state, quest.id);
            if (!claim && state.phase >= Scenario01Progress.Outro) return false;
            if (claim ? stage != 2 || !state.dutySteps.Contains(quest.id + ":1") : stage != 0) return false;
            var candidate = state.Clone(); var funds = items.Clone();
            if (claim)
            {
                if (quest.gold > long.MaxValue - funds.gold) return false;
                funds.gold += quest.gold;
                if (quest.itemId > 0)
                {
                    int count = funds.Count(quest.itemId); if (count == int.MaxValue) return false;
                    var stacks = funds.items.ToList(); var stack = stacks.Find(i => i.itemId == quest.itemId);
                    if (stack == null) stacks.Add(new NativeItemStack { itemId = quest.itemId, quantity = 1 });
                    else stack.quantity++;
                    funds.items = stacks.ToArray();
                }
            }
            candidate.dutySteps.Add(quest.id + (claim ? ":3" : ":1"));
            if (!candidate.Matches(scenario) || !funds.IsValid) return false;
            next = candidate; inventory = funds; return true;
        }
    }
}
