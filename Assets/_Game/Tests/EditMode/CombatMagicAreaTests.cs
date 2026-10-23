using DarkSaver.Combat;
using DarkSaver.Magic;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class CombatMagicAreaTests
    {
        [Test]
        public void StandardAreaUsesPositiveManhattanWeightsNotSquareOrEuclideanRadius()
        {
            var magic = Row(3, 2); var center = new CombatGridCell(3, 3); var positive = 0;
            for (var x = 0; x < 7; x++)
                for (var y = 0; y < 7; y++)
                {
                    Assert.That(CombatMagicArea.TryGetStandardWeight(magic, new CombatGridCell(0, 0),
                        center, new CombatGridCell(x, y), 7, 7, out var weight), Is.True);
                    Assert.That(weight, Is.EqualTo(System.Math.Max(0, 3 - System.Math.Abs(x - 3) - System.Math.Abs(y - 3))));
                    if (weight > 0) positive++;
                }
            Assert.That(positive, Is.EqualTo(13));
        }

        [Test]
        public void NonPositiveAreaClampsToCenterAndZeroRangeUsesCaster()
        {
            var caster = new CombatGridCell(1, 1); var clicked = new CombatGridCell(5, 5);
            foreach (var area in new long[] { 0, -1, -100 })
            {
                Assert.That(CombatMagicArea.TryGetStandardWeight(Row(0, area), caster, clicked, caster, 6, 6, out var center), Is.True);
                Assert.That(center, Is.EqualTo(1));
                Assert.That(CombatMagicArea.TryGetStandardWeight(Row(0, area), caster, clicked, clicked, 6, 6, out var distant), Is.True);
                Assert.That(distant, Is.Zero);
            }
        }

        [Test]
        public void MapEdgeClipsPositiveMaskWithoutInventingTerrainOrOccupancyRules()
        {
            var magic = Row(1, 2); var origin = new CombatGridCell(0, 0); var count = 0;
            for (var x = 0; x < 3; x++)
                for (var y = 0; y < 3; y++)
                {
                    Assert.That(CombatMagicArea.TryGetStandardWeight(magic, origin, origin, new CombatGridCell(x, y), 3, 3, out var weight), Is.True);
                    if (weight > 0) count++;
                }
            Assert.That(count, Is.EqualTo(6));
            Assert.That(CombatMagicArea.TryGetStandardWeight(magic, origin, origin, new CombatGridCell(-1, 0), 3, 3, out _), Is.False);
        }

        [Test]
        public void SpecialPatternsGroupEightAndInvalidInputsNeverFallbackToNormalMask()
        {
            var center = new CombatGridCell(0, 0);
            for (var range = -6; range <= -1; range++)
                Assert.That(CombatMagicArea.TryGetStandardWeight(Row(range, 2), center, center, center, 3, 3, out _), Is.False);
            Assert.That(CombatMagicArea.TryGetStandardWeight(Row(3, 2, 8), center, center, center, 3, 3, out _), Is.False);
            Assert.That(CombatMagicArea.TryGetStandardWeight(Row(3, int.MaxValue), center, center, center, 3, 3, out _), Is.False);
            Assert.That(CombatMagicArea.TryGetStandardWeight(null, center, center, center, 3, 3, out _), Is.False);
            Assert.That(CombatMagicArea.TryGetStandardWeight(Row(3, 2), center, new CombatGridCell(3, 0), center, 3, 3, out _), Is.False);
        }

        [Test]
        public void CrossUsesAreaNotAreaPlusOneAndIgnoresClickedCenter()
        {
            var caster = new CombatGridCell(3, 3); var clicked = new CombatGridCell(0, 0);
            for (var x = 0; x < 7; x++)
                for (var y = 0; y < 7; y++)
                {
                    Assert.That(CombatMagicArea.TryGetWeight(Row(-2, 3), caster, clicked,
                        new CombatGridCell(x, y), 7, 7, out var weight), Is.True);
                    var expected = x == 3 || y == 3 ? System.Math.Max(0, 3 - System.Math.Abs(x - 3) - System.Math.Abs(y - 3)) : 0;
                    Assert.That(weight, Is.EqualTo(expected));
                }
        }

        [Test]
        public void AlternatingDiamondPreservesNegativeBoundaryAndParity()
        {
            var caster = new CombatGridCell(3, 3);
            for (var area = -2; area <= 3; area++)
                for (var x = 0; x < 7; x++)
                    for (var y = 0; y < 7; y++)
                    {
                        var distance = System.Math.Abs(x - 3) + System.Math.Abs(y - 3);
                        var radius = area + 1;
                        var expected = radius < 0 || distance > radius ? 0 : distance == radius || distance % 2 == radius % 2 ? -1 : 1;
                        Assert.That(CombatMagicArea.TryGetWeight(Row(-5, area), caster, new CombatGridCell(0, 0),
                            new CombatGridCell(x, y), 7, 7, out var weight), Is.True);
                        Assert.That(weight, Is.EqualTo(expected));
                    }
        }

        [Test]
        public void SpecialGeometryRejectsGroupAndUnknownPatternsWithoutFallback()
        {
            var cell = new CombatGridCell(0, 0);
            Assert.That(CombatMagicArea.TryGetWeight(Row(-2, 2, 8), cell, cell, cell, 3, 3, out _), Is.False);
            Assert.That(CombatMagicArea.TryGetWeight(Row(-5, int.MaxValue), cell, cell, cell, 3, 3, out _), Is.False);
        }

        [Test]
        public void DirectionSentinelsKeepBeamAndSideWeightsDistinct()
        {
            var caster = new CombatGridCell(4, 4);
            var directions = new[] { new CombatGridCell(0, 1), new CombatGridCell(0, -1), new CombatGridCell(-1, 0), new CombatGridCell(1, 0) };
            for (var i = 0; i < 4; i++)
            {
                var direction = directions[i]; var click = new CombatGridCell(i - 4, 100);
                for (var step = 1; step <= 3; step++)
                {
                    var center = new CombatGridCell(4 + direction.X * step, 4 + direction.Y * step);
                    var side = new CombatGridCell(center.X + (direction.X == 0 ? 1 : 0), center.Y + (direction.Y == 0 ? 1 : 0));
                    foreach (var range in new[] { -1, -3, -4 })
                    {
                        Assert.That(CombatMagicArea.TryGetWeight(Row(range, 2), caster, click, center, 9, 9, out var centerWeight), Is.True);
                        Assert.That(centerWeight, Is.EqualTo(range == -3 ? 0 : 3 - step));
                        Assert.That(CombatMagicArea.TryGetWeight(Row(range, 2), caster, click, side, 9, 9, out var sideWeight), Is.True);
                        Assert.That(sideWeight, Is.EqualTo(range == -1 ? 0 : range == -3 ? 3 - step : 2 - step));
                    }
                }
            }
        }

        [Test]
        public void FixedPointLineIncludesEndpointsExceptCoincidentClick()
        {
            var caster = new CombatGridCell(2, 2); var clicked = new CombatGridCell(5, 3);
            var expected = new[] { new CombatGridCell(2, 2), new CombatGridCell(3, 2), new CombatGridCell(4, 3), new CombatGridCell(5, 3) };
            foreach (var cell in expected)
            {
                Assert.That(CombatMagicArea.TryGetWeight(Row(-6, 0), caster, clicked, cell, 8, 8, out var weight), Is.True);
                Assert.That(weight, Is.EqualTo(1));
            }
            Assert.That(CombatMagicArea.TryGetWeight(Row(-6, 0), caster, caster, caster, 8, 8, out var empty), Is.True);
            Assert.That(empty, Is.Zero);
        }

        private static MagicCatalogEntry Row(long range, long area, long unit = 1)
        {
            var data = new long[MagicCatalogEntry.NumericColumnCount];
            data[0] = 101; data[2] = range; data[3] = area; data[7] = unit;
            return new MagicCatalogEntry(data, "Native area check");
        }
    }
}
