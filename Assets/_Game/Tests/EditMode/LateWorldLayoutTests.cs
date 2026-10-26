using System;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class LateWorldLayoutTests
    {
        [Test]
        public void RecoveryDataPreservesKaosiaPositionAndDoesNotMutateSnapshot()
        {
            var data=JsonUtility.FromJson<WorldLayoutResumeData>(Resources.Load<TextAsset>("World/LateLayoutResume").text);
            Assert.AreEqual(3,data.maps.Length);Assert.IsTrue(data.maps.All(m=>m.IsValid));
            Assert.IsTrue(WorldLayoutMigration.TryResolve("Kaosia_Map",21,21,out var x,out var y));
            Assert.AreEqual(21,x);Assert.AreEqual(21,y);
            var saved=new NativeSaveData {mapScene="World1010_Map",worldX=23,worldY=34};
            string before=JsonUtility.ToJson(saved);
            Assert.IsTrue(WorldLayoutMigration.TryResolve(saved.mapScene,saved.worldX,saved.worldY,out x,out y));
            Assert.IsTrue(x!=23 || y!=34);
            Assert.AreEqual(before,JsonUtility.ToJson(saved));
            Assert.IsTrue(WorldLayoutMigration.TryResolve(saved.mapScene,x,y,out var x2,out var y2));
            Assert.AreEqual(x,x2);Assert.AreEqual(y,y2);
            Assert.IsFalse(WorldLayoutMigration.TryResolve(saved.mapScene,float.NaN,0,out _,out _));
        }
        [Test]
        public void RestoredKaosiaRecoversLateMapCoordinatesWithoutChangingSave()
        {
            Assert.IsTrue(TownCatalog.TryGet("Kaosia",out var town));
            Assert.AreEqual(43,town.Width);Assert.AreEqual(43,town.Height);
            var saved=new NativeSaveData {mapScene="Kaosia_Map",worldX=55.5f,worldY=54.5f};
            string before=JsonUtility.ToJson(saved);
            Assert.IsTrue(WorldLayoutMigration.TryResolve(saved.mapScene,saved.worldX,saved.worldY,out var x,out var y));
            Assert.That(x,Is.InRange(0f,42.999f));Assert.That(y,Is.InRange(0f,42.999f));
            Assert.AreEqual(before,JsonUtility.ToJson(saved));
            Assert.IsTrue(WorldLayoutMigration.TryResolve(saved.mapScene,x,y,out var x2,out var y2));
            Assert.AreEqual(x,x2);Assert.AreEqual(y,y2);
            Assert.IsFalse(WorldLayoutMigration.TryResolve(saved.mapScene,float.PositiveInfinity,54,out _,out _));
            Assert.IsFalse(WorldLayoutMigration.TryResolve(saved.mapScene,-1,54,out _,out _));
        }
        [TestCase("33")][TestCase("330")][TestCase("331")][TestCase("332")]
        [TestCase("333")][TestCase("404")][TestCase("405")][TestCase("406")]
        public void NewScenarioMapBuildsNativeGroundTerrainAndDimensions(string key)
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                Assert.IsTrue(ScenarioCatalog.TryGet(key,out var metadata));
                var root=new GameObject("late scenario map "+key);SceneManager.MoveGameObjectToScene(root,scene);
                Assert.IsTrue(root.AddComponent<NativeTownScene>().Configure("Scenario"+key,false));
                var layers=root.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true);
                Assert.IsTrue(layers.Any(t=>t.name=="Ground"));Assert.IsTrue(layers.Any(t=>t.name=="TerrainAttributes"));
                var definition=JsonUtility.FromJson<Scenario01Definition>(Resources.Load<TextAsset>("Scenarios/"+key+"/Content").text);
                Assert.IsTrue(definition.IsValid);Assert.AreEqual(metadata.Width,definition.width);Assert.AreEqual(metadata.Height,definition.height);
                var cell=new Vector3Int(definition.entryX,definition.height-1-definition.entryY,0);
                Assert.IsNotNull(layers.Single(t=>t.name=="Ground").GetTile(cell));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
