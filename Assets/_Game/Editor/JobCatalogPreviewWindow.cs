using System;
using System.Collections.Generic;
using System.Globalization;
using DarkSaver.Jobs;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public sealed class JobCatalogPreviewWindow : EditorWindow
    {
        [SerializeField] private int catalogSelection;
        [SerializeField] private string query = string.Empty;
        [SerializeField] private string spriteFilter = string.Empty;
        [SerializeField] private bool differentSpriteOnly;
        [SerializeField] private int selectedId = -1;
        private JobCatalog catalog;
        private Vector2 listScroll;
        private Vector2 detailScroll;
        private static readonly string[] NumericLabels = JobCatalogConverter.Header.Split(',');

        [MenuItem("DarkSaver/Preview/Job Catalog")]
        public static void Open() => GetWindow<JobCatalogPreviewWindow>("직업 데이터");

        private void OnEnable()
        {
            minSize = new Vector2(720f, 480f);
            LoadCatalog();
        }

        private void LoadCatalog()
        {
            catalogSelection = Mathf.Clamp(catalogSelection, 0, 2);
            var path = catalogSelection == 0 ? JobCatalogConverter.Output :
                catalogSelection == 1 ? JobCatalogConverter.MonsterOutput : JobCatalogConverter.NpcOutput;
            catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>(path);
            listScroll = detailScroll = Vector2.zero;
        }

        private void OnGUI()
        {
            var selection = GUILayout.Toolbar(catalogSelection, new[] { "플레이어", "몬스터", "NPC / 특수" });
            if (selection != catalogSelection) { catalogSelection = selection; selectedId = -1; LoadCatalog(); }
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("네이티브 에셋", catalog, typeof(JobCatalog), false);
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("네이티브 카탈로그가 없습니다. 해당 직업 카탈로그를 에디터에서 먼저 변환하세요.", MessageType.Warning);
                if (GUILayout.Button("네이티브 에셋 다시 읽기")) LoadCatalog();
                return;
            }
            EditorGUI.BeginChangeCheck();
            query = EditorGUILayout.TextField("직업 ID / 이름", query);
            spriteFilter = EditorGUILayout.TextField("그림 ID (정확 값)", spriteFilter);
            differentSpriteOnly = EditorGUILayout.ToggleLeft("직업 ID와 그림 ID가 다른 항목만", differentSpriteOnly);
            if (EditorGUI.EndChangeCheck()) listScroll = detailScroll = Vector2.zero;
            EditorGUILayout.HelpBox("네이티브 데이터 검토 전용입니다. 원문 열과 전환 표기를 그대로 표시하며, 스탯·성장·전직·AI·보상·그림 로딩을 실행하지 않습니다.", MessageType.Info);
            var rows = GetMatches(catalog, query, differentSpriteOnly, spriteFilter);
            EditorGUILayout.LabelField($"표시 {rows.Count}개 / 선택 카탈로그 {catalog.Count}개");
            var selected = GetSelected(rows, selectedId);
            if (selectedId != (selected?.Id ?? -1)) detailScroll = Vector2.zero;
            selectedId = selected?.Id ?? -1;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(245f));
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            foreach (var row in rows)
                if (GUILayout.Button($"{(row.Id == selectedId ? "> " : "")}{row.Id} / {DisplayName(row)} / 그림 {row.SpriteId}", EditorStyles.miniButton))
                { selectedId = row.Id; selected = row; detailScroll = Vector2.zero; }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            if (selected == null) EditorGUILayout.LabelField("검색 결과가 없습니다.");
            else DrawDetails(selected);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetails(JobCatalogEntry row)
        {
            EditorGUILayout.LabelField($"[{row.Id}] {DisplayName(row)}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"직업 ID {row.Id} / 그림 ID {row.SpriteId} / 분석 분류 {row.CategoryLabel}");
            if (row.Id != row.SpriteId)
                EditorGUILayout.HelpBox("직업 ID와 그림 ID가 다른 항목입니다. 그림 에셋 연결은 아직 실행하지 않습니다.", MessageType.Warning);
            EditorGUILayout.LabelField("수평 전환 원문 (실행하지 않음)");
            EditorGUILayout.SelectableLabel(row.HorizontalTransitionText ?? string.Empty, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField("이름 / 설명 원문");
            var originalText = (row.Name ?? string.Empty) + "\n" + (row.Description ?? string.Empty);
            var style = EditorStyles.wordWrappedLabel;
            var height = Mathf.Max(36f, style.CalcHeight(new GUIContent(originalText), Mathf.Max(180f, position.width - 290f)));
            EditorGUILayout.SelectableLabel(originalText, style, GUILayout.Height(height));
            EditorGUILayout.LabelField("숫자 열 원문 값 (음수도 보존)", EditorStyles.boldLabel);
            for (var i = 0; i < JobCatalogEntry.NumericColumnCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i}: {GetNumericColumnLabel(i)}", GUILayout.Width(120f));
                EditorGUILayout.SelectableLabel(row.GetNumericValue(i).ToString(CultureInfo.InvariantCulture), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.EndHorizontal();
            }
        }

        public static string GetNumericColumnLabel(int column)
        {
            if (column < 0 || column >= JobCatalogEntry.NumericColumnCount) throw new ArgumentOutOfRangeException(nameof(column));
            return NumericLabels[column];
        }

        public static List<JobCatalogEntry> GetMatches(JobCatalog source, string text, bool mismatchOnly, string exactSpriteId)
        {
            var rows = new List<JobCatalogEntry>();
            if (source == null) return rows;
            var search = (text ?? string.Empty).Trim().Replace('_', ' ');
            var spriteText = (exactSpriteId ?? string.Empty).Trim();
            var spriteId = 0L;
            if (spriteText.Length > 0 && !long.TryParse(spriteText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out spriteId)) return rows;
            for (var i = 0; i < source.Count; i++)
            {
                var row = source.GetAt(i);
                if (mismatchOnly && row.Id == row.SpriteId) continue;
                if (spriteText.Length > 0 && row.SpriteId != spriteId) continue;
                if (search.Length == 0 || row.Id.ToString(CultureInfo.InvariantCulture).IndexOf(search, StringComparison.Ordinal) >= 0 ||
                    (row.Name ?? string.Empty).Replace('_', ' ').IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) rows.Add(row);
            }
            return rows;
        }

        public static JobCatalogEntry GetSelected(IReadOnlyList<JobCatalogEntry> rows, int preferredId)
        {
            if (rows == null || rows.Count == 0) return null;
            for (var i = 0; i < rows.Count; i++) if (rows[i].Id == preferredId) return rows[i];
            return rows[0];
        }

        private static string DisplayName(JobCatalogEntry row) => string.IsNullOrEmpty(row.Name) ? "(이름 없음)" : row.Name.Replace('_', ' ');
    }
}
