using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DarkSaver.Events;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class ScenarioDialogueConverter
    {
        public const string Output = "Assets/_Game/Runtime/Events/Brugos01Dialogue.asset";

        [MenuItem("DarkSaver/Convert/Brugos 01 Dialogue")]
        public static void Convert()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Convert in edit mode only.");
            var folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "분석/Saver_1.2/decoded/all_arc/MAP/01set");
            var source = new List<ScenarioDialogueEntry>();
            source.AddRange(Parse("DEO", File.ReadAllText(Path.Combine(folder, "01.DEO"), Encoding.GetEncoding(949))));
            source.AddRange(Parse("EVT", File.ReadAllText(Path.Combine(folder, "01.evt"), Encoding.GetEncoding(949))));
            var rows = source.ToArray();
            var catalog = AssetDatabase.LoadAssetAtPath<ScenarioDialogueCatalog>(Output);
            if (catalog == null)
            {
                if (File.Exists(Output)) throw new InvalidDataException("Existing output is not a dialogue catalog; not overwritten.");
                catalog = ScriptableObject.CreateInstance<ScenarioDialogueCatalog>();
                try
                {
                    catalog.Initialize(rows);
                    AssetDatabase.CreateAsset(catalog, Output);
                    AssetDatabase.SaveAssetIfDirty(catalog);
                }
                finally { if (!AssetDatabase.Contains(catalog)) UnityEngine.Object.DestroyImmediate(catalog); }
            }
            Verify(catalog, rows);
            Debug.Log($"Verified {catalog.Count} native dialogue records. Existing data was not overwritten.");
        }

        public static ScenarioDialogueEntry[] Parse(string scriptKey, string text)
        {
            if (scriptKey != "DEO" && scriptKey != "EVT") throw new ArgumentException("Unsupported scenario script key.");
            var rows = new List<ScenarioDialogueEntry>();
            var eventToken = string.Empty;
            foreach (var command in EventScriptParser.Parse(text))
            {
                if (scriptKey == "EVT" && command.Name == "EVENT")
                {
                    if (command.Arguments.Length != 1) throw new InvalidDataException("Malformed EVENT header.");
                    eventToken = command.Arguments[0];
                }
                else if (scriptKey == "EVT" && command.Name == "END") eventToken = string.Empty;
                if (command.Name != "MESSAGE") continue;
                if (!command.TryReadMessage(out var message)) throw new InvalidDataException($"Malformed MESSAGE at {command.LineNumber}.");
                if (scriptKey == "EVT" && eventToken.Length == 0) throw new InvalidDataException("MESSAGE outside EVENT.");
                rows.Add(new ScenarioDialogueEntry
                {
                    scriptKey = scriptKey, sourceLine = command.LineNumber, eventToken = eventToken,
                    optionToken = message.OptionToken, definitionToken = message.DefinitionToken,
                    speakerToken = message.SpeakerToken, variantToken = message.VariantToken, text = message.Text
                });
            }
            return rows.ToArray();
        }

        public static void Verify(ScenarioDialogueCatalog catalog, ScenarioDialogueEntry[] source)
        {
            if (catalog == null || catalog.Count != source.Length) throw new InvalidDataException("Dialogue count differs.");
            for (var i = 0; i < source.Length; i++)
                if (!catalog.GetAt(i).Equals(source[i]) ||
                    !catalog.TryGet(source[i].scriptKey, source[i].sourceLine, out var row) || !row.Equals(source[i]))
                    throw new InvalidDataException($"Native dialogue differs at {source[i].scriptKey}/{source[i].sourceLine}; not overwritten.");
        }
    }
}
