using NUnit.Framework;

namespace DarkSaver.Prototype.Tests
{
    public sealed class QuestRulesTests
    {
        [TestCase(1, true, 1, false)]
        [TestCase(2, false, 1, false)]
        [TestCase(2, true, 0, false)]
        [TestCase(2, true, 1, true)]
        public void IsReadyToDeliver_RequiresEveryObjective(
            int kills, bool fieldItem, int drops, bool expected)
        {
            Assert.That(
                QuestRules.IsReadyToDeliver(kills, fieldItem, drops),
                Is.EqualTo(expected));
        }

        [TestCase(0, true)]
        [TestCase(39, true)]
        [TestCase(40, false)]
        [TestCase(99, false)]
        public void IsMonsterDrop_UsesFortyPercentBoundary(int roll, bool expected)
        {
            Assert.That(QuestRules.IsMonsterDrop(roll), Is.EqualTo(expected));
        }
    }
}
