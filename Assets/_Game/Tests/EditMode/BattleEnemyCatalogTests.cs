using System;
using DarkSaver.Combat;
using DarkSaver.Jobs;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DarkSaver.Tests
{
    public sealed class BattleEnemyCatalogTests
    {
        private const string EnemyPath = "Assets/_Game/Runtime/Combat/Brugos01Kisra.asset";

        [Test]
        public void KisraUsesNativeCatalogWithoutMutatingAssets()
        {
            var enemy = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>(EnemyPath);
            var catalog = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/MonsterJobCatalog.asset");
            var enemyJson = EditorJsonUtility.ToJson(enemy);
            var catalogJson = EditorJsonUtility.ToJson(catalog);
            var enemyDirty = EditorUtility.IsDirty(enemy);
            var catalogDirty = EditorUtility.IsDirty(catalog);
            Assert.That(enemy.UsesJobCatalogBaseStats, Is.True);
            var source = new SerializedObject(enemy).FindProperty("jobCatalog").objectReferenceValue;
            Assert.That(source, Is.SameAs(catalog));
            var first = enemy.CreateUnit();
            Assert.That((first.Id, first.X, first.Y, first.Name), Is.EqualTo(((short)700, 19, 10, "키스라")));
            Assert.That((first.Hp, first.Mp, first.Attack, first.Defense), Is.EqualTo((300, 150, 126, 55)));
            first.Hp = 0;
            Assert.That(enemy.CreateUnit().Hp, Is.EqualTo(300));
            Assert.That(EditorJsonUtility.ToJson(enemy), Is.EqualTo(enemyJson));
            Assert.That(EditorJsonUtility.ToJson(catalog), Is.EqualTo(catalogJson));
            Assert.That(EditorUtility.IsDirty(enemy), Is.EqualTo(enemyDirty));
            Assert.That(EditorUtility.IsDirty(catalog), Is.EqualTo(catalogDirty));
        }

        [Test]
        public void CatalogSuppliesOnlyFourBaseStats()
        {
            Check((enemy, catalog) =>
            {
                var values = Values();
                values[3] = 9999; values[5] = 9999; values[7] = 9999; values[9] = 9999;
                values[31] = 480; values[32] = 240;
                catalog.Initialize(new[] { new JobCatalogEntry(values, "-1", "Other source name", "", "npc_or_special") });
                Assert.That(enemy.TryCreateUnit(out var unit), Is.True);
                Assert.That((unit.Hp, unit.MaxHp, unit.Mp, unit.MaxMp, unit.Attack, unit.Defense),
                    Is.EqualTo((345, 345, 12, 12, 44, 7)));
                Assert.That(unit.Name, Is.EqualTo("키스라"));
                Assert.That(unit.Id, Is.EqualTo(700));
                Assert.That(unit.Enemy, Is.True);
                Assert.That(enemy.VictoryObjective.MapKey, Is.EqualTo("01"));
            });
        }

        [Test]
        public void ExplicitModeKeepsDesignerValues()
        {
            Check((enemy, catalog) =>
            {
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("useJobCatalogBaseStats").boolValue = false;
                serialized.FindProperty("baseHp").intValue = 1;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(enemy.TryCreateUnit(out var unit), Is.True);
                Assert.That((unit.Hp, unit.Mp, unit.Attack, unit.Defense), Is.EqualTo((1, 150, 126, 55)));
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingConfiguredSourceDoesNotFallBack(bool missingJob)
        {
            Check((enemy, catalog) =>
            {
                var serialized = new SerializedObject(enemy);
                if (missingJob) serialized.FindProperty("jobId").intValue = 702;
                else serialized.FindProperty("jobCatalog").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(enemy.TryCreateUnit(out var unit), Is.False);
                Assert.That(unit, Is.Null);
                Assert.Throws<InvalidOperationException>(() => enemy.CreateUnit());
            });
        }

        [TestCase(2, 0L)]
        [TestCase(2, -1L)]
        [TestCase(2, 2147483648L)]
        [TestCase(4, -1L)]
        [TestCase(4, 2147483648L)]
        [TestCase(6, -1L)]
        [TestCase(6, 2147483648L)]
        [TestCase(8, -1L)]
        [TestCase(8, 2147483648L)]
        public void InvalidCatalogStatIsRejected(int column, long value)
        {
            Check((enemy, catalog) =>
            {
                var values = Values(); values[column] = value;
                catalog.Initialize(new[] { Entry(values) });
                Assert.That(enemy.TryCreateUnit(out var unit), Is.False);
                Assert.That(unit, Is.Null);
                Assert.That(catalog.GetAt(0).GetNumericValue(column), Is.EqualTo(value));
            });
        }

        [Test]
        public void ZeroMpAttackAndDefenseAreAllowed()
        {
            Check((enemy, catalog) =>
            {
                var values = Values(); values[4] = values[6] = values[8] = 0;
                catalog.Initialize(new[] { Entry(values) });
                Assert.That(enemy.TryCreateUnit(out var unit), Is.True);
                Assert.That((unit.Mp, unit.Attack, unit.Defense), Is.EqualTo((0, 0, 0)));
            });
        }

        [Test]
        public void IntUpperBoundaryIsPreserved()
        {
            Check((enemy, catalog) =>
            {
                var values = Values(); values[2] = values[4] = values[6] = values[8] = int.MaxValue;
                catalog.Initialize(new[] { Entry(values) });
                Assert.That(enemy.TryCreateUnit(out var unit), Is.True);
                Assert.That((unit.Hp, unit.Mp, unit.Attack, unit.Defense),
                    Is.EqualTo((int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)));
            });
        }

        [TestCase("baseHp", 0)]
        [TestCase("baseMp", -1)]
        [TestCase("baseAttack", -1)]
        [TestCase("baseDefense", -1)]
        public void InvalidExplicitStatIsRejected(string property, int value)
        {
            Check((enemy, catalog) =>
            {
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("useJobCatalogBaseStats").boolValue = false;
                serialized.FindProperty(property).intValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(enemy.TryCreateUnit(out var unit), Is.False);
                Assert.That(unit, Is.Null);
            });
        }

        [Test]
        public void ExistingBattleRootReceivesCatalogHp()
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                Check((enemy, catalog) =>
                {
                    var go = new GameObject("Native enemy test"); SceneManager.MoveGameObjectToScene(go, preview);
                    var battle = go.AddComponent<BattleSceneRoot>();
                    Assert.That(battle.ConfigureEnemy(enemy), Is.True);
                    Assert.That(battle.EnemyHp, Is.EqualTo(345));
                    Assert.That(battle.ConfigureObjective(enemy.VictoryObjective), Is.True);
                    Assert.That(battle.PlayerExperience, Is.Zero);
                });
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.EqualTo(dirty));
        }

        private static long[] Values()
        {
            var values = new long[JobCatalogEntry.NumericColumnCount];
            values[0] = 701; values[1] = 713;
            values[2] = 345; values[4] = 12; values[6] = 44; values[8] = 7;
            return values;
        }

        private static JobCatalogEntry Entry(long[] values) => new JobCatalogEntry(values, "-1", "Test", "", "monster");

        private static void Check(Action<BattleEnemyDefinition, JobCatalog> check)
        {
            var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>(EnemyPath));
            var catalog = ScriptableObject.CreateInstance<JobCatalog>();
            try
            {
                catalog.Initialize(new[] { Entry(Values()) });
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("useJobCatalogBaseStats").boolValue = true;
                serialized.FindProperty("jobCatalog").objectReferenceValue = catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                check(enemy, catalog);
            }
            finally { Object.DestroyImmediate(enemy); Object.DestroyImmediate(catalog); }
        }
    }
}
