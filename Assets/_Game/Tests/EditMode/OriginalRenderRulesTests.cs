using DarkSaver.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class OriginalRenderRulesTests
    {
        [TestCase("Map_329_RenderPassTest", true)]
        [TestCase("Brugos01_Map", true)]
        [TestCase("Brugos01_Runtime", false)]
        public void LoadedSceneRepairPreservesSourceOrderAndLeavesOtherObjectsAlone(string sceneName, bool repaired)
        {
            if (SceneManager.GetSceneByName(sceneName).IsValid())
                Assert.Ignore("Close the matching map scene before this isolated scene test.");
            var scene = SceneManager.CreateScene(sceneName);
            try
            {
                var foreground = new GameObject("Object_007", typeof(SpriteRenderer));
                var character = new GameObject("NPC", typeof(SpriteRenderer));
                SceneManager.MoveGameObjectToScene(foreground, scene);
                SceneManager.MoveGameObjectToScene(character, scene);
                foreground.GetComponent<SpriteRenderer>().sortingOrder = 7;
                character.GetComponent<SpriteRenderer>().sortingOrder = 123;
                var type = typeof(OriginalRenderRules).Assembly.GetType("DarkSaver.World.MapForegroundPass");
                type.GetMethod("Apply", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .Invoke(null, new object[] { scene });
                Assert.That(foreground.GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(repaired ? 30007 : 7));
                Assert.That(character.GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(123));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [TestCase("Brugos01_Map")]
        [TestCase("Map_01_브르고스궁")]
        [TestCase("Map_107_;")]
        [TestCase("Map_329_;")]
        [TestCase("Kaosia_Map")]
        [TestCase("World1010_Map")]
        public void ForegroundPassIncludesScenarioMapScenes(string name)
            => Assert.That(OriginalRenderRules.IsNativeMapScene(name), Is.True);

        [TestCase("Brugos01_Runtime")]
        [TestCase("Training101_Runtime")]
        [TestCase("Battle")]
        [TestCase("Map_invalid_name")]
        [TestCase("Map_-1_name")]
        [TestCase(null)]
        public void ForegroundPassDoesNotRewriteGameplayScenes(string name)
            => Assert.That(OriginalRenderRules.IsNativeMapScene(name), Is.False);

        [Test]
        public void ForegroundUsesSourceOrderAndRejectsOutOfRangeSortingValues()
        {
            Assert.That(OriginalRenderRules.TryGetForegroundSortingOrder("Object_000", out var first), Is.True);
            Assert.That(first, Is.EqualTo(30000));
            Assert.That(OriginalRenderRules.ForegroundSortingOrder(7), Is.EqualTo(first + 7));
            Assert.That(OriginalRenderRules.ForegroundSortingOrder(OriginalRenderRules.MaximumForegroundIndex), Is.EqualTo(short.MaxValue));
            foreach (var name in new[] { "Object_-1", "Object_3000", "Object_x", "NPC_001", "Object_ 1" })
                Assert.That(OriginalRenderRules.TryGetForegroundSortingOrder(name, out _), Is.False);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => OriginalRenderRules.ForegroundSortingOrder(3000));
        }

        [Test]
        public void WaterAttributesMaskOcclusionWithoutChangingTerrainMeaning()
        {
            foreach (var attribute in new[] { 0x06, 0x14, 0x86, 0x94 })
                Assert.That(OriginalRenderRules.IsWaterSubmergeAttribute(attribute), Is.True);
            foreach (var attribute in new[] { 0, 5, 7, 0x80, 0x1B, 0x7F })
                Assert.That(OriginalRenderRules.IsWaterSubmergeAttribute(attribute), Is.False);
        }

        [Test]
        public void OriginalRenderExclusionsAreNotCombatLifeStates()
        {
            Assert.That(OriginalRenderRules.ShouldShowWaterSubmerge(true, 100, 0), Is.True);
            Assert.That(OriginalRenderRules.ShouldShowWaterSubmerge(false, 100, 0), Is.False);
            Assert.That(OriginalRenderRules.ShouldShowWaterSubmerge(true, 899, 0), Is.False);
            Assert.That(OriginalRenderRules.ShouldShowWaterSubmerge(true, 100, 1), Is.False);
            Assert.That(OriginalRenderRules.ShouldShowWaterSubmerge(true, 100, 2), Is.True);
        }

        [Test]
        public void WaterFrame_ChangesAtConfiguredIntervalAndLoops()
        {
            var interval = System.Math.Max(1, OriginalRenderRules.WaterFrameMilliseconds);
            Assert.That(OriginalRenderRules.WaterFrame(0), Is.Zero);
            Assert.That(OriginalRenderRules.WaterFrame(interval - 1), Is.Zero);
            Assert.That(OriginalRenderRules.WaterFrame(interval), Is.EqualTo(1));
            Assert.That(OriginalRenderRules.WaterFrame(interval * 2L), Is.Zero);
            Assert.That(OriginalRenderRules.WaterFrame(interval * 10000001L), Is.EqualTo(1));
        }

        [Test]
        public void WaterBodyClippingOnlyUsesVerifiedFrameSizes()
        {
            Assert.That(OriginalRenderRules.WaterBodyVisiblePixels(32), Is.EqualTo(25));
            Assert.That(OriginalRenderRules.WaterBodyVisiblePixels(64), Is.EqualTo(48));
            Assert.That(OriginalRenderRules.WaterBodyVisiblePixels(48), Is.Zero);
        }
    }
}
