using DarkSaver.Combat;
using NUnit.Framework;
using UnityEngine;
namespace DarkSaver.Tests
{
    public sealed class NativeThunderEffectsTests
    {
        [Test] public void NativeThunderUsesSimultaneousBoltAndImpactWithNativeCounterRules()
        {
            var effect = Resources.Load<NativeThunderEffects>("Effects/NativeThunderEffectsV2");
            Assert.That(effect, Is.Not.Null); Assert.That(effect.MagicCount, Is.EqualTo(29));
            Assert.That(effect.Supports(501), Is.True); Assert.That(effect.Supports(101), Is.False);
            Assert.That(effect.Bolt(0).rect.size, Is.EqualTo(new Vector2(50, 350)));
            Assert.That(effect.Bolt(1), Is.EqualTo(effect.Bolt(0))); Assert.That(effect.Bolt(2).name, Is.EqualTo("Thunder_1"));
            Assert.That(effect.Impact(0).name, Is.EqualTo("Thunder_5"));
            Assert.That(effect.Impact(10), Is.Not.Null); Assert.That(effect.Impact(19), Is.Not.Null);
            Assert.That(effect.Impact(20), Is.Null); Assert.That(effect.Bolt(20), Is.Null);
            Assert.That(NativeThunderEffects.NextCounter(0, 4, 0), Is.EqualTo(4));
            Assert.That(NativeThunderEffects.NextCounter(0, 4, 1), Is.EqualTo(5));
            Assert.That(NativeThunderEffects.NextCounter(7, 0, 0), Is.EqualTo(7));
            Assert.That(NativeThunderEffects.NextCounter(7, 0, 2), Is.EqualTo(8));
            Assert.That(effect.Caster(0).rect.size, Is.EqualTo(new Vector2(40, 40)));
            Assert.That(effect.Caster(5), Is.EqualTo(effect.Caster(0))); Assert.That(effect.Caster(10), Is.Null);
        }
    }
}
