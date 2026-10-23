using DarkSaver.Combat;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class NativeProjectileEffectsTests
    {
        [Test]
        public void SharedNativeFamilyPreservesDifferentFrameCountsAndDimensions()
        {
            var library = Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeProjectileEffects");
            Assert.That(library, Is.Not.Null); Assert.That(library.PatternCount, Is.EqualTo(9));
            Assert.That(library.MagicCount, Is.EqualTo(111));
            Assert.That(library.TryGet(101, out var fire), Is.True);
            Assert.That((fire.ProjectileCount, fire.ImpactCount), Is.EqualTo((3, 5)));
            Assert.That(library.TryGet(301, out var freeze), Is.True);
            Assert.That((freeze.ProjectileCount, freeze.ImpactCount), Is.EqualTo((5, 6)));
            Assert.That(freeze.Projectile(0).rect.size, Is.EqualTo(new Vector2(40, 40)));
            Assert.That(freeze.Impact(0).rect.size, Is.EqualTo(new Vector2(40, 40)));
            Assert.That(library.TryGet(701, out var wind), Is.True);
            Assert.That((wind.ProjectileCount, wind.ImpactCount), Is.EqualTo((2, 4)));
            Assert.That(wind.Projectile(0).rect.size, Is.EqualTo(new Vector2(40, 40)));
            Assert.That(wind.Impact(0).rect.size, Is.EqualTo(new Vector2(32, 32)));
            Assert.That(library.TryGet(2701, out _), Is.True);
            Assert.That(library.TryGet(32000, out _), Is.False);
            var tick = GameConstants.BattleMagicEffectCounterSeconds;
            Assert.That(NativeFireballEffects.CounterAt(tick * 15, freeze.ImpactCount), Is.EqualTo(15));
            Assert.That(NativeFireballEffects.CounterAt(tick * 16, freeze.ImpactCount), Is.EqualTo(-1));
            Assert.That(NativeFireballEffects.CounterAt(tick * 14, wind.ImpactCount), Is.EqualTo(-1));
        }
    }
}
