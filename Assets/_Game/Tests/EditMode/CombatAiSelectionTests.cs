using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class CombatAiSelectionTests
    {
        [Test]
        public void QueueCopiesInputAndProcessesOneIdPerCallInOriginalOrder()
        {
            var queue = new CombatAiUnitQueue();
            var ids = new short[] { 400, 401, 400 };
            Assert.That(queue.TrySetUnits(ids), Is.True); ids[0] = 999;
            foreach (var expected in new short[] { 400, 401, 400, 400, 401, 400 })
            {
                Assert.That(queue.TryNext(out var actual), Is.True);
                Assert.That(actual, Is.EqualTo(expected));
            }
            Assert.That(queue.Count, Is.EqualTo(3));
        }

        [Test]
        public void QueueCapacityIsOneHundredAndRejectedReplacementPreservesCursor()
        {
            var queue = new CombatAiUnitQueue(); var ids = new short[100];
            for (short i = 0; i < ids.Length; i++) ids[i] = (short)(400 + i);
            Assert.That(queue.TrySetUnits(ids), Is.True); queue.TryNext(out _);
            Assert.That(queue.TrySetUnits(new short[101]), Is.False);
            Assert.That(queue.TrySetUnits(null), Is.False);
            Assert.That(queue.TryNext(out var next), Is.True); Assert.That(next, Is.EqualTo(401));
            Assert.That(queue.Count, Is.EqualTo(100));
            Assert.That(queue.TrySetUnits(System.Array.Empty<short>()), Is.True);
            Assert.That(queue.TryNext(out _), Is.False);
        }

        [Test]
        public void SourceOrderFiftyTwentyThirtyMatchesEveryOriginalRoll()
        {
            var weights = new[] { 50, 20, 30 };
            for (var roll = 0; roll < 100; roll++)
            {
                Assert.That(CombatAiActionSelection.TrySelect(weights, roll, out var index), Is.True);
                Assert.That(index, Is.EqualTo(roll < 50 ? 0 : roll < 70 ? 1 : 2));
            }
        }

        [Test]
        public void OverweightClipsLaterActionsWithoutNormalizationOrOverflow()
        {
            var weights = new[] { 80, 50, 30 };
            for (var roll = 0; roll < 100; roll++)
            {
                Assert.That(CombatAiActionSelection.TrySelect(weights, roll, out var index), Is.True);
                Assert.That(index, Is.EqualTo(roll < 80 ? 0 : 1));
            }
            Assert.That(CombatAiActionSelection.TrySelect(new[] { 0, int.MaxValue, int.MaxValue }, 99, out var large), Is.True);
            Assert.That(large, Is.EqualTo(1));
        }

        [Test]
        public void UncoveredAndInvalidRollsNeverInventFallbackActions()
        {
            foreach (var roll in new[] { 20, 50, 99 })
            {
                Assert.That(CombatAiActionSelection.TrySelect(new[] { 10, 10 }, roll, out var index), Is.False);
                Assert.That(index, Is.EqualTo(-1));
            }
            Assert.That(CombatAiActionSelection.TrySelect(new[] { 0, 0 }, 0, out _), Is.False);
            Assert.That(CombatAiActionSelection.TrySelect(new[] { 100, -1 }, 0, out _), Is.False);
            Assert.That(CombatAiActionSelection.TrySelect(null, 0, out _), Is.False);
            Assert.That(CombatAiActionSelection.TrySelect(new[] { 100 }, -1, out _), Is.False);
            Assert.That(CombatAiActionSelection.TrySelect(new[] { 100 }, 100, out _), Is.False);
        }
    }
}
