using System;
using System.Reflection;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Network;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace DarkSaver.Tests
{
    public sealed class NativeUiPaintOrderTests
    {
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        private static void Above(InGameUi ui,string foreground,string background)
        {
            var a=ui.Root.Find(foreground);var b=ui.Root.Find(background);
            Assert.IsNotNull(a,foreground);Assert.IsNotNull(b,background);
            Assert.AreSame(a.parent,b.parent);
            Assert.Greater(a.GetSiblingIndex(),b.GetSiblingIndex(),foreground+" must be painted after "+background);
            Assert.IsTrue(a.gameObject.activeInHierarchy);
        }
        [TestCase(true)] [TestCase(false)]
        public void TutorialPortraitAndGlyphsRemainAboveNativeBackgroundOnRepeatedFrames(bool waitingForClick)
        {
            var go=new GameObject("native tutorial paint regression");
            try
            {
                var tutorial=go.AddComponent<FirstPlayTutorial>();InGameUi ui=null;InGameUi.Ensure(ref ui,tutorial);
                Set(tutorial,"ui",ui);var script=new OriginalTutorialScript(Resources.Load<TextAsset>("Tutorial/TUTORIAL").text,100);
                var progress=new NativeTutorialProgress();script.Prepare(progress);
                if(!waitingForClick)
                    while(script.Current(progress).Name=="WAITMOUSEBUTTON")Assert.IsTrue(script.Input(progress,"Click"));
                Set(tutorial,"progress",progress);
                var face=Resources.Load<Texture2D>("UI/CharacterPortraits/125");Assert.IsNotNull(face);
                for(int frame=0;frame<4;frame++)
                {
                    ui.Begin();Call(tutorial,"DrawDialogue",script.Current(progress));ui.End();Canvas.ForceUpdateCanvases();
                    Assert.AreSame(face,ui.Find<RawImage>("portrait").texture);
                    Above(ui,"portrait","dialogue");Above(ui,"dialogue-line-0","dialogue");
                    if(waitingForClick)
                    {
                        Above(ui,"dialogue-ack","portrait");
                        Assert.AreEqual(0,ui.Find<RawImage>("dialogue-ack").color.a);
                        Assert.IsTrue(ui.Find<Button>("dialogue-ack").interactable);
                    }
                }
                ui.Hide();ui.Begin();Call(tutorial,"DrawDialogue",script.Current(progress));ui.End();Above(ui,"portrait","dialogue");
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test]
        public void TutorialF2DrawsLiteralTickSequenceWithoutMutatingPracticeOrAddingNumberLabels()
        {
            var go=new GameObject("tutorial literal AP gauge regression");
            try
            {
                var tutorial=go.AddComponent<FirstPlayTutorial>();
                var session=go.AddComponent<DarkSaver.Combat.BattleTransitionController>();
                InGameUi ui=null;InGameUi.Ensure(ref ui,tutorial);Set(tutorial,"ui",ui);Set(tutorial,"session",session);
                var actor=new GameObject("practice actor",typeof(SpriteRenderer));actor.transform.SetParent(go.transform,false);
                var actors=(System.Collections.Generic.Dictionary<int,GameObject>)typeof(FirstPlayTutorial).GetField("actors",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tutorial);
                actors.Add(0,actor);
                var data=new TutorialActor{id=0,job=100,hp=95,maxHp=200,mp=45,maxMp=45};
                var state=new NativeTutorialProgress{step=142,actors=new[]{data},dialogs=new[]{"F2"}};Set(tutorial,"progress",state);
                int[] ticks={2,3,4,5,20,99,999,0};int[] widths={8,12,16,20,80,80,80,0};
                for(int i=0;i<ticks.Length;i++)
                {
                    data.tick=ticks[i];string before=JsonUtility.ToJson(state);
                    ui.Begin();Call(tutorial,"DrawDialogs");ui.End();
                    var clip=ui.Root.Find("practice-ap-clip") as RectTransform;Assert.IsNotNull(clip);
                    Assert.AreEqual(widths[i]>0,clip.gameObject.activeSelf);
                    if(widths[i]>0)
                    {
                        Assert.AreEqual((float)widths[i],clip.sizeDelta.x);Assert.AreEqual(9f,clip.sizeDelta.y);
                        Assert.AreEqual(new Vector2(80,9),ui.Find<RawImage>("practice-ap-fill").rectTransform.sizeDelta);
                        Above(ui,"practice-ap-clip","status");
                    }
                    Assert.IsNull(ui.Find<RawImage>("practice-ap-digit-0"));
                    Assert.AreEqual(before,JsonUtility.ToJson(state),"Display must not clamp99/999 in the script state.");
                }
                // A closed F2 window leaves no retained gauge; reopening redraws its current literal.
                state.dialogs=Array.Empty<string>();ui.Begin();Call(tutorial,"DrawDialogs");ui.End();
                Assert.IsFalse(ui.Root.Find("practice-ap-clip").gameObject.activeSelf);
                data.tick=99;state.dialogs=new[]{"F2"};ui.Begin();Call(tutorial,"DrawDialogs");ui.End();
                Assert.IsTrue(ui.Root.Find("practice-ap-clip").gameObject.activeSelf);
                Assert.AreEqual(80f,((RectTransform)ui.Root.Find("practice-ap-clip")).sizeDelta.x);Assert.AreEqual(99,data.tick);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test]
        public void ActualServerRendererKeepsLocalNameAndCoverAboveBakedOnlineName()
        {
            var go=new GameObject("native server paint regression");
            try
            {
                var menu=go.AddComponent<StartMenuController>();InGameUi ui=null;InGameUi.Ensure(ref ui,menu);
                Set(menu,"ui",ui);Set(menu,"entryAtlas",Resources.Load<Texture2D>("UI/Entry/LoginAtlas"));
                Set(menu,"listedServers",new[]{new LocalServerInfo(1,"로컬 서버",true)});
                for(int frame=0;frame<4;frame++)
                {
                    Set(menu,"proposedServerId",frame%2);ui.Begin();Call(menu,"DrawServerSelection");ui.End();Canvas.ForceUpdateCanvases();
                    Assert.AreEqual("로컬 서버",ui.Find<Text>("server-name-1").text);
                    Above(ui,"server-row-1","servers-clear-list");Above(ui,"server-name-cover-1","server-row-1");Above(ui,"server-name-1","server-name-cover-1");
                    Above(ui,"server-hit-1","server-name-1");Assert.IsTrue(ui.Find<Button>("server-hit-1").interactable);
                    Assert.AreEqual(0,ui.Find<RawImage>("server-hit-1").color.a);
                }
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test]
        public void ExplicitBackgroundStillStaysBehindPreviouslyDrawnForeground()
        {
            var go=new GameObject("explicit background paint regression");
            try
            {
                var owner=go.AddComponent<StartMenuController>();InGameUi ui=null;InGameUi.Ensure(ref ui,owner);
                for(int frame=0;frame<3;frame++)
                {
                    ui.Begin();ui.Label("foreground",new Rect(0,0,100,20),"내용");
                    ui.EntryArtwork("background",new Rect(0,0,100,100),null,background:true);ui.End();
                    Above(ui,"foreground","background");
                }
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
