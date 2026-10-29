using System.IO;
using System.Linq;
using DarkSaver.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Tests
{
    public sealed class SavedNonScenarioMapTests
    {
        public static string[] SavedMapPaths=>AssetDatabase.GetAllAssetPaths()
            .Where(p=>p.StartsWith("Assets/_Game/Scenes/Maps/",System.StringComparison.Ordinal) && p.EndsWith(".unity",System.StringComparison.Ordinal)).OrderBy(p=>p).ToArray();

        [TestCaseSource(nameof(SavedMapPaths))]
        public void EveryMapSceneContainsSerializedTilesAndResolvableArtwork(string path)
        {
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var roots=scene.GetRootGameObjects();
                var maps=roots.SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)).ToArray();
                var ground=maps.Single(m=>m.name=="Ground");
                Assert.Greater(ground.GetUsedTilesCount(),0,path);
                var used=new TileBase[ground.GetUsedTilesCount()];ground.GetUsedTilesNonAlloc(used);
                foreach(var value in used)
                {
                    Assert.IsNotNull(value,path);
                    if(value is Tile tile)Assert.IsNotNull(tile.sprite,path+" / "+tile.name);
                }
                Assert.IsNotNull(maps.Single(m=>m.name=="TerrainAttributes").GetComponent<TilemapCollider2D>(),path);
                foreach(var body in roots.SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)))
                    if(body.name.StartsWith("Object_",System.StringComparison.Ordinal))Assert.IsNotNull(body.sprite,path+" / "+body.name);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }

        [Test]
        public void RuntimeAssetsContainNoRetiredMapGeometryFiles()
        {
            var files=Directory.EnumerateFiles(UnityEngine.Application.dataPath,"*",SearchOption.AllDirectories)
                .Where(p=>string.Equals(Path.GetFileName(p),"Map.json",System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileName(p),"Map.json.meta",System.StringComparison.OrdinalIgnoreCase));
            Assert.IsEmpty(files,"Geometry must be saved in scenes, not recreated in runtime resources.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnteringSavedMapKeepsPaintedCellsAndDoesNotGenerateAnotherGrid(bool shop)
        {
            var scene=EditorSceneManager.NewPreviewScene();var tile=ScriptableObject.CreateInstance<Tile>();
            GameObject Create(string name,Transform parent=null)
            {
                var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);
                if(parent!=null)go.transform.SetParent(parent,false);return go;
            }
            try
            {
                var root=Create("saved map");var grid=Create("Map Grid",root.transform);grid.AddComponent<Grid>();
                Tilemap Layer(string name)
                {
                    var go=Create(name,grid.transform);var map=go.AddComponent<Tilemap>();go.AddComponent<TilemapRenderer>();return map;
                }
                var ground=Layer("Ground");Layer("Detail");var terrain=Layer("TerrainAttributes");
                ground.SetTile(new Vector3Int(4,5,0),tile);terrain.SetTile(new Vector3Int(6,7,0),tile);
                ground.origin=Vector3Int.zero;ground.size=new Vector3Int(shop?20:43,shop?15:43,1);
                var decoration=Create("authored decoration",grid.transform);decoration.transform.localPosition=new Vector3(3,2,0);
                Component loader=shop ? root.AddComponent<ShopInteriorScene>() : root.AddComponent<NativeTownScene>();
                var settings=new SerializedObject(loader);
                settings.FindProperty(shop?"shopKey":"townKey").stringValue=shop?"KaosiaWeaponShop":"Kaosia";
                settings.FindProperty("runtimeOnly").boolValue=false;settings.FindProperty("mapTilesBaked").boolValue=true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                bool Build()=>shop ? ((ShopInteriorScene)loader).Build() : ((NativeTownScene)loader).Build();
                Assert.IsTrue(Build());Assert.IsTrue(Build());
                Assert.AreEqual(1,root.GetComponentsInChildren<Grid>().Length);
                Assert.AreEqual(3,root.GetComponentsInChildren<Tilemap>().Length);
                Assert.AreSame(tile,ground.GetTile(new Vector3Int(4,5,0)));
                Assert.AreSame(tile,terrain.GetTile(new Vector3Int(6,7,0)));
                Assert.IsTrue(ground.GetComponent<TilemapRenderer>().enabled);
                Assert.AreEqual(new Vector3(3,2,0),decoration.transform.localPosition);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);Object.DestroyImmediate(tile);}
        }
    }
}
