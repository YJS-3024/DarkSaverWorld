using System;
using System.Collections.Generic;
using DarkSaver.Events;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public sealed class ScenarioDialoguePreviewWindow : EditorWindow
    {
        [SerializeField] private ScenarioDialogueCatalog catalog;
        [SerializeField] private int scriptSelection;
        [SerializeField] private string eventFilter = string.Empty;
        [SerializeField] private ScenarioIntroCatalog introCatalog;
        [SerializeField] private bool unsupportedOnly;
        [SerializeField] private string commandFilter = string.Empty;
        private Vector2 scroll;

        [MenuItem("DarkSaver/Preview/Scenario Dialogue")]
        public static void Open() => GetWindow<ScenarioDialoguePreviewWindow>("시나리오 대사");

        private void OnEnable()
        {
            minSize = new Vector2(420f, 320f);
            if (catalog == null) catalog = AssetDatabase.LoadAssetAtPath<ScenarioDialogueCatalog>(ScenarioDialogueConverter.Output);
            if (introCatalog == null) introCatalog = AssetDatabase.LoadAssetAtPath<ScenarioIntroCatalog>(ScenarioIntroConverter.Output);
        }

        private void OnGUI()
        {
            var selection = GUILayout.Toolbar(scriptSelection, new[] { "진입 대사 DEO", "이벤트 대사 EVT", "진입 순서" });
            if (selection != scriptSelection) { scriptSelection = selection; scroll = Vector2.zero; }
            if (scriptSelection == 2) { DrawIntro(); return; }
            catalog = (ScenarioDialogueCatalog)EditorGUILayout.ObjectField("네이티브 카탈로그", catalog,
                typeof(ScenarioDialogueCatalog), false);
            if (scriptSelection == 1) eventFilter = EditorGUILayout.TextField("이벤트 토큰 (빈 값: 전체)", eventFilter);
            EditorGUILayout.HelpBox("대사 검토 전용입니다. 이동·공격·카메라·대기 연출은 재생하지 않습니다. " +
                "EVT의 조건 분기 대사는 함께 나열되며 조건 선택·화자 참조·%s 치환은 실행하지 않습니다.", MessageType.Info);
            var rows = GetMatches(catalog, scriptSelection == 0 ? "DEO" : "EVT", eventFilter?.Trim());
            EditorGUILayout.LabelField($"표시 대사 {rows.Count}개 / 카탈로그 {catalog?.Count ?? 0}개");
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var row in rows)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{row.scriptKey} / 원문 {row.sourceLine}행 / 이벤트 {row.eventToken}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"헤더 토큰: {row.optionToken} | {row.definitionToken} | {row.speakerToken} | {row.variantToken}");
                var style = EditorStyles.wordWrappedLabel;
                var height = Mathf.Max(36f, style.CalcHeight(new GUIContent(row.Message.DisplayText), Mathf.Max(160f, position.width - 55f)));
                EditorGUILayout.SelectableLabel(row.Message.DisplayText, style, GUILayout.Height(height));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawIntro()
        {
            introCatalog = (ScenarioIntroCatalog)EditorGUILayout.ObjectField("네이티브 진입 카탈로그", introCatalog,
                typeof(ScenarioIntroCatalog), false);
            commandFilter = EditorGUILayout.TextField("명령 검색", commandFilter);
            unsupportedOnly = EditorGUILayout.Toggle("미지원 경계만 표시", unsupportedOnly);
            EditorGUILayout.HelpBox("순서 검토 전용이며 구조화된 데이터도 아직 실행하지 않습니다. 미지원 경계의 인수·동작은 미복원입니다. " +
                "필터는 표시만 줄이며 명령을 건너뛰거나 자동 재생하지 않습니다. 재사용된 인스턴스 ID는 줄 순서대로 구분합니다.", MessageType.Warning);
            var matches = GetIntroMatches(introCatalog, unsupportedOnly, commandFilter);
            var unsupported = GetIntroMatches(introCatalog, true, null).Count;
            EditorGUILayout.LabelField($"표시 {matches.Count}단계 / 전체 {introCatalog?.Count ?? 0}단계 / 미지원 경계 {unsupported}개");
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var index in matches)
            {
                var row = introCatalog.GetAt(index);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"#{index + 1} / {introCatalog.ScriptKey} {row.sourceLine}행 / {row.commandName}", EditorStyles.boldLabel);
                var text = GetIntroDescription(introCatalog, index);
                var style = EditorStyles.wordWrappedLabel;
                var height = Mathf.Max(36f, style.CalcHeight(new GUIContent(text), Mathf.Max(160f, position.width - 55f)));
                EditorGUILayout.SelectableLabel(text, style, GUILayout.Height(height));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        // Return source indices so filtering never renumbers or merges native steps.
        public static List<int> GetIntroMatches(ScenarioIntroCatalog source, bool onlyUnsupported, string command)
        {
            var matches = new List<int>();
            if (source == null) return matches;
            command = command?.Trim();
            for (var i = 0; i < source.Count; i++)
            {
                var row = source.GetAt(i);
                if (onlyUnsupported && row.kind != ScenarioIntroStepKind.Unsupported) continue;
                if (!string.IsNullOrEmpty(command) &&
                    (row.commandName == null || row.commandName.IndexOf(command, StringComparison.OrdinalIgnoreCase) < 0)) continue;
                matches.Add(i);
            }
            return matches;
        }

        public static string GetIntroDescription(ScenarioIntroCatalog source, int index)
        {
            if (source == null || index < 0 || index >= source.Count) return string.Empty;
            var row = source.GetAt(index);
            switch (row.kind)
            {
                case ScenarioIntroStepKind.Placement:
                    return $"배치 데이터: 정의 참조 {row.definitionToken} / 인스턴스 {row.instanceId} / 원본 좌표 ({row.x}, {row.y})";
                case ScenarioIntroStepKind.Move:
                    return $"이동 데이터: 인스턴스 {row.instanceId} / 원본 목적 좌표 ({row.x}, {row.y})\n속도·경로·도착 대기는 미연결입니다.";
                case ScenarioIntroStepKind.Dialogue:
                    if (!source.TryGetDialogueAt(index, out var message)) return "대사 참조 누락: 재생하지 않습니다.";
                    return $"대사 헤더: {message.optionToken} | {message.definitionToken} | {message.speakerToken} | {message.variantToken}\n{message.Message.DisplayText}";
                default:
                    return "미지원 경계: 인수·동작 미복원. 건너뛰거나 재생하지 않습니다.";
            }
        }

        public static List<ScenarioDialogueEntry> GetMatches(ScenarioDialogueCatalog source, string scriptKey, string eventToken)
        {
            var rows = new List<ScenarioDialogueEntry>();
            if (source == null || (scriptKey != "DEO" && scriptKey != "EVT")) return rows;
            for (var i = 0; i < source.Count; i++)
            {
                var row = source.GetAt(i);
                if (row.scriptKey == scriptKey && (scriptKey == "DEO" || string.IsNullOrEmpty(eventToken) || row.eventToken == eventToken))
                    rows.Add(row);
            }
            rows.Sort((a, b) => a.sourceLine.CompareTo(b.sourceLine));
            return rows;
        }
    }
}
