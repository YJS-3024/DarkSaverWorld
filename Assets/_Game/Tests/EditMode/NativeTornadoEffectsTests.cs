using DarkSaver.Combat;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class NativeTornadoEffectsTests
    {
        [Test] public void NativeTornadoUsesOriginalSwirlAndImpactFrames()
        {
            var effect = Resources.Load<NativeFireballEffects>("Effects/NativeTornadoEffects");
            Assert.That(effect, Is.Not.Null); Assert.That(effect.MagicIds.Count, Is.EqualTo(10));
            Assert.That(effect.Supports(801), Is.True); Assert.That(effect.Supports(4804), Is.True);
            Assert.That(effect.Supports(810), Is.False);
            Assert.That(effect.Supports(5701), Is.False); Assert.That(effect.Supports(501), Is.False);
            Assert.That((effect.ProjectileCount, effect.ImpactCount), Is.EqualTo((4, 4)));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(effect.Projectile(i).rect.size, Is.EqualTo(new Vector2(64, 64)));
                Assert.That(effect.Impact(i).rect.size, Is.EqualTo(new Vector2(32, 32)));
            }
        }
    }
}
