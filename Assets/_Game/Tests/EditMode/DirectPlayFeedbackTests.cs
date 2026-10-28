using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.UI;
using DarkSaver.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Tests
{
    public sealed class DirectPlayFeedbackTests
    {
        [Test] public void NativeTutorialMinimapUsesOriginalCoordinatesAndMarkerIndices()
        {
            Assert.IsTrue(DarkSaver.Events.OriginalTutorialMinimap.TryPoint(2,6,20,15,out int x,out int y));
            Assert.AreEqual(10,x);Assert.AreEqual(40,y);
            Assert.IsTrue(DarkSaver.Events.OriginalTutorialMinimap.TryPoint(13,6,20,15,out x,out y));
            Assert.AreEqual(65,x);Assert.AreEqual(40,y);
            Assert.AreEqual(0,DarkSaver.Events.OriginalTutorialMinimap.Marker(0,true));
            Assert.AreEqual(3,DarkSaver.Events.OriginalTutorialMinimap.Marker(100,true));
            Assert.AreEqual(2,DarkSaver.Events.OriginalTutorialMinimap.Marker(100,false));
            Assert.AreEqual(4,DarkSaver.Events.OriginalTutorialMinimap.Marker(400,true));
            Assert.AreEqual(4,DarkSaver.Events.OriginalTutorialMinimap.Marker(410,false));
            Assert.AreEqual(8,DarkSaver.Events.OriginalTutorialMinimap.Marker(800,true));
            Assert.IsFalse(DarkSaver.Events.OriginalTutorialMinimap.TryPoint(20,0,20,15,out _,out _));
        }
        [Test] public void DoorAuthorityMatchesTwoTileTriggerAtBothEdges()
        {
            Assert.IsTrue(LocalDoorGeometry.Contains(9.7f,20.5f,10.5f,20.5f));
            Assert.IsTrue(LocalDoorGeometry.Contains(12.3f,20.5f,10.5f,20.5f));
            Assert.IsFalse(LocalDoorGeometry.Contains(9.5f,20.5f,10.5f,20.5f));
            Assert.IsFalse(LocalDoorGeometry.Contains(12.5f,20.5f,10.5f,20.5f));
            Assert.IsFalse(LocalDoorGeometry.Contains(11,21.5f,10.5f,20.5f));
            Assert.IsFalse(LocalDoorGeometry.Contains(float.NaN,20.5f,10.5f,20.5f));
        }
        [Test] public void CoarseMovementKeepsBudgetAcrossCellBordersAndStopsAtInteractionCell()
        {
            var straight=new[]{new Vector3(1.5f,.5f,0),new Vector3(2.5f,.5f,0)};
            Assert.IsTrue(WorldMovementStep.TryAdvance(straight,0,new Vector2(1.4f,.5f),.48f,out var point,out int next));
            Assert.That(point.x,Is.EqualTo(1.88f).Within(.0001f));Assert.AreEqual(1,next);
            var corner=new[]{new Vector3(1.5f,.5f,0),new Vector3(1.5f,1.5f,0)};
            Assert.IsTrue(WorldMovementStep.TryAdvance(corner,0,new Vector2(1.4f,.5f),.48f,out point,out next));
            Assert.That(point.x,Is.EqualTo(1.5f).Within(.0001f));Assert.That(point.y,Is.EqualTo(.88f).Within(.0001f));
            var interaction=new[]{new Vector3(1.5f,.5f,0)};
            Assert.IsTrue(WorldMovementStep.TryAdvance(interaction,0,new Vector2(1.4f,.5f),.48f,out point,out next));
            Assert.AreEqual(new Vector2(1.5f,.5f),point);Assert.AreEqual(1,next);
            Assert.IsFalse(WorldMovementStep.TryAdvance(straight,0,Vector2.zero,float.NaN,out _,out _));
        }
        [Test] public void AllianceCheckboxHoverDoesNotSelectAnotherImage()
        {
            var go=new GameObject("native checkbox fixture");var frames=new[]{new Texture2D(16,16),new Texture2D(16,16),new Texture2D(16,16)};
            try
            {
                var owner=go.AddComponent<FirstPlayTutorial>();InGameUi ui=null;InGameUi.Ensure(ref ui,owner);
                var control=new NativeBattleWindows.Control{nativeId=47,rect=new Rect(0,0,16,16),frames=frames};
                ui.Begin();ui.NativeCheckbox("check",new Vector2(10,10),control,false,()=>{},true,"");ui.End();
                var button=ui.Find<Button>("check");
                var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
                Assert.AreSame(frames[0],ui.Find<RawImage>("check").texture);
                ui.Begin();ui.NativeCheckbox("check",new Vector2(10,10),control,true,()=>{},true,"");ui.End();
                Assert.AreSame(frames[1],ui.Find<RawImage>("check").texture);
            }
            finally{Object.DestroyImmediate(go);foreach(var frame in frames)Object.DestroyImmediate(frame);}
        }
        [Test] public void BuildingsArriveNearDoorAndIncludeMageHouse()
        {
            var table=JsonUtility.FromJson<BuildingTravelRoutes>(Resources.Load<TextAsset>("World/BuildingRoutes").text);
            Assert.IsTrue(table.IsValid);Assert.AreEqual(18,table.routes.Length);
            foreach(var route in table.routes)if(route.fromKey=="Kaosia")
            {
                Assert.AreEqual(9.5f,route.x);Assert.AreEqual(1.5f,route.y);
                var reply=new BuildingTravelService(table).Enter(new Request_EnterMap(route.toKey,route.spawnId),
                    new BuildingTravelContext("Kaosia",route.doorX,route.doorY,true,false));
                Assert.IsTrue(reply.Accepted);Assert.AreEqual(route.x,reply.X);Assert.AreEqual(route.y,reply.Y);
            }
            Assert.IsNotNull(System.Array.Find(table.routes,r=>r.toKey=="Interior2800"));
            Assert.IsNotNull(System.Array.Find(table.routes,r=>r.toKey=="Interior2900"));
        }
        [Test] public void DoorsAreTwoUnitsWideAndShopCellsAreBlack()
        {
            var go=new GameObject("feedback UI fixture");
            try
            {
                go.AddComponent<BuildingDoor2D>().Configure("shop","entry");
                Assert.AreEqual(new Vector2(2,1),go.GetComponent<BoxCollider2D>().size);
                Assert.AreEqual(new Vector2(.5f,0),go.GetComponent<BoxCollider2D>().offset);
                var owner=go.AddComponent<FirstPlayTutorial>();InGameUi ui=null;InGameUi.Ensure(ref ui,owner);
                ui.Begin();ui.ItemSlotButton("item",new Rect(10,10,32,32),null,null,0,false,"");ui.End();
                Assert.AreEqual(Color.black,ui.Find<RawImage>("item").color);
                Assert.AreEqual(Selectable.Transition.None,ui.Find<Button>("item").transition);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void NpcInteractionWaitsForCellAndCanBeCancelledWithoutTeleport()
        {
            var go=new GameObject("grid interaction fixture");
            try
            {
                var player=go.AddComponent<TopDownPlayerController>();var body=go.GetComponent<Rigidbody2D>();
                body.position=new Vector2(4.7f,8.5f);bool invoked=false;
                Assert.IsTrue(player.QueueNpcInteraction(()=>invoked=true));Assert.IsFalse(invoked);
                Assert.AreEqual(new Vector2(4.7f,8.5f),body.position);
                Assert.IsFalse(player.QueueNpcInteraction(()=>{}));
                player.SetUiInputBlocked(true);Assert.IsFalse(invoked);player.SetUiInputBlocked(false);
                body.position=new Vector2(4.5f,8.5f);
                Assert.IsTrue(player.QueueNpcInteraction(()=>invoked=true));Assert.IsTrue(invoked);
                Assert.AreEqual(RigidbodyInterpolation2D.None,body.interpolation);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void TwoByTwoTreeRowsUseSeparateDepthWithoutMovingRoot()
        {
            var go=new GameObject("tree fixture");var texture=new Texture2D(64,64);Sprite sprite=null;
            try
            {
                sprite=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),32);
                var original=go.AddComponent<SpriteRenderer>();original.sprite=sprite;go.transform.position=new Vector3(4,8,0);
                go.AddComponent<MapTreeSlices>().Configure(original,3);
                Assert.IsFalse(original.enabled);Assert.AreEqual(new Vector3(4,8,0),go.transform.position);
                var bottom=go.transform.Find("Tree bottom row").GetComponent<SpriteRenderer>();
                var top=go.transform.Find("Tree canopy row").GetComponent<SpriteRenderer>();
                Assert.AreEqual(32,bottom.sprite.rect.height);Assert.AreEqual(32,top.sprite.rect.height);
                Assert.Less(bottom.sortingOrder,20000);Assert.Greater(top.sortingOrder,20000);
                Assert.AreEqual(7.5f,bottom.transform.position.y);Assert.AreEqual(8.5f,top.transform.position.y);
            }
            finally{Object.DestroyImmediate(go);if(sprite!=null)Object.DestroyImmediate(sprite);Object.DestroyImmediate(texture);}
        }
    }
}
