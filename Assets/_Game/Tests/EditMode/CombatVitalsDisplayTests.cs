using System.Collections.Generic;
using DarkSaver.Combat;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class CombatVitalsDisplayTests
    {
        [Test]
        public void UpdatesRestAndOverkillPublishActualDeltasWithoutDuplicates()
        {
            var world = new CombatWorld();
            var ally = new CombatUnit { Id = 1, Hp = 90, MaxHp = 120, Mp = 30, MaxMp = 30, Attack = 100 };
            world.Add(ally);
            world.Add(new CombatUnit { Id = 2, Enemy = true, Hp = 7, MaxHp = 75 });
            var flow = new CombatFlow(world, new CombatServerBoundary(), useActionPoints: false);
            flow.TemporaryRulesEnabled = true;
            var changes = new List<(short Id, int Delta, bool Mp)>();
            flow.VitalsDisplayChanged += (id, delta, mp) => changes.Add((id, delta, mp));
            Assert.That(flow.TryApplyHp(1, 80), Is.True);
            Assert.That(flow.TryApplyHp(1, 80), Is.True);
            Assert.That(flow.TryApplyHp(1, 121), Is.False);
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(flow.TryApplyHp(1, 90), Is.True);
            Assert.That(flow.TryApplyMp(1, 20), Is.True);
            Assert.That(flow.TryApplyMp(1, 20), Is.True);
            ally.ActionPoints.Advance(4);
            Assert.That(flow.TryTemporaryRest(1), Is.True);
            Assert.That(ally.Hp, Is.EqualTo(114));
            flow.ConfigurePhysicalAccuracy(1, 100, 0); flow.ConfigurePhysicalAccuracy(2, 100, 0);
            Assert.That(flow.TryExecute(new CombatRequest(1, 2, CombatActionType.Attack), out var result, out _), Is.True);
            Assert.That(result.Damage, Is.EqualTo(100));
            Assert.That(changes, Is.EqualTo(new[] {
                ((short)1, -10, false), ((short)1, 10, false), ((short)1, -10, true),
                ((short)1, 24, false), ((short)2, -7, false) }));
            world.Add(new CombatUnit { Id = 3, Enemy = true, Hp = 100, MaxHp = 100 });
            Assert.That(flow.TryExecute(new CombatRequest(1, 3, CombatActionType.Magic,
                CombatServerBoundary.PrototypeMagicId), out _, out _), Is.True);
            Assert.That(changes[5], Is.EqualTo(((short)3, -100, false)));
            Assert.That(changes[6], Is.EqualTo(((short)1, -10, true)));
            Assert.That(ally.Mp, Is.EqualTo(10));
        }
    }
}
