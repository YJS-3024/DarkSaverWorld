using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.Items;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class ItemDefinitionPublisher
    {
        [MenuItem("DarkSaver/Items/Apply individual definitions to game catalog")]
        public static void ApplyAll()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogConverter.Output);
            var guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] {ItemDefinitionGenerator.Output});
            var definitions = new ItemDefinition[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                definitions[i] = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
            Apply(catalog, definitions);
            Debug.Log("Applied " + catalog.Count + " individual item definitions; catalog references/GUID retained.");
        }

        // Build the entire detached candidate before touching a live catalog.
        public static ItemCatalogEntry[] BuildCandidate(ItemCatalog target, ItemDefinition[] definitions)
        {
            if (target == null || target.Count == 0 || definitions == null || definitions.Length == 0)
                throw new InvalidDataException("Target catalog or item definitions missing.");
            var ids = new HashSet<int>();
            var rows = new ItemCatalogEntry[definitions.Length];
            for (int i = 0; i < definitions.Length; i++) {
                var data = definitions[i] == null ? null : definitions[i].Data;
                if (data == null) throw new InvalidDataException("Uninitialized item definition.");
                var values = new long[ItemCatalogEntry.NumericColumnCount];
                for (int column = 0; column < values.Length; column++) values[column] = data.GetNumericValue(column);
                var row = new ItemCatalogEntry(values, data.PriceNotation, data.DamageTargets,
                    data.Name, data.Description, data.CategoryLabel);
                if (!ids.Add(row.Id)) throw new InvalidDataException("Duplicate item ID: " + row.Id);
                rows[i] = row;
            }
            // Missing files must not silently erase previously available items.
            for (int i = 0; i < target.Count; i++)
                if (!ids.Contains(target.GetAt(i).Id)) throw new InvalidDataException("Missing existing item ID: " + target.GetAt(i).Id);
            Array.Sort(rows, (a, b) => a.Id.CompareTo(b.Id));
            return rows;
        }

        public static void Apply(ItemCatalog target, ItemDefinition[] definitions)
        {
            string blocked = DarkSaver.EditorTools.WorkBatchMemoryMaintenance.BlockingReason();
            if (!string.IsNullOrEmpty(blocked)) throw new InvalidOperationException(blocked);
            var rows = BuildCandidate(target, definitions);
            Undo.RecordObject(target, "Apply individual item definitions");
            target.Initialize(rows);
            EditorUtility.SetDirty(target);
            if (AssetDatabase.Contains(target)) AssetDatabase.SaveAssetIfDirty(target);
        }
    }
}
