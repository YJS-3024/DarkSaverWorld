using System;
using DarkSaver.Combat;
using DarkSaver.Magic;
using DarkSaver.Network;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class CombatMagicTargetsTests
    {
        [Test]
        public void RelationOneIsBothFiltersAndOtherStatesAreNotBitmasks()
        {
            var relations = new int[8];
            for (var state = 0; state < 8; state++)
            {
                relations[2] = state;
                foreach (var reverse in new[] { false, true })
                {
                    Assert.That(CombatMagicTargets.TryAllowsTeam(1, reverse ? 2 : 0, reverse ? 0 : 2, 0, relations, out var ally), Is.True);
                    Assert.That(ally, Is.EqualTo(state == 1 || state == 2));
                    Assert.That(CombatMagicTargets.TryAllowsTeam(2, reverse ? 2 : 0, reverse ? 0 : 2, 0, relations, out var opponent), Is.True);
                    Assert.That(opponent, Is.EqualTo(state == 1 || state == 4));
                }
            }
        }

        [Test]
        public void EqualNpcAndHighTeamsDoNotRequireRelations()
        {
            foreach (var pair in new[] { (0, 0), (8, 15), (16, 0), (-1, -1) })
            {
                Assert.That(CombatMagicTargets.TryAllowsTeam(1, pair.Item1, pair.Item2, null, null, out var ally), Is.True);
                Assert.That(ally, Is.True);
                Assert.That(CombatMagicTargets.TryAllowsTeam(2, pair.Item1, pair.Item2, null, null, out var enemy), Is.True);
                Assert.That(enemy, Is.False);
            }
            Assert.That(CombatMagicTargets.TryAllowsTeam(2, 0, 8, null, null, out var mixed), Is.True);
            Assert.That(mixed, Is.True);
            Assert.That(CombatMagicTargets.TryAllowsTeam(1, 0, 2, null, null, out _), Is.False);
            Assert.That(CombatMagicTargets.TryAllowsTeam(1, 0, 2, 0, null, out _), Is.False);
            Assert.That(CombatMagicTargets.TryAllowsTeam(1, 1, 2, 0, null, out var unrelated), Is.True);
            Assert.That(unrelated, Is.False);
        }

        [Test]
        public void GroupEightScansZeroThroughEightAndPreservesEmptySuccess()
        {
            var seen = new System.Collections.Generic.List<short>();
            bool? Eligible(short id) { seen.Add(id); return id == 700 || id == 708; }
            Assert.That(Build(Row(8), Command(), _ => 705, Eligible, null, out var packet), Is.True);
            Assert.That(seen, Is.EqualTo(new short[] {700,701,702,703,704,705,706,707,708}));
            Assert.That(packet.TargetUnitIds, Is.EqualTo(new short[] { 700, 708 }));
            Assert.That(packet.X, Is.EqualTo(-1)); Assert.That(packet.Y, Is.EqualTo(-1));
            Assert.That(packet.ComputedCost, Is.EqualTo(-123)); Assert.That(packet.Mode, Is.EqualTo(7));
            Assert.That(packet.OriginalOpcode, Is.EqualTo(0x92));
            Assert.That(Build(Row(8), Command(), _ => 705, _ => false, null, out var empty), Is.True);
            Assert.That(empty.TargetCount, Is.Zero);
            Assert.That(Build(Row(8), Command(), _ => null, _ => true, null, out var failed), Is.False);
            Assert.That(failed, Is.Null);
        }

        [Test]
        public void GeneralTargetsKeepCellOrderDuplicatesAndExplicitEligibility()
        {
            short? Occupant(CombatGridCell cell) => cell.X == 0 ? (short)200 : (short)100;
            Assert.That(Build(Row(1, 3), Command(), Occupant, _ => true, id => id == 100 ? 0 : 8, out var packet), Is.True);
            Assert.That(packet.TargetUnitIds, Is.EqualTo(new short[] { 200, 200, 100, 100 }));
            Assert.That(packet.X, Is.Zero); Assert.That(packet.Y, Is.Zero);
            Assert.That(Build(Row(1, 3), Command(), Occupant, _ => false, id => id == 100 ? 0 : 8, out _), Is.False);
            Assert.That(Build(Row(1, 3), Command(), Occupant, _ => null, id => id == 100 ? 0 : 8, out _), Is.False);
            Assert.That(Build(Row(1, 3), Command(), Occupant, _ => true, _ => null, out _), Is.False);
        }

        [Test]
        public void ReferenceSentinelsNormalizeWithoutInventingCosts()
        {
            foreach (var source in new short[] { -3, -2 })
            {
                short? read = null;
                Assert.That(Build(Row(1, 1), Command(source), _ => 200, _ => true, id => { if (id != 200) read = id; return 0; }, out var packet), Is.True);
                Assert.That(read, Is.EqualTo(source == -3 ? 700 : 0));
                Assert.That(packet.CasterUnitId, Is.EqualTo(read)); Assert.That(packet.ComputedCost, Is.EqualTo(-123));
            }
            Assert.That(Build(Row(1, 0), Command(-1), _ => 200, _ => true, null, out var bypass), Is.True);
            Assert.That(bypass.CasterUnitId, Is.EqualTo(-1));
            Assert.That(bypass.ActingUnitId, Is.EqualTo(100));
            Assert.That(Build(Row(1, 0), Command(), _ => 200, _ => true, null, out _), Is.False);
        }

        [Test]
        public void UnknownGroupRecordAndPrepopulatedCommandNeverReturnPartialPacket()
        {
            Assert.That(Build(Row(8), Command(), _ => 705, id => id == 708 ? null : true, null, out var packet), Is.False);
            Assert.That(packet, Is.Null);
            var populated = new MagicRequest(101, 100, 0, 0, 0, new short[] { 700 });
            Assert.That(Build(Row(8), populated, _ => 705, _ => true, null, out packet), Is.False);
            Assert.That(packet, Is.Null);
        }

        [Test]
        public void MoreThanByteCapacityNeverWrapsCountOrTruncatesTargets()
        {
            var data = new long[MagicCatalogEntry.NumericColumnCount];
            data[0] = 101; data[2] = 3; data[3] = 30; data[7] = 1;
            Assert.That(CombatMagicTargets.TryBuildRequest(new MagicCatalogEntry(data, "Capacity"), Command(-1),
                new CombatGridCell(0, 0), new CombatGridCell(0, 0), 16, 16, _ => 200, _ => true,
                null, null, null, out var packet, out var reason), Is.False);
            Assert.That(packet, Is.Null); Assert.That(reason, Does.Contain("한도"));
        }

        private static bool Build(MagicCatalogEntry row, MagicRequest command, Func<CombatGridCell, short?> occupant,
            Func<short, bool?> eligible, Func<short, int?> teams, out MagicRequest packet) =>
            CombatMagicTargets.TryBuildRequest(row, command, new CombatGridCell(0, 0), new CombatGridCell(0, 0),
                2, 2, occupant, eligible, teams, 0, new int[8], out packet, out _);
        private static MagicRequest Command(short source = 100) => new(101, source, 0, 0, -123, Array.Empty<short>(), 0x92, 7, 100);
        private static MagicCatalogEntry Row(long unit, long team = 0)
        {
            var data = new long[MagicCatalogEntry.NumericColumnCount];
            data[0] = 101; data[2] = 3; data[3] = 2; data[6] = team; data[7] = unit;
            return new MagicCatalogEntry(data, "Native target check");
        }
    }
}
