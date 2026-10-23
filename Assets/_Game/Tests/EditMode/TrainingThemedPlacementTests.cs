using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Jobs;
using DarkSaver.Player;
using DarkSaver.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DarkSaver.Tests
{
    public sealed class TrainingThemedPlacementTests
    {
        [TestCase(101,302)]
        [TestCase(102,301)]
        [TestCase(103,305)]
        [TestCase(104,310)]
        [TestCase(105,336)]
        [TestCase(106,308)]
        [TestCase(107,339)]
        public void ThemedEncounterUsesNativeStatsArtworkAndPassableCells(int id,int job)
        {
            var definition=AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>($"Assets/_Game/Runtime/Combat/Training/Training{id}_Enemy.asset");
            var monsters=AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/MonsterJobCatalog.asset");
            var jobs=AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
            Assert.NotNull(definition);Assert.AreEqual(job,definition.JobId);
            Assert.IsTrue(definition.UsesJobCatalogBaseStats);Assert.IsNull(definition.VictoryObjective);
            Assert.IsTrue(monsters.TryGet(job,out var entry));Assert.IsTrue(definition.TryCreateUnit(out var unit));
            Assert.AreEqual(entry.GetNumericValue(2),unit.Hp);Assert.AreEqual(entry.GetNumericValue(4),unit.Mp);
            Assert.AreEqual(entry.GetNumericValue(6),unit.Attack);Assert.AreEqual(entry.GetNumericValue(8),unit.Defense);
            var runtime=EditorSceneManager.OpenPreviewScene($"Assets/_Game/Scenes/Runtime/Training{id}_Runtime.unity");
            var map=EditorSceneManager.OpenPreviewScene($"Assets/_Game/Scenes/Maps/Map_{id}_;.unity");
            try
            {
                var encounters=runtime.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BattleEncounter2D>()).ToArray();
                Assert.AreEqual(1,encounters.Length);
                var encounter=encounters[0];
                Assert.AreSame(definition,new SerializedObject(encounter).FindProperty("enemyDefinition").objectReferenceValue);
                Assert.IsTrue(encounter.GetComponent<DirectionalSpriteAnimator>().HasCompleteDirections);
                var sprite=encounter.GetComponent<SpriteRenderer>().sprite;
                Assert.AreEqual(32,sprite.pixelsPerUnit);Assert.AreEqual(new Vector2(32,32),sprite.rect.size);
                Assert.AreEqual("Assets/_Game/Art/Converted/Monsters/Training/Monster"+entry.GetNumericValue(1)+".png",AssetDatabase.GetAssetPath(sprite));
                var spawn=runtime.GetRootGameObjects().Select(r=>r.GetComponent<SceneSpawnPoint>()).First(s=>s!=null);
                var tilemaps=map.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Tilemap>()).ToArray();
                var ground=tilemaps.Single(t=>t.name=="Ground");var terrain=tilemaps.Single(t=>t.name=="TerrainAttributes");
                CombatGridCell Cell(Vector3 position){var cell=ground.WorldToCell(position);return new CombatGridCell(cell.x,ground.cellBounds.yMax-1-cell.y);}
                Assert.AreEqual(unit.X,Cell(encounter.transform.position).X);Assert.AreEqual(unit.Y,Cell(encounter.transform.position).Y);
                Assert.IsTrue(BattleMapPresentation.TryGetPlacementMovement(ground,terrain,ground.cellBounds,new[]{jobs,monsters},100,Cell(spawn.transform.position),out var ally));
                Assert.IsTrue(BattleMapPresentation.TryGetPlacementMovement(ground,terrain,ground.cellBounds,new[]{jobs,monsters},job,Cell(encounter.transform.position),out var foe));
                Assert.AreNotEqual(CombatTerrainTraversal.ImpassableValue,ally);Assert.AreNotEqual(CombatTerrainTraversal.ImpassableValue,foe);
                Assert.IsTrue(EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==$"Assets/_Game/Scenes/Entry/Training{id}_Play.unity"));
            }
            finally { EditorSceneManager.ClosePreviewScene(runtime);EditorSceneManager.ClosePreviewScene(map); }
        }
    }
}
