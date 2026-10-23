using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.Jobs;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class MercenaryHireConverter
    {
        public const string Output = "Assets/_Game/Runtime/Jobs/MercenaryHireCatalog.asset";
        public static MercenaryHireEntry[] Parse(string csv)
        {
            using var reader = new StringReader(csv);
            if (string.Join(",", ExperienceProgressionConverter.Fields(reader.ReadLine()?.TrimStart('\uFEFF')))
                != "특수스킬,이름,Level,용병고용종수,용병고용수") throw new InvalidDataException("Unexpected hire header");
            var rows = new List<MercenaryHireEntry>(); string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = ExperienceProgressionConverter.Fields(line);
                if (fields.Count != 5) throw new InvalidDataException("Expected five hire columns");
                var ids = fields[3].Split(','); var jobs = new int[ids.Length];
                for (int i = 0; i < ids.Length; i++) jobs[i] = Number(ids[i]);
                rows.Add(new MercenaryHireEntry(Number(fields[0]), fields[1], Number(fields[2]), Number(fields[4]), jobs));
            }
            return rows.ToArray();
        }
        private static int Number(string text) => int.TryParse(text, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out int number) ? number : throw new InvalidDataException("Invalid hire number");
        public static void Verify(MercenaryHireCatalog catalog, MercenaryHireEntry[] rows)
        {
            if (catalog == null || catalog.Count != rows.Length) throw new InvalidDataException("Hire catalog count differs");
            foreach (var row in rows)
                if (!catalog.TryGet(row.SkillId, out var actual) || actual.Name != row.Name || actual.Level != row.Level
                    || actual.Maximum != row.Maximum || string.Join(",", actual.CopyJobIds()) != string.Join(",", row.CopyJobIds()))
                    throw new InvalidDataException("Hire record differs: " + row.SkillId);
        }
        [MenuItem("DarkSaver/Convert/Mercenary Hire Catalog")]
        public static void Convert()
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "분석/Saver_1.2/converted/solskill.csv");
            var rows = Parse(File.ReadAllText(path)); var catalog = AssetDatabase.LoadAssetAtPath<MercenaryHireCatalog>(Output);
            if (catalog == null)
            {
                if (File.Exists(Output)) throw new InvalidDataException("Unexpected existing output");
                catalog = ScriptableObject.CreateInstance<MercenaryHireCatalog>();
                try { catalog.Initialize(rows); AssetDatabase.CreateAsset(catalog, Output); AssetDatabase.SaveAssetIfDirty(catalog); }
                finally { if (!AssetDatabase.Contains(catalog)) UnityEngine.Object.DestroyImmediate(catalog); }
            }
            Verify(catalog, rows);
        }
    }
}
