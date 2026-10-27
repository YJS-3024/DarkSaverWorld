using System;
using System.Collections;
using System.Reflection;
using DarkSaver.Core;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class OriginalTutorialPresentationOrderTests
    {
        private sealed class Fixture:IDisposable
        {
            public readonly UnityEngine.SceneManagement.Scene scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            public readonly FirstPlayTutorial tutorial;
            public readonly NativeTutorialProgress state=new NativeTutorialProgress{actors=new[]{new TutorialActor{id=0,job=100,x=4,y=7,hp=95,mp=45},new TutorialActor{id=400,job=301,x=5,y=7,hp=0}}};
            public Fixture()
            {
                var go=new GameObject("native tutorial presentation fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
                tutorial=go.AddComponent<FirstPlayTutorial>();Set(tutorial,"progress",state);Call("Reconcile");
            }
            public static object Get(object obj,string name)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(obj);
            public static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(obj,value);
            public void Call(string name,params object[] args)=>typeof(FirstPlayTutorial).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tutorial,args);
            public IList Effects=>(IList)Get(tutorial,"effects");
            public GameObject Actor(int id)=>(GameObject)((IDictionary)Get(tutorial,"actors"))[id];
            public void Draw(){Call("Reconcile");Call("PresentPracticeEffects");}
            public void Dispose(){Call("ClearPracticeEffects");UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void AttackStartsBothNativeEffectsWithoutASecondSimulatedAttack()
        {
            using(var f=new Fixture())
            {
                f.Call("StartPracticeEffect",0,400,false);f.Draw();Assert.AreEqual(2,f.Effects.Count);
                var swing=(SpriteRenderer)Fixture.Get(f.Effects[0],"body");var hit=(SpriteRenderer)Fixture.Get(f.Effects[1],"body");
                Assert.IsTrue(swing.enabled);Assert.IsTrue(hit.enabled);Assert.AreEqual(5f,swing.transform.position.x);
                Assert.AreEqual(95,f.state.actors[0].hp);Assert.AreEqual(0,f.state.actors[1].hp);
                // Physical 0x04 must not delay the native 0x80 corpse branch.
                f.state.actors[1].dead=true;f.Draw();Assert.IsFalse(hit.enabled);
                Assert.IsTrue(f.Actor(400).activeSelf);Assert.IsFalse(f.Actor(400).GetComponent<SpriteRenderer>().enabled);
            }
        }
        [Test] public void MagicKeepsDeadTargetVisibleThroughTravelAndImpactThenRunsCorpseCounter()
        {
            using(var f=new Fixture())
            {
                f.state.actors[1].dead=true;f.Call("StartPracticeEffect",0,400,true);f.Draw();
                Assert.IsTrue(f.Actor(400).activeSelf);Assert.IsTrue(f.Actor(400).GetComponent<SpriteRenderer>().enabled);
                var magic=f.Effects[0];Assert.IsFalse((bool)Fixture.Get(magic,"soundedImpact"));
                f.Call("AdvancePracticeEffects",.4f);f.Draw();Assert.IsTrue((bool)Fixture.Get(magic,"soundedImpact"));
                Assert.IsTrue(f.Actor(400).GetComponent<SpriteRenderer>().enabled);
                f.Call("AdvancePracticeEffects",1f);f.Draw();Assert.AreEqual(0,f.Effects.Count);
                Assert.IsTrue(f.Actor(400).activeSelf);Assert.IsFalse(f.Actor(400).GetComponent<SpriteRenderer>().enabled);
                f.Call("AdvancePracticeEffects",.16f);f.Draw();Assert.IsTrue(f.Actor(400).GetComponent<SpriteRenderer>().enabled);
                f.Call("AdvancePracticeEffects",.23f);f.Draw();Assert.IsFalse(f.Actor(400).activeSelf);
                Assert.IsTrue(f.state.actors[1].dead);Assert.IsFalse(f.state.actors[1].removed);Assert.AreEqual(45,f.state.actors[0].mp);
            }
        }
        [Test] public void ReconnectedDeadStateDoesNotReplayAnOldCorpse()
        {
            using(var f=new Fixture())
            {f.state.actors[1].dead=true;f.Call("SeedPracticeDeaths");f.Draw();Assert.IsFalse(f.Actor(400).activeSelf);Assert.AreEqual(0,f.Effects.Count);}
        }
        [Test] public void ImmediateRemovalWinsEvenWhileMagicIsActiveAndCleanupReleasesAllEffects()
        {
            using(var f=new Fixture())
            {
                f.Call("StartPracticeEffect",0,400,true);f.state.actors[1].removed=true;f.Draw();
                Assert.IsFalse(f.Actor(400).activeSelf);Assert.IsFalse(((SpriteRenderer)Fixture.Get(f.Effects[0],"body")).enabled);
                f.Call("ClearPracticeEffects");Assert.AreEqual(0,f.Effects.Count);
                Assert.AreEqual(0,((IDictionary)Fixture.Get(f.tutorial,"practiceSounds")).Count);
            }
        }
    }
}
