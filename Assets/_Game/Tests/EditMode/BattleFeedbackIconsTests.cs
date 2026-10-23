using DarkSaver.UI;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class BattleFeedbackIconsTests
    {
        [Test]
        public void NativeDamageAndMissShareTheRecoveredBounceCurve()
        {
            var values = new[] { 0, -4, -5, -4, 0, 1, 4, 10, 4, 1, 0, 1, 4, 10 };
            for (int i = 1; i < 14; i++)
            {
                Assert.That(BattleFeedbackMotion.TryGetStepOffset(i, out var point), Is.True);
                Assert.That(point, Is.EqualTo(new Vector2(i, values[i])));
            }
            Assert.That(BattleFeedbackMotion.TryGetStepOffset(0, out _), Is.False);
            Assert.That(BattleFeedbackMotion.TryGetStepOffset(14, out _), Is.False);
            Assert.That(BattleFeedbackMotion.TryGetOffset(float.NaN, out _), Is.False);
            Assert.That(BattleFeedbackMotion.TryGetOffset(0, out _), Is.False);
            Assert.That(BattleFeedbackMotion.TryGetOffset(GameConstants.BattleFeedbackSeconds, out _), Is.False);
        }
        [Test]
        public void NativeNumberCellsAndMissUseOriginalDimensions()
        {
            for (int row = 0; row < 4; row++) foreach (char value in "0123456789+-")
            {
                var sprite = BattleFeedbackIcons.Number(value, row);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(8, 14)));
            }
            Assert.That(BattleFeedbackIcons.Number('0').name, Is.EqualTo("Number_0_10"));
            Assert.That(BattleFeedbackIcons.Number('-').name, Is.EqualTo("Number_0_12"));
            Assert.That(BattleFeedbackIcons.Number('x'), Is.Null);
            Assert.That(BattleFeedbackIcons.Number('1', -1), Is.Null);
            Assert.That(BattleFeedbackIcons.Miss, Is.Not.Null);
            Assert.That(BattleFeedbackIcons.Miss.rect.size, Is.EqualTo(new Vector2(36, 11)));
        }
    }
}
