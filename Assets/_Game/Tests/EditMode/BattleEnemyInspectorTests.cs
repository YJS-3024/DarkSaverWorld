using System;
using DarkSaver.Combat;
using DarkSaver.Editor;
using DarkSaver.Jobs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DarkSaver.Tests
{
    public sealed class BattleEnemyInspectorTests
    {
        private const string EnemyPath = "Assets/_Game/Runtime/Combat/Brugos01Kisra.asset";

        [Test]
        public void KisraPreviewSeparatesIdentityAndAppliedStats()
        {
            var enemy = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>(EnemyPath);
            var text = BattleEnemyDefinitionInspector.GetPreview(enemy, out var severity);
            Assert.That(severity, Is.EqualTo(MessageType.Info));
            Assert.That(text, Does.Contain("전투 개체 ID: 700 / 직업 ID: 701"));
            Assert.That(text, Does.Contain("그림 ID: 701"));
            Assert.That(text, Does.Contain("HP 450 / MP 150 / 공격 126 / 방어 55"));
            Assert.That(text, Does.Contain("맵 01 / 개체 일치 (승리 달성 아님)"));
        }

        [Test]
        public void UnitySelectsCustomInspector()
        {
            var inspector = UnityEditor.Editor.CreateEditor(AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>(EnemyPath));
            try { Assert.That(inspector, Is.TypeOf<BattleEnemyDefinitionInspector>()); }
            finally { Object.DestroyImmediate(inspector); }
        }

        [Test]
        public void PreviewDoesNotModifyNativeAssets()
        {
            var enemy = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>(EnemyPath);
            var catalog = new SerializedObject(enemy).FindProperty("jobCatalog").objectReferenceValue;
            var objective = enemy.VictoryObjective;
            foreach (var asset in new Object[] { enemy, catalog, objective })
            {
                var json = EditorJsonUtility.ToJson(asset); var dirty = EditorUtility.IsDirty(asset);
                BattleEnemyDefinitionInspector.GetPreview(enemy, out _);
                Assert.That(EditorJsonUtility.ToJson(asset), Is.EqualTo(json));
                Assert.That(EditorUtility.IsDirty(asset), Is.EqualTo(dirty));
            }
        }

        [Test]
        public void ExplicitModeIgnoresMissingCatalog()
        {
            Check((enemy, catalog, objective) =>
            {
                var so = new SerializedObject(enemy);
                so.FindProperty("useJobCatalogBaseStats").boolValue = false;
                so.FindProperty("jobCatalog").objectReferenceValue = null;
                so.FindProperty("baseHp").intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
                var text = BattleEnemyDefinitionInspector.GetPreview(enemy, out var severity);
                Assert.That(severity, Is.EqualTo(MessageType.Info));
                Assert.That(text, Does.Contain("수치 출처: 직접 입력"));
                Assert.That(text, Does.Contain("HP 1 / MP 150"));
                Assert.That(text, Does.Not.Contain("네이티브 카탈로그"));
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingSourceReportsAnErrorWithoutFallback(bool missingJob)
        {
            Check((enemy, catalog, objective) =>
            {
                var so = new SerializedObject(enemy);
                if (missingJob) so.FindProperty("jobId").intValue = 702;
                else so.FindProperty("jobCatalog").objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
                var text = BattleEnemyDefinitionInspector.GetPreview(enemy, out var severity);
                Assert.That(severity, Is.EqualTo(MessageType.Error));
                Assert.That(text, Does.Contain(missingJob ? "직업 ID 702이 없습니다" : "카탈로그가 없습니다"));
                Assert.That(text, Does.Contain("대체하지 않습니다"));
            });
        }

        [TestCase(2, 0L)]
        [TestCase(2, -1L)]
        [TestCase(2, 2147483648L)]
        [TestCase(4, -1L)]
        [TestCase(6, -1L)]
        [TestCase(8, -1L)]
        public void InvalidSourceReportsRawValues(int column, long value)
        {
            Check((enemy, catalog, objective) =>
            {
                var values = Values(); values[column] = value;
                catalog.Initialize(new[] { Entry(values) });
                var text = BattleEnemyDefinitionInspector.GetPreview(enemy, out var severity);
                Assert.That(severity, Is.EqualTo(MessageType.Error));
                Assert.That(text, Does.Contain($"({column}): {value}"));
                Assert.That(text, Does.Contain("기본 수치 오류"));
                Assert.That(catalog.GetAt(0).GetNumericValue(column), Is.EqualTo(value));
            });
        }

        [Test]
        public void ObjectiveUsesInstanceIdNotJobId()
        {
            Check((enemy, catalog, objective) =>
            {
                var so = new SerializedObject(enemy); so.FindProperty("instanceId").intValue = 701;
                so.ApplyModifiedPropertiesWithoutUndo();
                var text = BattleEnemyDefinitionInspector.GetPreview(enemy, out var severity);
                Assert.That(severity, Is.EqualTo(MessageType.Error));
                Assert.That(text, Does.Contain("승리 목표 불일치"));
            });
        }

        [Test]
        public void EmptyObjectiveDoesNotLookLikeElimination()
        {
            Check((enemy, catalog, objective) =>
            {
                var so = new SerializedObject(objective); so.FindProperty("targetInstanceIds").arraySize = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                var text = BattleEnemyDefinitionInspector.GetPreview(enemy, out var severity);
                Assert.That(severity, Is.EqualTo(MessageType.Error));
                Assert.That(text, Does.Contain("목표 목록이 비어"));
            });
        }

        [Test]
        public void NoObjectiveIsReportedSeparately()
        {
            Check((enemy, catalog, objective) =>
            {
                var so = new SerializedObject(enemy); so.FindProperty("victoryObjective").objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
                var text = BattleEnemyDefinitionInspector.GetPreview(enemy, out var severity);
                Assert.That(severity, Is.EqualTo(MessageType.Info));
                Assert.That(text, Does.Contain("승리 목표: 별도 지정 없음"));
            });
        }

        [Test]
        public void SharedSpriteAndZeroStatsRemainUnchanged()
        {
            Check((enemy, catalog, objective) =>
            {
                var values = Values(); values[4] = values[6] = values[8] = 0;
                catalog.Initialize(new[] { Entry(values) });
                var text = BattleEnemyDefinitionInspector.GetPreview(enemy, out var severity);
                Assert.That(severity, Is.EqualTo(MessageType.Info));
                Assert.That(text, Does.Contain("직업 ID 701"));
                Assert.That(text, Does.Contain("그림 ID: 713"));
                Assert.That(text, Does.Contain("원문 이름: Test_Name"));
                Assert.That(text, Does.Contain("HP 345 / MP 0 / 공격 0 / 방어 0"));
                Assert.That(catalog.GetAt(0).SpriteId, Is.EqualTo(713));
            });
        }

        [Test]
        public void MissingDefinitionIsReported()
        {
            Assert.That(BattleEnemyDefinitionInspector.GetPreview(null, out var severity), Does.Contain("에셋"));
            Assert.That(severity, Is.EqualTo(MessageType.Error));
        }

        private static long[] Values()
        {
            var values = new long[JobCatalogEntry.NumericColumnCount];
            values[0] = 701; values[1] = 713; values[2] = 345;
            values[4] = 12; values[6] = 44; values[8] = 7;
            return values;
        }

        private static JobCatalogEntry Entry(long[] values) => new JobCatalogEntry(values, "-1", "Test_Name", "", "monster");

        private static void Check(Action<BattleEnemyDefinition, JobCatalog, BattleObjectiveDefinition> check)
        {
            var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>(EnemyPath));
            var catalog = ScriptableObject.CreateInstance<JobCatalog>();
            var objective = Object.Instantiate(enemy.VictoryObjective);
            try
            {
                catalog.Initialize(new[] { Entry(Values()) });
                var so = new SerializedObject(enemy);
                so.FindProperty("useJobCatalogBaseStats").boolValue = true;
                so.FindProperty("jobCatalog").objectReferenceValue = catalog;
                so.FindProperty("victoryObjective").objectReferenceValue = objective;
                so.ApplyModifiedPropertiesWithoutUndo();
                check(enemy, catalog, objective);
            }
            finally { Object.DestroyImmediate(enemy); Object.DestroyImmediate(catalog); Object.DestroyImmediate(objective); }
        }
    }
}
