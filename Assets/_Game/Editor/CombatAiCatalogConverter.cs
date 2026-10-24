using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class CombatAiCatalogConverter
    {
        public const string Output = "Assets/_Game/Runtime/Combat/CombatAiCatalog.asset";
        private const string RuleHeader = "ruleId,source,isBackup,unit,aiType,aiModeCode,targetType,targetX,targetY,radius,targetUnit,actionCount,weightTotal";
        private const string ActionHeader = "ruleId,source,isBackup,unit,actionOrder,actionType,token,magicId,weight,cumulativeStart,cumulativeEnd,effectiveProbability";

        [MenuItem("DarkSaver/Convert/Combat AI Catalog")]
        public static void Convert()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Convert in edit mode only.");
            var folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "분석/Saver_1.2/catalogs");
            var rows = Parse(File.ReadAllText(Path.Combine(folder, "ai_rules.csv"), Encoding.UTF8),
                File.ReadAllText(Path.Combine(folder, "ai_actions.csv"), Encoding.UTF8));
            var catalog = AssetDatabase.LoadAssetAtPath<CombatAiCatalog>(Output);
            if (catalog == null)
            {
                if (File.Exists(Output)) throw new InvalidDataException("Existing output is not an AI catalog; not overwritten.");
                catalog = ScriptableObject.CreateInstance<CombatAiCatalog>();
                try { catalog.Initialize(rows); AssetDatabase.CreateAsset(catalog, Output); AssetDatabase.SaveAssetIfDirty(catalog); }
                finally { if (!AssetDatabase.Contains(catalog)) UnityEngine.Object.DestroyImmediate(catalog); }
            }
            Verify(catalog, rows);
            Debug.Log($"Verified {catalog.Count} native AI rules. No original files referenced at runtime; existing data not overwritten.");
        }

        public static CombatAiRule[] Parse(string ruleCsv, string actionCsv)
        {
            var rules = Read(ruleCsv, RuleHeader, 13);
            var actions = Read(actionCsv, ActionHeader, 12);
            var grouped = new Dictionary<int, List<string[]>>();
            foreach (var row in actions)
            {
                var id = Number(row[0]);
                if (!grouped.TryGetValue(id, out var list)) grouped.Add(id, list = new List<string[]>());
                list.Add(row);
            }
            var output = new List<CombatAiRule>(); var unique = new HashSet<int>();
            foreach (var row in rules)
            {
                var id = Number(row[0]);
                if (!unique.Add(id) || !grouped.TryGetValue(id, out var source) || source.Count != Number(row[11]))
                    throw new InvalidDataException("Duplicate/missing AI rule or action count mismatch.");
                var steps = new CombatAiAction[source.Count]; long cumulative = 0;
                for (var i = 0; i < source.Count; i++)
                {
                    var step = source[i];
                    if (step[1] != row[1] || step[2] != row[2] || Short(step[3]) != Short(row[3]) || Number(step[4]) != i + 1)
                        throw new InvalidDataException("AI action metadata/order mismatch.");
                    var weight = Number(step[8]);
                    if (weight < 0 || Number(step[9]) != cumulative) throw new InvalidDataException("AI weight/start mismatch.");
                    cumulative += weight;
                    var probability = Math.Max(0L, Math.Min(100L, cumulative) - Math.Min(100L, cumulative - weight));
                    if (Number(step[10]) != cumulative || Number(step[11]) != probability)
                        throw new InvalidDataException("AI cumulative end/probability mismatch.");
                    steps[i] = new CombatAiAction(step[5], step[6], Optional(step[7]), weight);
                }
                if (cumulative != Number(row[12])) throw new InvalidDataException("AI total weight mismatch.");
                var slash = row[1].IndexOf('/');
                var mapFolder = slash >= 0 ? row[1].Substring(0, slash) : string.Empty;
                if (!mapFolder.EndsWith("set", StringComparison.OrdinalIgnoreCase) || mapFolder.Length <= 3)
                    throw new InvalidDataException("Invalid AI map key.");
                if (!bool.TryParse(row[2], out var backup)) throw new InvalidDataException("Invalid backup flag.");
                if (Optional(row[5]) != CombatAiRule.NativeModeCode(row[4]))
                    throw new InvalidDataException("AI mode differs from the original userunit parser.");
                var target = Optional(row[10]);
                if (target.HasValue && (target < short.MinValue || target > short.MaxValue))
                    throw new InvalidDataException("AI target ID outside short range.");
                output.Add(new CombatAiRule(id, mapFolder.Substring(0, mapFolder.Length - 3), backup,
                    Short(row[3]), row[4], Optional(row[5]), row[6], Optional(row[7]), Optional(row[8]),
                    Optional(row[9]), target.HasValue ? (short?)target.Value : null, steps));
                grouped.Remove(id);
            }
            if (output.Count == 0 || grouped.Count != 0) throw new InvalidDataException("Empty AI rules or orphaned actions.");
            return output.ToArray();
        }

        public static void Verify(CombatAiCatalog catalog, CombatAiRule[] expected)
        {
            if (catalog == null || expected == null || catalog.Count != expected.Length) throw new InvalidDataException("AI count differs.");
            var ids = new HashSet<int>();
            foreach (var row in expected)
                if (row == null || !ids.Add(row.Id) || !catalog.TryGet(row.Id, out var actual) || !actual.HasSameData(row))
                    throw new InvalidDataException("Native AI rule differs; existing data not overwritten.");
        }

        private static int Number(string text)
        {
            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException("Invalid AI integer.");
            return value;
        }
        private static int? Optional(string text) => text.Length == 0 ? null : Number(text);
        private static short Short(string text)
        {
            var value = Number(text);
            if (value < short.MinValue || value > short.MaxValue) throw new InvalidDataException("AI ID outside short range.");
            return (short)value;
        }
        private static List<string[]> Read(string csv, string header, int count)
        {
            using var reader = new StringReader(csv ?? string.Empty);
            if (string.Join(",", Fields(reader.ReadLine()?.TrimStart('\uFEFF'))) != header)
                throw new InvalidDataException("Unexpected AI header.");
            var rows = new List<string[]>(); string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cells = Fields(line);
                if (cells.Count != count) throw new InvalidDataException("Unexpected AI column count.");
                rows.Add(cells.ToArray());
            }
            return rows;
        }
        private static List<string> Fields(string line)
        {
            if (line == null) throw new InvalidDataException("Missing AI CSV line.");
            var fields = new List<string>(); var cell = new StringBuilder(); var quoted = false; var closed = false;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (quoted)
                {
                    if (c != '"') cell.Append(c);
                    else if (i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; }
                    else { quoted = false; closed = true; }
                }
                else if (c == ',') { fields.Add(cell.ToString()); cell.Clear(); closed = false; }
                else if (c == '"' && cell.Length == 0 && !closed) quoted = true;
                else if (c == '"' || closed) throw new InvalidDataException("Malformed AI CSV quote.");
                else cell.Append(c);
            }
            if (quoted) throw new InvalidDataException("Unterminated AI CSV quote.");
            fields.Add(cell.ToString()); return fields;
        }
    }
}
