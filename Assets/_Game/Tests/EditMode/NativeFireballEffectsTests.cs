using DarkSaver.Combat;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class NativeFireballEffectsTests
    {
        [Test]
        public void OriginalFireballFramesAndCurveHaveConfirmedPhaseBoundaries()
        {
            var catalog = Resources.Load<NativeFireballEffects>("Effects/NativeFireballEffects");
            Assert.That(catalog, Is.Not.Null); Assert.That(catalog.Supports(101), Is.True);
            Assert.That(catalog.Supports(105), Is.True); Assert.That(catalog.Supports(32000), Is.False);
            for (int i = 0; i < 3; i++) Assert.That(catalog.Projectile(i).rect.size, Is.EqualTo(new Vector2(32, 32)));
            for (int i = 0; i < 5; i++) Assert.That(catalog.Impact(i).rect.size, Is.EqualTo(new Vector2(32, 32)));
            var tick = GameConstants.BattleMagicEffectCounterSeconds;
            Assert.That(NativeFireballEffects.CounterAt(0), Is.Zero);
            Assert.That(NativeFireballEffects.CounterAt(tick * 9), Is.EqualTo(9));
            Assert.That(NativeFireballEffects.CounterAt(tick * 10), Is.EqualTo(10));
            Assert.That(NativeFireballEffects.CounterAt(tick * 14), Is.EqualTo(14));
            Assert.That(NativeFireballEffects.CounterAt(tick * 15), Is.EqualTo(-1));
            Assert.That(NativeFireballEffects.CounterAt(float.NaN), Is.EqualTo(-1));
            var source = new Vector3(1, 2); var target = new Vector3(5, 2); var lift = Vector3.up * (50f / 32);
            Assert.That(NativeFireballEffects.TravelPosition(source, target, 0, lift), Is.EqualTo(source));
            Assert.That(NativeFireballEffects.TravelPosition(source, target, 1, lift), Is.EqualTo(target));
            var middle = NativeFireballEffects.TravelPosition(source, target, .5f, lift);
            Assert.That(middle.x, Is.EqualTo(3).Within(.0001f));
            Assert.That(middle.y, Is.EqualTo(2 + 50f / 32 * .75f).Within(.0001f));
        }
    }
}
