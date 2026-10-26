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
    public static class ExperienceProgressionConverter
    {
        public const string Output = "Assets/_Game/Runtime/Combat/ExperienceProgression.asset";
        private const string Header = "jobId,jobName,level,requiredXp,nextJobId,rewardGold,rewardXp,learnedMagic,usableSkills";

        [MenuItem("DarkSaver/Convert/Experience Progression")]
        public static void Convert()
        {
            var source = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "분석/Saver_Late/unity_catalogs/experience_progression.csv");
            var rows = Parse(File.ReadAllText(source, Encoding.UTF8));
            var table = AssetDatabase.LoadAssetAtPath<ExperienceProgressionTable>(Output);
            if (table == null)
            {
                if (File.Exists(Output)) throw new InvalidDataException("Output exists but is not the expected asset; not overwritten.");
                table = ScriptableObject.CreateInstance<ExperienceProgressionTable>();
                try
                {
                    table.Initialize(rows);
                    AssetDatabase.CreateAsset(table, Output);
                    AssetDatabase.SaveAssetIfDirty(table);
                }
                finally { if (!AssetDatabase.Contains(table)) UnityEngine.Object.DestroyImmediate(table); }
            }
            Verify(table, rows);
            Debug.Log($"Verified native experience table: {table.Count} records. Existing data was not overwritten.");
        }

        public static ExperienceLevelEntry[] Parse(string csv)
        {
            using var reader = new StringReader(csv);
            if (string.Join(",", Fields(reader.ReadLine()?.TrimStart('\uFEFF'))) != Header)
                throw new InvalidDataException("Unexpected experience CSV header.");
            var rows = new List<ExperienceLevelEntry>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = Fields(line);
                if (fields.Count != 9) throw new InvalidDataException("Expected nine experience columns.");
                rows.Add(new ExperienceLevelEntry
                {
                    jobId = checked((int)Number(fields[0])), jobName = fields[1], level = checked((int)Number(fields[2])),
                    requiredXp = Number(fields[3]), nextJobId = checked((int)Number(fields[4])),
                    rewardGold = Number(fields[5]), rewardXp = Number(fields[6]),
                    learnedMagic = fields[7], usableSkills = fields[8]
                });
            }
            return rows.ToArray();
        }

        private static long Number(string value)
        {
            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                throw new InvalidDataException("Expected non-negative integer: " + value);
            return number;
        }

        internal static List<string> Fields(string line)
        {
            if (line == null) throw new InvalidDataException("Missing CSV header.");
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
                else if (c == '"' || closed) throw new InvalidDataException("Malformed CSV quote.");
                else cell.Append(c);
            }
            if (quoted) throw new InvalidDataException("Unterminated CSV quote.");
            fields.Add(cell.ToString());
            return fields;
        }

        public static void Verify(ExperienceProgressionTable table, ExperienceLevelEntry[] source)
        {
            if (table == null || table.Count != source.Length) throw new InvalidDataException("Experience row count differs.");
            foreach (var row in source)
                if (!table.TryGet(row.jobId, row.level, out var actual) || !actual.Equals(row))
                    throw new InvalidDataException($"Native experience data differs at {row.jobId}/{row.level}; not overwritten.");
        }
    }
}
