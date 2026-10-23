using DarkSaver.Rendering;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class OriginalRenderRulesTests
    {
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
