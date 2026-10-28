using DarkSaver.Combat;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
namespace DarkSaver.Tests
{
    public sealed class TrainingVisitTests
    {
        [Test] public void VisitAuthorityContracts()=>TrainingVisitChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
        [Test] public void SingleVisitorGridOffersSystemMenuWithoutInventedEnemy()
        {
            var scene=EditorSceneManager.NewPreviewScene();var tile=ScriptableObject.CreateInstance<Tile>();
            GameObject Create(string name){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
            try
            {
                var grid=Create("Grid").AddComponent<Grid>();var ground=Create("Ground").AddComponent<Tilemap>();ground.transform.SetParent(grid.transform);
                var terrain=Create("TerrainAttributes").AddComponent<Tilemap>();terrain.transform.SetParent(grid.transform);
                for(int y=0;y<15;y++)for(int x=0;x<20;x++)ground.SetTile(new Vector3Int(x,y,0),tile);
                var actor=Create("Existing player").AddComponent<SpriteRenderer>();
                actor.sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Down_1.png");
                actor.transform.position=ground.GetCellCenterWorld(new Vector3Int(10,7,0));
                var camera=Create("Camera").AddComponent<Camera>();var root=Create("Visit");var battle=root.AddComponent<BattleSceneRoot>();
                Assert.IsTrue(battle.ConfigureTrainingMap(101));Assert.IsTrue(battle.InitializeTrainingVisitor(100,"fixture",17,200,3,45,123));
                Assert.IsFalse(battle.TryGetUnit(800,out _));
                Assert.That(battle.GetBattleHudText(),Does.Contain("훈련장"));
                var view=root.AddComponent<BattleMapPresentation>();
                Assert.IsTrue(view.TryBind(ground,terrain,camera,new[]{new BattleMapVisual(100,actor)}));
                Assert.IsTrue(battle.TryGetUnit(100,out var visitor));Assert.AreEqual((10,7),(visitor.X,visitor.Y));
                Assert.AreEqual((17,200,3,45,123u),(visitor.Hp,visitor.MaxHp,visitor.Mp,visitor.MaxMp,visitor.Experience));
                Assert.IsTrue(view.TryOpenSystemMenu());Assert.IsTrue(view.IsSystemMenuOpen);
                view.CloseSystemMenu();view.SuspendPresentation();Assert.IsFalse(battle.MapPresentationActive);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);Object.DestroyImmediate(tile);}
        }
    }
}
