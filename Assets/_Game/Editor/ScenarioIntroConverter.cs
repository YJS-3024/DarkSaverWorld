using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DarkSaver.Events;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class ScenarioIntroConverter
    {
        public const string Output = "Assets/_Game/Runtime/Events/Brugos01Intro.asset";

        [MenuItem("DarkSaver/Convert/Brugos 01 Intro")]
        public static void Convert()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Convert in edit mode only.");
            var file = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "분석/Saver_1.2/decoded/all_arc/MAP/01set/01.DEO");
            var text = File.ReadAllText(file, Encoding.GetEncoding(949));
            var rows = Parse("DEO", text);
            var messages = AssetDatabase.LoadAssetAtPath<ScenarioDialogueCatalog>(ScenarioDialogueConverter.Output);
            foreach (var source in ScenarioDialogueConverter.Parse("DEO", text))
                if (messages == null || !messages.TryGet("DEO", source.sourceLine, out var native) || !native.Equals(source))
                    throw new InvalidDataException("Existing native intro dialogue differs; not overwritten.");
            var catalog = AssetDatabase.LoadAssetAtPath<ScenarioIntroCatalog>(Output);
            if (catalog == null)
            {
                if (File.Exists(Output)) throw new InvalidDataException("Existing output is not an intro catalog; not overwritten.");
                catalog = ScriptableObject.CreateInstance<ScenarioIntroCatalog>();
                try
                {
                    catalog.Initialize("DEO", rows, messages);
                    AssetDatabase.CreateAsset(catalog, Output);
                    AssetDatabase.SaveAssetIfDirty(catalog);
                }
                finally { if (!AssetDatabase.Contains(catalog)) UnityEngine.Object.DestroyImmediate(catalog); }
            }
            Verify(catalog, "DEO", rows, messages);
            Debug.Log($"Verified {catalog.Count} ordered native intro steps. No commands executed; existing data not overwritten.");
        }

        public static ScenarioIntroStep[] Parse(string scriptKey, string text)
        {
            if (scriptKey != "DEO" && scriptKey != "DEE") throw new ArgumentException("Unsupported intro script key.");
            var rows = new List<ScenarioIntroStep>();
            foreach (var command in EventScriptParser.Parse(text))
            {
                var row = new ScenarioIntroStep { sourceLine = command.LineNumber, commandName = command.Name, definitionToken = string.Empty };
                switch (command.Name)
                {
                    case "CHARPOS":
                        if (!command.TryReadScenePlacement(out var placement)) throw new InvalidDataException($"Malformed CHARPOS at {command.LineNumber}.");
                        row.kind = ScenarioIntroStepKind.Placement;
                        row.definitionToken = placement.DefinitionToken;
                        row.instanceId = placement.InstanceId;
                        row.x = placement.X;
                        row.y = placement.Y;
                        break;
                    case "CHARMOVE":
                        if (!command.TryReadSceneMove(out var move)) throw new InvalidDataException($"Malformed CHARMOVE at {command.LineNumber}.");
                        row.kind = ScenarioIntroStepKind.Move;
                        row.instanceId = move.InstanceId;
                        row.x = move.X;
                        row.y = move.Y;
                        break;
                    case "MESSAGE":
                        if (!command.TryReadMessage(out _)) throw new InvalidDataException($"Malformed MESSAGE at {command.LineNumber}.");
                        row.kind = ScenarioIntroStepKind.Dialogue;
                        break;
                    // Keep unsupported command boundaries. Their arguments/semantics are not converted.
                }
                rows.Add(row);
            }
            if (rows.Count == 0) throw new InvalidDataException("Intro has no commands.");
            return rows.ToArray();
        }

        public static void Verify(ScenarioIntroCatalog catalog, string key, ScenarioIntroStep[] rows, ScenarioDialogueCatalog messages)
        {
            if (catalog == null || catalog.ScriptKey != key || catalog.Count != rows.Length)
                throw new InvalidDataException("Native intro count/key differs.");
            for (var i = 0; i < rows.Length; i++)
            {
                if (!catalog.GetAt(i).Equals(rows[i])) throw new InvalidDataException($"Native intro differs at step {i}; not overwritten.");
                if (rows[i].kind == ScenarioIntroStepKind.Dialogue &&
                    (!catalog.TryGetDialogueAt(i, out var bound) || messages == null ||
                     !messages.TryGet(key, rows[i].sourceLine, out var expected) || !bound.Equals(expected)))
                    throw new InvalidDataException("Native dialogue binding differs.");
            }
        }
    }
}
