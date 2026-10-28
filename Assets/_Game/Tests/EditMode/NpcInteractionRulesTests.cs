using DarkSaver.World;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class NpcInteractionRulesTests
    {
        [Test]
        public void OnlyFourAdjacentCellsAllowUnobstructedNpcInteraction()
        {
            var npc = new Vector2(4.5f, 10.5f);
            foreach (var offset in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
                Assert.IsTrue(NpcInteractionRules.IsAdjacent(npc + offset, npc));
            foreach (var offset in new[] { Vector2.zero, new Vector2(1, 1), new Vector2(-1, 1),
                new Vector2(1, -1), new Vector2(-1, -1), Vector2.down * 2, Vector2.right * 2 })
                Assert.IsFalse(NpcInteractionRules.IsAdjacent(npc + offset, npc));
            Assert.IsTrue(NpcInteractionRules.IsAdjacent(new Vector2(5.01f, 10.99f), npc));
        }
        [Test]
        public void CounterFrontUsesAuthoredCellRatherThanCircularDistance()
        {
            var front = new Vector2Int(4, 8);
            Assert.IsTrue(NpcInteractionRules.IsCounterFront(new Vector2(4.5f, 8.5f), front));
            Assert.IsTrue(NpcInteractionRules.IsCounterFront(new Vector2(4.01f, 8.99f), front));
            Assert.IsFalse(NpcInteractionRules.IsCounterFront(new Vector2(4.5f, 7.5f), front));
            Assert.IsFalse(NpcInteractionRules.IsCounterFront(new Vector2(5.5f, 8.5f), front));
            Assert.IsFalse(NpcInteractionRules.IsCounterFront(new Vector2(4.5f, 9.5f), front));
        }
        [Test]
        public void InvalidPositionsCannotInteract()
        {
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Assert.IsFalse(NpcInteractionRules.IsAdjacent(new Vector2(value, 8), new Vector2(4, 10)));
                Assert.IsFalse(NpcInteractionRules.IsAdjacent(new Vector2(4, 8), new Vector2(4, value)));
                Assert.IsFalse(NpcInteractionRules.IsCounterFront(new Vector2(4, value), new Vector2Int(4, 8)));
            }
        }
    }
}
