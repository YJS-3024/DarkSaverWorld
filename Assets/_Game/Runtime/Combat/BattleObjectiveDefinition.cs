using UnityEngine;
using DarkSaver.Events;

namespace DarkSaver.Combat
{
    [CreateAssetMenu(menuName = "DarkSaver/Battle Objective")]
    public sealed class BattleObjectiveDefinition : ScriptableObject
    {
        [SerializeField] private string mapKey;
        [SerializeField, TextArea] private string description;
        [SerializeField] private short[] targetInstanceIds;
        [SerializeField, TextArea] private string completionDialogue;
        [SerializeField] private ScenarioDialogueCatalog completionDialogueCatalog;
        [SerializeField] private string completionDialogueScriptKey;
        [SerializeField] private int completionDialogueSourceLine;
        [SerializeField] private bool clearsScenarioOnVictory;

        public string MapKey => mapKey;
        public string Description => description;
        public string GetClearedScenarioKey(CombatWorld world) =>
            clearsScenarioOnVictory && !string.IsNullOrWhiteSpace(mapKey) && IsSatisfied(world)
                ? mapKey : string.Empty;
        public bool TryGetCompletionMessage(CombatWorld world, out EventMessage message)
        {
            message = default;
            if (!IsSatisfied(world) || completionDialogueCatalog == null ||
                !completionDialogueCatalog.TryGet(completionDialogueScriptKey, completionDialogueSourceLine, out var row))
                return false;
            message = row.Message;
            return true;
        }

        public string GetCompletionDialogue(CombatWorld world)
        {
            // Existing authored objectives retain their inline text. A broken catalog key must not hide behind it.
            if (completionDialogueCatalog != null)
                return TryGetCompletionMessage(world, out var message) ? message.Text : string.Empty;
            return IsSatisfied(world) ? completionDialogue ?? string.Empty : string.Empty;
        }
        public bool HasRequiredUnits(CombatWorld world)
        {
            if (targetInstanceIds == null || targetInstanceIds.Length == 0) return false;
            foreach (var id in targetInstanceIds)
            {
                var found = false;
                foreach (var unit in world.Units)
                    if (unit.Id == id && unit.Enemy) { found = true; break; }
                if (!found) return false;
            }
            return true;
        }
        public bool IsSatisfied(CombatWorld world)
        {
            // An authored objective without targets is incomplete, not an elimination rule.
            return targetInstanceIds != null && targetInstanceIds.Length > 0 &&
                BattleVictoryRules.IsSatisfied(world, targetInstanceIds);
        }
    }
}
