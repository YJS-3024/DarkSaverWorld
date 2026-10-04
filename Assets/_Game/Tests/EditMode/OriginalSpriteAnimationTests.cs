using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Prototype.Tests
{
    public sealed class OriginalSpriteAnimationTests
    {
        [TestCase(SpriteFacing.Up, 0, 0)]
        [TestCase(SpriteFacing.Right, 1, 4)]
        [TestCase(SpriteFacing.Down, 2, 8)]
        [TestCase(SpriteFacing.Left, 1, 10)]
        public void GetColumn_MapsDirectionAndFrameToOriginalSheet(
            SpriteFacing facing, int frame, int expected)
        {
            Assert.That(OriginalSpriteAnimation.GetColumn(facing, frame), Is.EqualTo(expected));
        }

        [TestCase(-1, 0, SpriteFacing.Left)]
        [TestCase(1, 0, SpriteFacing.Right)]
        [TestCase(0, -1, SpriteFacing.Up)]
        [TestCase(0, 1, SpriteFacing.Down)]
        public void GetFacing_UsesMovementDirection(int x, int y, SpriteFacing expected)
        {
            Assert.That(
                OriginalSpriteAnimation.GetFacing(new Vector2Int(x, y), SpriteFacing.Down),
                Is.EqualTo(expected));
        }
    }
}
