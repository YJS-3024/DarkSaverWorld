using DarkSaver.Combat;
using DarkSaver.Jobs;
using UnityEditor;

namespace DarkSaver.Editor
{
    [CustomEditor(typeof(BattleEnemyDefinition))]
    public sealed class BattleEnemyDefinitionInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            DrawPropertiesExcluding(serializedObject, "m_Script", "jobCatalog", "baseHp", "baseMp", "baseAttack", "baseDefense");

            if (serializedObject.FindProperty("useJobCatalogBaseStats").boolValue || serializedObject.FindProperty("useNativeBaseDefeatExperience").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jobCatalog"));
                EditorGUILayout.HelpBox("네이티브 카탈로그만 참조합니다. 처치 경험치는 별도 활성화하는 싱글판 기본 보상이며 서버 분배 공식이 아닙니다.", MessageType.Info);
            }
            if (!serializedObject.FindProperty("useJobCatalogBaseStats").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseHp"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseMp"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseAttack"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseDefense"));
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("현재 적용 기본 수치 / 단일 적 승리 목표", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(GetPreview(target as BattleEnemyDefinition, out var severity), severity);
            EditorGUILayout.HelpBox("서버 레벨·보상 공식을 적용한 최종 능력치가 아닙니다. 플레이어 개체 ID 충돌 및 씬 구성은 런타임에서 별도 검사합니다.", MessageType.Info);
        }

        // Read-only: use the same unit creation and objective rules as the existing runtime.
        public static string GetPreview(BattleEnemyDefinition definition, out MessageType severity)
        {
            severity = MessageType.Error;
            if (definition == null) return "전투 적 에셋이 없습니다.";
            var sourceText = "수치 출처: 직접 입력";
            if (definition.UsesJobCatalogBaseStats)
            {
                var catalog = new SerializedObject(definition).FindProperty("jobCatalog").objectReferenceValue as JobCatalog;
                if (catalog == null) return "네이티브 직업 카탈로그가 없습니다. 직접 입력 수치로 대체하지 않습니다.";
                if (!catalog.TryGet(definition.JobId, out var entry))
                    return $"네이티브 카탈로그에 직업 ID {definition.JobId}이 없습니다. 직접 입력 수치로 대체하지 않습니다.";
                sourceText = $"수치 출처: 네이티브 카탈로그 / 직업 ID {entry.Id}\n"
                    + $"원문 이름: {entry.Name}\n그림 ID: {entry.SpriteId} (그림 로딩은 미연결)\n"
                    + $"체력수(2): {entry.GetNumericValue(2)} / 마력수(4): {entry.GetNumericValue(4)}\n"
                    + $"공력수(6): {entry.GetNumericValue(6)} / 방력수(8): {entry.GetNumericValue(8)}";
            }
            if (!definition.TryCreateUnit(out var unit))
                return sourceText + "\n기본 수치 오류: HP는 1~int.MaxValue, MP·공격·방어는 0~int.MaxValue여야 합니다. 음수 표기를 보정하지 않습니다.";

            var summary = sourceText + $"\n전투 개체 ID: {unit.Id} / 직업 ID: {definition.JobId}\n"
                + $"표시 이름: {unit.Name} / 배치: ({unit.X}, {unit.Y})\n"
                + $"적용 수치: HP {unit.Hp} / MP {unit.Mp} / 공격 {unit.Attack} / 방어 {unit.Defense}";
            if(definition.JobId==701 && unit.MaxHp==450)summary+="\n키스라 HP: 사용자 확정 스크린샷 최대450 적용 (원문 ATR 보존)";
            if (definition.VictoryObjective != null)
            {
                var candidate = new CombatWorld(); candidate.Add(unit);
                if (!definition.VictoryObjective.HasRequiredUnits(candidate))
                    return summary + "\n승리 목표 불일치: 현재 단일 적 구성에 필요한 목표 개체가 없거나 목표 목록이 비어 있습니다. 직업 ID와 전투 개체 ID를 구분하세요.";
                summary += $"\n승리 목표: 맵 {definition.VictoryObjective.MapKey} / 개체 일치 (승리 달성 아님)";
            }
            else summary += "\n승리 목표: 별도 지정 없음";
            if (!definition.TryGetDefeatExperience(out var experience))
                return summary + "\n처치 경험치 오류: 카탈로그/직업 누락 또는 획득경 값이 uint32 범위 밖입니다.";
            summary += definition.UsesNativeBaseDefeatExperience
                ? $"\n싱글판 기본 처치 경험치: {experience} (단일 플레이어, 서버 보정/분배 미적용)"
                : "\n처치 경험치 자동 지급: 비활성";
            severity = MessageType.Info;
            return summary;
        }
    }
}
