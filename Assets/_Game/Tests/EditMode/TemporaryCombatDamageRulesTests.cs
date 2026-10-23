using DarkSaver.Combat;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class TemporaryCombatDamageRulesTests
    {
        [TestCase(65, 55, false, 10)]
        [TestCase(65, 55, true, 58)]
        [TestCase(0, 999, false, 1)]
        [TestCase(10, -20, false, 10)]
        [TestCase(int.MaxValue, 0, true, int.MaxValue)]
        public void ExtractionPreservesExistingProvisionalDamage(int attack, int defense, bool magic, int expected)
            => Assert.That(TemporaryCombatDamageRules.Calculate(attack, defense, magic), Is.EqualTo(expected));
    }
}
