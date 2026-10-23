using System;
using UnityEngine;

namespace DarkSaver.Events
{
    [Serializable]
    public struct ScenarioDialogueEntry
    {
        public string scriptKey;
        public int sourceLine;
        public string eventToken;
        public string optionToken;
        public string definitionToken;
        public string speakerToken;
        public string variantToken;
        [TextArea] public string text;

        public EventMessage Message => new EventMessage(optionToken, definitionToken, speakerToken, variantToken, text);
    }

    // Authored Unity data only. This is not an event executor or a branch-selection rule.
    [CreateAssetMenu(menuName = "DarkSaver/Scenario Dialogue Catalog")]
    public sealed class ScenarioDialogueCatalog : ScriptableObject
    {
        [SerializeField] private ScenarioDialogueEntry[] entries = Array.Empty<ScenarioDialogueEntry>();
        public int Count => entries.Length;
        public ScenarioDialogueEntry GetAt(int index) => entries[index];

        public void Initialize(ScenarioDialogueEntry[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Dialogue catalog is empty.");
            for (var i = 0; i < source.Length; i++)
            {
                var row = source[i];
                if (string.IsNullOrWhiteSpace(row.scriptKey) || row.sourceLine < 1 ||
                    string.IsNullOrWhiteSpace(row.optionToken) || string.IsNullOrWhiteSpace(row.definitionToken) ||
                    string.IsNullOrWhiteSpace(row.speakerToken) || string.IsNullOrWhiteSpace(row.variantToken) ||
                    string.IsNullOrWhiteSpace(row.text)) throw new ArgumentException("Invalid dialogue record.");
                for (var j = 0; j < i; j++)
                    if (source[j].scriptKey == row.scriptKey && source[j].sourceLine == row.sourceLine)
                        throw new ArgumentException("Duplicate dialogue source key.");
            }
            entries = (ScenarioDialogueEntry[])source.Clone();
        }

        public bool TryGet(string scriptKey, int sourceLine, out ScenarioDialogueEntry entry)
        {
            foreach (var row in entries)
                if (row.scriptKey == scriptKey && row.sourceLine == sourceLine) { entry = row; return true; }
            entry = default;
            return false;
        }
    }
}
