using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class CombatDeathPresentationTests
    {
        [Test]
        public void EveryOrdinaryDeathCounterSelectsRecoveredBodyAndEffectFrames()
        {
            var body = new[] { 32, 32, 32, 32, 32, 32, 32, 32, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2 };
            var effect = new[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6 };
            for (var counter = 0; counter < body.Length; counter++)
            {
                Assert.That(CombatDeathPresentation.TryGetFrame(counter, out var frame), Is.True);
                Assert.That(frame.BodyRow, Is.EqualTo(body[counter]));
                Assert.That(frame.EffectColumn, Is.EqualTo(effect[counter]));
            }
        }

        [Test]
        public void SourceRectanglesKeepOriginalStrideAndExclusive31PixelEffectBounds()
        {
            CombatDeathPresentation.TryGetFrame(0, out var first);
            Assert.That(first.BodySourceLeft, Is.Zero); Assert.That(first.BodySourceTop, Is.EqualTo(1024));
            Assert.That(first.BodySourceWidth, Is.EqualTo(32)); Assert.That(first.BodySourceHeight, Is.EqualTo(32));
            CombatDeathPresentation.TryGetFrame(18, out var last);
            Assert.That(last.BodySourceTop, Is.EqualTo(64)); Assert.That(last.EffectSourceLeft, Is.EqualTo(192));
            Assert.That(last.EffectSourceTop, Is.Zero);
            Assert.That(last.EffectSourceWidth, Is.EqualTo(31)); Assert.That(last.EffectSourceHeight, Is.EqualTo(31));
        }

        [Test]
        public void InvalidAndPostRemovalCountersNeverLoopOrExposePartialFrames()
        {
            foreach (var counter in new[] { int.MinValue, -1, 19, 20, int.MaxValue })
            {
                Assert.That(CombatDeathPresentation.TryGetFrame(counter, out var frame), Is.False);
                Assert.That(frame.BodyRow, Is.Zero); Assert.That(frame.EffectColumn, Is.Zero);
            }
        }

        [Test]
        public void RepeatedQueriesHaveNoSharedClockAndPreserveEarlierFrameValues()
        {
            CombatDeathPresentation.TryGetFrame(7, out var early);
            CombatDeathPresentation.TryGetFrame(8, out var transition);
            CombatDeathPresentation.TryGetFrame(18, out _);
            CombatDeathPresentation.TryGetFrame(19, out _);
            CombatDeathPresentation.TryGetFrame(7, out var repeated);
            Assert.That(early.BodyRow, Is.EqualTo(32)); Assert.That(early.EffectColumn, Is.EqualTo(2));
            Assert.That(transition.BodyRow, Is.Zero); Assert.That(transition.EffectColumn, Is.EqualTo(2));
            Assert.That(repeated.BodyRow, Is.EqualTo(early.BodyRow));
            Assert.That(repeated.EffectColumn, Is.EqualTo(early.EffectColumn));
        }
    }
}
