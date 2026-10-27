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
