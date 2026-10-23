using System;
using UnityEngine;

namespace DarkSaver.Events
{
    public enum ScenarioIntroStepKind { Unsupported = 0, Placement = 1, Move = 2, Dialogue = 3 }

    [Serializable]
    public struct ScenarioIntroStep
    {
        public int sourceLine;
        public string commandName;
        public ScenarioIntroStepKind kind;
        public string definitionToken;
        public short instanceId;
        public int x;
        public int y;
    }

    // Ordered native metadata, not a cutscene executor. Unsupported entries must not be skipped.
    [CreateAssetMenu(menuName = "DarkSaver/Scenario Intro Catalog")]
    public sealed class ScenarioIntroCatalog : ScriptableObject
    {
        [SerializeField] private string scriptKey;
        [SerializeField] private ScenarioDialogueCatalog dialogue;
        [SerializeField] private ScenarioIntroStep[] steps = Array.Empty<ScenarioIntroStep>();
        public string ScriptKey => scriptKey;
        public int Count => steps.Length;
        public ScenarioIntroStep GetAt(int index) => steps[index];

        public void Initialize(string key, ScenarioIntroStep[] source, ScenarioDialogueCatalog messages)
        {
            if (key != "DEO" && key != "DEE") throw new ArgumentException("Only scene intro scripts are supported.");
            if (source == null || source.Length == 0) throw new ArgumentException("Intro is empty.");
            var previousLine = 0;
            foreach (var row in source)
            {
                if (row.sourceLine <= previousLine || string.IsNullOrWhiteSpace(row.commandName))
                    throw new ArgumentException("Intro lines must be strictly ordered.");
                previousLine = row.sourceLine;
                switch (row.kind)
                {
                    case ScenarioIntroStepKind.Placement:
                        if (row.commandName != "CHARPOS" || string.IsNullOrWhiteSpace(row.definitionToken))
                            throw new ArgumentException("Invalid placement.");
                        break;
                    case ScenarioIntroStepKind.Move:
                        if (row.commandName != "CHARMOVE" || !string.IsNullOrEmpty(row.definitionToken))
                            throw new ArgumentException("Invalid movement.");
                        break;
                    case ScenarioIntroStepKind.Dialogue:
                        if (row.commandName != "MESSAGE" || messages == null ||
                            !messages.TryGet(key, row.sourceLine, out _))
                            throw new ArgumentException("Native dialogue reference is missing.");
                        goto case ScenarioIntroStepKind.Unsupported;
                    case ScenarioIntroStepKind.Unsupported:
                        if (!string.IsNullOrEmpty(row.definitionToken) || row.instanceId != 0 || row.x != 0 || row.y != 0)
                            throw new ArgumentException("Non-spatial step contains spatial data.");
                        if (row.kind == ScenarioIntroStepKind.Unsupported &&
                            (row.commandName == "CHARPOS" || row.commandName == "CHARMOVE" || row.commandName == "MESSAGE"))
                            throw new ArgumentException("Known syntax must not be hidden as unsupported.");
                        break;
                    default: throw new ArgumentException("Unknown intro step kind.");
                }
            }
            scriptKey = key;
            dialogue = messages;
            steps = (ScenarioIntroStep[])source.Clone();
            for (var i = 0; i < steps.Length; i++)
                steps[i].definitionToken ??= string.Empty;
        }

        public bool TryGetDialogueAt(int index, out ScenarioDialogueEntry entry)
        {
            entry = default;
            return index >= 0 && index < steps.Length && steps[index].kind == ScenarioIntroStepKind.Dialogue &&
                dialogue != null && dialogue.TryGet(scriptKey, steps[index].sourceLine, out entry);
        }
    }
}
