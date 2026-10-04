using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Prototype.Tests
{
    public sealed class BattleGridRulesTests
    {
        [Test]
        public void CanReach_ReturnsTrueForOpenCellWithinRange()
        {
            var result = BattleGridRules.CanReach(
                Vector2Int.zero, new Vector2Int(2, 0), 2, 4, 4,
                new HashSet<Vector2Int>(), _ => false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanReach_ReturnsFalseWhenObstacleBlocksOnlyRoute()
        {
            var obstacles = new HashSet<Vector2Int> { new Vector2Int(1, 0) };
            var result = BattleGridRules.CanReach(
                Vector2Int.zero, new Vector2Int(2, 0), 2, 3, 1, obstacles, _ => false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void FindNextStep_AvoidsOccupiedCell()
        {
            var occupied = new HashSet<Vector2Int> { new Vector2Int(1, 0) };
            var result = BattleGridRules.FindNextStep(
                Vector2Int.zero, new Vector2Int(2, 0), 3, 2,
                new HashSet<Vector2Int>(), occupied.Contains);

            Assert.That(result, Is.EqualTo(new Vector2Int(0, 1)));
        }
    }
}
