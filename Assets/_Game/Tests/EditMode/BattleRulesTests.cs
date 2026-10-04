using NUnit.Framework;

namespace DarkSaver.Prototype.Tests
{
    public sealed class BattleRulesTests
    {
        [TestCase(26, 7, false, 19)]
        [TestCase(42, 7, true, 39)]
        [TestCase(3, 20, false, 1)]
        [TestCase(10, -5, false, 10)]
        public void CalculateDamage_ReturnsExpectedDamage(
            int rawDamage, int defense, bool halveDefense, int expected)
        {
            Assert.That(BattleRules.CalculateDamage(rawDamage, defense, halveDefense), Is.EqualTo(expected));
        }

        [TestCase(20, 100, 35, 55)]
        [TestCase(90, 100, 35, 100)]
        [TestCase(20, 100, -5, 20)]
        public void ApplyRecovery_ClampsResult(int current, int maximum, int amount, int expected)
        {
            Assert.That(BattleRules.ApplyRecovery(current, maximum, amount), Is.EqualTo(expected));
        }

        [TestCase(0f, .9f, 1f, 0)]
        [TestCase(.5f, .5f, 1f, 1)]
        [TestCase(.2f, 2f, 1.5f, 3)]
        public void CalculateRecoveredActionPoints_ReturnsWholePoints(
            float charge, float deltaTime, float rate, int expected)
        {
            Assert.That(BattleRules.CalculateRecoveredActionPoints(charge, deltaTime, rate), Is.EqualTo(expected));
        }

        [TestCase(4, 4, true)]
        [TestCase(3, 4, false)]
        [TestCase(10, -1, false)]
        public void CanAfford_ValidatesCost(int current, int cost, bool expected)
        {
            Assert.That(BattleRules.CanAfford(current, cost), Is.EqualTo(expected));
        }

        [TestCase(0, 60)]
        [TestCase(3, 180)]
        public void GetNextLevelExperience_UsesAtLeastLevelOne(int level, int expected)
        {
            Assert.That(BattleRules.GetNextLevelExperience(level), Is.EqualTo(expected));
        }
    }
}
