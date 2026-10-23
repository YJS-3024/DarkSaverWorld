using System;
using System.IO;
using DarkSaver.Items;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class ItemDefinitionGenerator
    {
        public const string Output = "Assets/_Game/Data/Items";
        public static string PathFor(int id) => Output + "/Item_" + id.ToString("D4", System.Globalization.CultureInfo.InvariantCulture) + ".asset";

        [MenuItem("DarkSaver/Convert/Individual Item Definitions (create missing only)")]
        public static void GenerateAll()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogConverter.Output);
            if (catalog == null) throw new InvalidDataException("Native item catalog missing.");
            Debug.Log("Created individual items: " + GenerateBatch(0, catalog.Count));
        }

        public static int GenerateBatch(int start, int count)
        {
            string blocked = DarkSaver.EditorTools.WorkBatchMemoryMaintenance.BlockingReason();
            if (!string.IsNullOrEmpty(blocked)) throw new InvalidOperationException(blocked);
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogConverter.Output);
            if (catalog == null || start < 0 || count < 1 || start > catalog.Count - count)
                throw new ArgumentOutOfRangeException(nameof(start));
            // Validate every existing target before writing this batch. Preserve authored edits.
            for (int i = start; i < start + count; i++) {
                var row = catalog.GetAt(i); string path = PathFor(row.Id);
                var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (existing != null) Verify(existing, row);
                else if (File.Exists(path) || File.Exists(path + ".meta"))
                    throw new InvalidDataException("Existing non-item/orphan target not overwritten: " + path);
            }
            EnsureFolder(Output);
            int created = 0;
            AssetDatabase.StartAssetEditing();
            try {
                for (int i = start; i < start + count; i++) {
                    var row = catalog.GetAt(i); string path = PathFor(row.Id);
                    if (File.Exists(path)) continue;
                    var definition = ScriptableObject.CreateInstance<ItemDefinition>();
                    bool createdAsset = false;
                    try {
                        definition.Initialize(row);
                        AssetDatabase.CreateAsset(definition, path);
                        createdAsset = true;
                        AssetDatabase.SaveAssetIfDirty(definition);
                        created++;
                    } finally {
                        // Contains may be false until StopAssetEditing imports the file.
                        // Never destroy an object that CreateAsset has already accepted.
                        if (!createdAsset && !EditorUtility.IsPersistent(definition))
                            UnityEngine.Object.DestroyImmediate(definition);
                    }
                }
            } finally { AssetDatabase.StopAssetEditing(); }
            return created;
        }

        public static void Verify(ItemDefinition definition, ItemCatalogEntry source)
        {
            var actual = definition == null ? null : definition.Data;
            if (source == null || actual == null || actual.Id != source.Id || actual.Name != source.Name
                || actual.Description != source.Description || actual.CategoryLabel != source.CategoryLabel
                || actual.PriceNotation != source.PriceNotation || actual.DamageTargets != source.DamageTargets)
                throw new InvalidDataException("Item text/identity differs; existing definition not overwritten.");
            for (int i = 0; i < ItemCatalogEntry.NumericColumnCount; i++)
                if (actual.GetNumericValue(i) != source.GetNumericValue(i))
                    throw new InvalidDataException("Item " + source.Id + " field " + i + " differs; not overwritten.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1));
        }
    }

    [CustomEditor(typeof(ItemDefinition))]
    public sealed class ItemDefinitionInspector : UnityEditor.Editor
    {
        private static readonly string[] Labels = ItemCatalogConverter.Header.Split(',');
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var record = serializedObject.FindProperty("data");
            var values = record.FindPropertyRelative("numericValues");
            if (values.arraySize != ItemCatalogEntry.NumericColumnCount) {
                EditorGUILayout.HelpBox("카탈로그 기반 생성기로 데이터를 초기화하세요.", MessageType.Info);
                return;
            }
            EditorGUILayout.HelpBox("편집 후 DarkSaver/Items/Apply individual definitions to game catalog 메뉴로 반영하세요. 자동 반영은 하지 않습니다. 원본 표의 값이며 장비 효과·거래 가격 판정이 아닙니다.", MessageType.Info);
            EditorGUILayout.PropertyField(record.FindPropertyRelative("itemName"), new GUIContent("이름"));
            EditorGUILayout.PropertyField(record.FindPropertyRelative("description"), new GUIContent("설명"));
            EditorGUILayout.PropertyField(record.FindPropertyRelative("categoryLabel"), new GUIContent("분석 분류 (사용 규칙 아님)"));
            for (int i = 0; i < values.arraySize; i++) {
                using (new EditorGUI.DisabledScope(i == 0))
                    EditorGUILayout.PropertyField(values.GetArrayElementAtIndex(i), new GUIContent(Labels[i] + " [" + i + "]"));
            }
            EditorGUILayout.PropertyField(record.FindPropertyRelative("priceNotation"), new GUIContent("원본 가격 표기"));
            EditorGUILayout.PropertyField(record.FindPropertyRelative("damageTargets"), new GUIContent("데미지 병종 원문"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
