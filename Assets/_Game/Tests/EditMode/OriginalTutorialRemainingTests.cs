using System;
using System.Collections.Generic;
using System.Reflection;
using DarkSaver.Core;
using DarkSaver.Combat;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
namespace DarkSaver.Tests
{
    public sealed class OriginalTutorialRemainingTests
    {
        private static string Source=>Resources.Load<TextAsset>("Tutorial/TUTORIAL").text;
        [Test] public void ClockAndNativeWindowActionsPreserveAllOriginalWaits()=>OriginalTutorialWindowChecks.Run((ok,name)=>Assert.IsTrue(ok,name),Source);
        [Test] public void NativePathAndTerrainChecks()=>OriginalTutorialMovementChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
        [Test] public void AccountEntryPreservesExistingAndDamagedSaves()=>NativeEntryRouteChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
        [Test] public void NativeStatusBodyKeepsCellAspectAndPadding()
        {
            var go=new GameObject("native body aspect fixture");var texture=new Texture2D(32,32);
            var sprite=Sprite.Create(texture,new Rect(0,0,16,32),new Vector2(.5f,.5f),32);
            try
            {
                var owner=go.AddComponent<StartMenuController>();InGameUi ui=null;InGameUi.Ensure(ref ui,owner);
                ui.Begin();ui.NativeUnitPicture("body",new Rect(20,30,32,32),sprite);ui.End();
                var image=ui.Find<RawImage>("body");
                Assert.AreEqual(new Vector2(16,32),image.rectTransform.sizeDelta);
                Assert.AreEqual(new Rect(0,0,.5f,1),image.uvRect);
                Assert.AreEqual(28,image.rectTransform.anchoredPosition.x);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
        }
        [Test] public void AutomaticDefenseWaitReopensCommanderMenuWithoutResendingStageZero()
        {
            var go=new GameObject("automatic defense fixture");
            try
            {
                var tutorial=go.AddComponent<FirstPlayTutorial>();
                var script=new DarkSaver.Events.OriginalTutorialScript(Source,100);
                var commands=DarkSaver.Events.EventScriptParser.Parse(Source);int step=-1;
                for(int i=0;i<commands.Count;i++)if(commands[i].Name=="WAITORDERMENU" && commands[i].Arguments[0]=="2"){step=i;break;}
                Assert.GreaterOrEqual(step,0);
                var state=new NativeTutorialProgress{step=step,mouseMenu=true};Set(tutorial,"script",script);Set(tutorial,"progress",state);
                Call(tutorial,"ChooseAction",6,0,step);
                Assert.AreEqual("Order",typeof(FirstPlayTutorial).GetField("subMenu",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tutorial));
                Assert.AreEqual(step,state.step,"reopening must not send another stage0 response");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Serializable] private sealed class TerrainFixture {public int width,height;public int[] attributes;}
        [Test] public void AllLiteralMovesUseConverted301TerrainFixture()
        {
            var source=UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Tests/EditMode/OriginalTutorial301Terrain.json");
            Assert.IsNotNull(source);var data=JsonUtility.FromJson<TerrainFixture>(source.text);
            Assert.AreEqual(data.width*data.height,data.attributes.Length);
            OriginalTutorialMapRouteChecks.Run((ok,name)=>Assert.IsTrue(ok,name),Source,data.width,data.height,(x,y)=>data.attributes[y*data.width+x]);
        }
        private static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);
        private static object Call(object owner,string name,params object[] args)=>owner.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(owner,args);
        [Test]
        public void F5UsesCommanderOnlyAndLiveApHpMpWhileF12HasNativeEnabledButtons()
        {
            var go=new GameObject("tutorial native windows fixture");
            try
            {
                var tutorial=go.AddComponent<FirstPlayTutorial>();var session=go.AddComponent<BattleTransitionController>();
                Set(session,"character",new NativeCharacterProfile{name="테스트",jobId=100});Set(tutorial,"session",session);
                InGameUi ui=null;InGameUi.Ensure(ref ui,tutorial);Set(tutorial,"ui",ui);
                var actor=new GameObject("commander fixture",typeof(SpriteRenderer));actor.transform.SetParent(go.transform,false);
                var actors=(Dictionary<int,GameObject>)typeof(FirstPlayTutorial).GetField("actors",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tutorial);actors.Add(0,actor);
                var state=new NativeTutorialProgress{step=29,actors=new[]{new TutorialActor{id=0,job=100,hp=95,maxHp=200,mp=45,maxMp=45,tick=5},new TutorialActor{id=1,job=201},new TutorialActor{id=2,job=201}},dialogs=new[]{"F5","F12"}};
                Set(tutorial,"progress",state);string before=JsonUtility.ToJson(state);
                for(int frame=0;frame<3;frame++)
                {
                    ui.Begin();Call(tutorial,"DrawDialogs");ui.End();
                    Assert.AreEqual(12f,((RectTransform)ui.Root.Find("roster-0-ap-clip")).sizeDelta.x);
                    Assert.AreEqual(22f,((RectTransform)ui.Root.Find("roster-0-hp-clip")).sizeDelta.x);
                    Assert.AreEqual(48f,((RectTransform)ui.Root.Find("roster-0-mp-clip")).sizeDelta.x);
                    Assert.IsNull(ui.Root.Find("roster-1-unit"));Assert.IsNull(ui.Root.Find("roster-2-unit"));
                    for(int i=0;i<9;i++)Assert.IsTrue(ui.Find<Button>("window-F12-control-"+i).interactable,"Native control"+i);
                    Assert.IsTrue(ui.Find<Button>("roster-close").interactable);Assert.IsTrue(ui.Find<Button>("roster-minimize").interactable);
                }
                Call(tutorial,"AdvancePracticeTicks",.5f);ui.Begin();Call(tutorial,"DrawDialogs");ui.End();
                Assert.AreEqual(14f,((RectTransform)ui.Root.Find("roster-0-ap-clip")).sizeDelta.x,"display ticks advance by one per half-second");
                ui.Find<Button>("roster-minimize").onClick.Invoke();ui.Begin();Call(tutorial,"DrawDialogs");ui.End();
                Assert.AreEqual(new Vector2(33,22),((RectTransform)ui.Root.Find("roster-collapsed")).sizeDelta);
                Assert.IsFalse(ui.Root.Find("roster-0-ap-clip").gameObject.activeSelf);
                ui.Find<Button>("roster-restore").onClick.Invoke();ui.Begin();Call(tutorial,"DrawDialogs");ui.End();
                Assert.IsTrue(ui.Root.Find("roster-0-ap-clip").gameObject.activeSelf);
                Assert.AreEqual(before,JsonUtility.ToJson(state));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test]
        public void F8ReadsPointerTerrainInsteadOfActorPositionAndLeavesStateUnchanged()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var floorTile=ScriptableObject.CreateInstance<Tile>();var attribute=ScriptableObject.CreateInstance<Tile>();attribute.name="Terrain_4";
            try
            {
                GameObject Create(string name){var go=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);return go;}
                var tutorial=Create("terrain fixture").AddComponent<FirstPlayTutorial>();
                var grid=Create("grid").AddComponent<Grid>();var ground=Create("Ground").AddComponent<Tilemap>();ground.transform.SetParent(grid.transform,false);
                var terrain=Create("TerrainAttributes").AddComponent<Tilemap>();terrain.transform.SetParent(grid.transform,false);
                ground.SetTile(new Vector3Int(0,0,0),floorTile);ground.SetTile(new Vector3Int(1,0,0),floorTile);terrain.SetTile(new Vector3Int(1,0,0),attribute);
                Set(tutorial,"rangeGround",ground);Set(tutorial,"rangeTerrain",terrain);
                var state=new NativeTutorialProgress{step=29,actors=new[]{new TutorialActor{id=0,job=101,x=0,y=0,hp=95}}};Set(tutorial,"progress",state);
                Set(tutorial,"terrainPointer",new CombatGridCell(1,0));var args=new object[]{null,0,0,0};string before=JsonUtility.ToJson(state);
                Assert.IsTrue((bool)Call(tutorial,"TryGetPracticeTerrain",args));Assert.AreEqual(25,args[1]);Assert.AreEqual(25,args[2]);Assert.AreEqual(2,args[3]);
                // Tutorial knight101 uses troop0 even though its permanent job has cavalry column2.
                Set(tutorial,"terrainPointer",new CombatGridCell(3,0));Assert.IsFalse((bool)Call(tutorial,"TryGetPracticeTerrain",new object[]{null,0,0,0}));
                Assert.AreEqual(before,JsonUtility.ToJson(state));
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(floorTile);UnityEngine.Object.DestroyImmediate(attribute);}
        }
    }
}
