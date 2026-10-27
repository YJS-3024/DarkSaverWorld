using System;
using System.Reflection;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class OriginalTutorialRangeTests
    {
        [Test] public void SharedSourceRangeChecks()=>OriginalTutorialRangeChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
        [TestCase("Move")] [TestCase("Attack")] [TestCase("Magic")] [TestCase("Target")]
        public void RangeArtworkUsesNativeCheckerAndClearTileEdges(string name)
        {
            var texture=Resources.Load<Texture2D>("Tutorial/Ranges/"+name);Assert.IsNotNull(texture);
            Assert.AreEqual(32,texture.width);Assert.AreEqual(32,texture.height);Assert.AreEqual(FilterMode.Point,texture.filterMode);
        }
        [Test]
        public void RuntimeRendererUsesNativeMasksAndTerrainWithoutMutatingPracticeState()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var tile=ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            System.Collections.Generic.List<SpriteRenderer> bodies=null;FirstPlayTutorial owned=null;
            try
            {
                GameObject Create(string name){var obj=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,scene);return obj;}
                void Set(object owner,string field,object value)=>owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);
                var go=Create("range rendering fixture");var tutor=go.AddComponent<FirstPlayTutorial>();owned=tutor;
                var session=go.AddComponent<DarkSaver.Combat.BattleTransitionController>();
                // Configure has the same required player contract as the real tutorial scene.
                // Preview-scene fixtures own this player; no live account/save state is used.
                var player=Create("range player fixture").AddComponent<DarkSaver.Player.TopDownPlayerController>();
                var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<DarkSaver.Magic.MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                Assert.IsNotNull(catalog,"Original magic catalog must be present before renderer assertions.");
                Assert.IsTrue(catalog.TryGet(101,out var fireball),"Tutorial fireball101 must exist.");
                Assert.AreEqual(3,fireball.Range,"Expected native fireball range; not a fixture fallback.");
                var magic=go.AddComponent<MagicCatalogPanel>();magic.Configure(catalog,player);
                Assert.AreSame(catalog,magic.Catalog);
                var grid=Create("range grid").AddComponent<Grid>();
                var ground=Create("Ground").AddComponent<UnityEngine.Tilemaps.Tilemap>();ground.transform.SetParent(grid.transform,false);
                var floor=ground.gameObject.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();floor.sortingOrder=-100;
                var terrain=Create("TerrainAttributes").AddComponent<UnityEngine.Tilemaps.Tilemap>();terrain.transform.SetParent(grid.transform,false);
                for(int x=0;x<20;x++)for(int y=0;y<15;y++)ground.SetTile(new Vector3Int(x,y),tile);
                var state=new NativeTutorialProgress {step=29,actors=new[]{new TutorialActor{id=0,job=100,x=4,y=7,hp=95},new TutorialActor{id=400,job=301,x=5,y=7},new TutorialActor{id=410,job=301,x=6,y=7}}};
                Set(tutor,"progress",state);Set(tutor,"session",session);Set(tutor,"rangeGround",ground);Set(tutor,"rangeTerrain",terrain);
                bodies=(System.Collections.Generic.List<SpriteRenderer>)typeof(FirstPlayTutorial).GetField("rangeBodies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tutor);
                void Draw()=>typeof(FirstPlayTutorial).GetMethod("DrawRanges",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tutor,null);
                Set(tutor,"movementArmed",true);Draw();Assert.Greater(tutor.RangeCellCount,5);
                Assert.IsTrue(bodies.FindAll(b=>b.enabled).TrueForAll(b=>b.sprite.texture.name=="Move" && b.sortingOrder==-99 && b.GetComponent<Collider2D>()==null));
                Assert.IsFalse(bodies.Exists(b=>b.enabled && b.transform.position==new Vector3(5.5f,7.5f,0))); // Enemy occupancy blocks movement cell.
                Set(tutor,"movementArmed",false);Set(tutor,"attackArmed",true);Draw();
                Assert.AreEqual(5,tutor.RangeCellCount);Assert.AreEqual(1,bodies.FindAll(b=>b.enabled && b.sprite.texture.name=="Target").Count);
                Set(tutor,"attackArmed",false);Set(tutor,"magicArmed",true);Draw();
                Assert.AreEqual(25,tutor.RangeCellCount);Assert.AreEqual(2,bodies.FindAll(b=>b.enabled && b.sprite.texture.name=="Target").Count);
                Set(tutor,"magicArmed",false);Draw();Assert.AreEqual(0,tutor.RangeCellCount);Assert.IsTrue(bodies.TrueForAll(b=>!b.enabled));
                Assert.AreEqual(29,state.step);Assert.AreEqual(95,state.actors[0].hp);Assert.AreEqual(4,state.actors[0].x);
            }
            finally
            {
                if(owned!=null)
                {
                    var sprites=(System.Collections.Generic.Dictionary<string,Sprite>)typeof(FirstPlayTutorial).GetField("rangeSprites",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owned);
                    foreach(var sprite in sprites.Values)UnityEngine.Object.DestroyImmediate(sprite);sprites.Clear();
                }
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(tile);
            }
        }
        [Test]
        public void RangePoolsHideOnClosedMenuAndCannotBlockCanvasInput()
        {
            var go=new GameObject("tutorial range lifecycle fixture");
            try
            {
                var tutorial=go.AddComponent<FirstPlayTutorial>();
                typeof(FirstPlayTutorial).GetField("progress",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(tutorial,new NativeTutorialProgress());
                var node=new GameObject("retained range",typeof(SpriteRenderer));node.transform.SetParent(go.transform);
                var bodies=(System.Collections.Generic.List<SpriteRenderer>)typeof(FirstPlayTutorial).GetField("rangeBodies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tutorial);
                bodies.Add(node.GetComponent<SpriteRenderer>());
                typeof(FirstPlayTutorial).GetMethod("DrawRanges",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tutorial,null);
                Assert.IsFalse(bodies[0].enabled);Assert.AreEqual(0,tutorial.RangeCellCount);
                Assert.IsNull(node.GetComponent<Collider2D>());Assert.IsNull(node.GetComponent<UnityEngine.UI.Graphic>());
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
