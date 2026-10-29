using System;
using DarkSaver.UI;
using DarkSaver.World;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class MercenaryPresentationTests
    {
        [Test] public void OriginalSceneNineHasHireDismissConfirmAndNineCellGeometry()
        {
            var source=Resources.Load<NativeBattleWindows>("UI/Mercenary/NativeMercenary");Assert.NotNull(source);var w=source.Find(9);Assert.NotNull(w);
            Assert.AreEqual(new Vector2Int(223,277),w.size);Assert.AreEqual(5,w.controls.Length);
            CollectionAssert.AreEqual(new[]{8,7,19,40,39},Array.ConvertAll(w.controls,c=>c.nativeId));
            CollectionAssert.AreEqual(new[]{new Vector2(158,156),new Vector2(158,181),new Vector2(158,206),new Vector2(129,41),new Vector2(129,127)},Array.ConvertAll(w.controls,c=>c.rect.position));
            foreach(var c in w.controls){Assert.AreEqual(3,c.frames.Length);foreach(var frame in c.frames)Assert.NotNull(frame);}
            Assert.IsTrue(KaosiaOriginalPresentation.IsInterior("Interior2900",out var map));Assert.AreEqual("2900",map);
            var staff=Array.FindAll(KaosiaOriginalPresentation.LoadStaff().entries,e=>e.nativeMap=="2900");Assert.AreEqual(1,staff.Length);Assert.AreEqual("local-mercenary-counter",staff[0].evidence);
        }
    }
}
