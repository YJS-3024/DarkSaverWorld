using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DarkSaver.Jobs;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class JobCatalogConverter
    {
        public const string Output = "Assets/_Game/Runtime/Jobs/JobCatalog.asset";
        public const string MonsterOutput = "Assets/_Game/Runtime/Jobs/MonsterJobCatalog.asset";
        public const string NpcOutput = "Assets/_Game/Runtime/Jobs/NpcSpecialJobCatalog.asset";
        public const string MercenaryOutput = "Assets/_Game/Runtime/Jobs/MercenaryJobCatalog.asset";
        [MenuItem("DarkSaver/Convert/Mercenary Job Catalog")]
        public static void ConvertMercenaries() => ConvertCatalog("mercenaries.csv", MercenaryOutput, "mercenary jobs");
        public const string Header = "직업,직그림,체력수,체력한,마력수,마력한,공력수,공력한,방력수,방력한,마공수,마공한,마방수,마방한,속도,이동력,명중률,회피율,크릭율,마명율,마회율,공거리,병종,특병종,이틱감,공틱감,휴틱감,마틱감,지범,지공,지방,획득금,획득경,가격,수평전환리스트,이름,설명,category";

        [MenuItem("DarkSaver/Convert/Job Catalog")]
        public static void Convert() => ConvertCatalog("jobs.csv", Output, "player jobs");

        [MenuItem("DarkSaver/Convert/Monster Job Catalog")]
        public static void ConvertMonsters() => ConvertCatalog("monsters.csv", MonsterOutput, "monster jobs");

        [MenuItem("DarkSaver/Convert/NPC Special Job Catalog")]
        public static void ConvertNpcsAndSpecial() => ConvertCatalog("npcs_and_special.csv", NpcOutput, "NPC/special jobs");

        private static void ConvertCatalog(string sourceFile, string output, string label)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Convert in edit mode only.");
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "분석/Saver_1.2/catalogs", sourceFile);
            var rows = Parse(File.ReadAllText(path, Encoding.UTF8));
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(output);
            if (catalog == null)
            {
                if (File.Exists(output)) throw new InvalidDataException("Existing output is not a job catalog; not overwritten.");
                catalog = ScriptableObject.CreateInstance<JobCatalog>();
                try
                {
                    catalog.Initialize(rows);
                    AssetDatabase.CreateAsset(catalog, output);
                    AssetDatabase.SaveAssetIfDirty(catalog);
                }
                finally { if (!AssetDatabase.Contains(catalog)) UnityEngine.Object.DestroyImmediate(catalog); }
            }
            Verify(catalog, rows);
            Debug.Log($"Verified {catalog.Count} native {label}. Existing data was not overwritten.");
        }

        public static JobCatalogEntry[] Parse(string csv)
        {
            using var reader = new StringReader(csv ?? string.Empty);
            if (string.Join(",", Fields(reader.ReadLine()?.TrimStart('\uFEFF'))) != Header)
                throw new InvalidDataException("Unexpected job CSV header.");
            var rows = new List<JobCatalogEntry>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = Fields(line);
                if (fields.Count != 38) throw new InvalidDataException("Expected 38 job columns.");
                var numbers = new long[JobCatalogEntry.NumericColumnCount];
                for (var i = 0; i < numbers.Length; i++)
                    if (!long.TryParse(fields[i], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out numbers[i]))
                        throw new InvalidDataException("Invalid job integer at column " + i + ": " + fields[i]);
                rows.Add(new JobCatalogEntry(numbers, fields[34], fields[35], fields[36], fields[37]));
            }
            if (rows.Count == 0) throw new InvalidDataException("Job CSV is empty.");
            return rows.ToArray();
        }

        public static void Verify(JobCatalog catalog, JobCatalogEntry[] source)
        {
            if (catalog == null || source == null || catalog.Count != source.Length) throw new InvalidDataException("Job count differs.");
            var ids = new HashSet<int>();
            foreach (var expected in source)
            {
                if (expected == null || !ids.Add(expected.Id)) throw new InvalidDataException("Null/duplicate job source.");
                if (!catalog.TryGet(expected.Id, out var actual) || actual.Name != expected.Name ||
                    actual.Description != expected.Description || actual.HorizontalTransitionText != expected.HorizontalTransitionText ||
                    actual.CategoryLabel != expected.CategoryLabel)
                    throw new InvalidDataException("Job text differs; not overwritten.");
                for (var i = 0; i < JobCatalogEntry.NumericColumnCount; i++)
                    if (actual.GetNumericValue(i) != expected.GetNumericValue(i))
                        throw new InvalidDataException("Job numeric field differs; not overwritten.");
            }
        }

        private static List<string> Fields(string line)
        {
            if (line == null) throw new InvalidDataException("Missing job CSV header.");
            var fields = new List<string>(); var cell = new StringBuilder();
            var quoted = false; var closed = false;
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
                else if (c == '"' || closed) throw new InvalidDataException("Malformed job CSV quote.");
                else cell.Append(c);
            }
            if (quoted) throw new InvalidDataException("Unterminated job CSV quote.");
            fields.Add(cell.ToString());
            return fields;
        }
    }
}
