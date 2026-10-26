using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DarkSaver.Items;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class ItemCatalogConverter
    {
        public const string Output = "Assets/_Game/Runtime/Items/ItemCatalog.asset";
        public const string Header = "번호,그림1,그림2,장착위,장착렙,내구,보,비,기,수,창,궁,승,신,마,사,가격,마법번,이동,체력,공격,방어,공범위,마법력,마법공,마법방,마법범위,지휘범위,명중률,회피율,크컬율,마명률,마회율,마렙치,이틱감,공틱감,휴틱감,마틱감,S항목,S수치,데미지,데미지병종(n명),이름,설명,itemCategory";

        [MenuItem("DarkSaver/Convert/Item Catalog")]
        public static void Convert()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Convert in edit mode only.");
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "분석/Saver_Late/unity_catalogs/items.csv");
            var rows = Parse(File.ReadAllText(path, Encoding.UTF8));
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(Output);
            if (catalog == null)
            {
                if (File.Exists(Output)) throw new InvalidDataException("Existing output is not an item catalog; not overwritten.");
                catalog = ScriptableObject.CreateInstance<ItemCatalog>();
                try
                {
                    catalog.Initialize(rows);
                    AssetDatabase.CreateAsset(catalog, Output);
                    AssetDatabase.SaveAssetIfDirty(catalog);
                }
                finally { if (!AssetDatabase.Contains(catalog)) UnityEngine.Object.DestroyImmediate(catalog); }
            }
            Verify(catalog, rows);
            Debug.Log($"Verified {catalog.Count} native items. Existing data was not overwritten.");
        }

        public static ItemCatalogEntry[] Parse(string csv)
        {
            using var reader = new StringReader(csv ?? string.Empty);
            if (string.Join(",", Fields(reader.ReadLine()?.TrimStart('\uFEFF'))) != Header)
                throw new InvalidDataException("Unexpected item CSV header.");
            var rows = new List<ItemCatalogEntry>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = Fields(line);
                if (fields.Count != 45) throw new InvalidDataException("Expected 45 item columns.");
                var numbers = new long[ItemCatalogEntry.NumericColumnCount];
                for (var i = 0; i < numbers.Length; i++)
                {
                    if (i == ItemCatalogEntry.PriceColumn)
                    {
                        // Catalog price uses exponent notation (1E+11). Preserve exact integer and original notation.
                        numbers[i] = IntegerPrice(fields[i]);
                    }
                    else if (!long.TryParse(fields[i], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out numbers[i]))
                        throw new InvalidDataException("Invalid item integer: " + fields[i]);
                }
                rows.Add(new ItemCatalogEntry(numbers, fields[16], fields[41], fields[42], fields[43], fields[44]));
            }
            return rows.ToArray();
        }

        public static void Verify(ItemCatalog catalog, ItemCatalogEntry[] source)
        {
            if (catalog == null || catalog.Count != source.Length) throw new InvalidDataException("Item count differs.");
            foreach (var expected in source)
            {
                if (!catalog.TryGet(expected.Id, out var actual) || actual.Name != expected.Name ||
                    actual.Description != expected.Description || actual.DamageTargets != expected.DamageTargets ||
                    actual.CategoryLabel != expected.CategoryLabel || actual.PriceNotation != expected.PriceNotation)
                    throw new InvalidDataException("Item text differs; not overwritten.");
                for (var i = 0; i < ItemCatalogEntry.NumericColumnCount; i++)
                    if (actual.GetNumericValue(i) != expected.GetNumericValue(i))
                        throw new InvalidDataException("Item numeric field differs; not overwritten.");
            }
        }

        private static long IntegerPrice(string text)
        {
            if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer)) return integer;
            var parts = text.Split('E', 'e');
            if (parts.Length != 2 || !long.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out integer) ||
                !int.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var exponent) || exponent < 0 || exponent > 18)
                throw new InvalidDataException("Invalid integer price.");
            try { for (var i = 0; i < exponent; i++) integer = checked(integer * 10); }
            catch (OverflowException error) { throw new InvalidDataException("Item price exceeds int64.", error); }
            return integer;
        }

        private static List<string> Fields(string line)
        {
            if (line == null) throw new InvalidDataException("Missing item CSV header.");
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
                else if (c == '"' || closed) throw new InvalidDataException("Malformed item CSV quote.");
                else cell.Append(c);
            }
            if (quoted) throw new InvalidDataException("Unterminated item CSV quote.");
            fields.Add(cell.ToString());
            return fields;
        }
    }
}
