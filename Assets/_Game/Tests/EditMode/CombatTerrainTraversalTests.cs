using System;
using System.IO;
using System.Text;
using DarkSaver.Combat;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class CombatTerrainTraversalTests
    {
        [Test]
        public void OriginalAreaUsesCardinalBudgetAndRecordsBeforeTerrainPayment()
        {
            Assert.That(CombatTerrainTraversal.TryBuildMovementArea(3, 3, new CombatGridCell(1, 1),
                1, 100, _ => 4, _ => null, out var area), Is.True);
            Assert.That(area.Count, Is.EqualTo(5)); Assert.That(area[new CombatGridCell(1, 1)], Is.EqualTo(2));
            Assert.That(area[new CombatGridCell(2, 1)], Is.EqualTo(1));
            Assert.That(area.ContainsKey(new CombatGridCell(2, 2)), Is.False); // No direct diagonal expansion.
            Assert.That(CombatTerrainTraversal.TryBuildMovementArea(3, 3, new CombatGridCell(1, 1),
                0, 100, _ => 1, _ => null, out area), Is.True); Assert.That(area.Count, Is.EqualTo(1));
        }

        [Test]
        public void OriginalAreaPassesOnlySameHundredGroupNotEveryAlly()
        {
            foreach (var id in new[] { 109, 200, 800 })
            {
                Assert.That(CombatTerrainTraversal.TryBuildMovementArea(3, 1, new CombatGridCell(0, 0),
                    3, 100, _ => 1, c => c.X == 1 ? id : (int?)null, out var area), Is.True);
                Assert.That(area.ContainsKey(new CombatGridCell(2, 0)), Is.EqualTo(id == 109));
                Assert.That(area.ContainsKey(new CombatGridCell(1, 0)), Is.EqualTo(id == 109));
            }
        }

        [Test]
        public void OriginalAreaRevisitsCheaperDetourAndRejectsUnknownTerrain()
        {
            Assert.That(CombatTerrainTraversal.TryBuildMovementArea(4, 3, new CombatGridCell(0, 1),
                6, 100, c => c.Equals(new CombatGridCell(1, 1)) ? 4 : 1, _ => null, out var area), Is.True);
            Assert.That(area[new CombatGridCell(2, 1)], Is.EqualTo(3));
            foreach (int? cost in new int?[] { 99, -99, 0, null })
            {
                CombatTerrainTraversal.TryBuildMovementArea(3, 1, new CombatGridCell(0, 0),
                    3, 100, _ => cost, _ => null, out area);
                Assert.That(area.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void OriginalAreaInvalidInputsReturnEmptyReadOnlyResults()
        {
            foreach (var budget in new[] { -1, int.MaxValue })
            {
                Assert.That(CombatTerrainTraversal.TryBuildMovementArea(3, 1, new CombatGridCell(0, 0),
                    budget, 100, _ => 1, _ => null, out var area), Is.False); Assert.That(area.Count, Is.Zero);
            }
            Assert.That(CombatTerrainTraversal.TryBuildMovementArea(3, 1, new CombatGridCell(3, 0),
                1, 100, _ => 1, _ => null, out _), Is.False);
            Assert.That(CombatTerrainTraversal.TryBuildMovementArea(3, 1, new CombatGridCell(0, 0),
                1, 100, null, _ => null, out _), Is.False);
            CombatTerrainTraversal.TryBuildMovementArea(3, 1, new CombatGridCell(0, 0),
                1, 100, _ => 1, _ => null, out var result);
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IDictionary<CombatGridCell, int>)result)
                .Add(new CombatGridCell(2, 0), 999));
        }

        [Test]
        public void EveryMovementValueMatchesOriginalAtrWithoutRuntimeFileDependencies()
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "분석", "Saver_1.2", "decoded", "gameres_set", "TileAttr.atr");
            var seen = new bool[CombatTerrainTraversal.TerrainCount]; var count = 0;
            foreach (var raw in File.ReadAllLines(path, Encoding.ASCII))
            {
                var line = raw.Trim(); if (line.Length == 0 || line[0] == ';') continue;
                var fields = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                Assert.That(fields.Length, Is.EqualTo(31)); Assert.That(fields[0].Length, Is.EqualTo(1));
                var symbol = fields[0][0]; var attribute = symbol <= '9' ? symbol - '0' : symbol - 'A' + 10;
                Assert.That(attribute, Is.InRange(0, 26)); Assert.That(seen[attribute], Is.False);
                seen[attribute] = true;
                for (var troop = 0; troop < CombatTerrainTraversal.TroopColumnCount; troop++)
                {
                    Assert.That(CombatTerrainTraversal.TryGetMovementValue(attribute, troop, out var value), Is.True);
                    var expected = int.Parse(fields[3 + troop * 3], System.Globalization.CultureInfo.InvariantCulture);
                    Assert.That(value, Is.EqualTo(expected), $"Terrain {attribute}, troop {troop}");
                    Assert.That(CombatTerrainTraversal.CanTraverse(attribute, troop), Is.EqualTo(expected != 99)); count++;
                }
            }
            Assert.That(count, Is.EqualTo(270)); Assert.That(seen, Has.All.True);
        }

        [Test]
        public void FlyingNavalAndCavalryUseTheirOwnColumnsRatherThanInfantryColliders()
        {
            Assert.That(CombatTerrainTraversal.CanTraverse(6, 0), Is.False);
            Assert.That(CombatTerrainTraversal.CanTraverse(6, 1), Is.True);
            Assert.That(CombatTerrainTraversal.CanTraverse(6, 3), Is.True);
            Assert.That(CombatTerrainTraversal.CanTraverse(0, 3), Is.False);
            Assert.That(CombatTerrainTraversal.CanTraverse(7, 0), Is.True);
            Assert.That(CombatTerrainTraversal.CanTraverse(7, 2), Is.False);
            for (var troop = 0; troop < 10; troop++) Assert.That(CombatTerrainTraversal.CanTraverse(24, troop), Is.False);
        }

        [Test]
        public void MissingTerrainAndInvalidColumnsNeverAssumeFloorOrInfantry()
        {
            foreach (var attribute in new[] { -1, 27, 28, 0x86, int.MaxValue })
            {
                Assert.That(CombatTerrainTraversal.TryGetMovementValue(attribute, 0, out var value), Is.False);
                Assert.That(value, Is.EqualTo(0)); Assert.That(CombatTerrainTraversal.CanTraverse(attribute, 0), Is.False);
            }
            foreach (var troop in new[] { -1, 10, 100, 701, int.MaxValue })
                Assert.That(CombatTerrainTraversal.CanTraverse(0, troop), Is.False);
        }

        [Test]
        public void MovementValuesRemainRawTerrainValuesAndNeverBecomeApCosts()
        {
            CombatTerrainTraversal.TryGetMovementValue(4, 2, out var cavalry); Assert.That(cavalry, Is.EqualTo(3));
            CombatTerrainTraversal.TryGetMovementValue(12, 0, out var infantry); Assert.That(infantry, Is.EqualTo(2));
            CombatTerrainTraversal.TryGetMovementValue(25, 0, out var slow); Assert.That(slow, Is.EqualTo(4));
            CombatTerrainTraversal.TryGetMovementValue(6, 0, out var blocked); Assert.That(blocked, Is.EqualTo(99));
        }

        [Test]
        public void ExplicitTerrainPolicyCombinesWithGridOccupancyWithoutChangingStats()
        {
            var world = new CombatWorld(); var unit = new CombatUnit { Id = 100, JobId = 100, Hp = 50, MaxHp = 50, Mp = 10, MaxMp = 10 };
            world.Add(unit);
            var grid = new CombatOccupancyGrid(world, 3, 1,
                (_, cell) => CombatTerrainTraversal.CanTraverse(cell.X == 1 ? 6 : 0, 0),
                (from, to) => from.Y == to.Y);
            Assert.That(grid.TryPlace(100, new CombatGridCell(0, 0)), Is.True);
            Assert.That(grid.CanTraverse(100, new CombatGridCell(1, 0)), Is.False);
            Assert.That(grid.TryReserveStep(100, new CombatGridCell(1, 0)), Is.False);
            Assert.That(unit.X, Is.EqualTo(0)); Assert.That(unit.Hp, Is.EqualTo(50));
            Assert.That(unit.Mp, Is.EqualTo(10)); Assert.That(unit.ActionPoints.Current, Is.EqualTo(6));
        }
    }
}
