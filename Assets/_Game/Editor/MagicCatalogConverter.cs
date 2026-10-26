using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DarkSaver.Magic;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class MagicCatalogConverter
    {
        public const string Output = "Assets/_Game/Runtime/Magic/MagicCatalog.asset";
        public const string Header = "번호,이름,속성,거리,범위,MP,시간,팀,유닛,EA,EB,EC,TICK,LVPT,패턴,iconx,icony";

        [MenuItem("DarkSaver/Convert/Magic Catalog")]
        public static void Convert()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Convert in edit mode only.");
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "분석/Saver_Late/unity_catalogs/Magictbl.csv");
            var rows = Parse(File.ReadAllText(path, Encoding.UTF8));
            var catalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>(Output);
            if (catalog == null)
            {
                if (File.Exists(Output)) throw new InvalidDataException("Existing output is not a magic catalog; not overwritten.");
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Runtime/Magic"))
                    AssetDatabase.CreateFolder("Assets/_Game/Runtime", "Magic");
                catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try
                {
                    catalog.Initialize(rows);
                    AssetDatabase.CreateAsset(catalog, Output);
                    AssetDatabase.SaveAssetIfDirty(catalog);
                }
                finally { if (!AssetDatabase.Contains(catalog)) UnityEngine.Object.DestroyImmediate(catalog); }
            }
            Verify(catalog, rows);
        }

        public static MagicCatalogEntry[] Parse(string csv)
        {
            using var reader = new StringReader(csv ?? string.Empty);
            if (string.Join(",", Fields(reader.ReadLine()?.TrimStart('\uFEFF'))) != Header)
                throw new InvalidDataException("Unexpected magic CSV header.");
            var rows = new List<MagicCatalogEntry>();
            var ids = new HashSet<int>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = Fields(line);
                if (fields.Count != 17) throw new InvalidDataException("Expected 17 magic columns.");
                var numbers = new long[MagicCatalogEntry.NumericColumnCount];
                for (var column = 0; column < 17; column++)
                {
                    if (column == 1) continue;
                    if (!long.TryParse(fields[column], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
                        out numbers[column == 0 ? 0 : column - 1]))
                        throw new InvalidDataException("Invalid magic integer at column " + column);
                }
                if (numbers[0] < 0 || numbers[0] > int.MaxValue || !ids.Add((int)numbers[0]))
                    throw new InvalidDataException("Invalid/duplicate magic ID.");
                rows.Add(new MagicCatalogEntry(numbers, fields[1]));
            }
            if (rows.Count == 0) throw new InvalidDataException("Magic CSV is empty.");
            return rows.ToArray();
        }

        public static void Verify(MagicCatalog catalog, MagicCatalogEntry[] source)
        {
            if (catalog == null || source == null || catalog.Count != source.Length) throw new InvalidDataException("Magic count differs.");
            var ids = new HashSet<int>();
            foreach (var expected in source)
            {
                if (expected == null || !ids.Add(expected.Id) || !catalog.TryGet(expected.Id, out var actual) || actual.Name != expected.Name)
                    throw new InvalidDataException("Magic ID/name differs; not overwritten.");
                for (var column = 0; column < 17; column++)
                    if (column != 1 && actual.GetNumericValue(column) != expected.GetNumericValue(column))
                        throw new InvalidDataException("Magic numeric field differs; not overwritten.");
            }
        }

        private static List<string> Fields(string line)
        {
            if (line == null) throw new InvalidDataException("Missing magic CSV header.");
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
                else if (c == '"' || closed) throw new InvalidDataException("Malformed magic CSV quote.");
                else cell.Append(c);
            }
            if (quoted) throw new InvalidDataException("Unterminated magic CSV quote.");
            fields.Add(cell.ToString());
            return fields;
        }
    }
}
