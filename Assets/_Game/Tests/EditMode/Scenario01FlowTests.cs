using System;
using System.IO;
using DarkSaver.Core;
using DarkSaver.Events;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class Scenario01FlowTests
    {
        [TestCase(false)] [TestCase(true)]
        public void NonCasterRetreatAndVictorySnapshotsRoundTripWithoutGrantingMp(bool defeated)
        {
            var definition = Content();
            var source = Array.Find(definition.units, u => u.id == 401);
            Assert.AreEqual(-1, source.mp);
            var state = Scenario01Progress.Create(definition);
            state.phase = Scenario01Progress.Playing; state.cursor=0; state.pendingBattle=401;
            var snapshot = new Scenario01EnemyState { id=401, hp=defeated ? 0 : 70, mp=1, x=13, y=30 };
            Assert.IsFalse(Scenario01Rules.RecordBattleReturn(definition,state,401,defeated,snapshot));
            Assert.AreEqual(401,state.pendingBattle); Assert.IsEmpty(state.defeated);
            snapshot.mp=0;
            Assert.IsTrue(Scenario01Rules.RecordBattleReturn(definition,state,401,defeated,snapshot));
            Assert.IsTrue(state.Matches(definition));
            var restored = JsonUtility.FromJson<Scenario01Progress>(JsonUtility.ToJson(state));
            Assert.IsTrue(restored.Matches(definition)); Assert.AreEqual(0,restored.enemies[0].mp);
            Assert.AreEqual(defeated,restored.defeated.Contains(401)); Assert.AreEqual(-1,source.mp);
        }

        private static Scenario01Definition Content()
        {
            var asset = Resources.Load<TextAsset>("Scenarios/01/Content"); Assert.That(asset, Is.Not.Null);
            var definition = JsonUtility.FromJson<Scenario01Definition>(asset.text);
            Assert.That(definition.IsValid, Is.True); return definition;
        }

        [Test]
        public void OriginalIntroAndEndingResumeAtEveryCommandWithNativeJsonAndDiskSave()
        {
            var definition = Content(); var state = Scenario01Progress.Create(definition); var inventory = new NativeInventory();
            var directory = Path.Combine(Path.GetTempPath(), "Scenario01-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); var store = new NativeSaveStore(Path.Combine(directory, "save.json"));
            try
            {
                void Run()
                {
                    int limit = 0;
                    while ((state.phase == Scenario01Progress.Intro || state.phase == Scenario01Progress.Outro || state.activeEvent != 0) && limit++ < 1000)
                    {
                        var result = Scenario01Rules.Step(definition, state, inventory, 100, out var next, out var items);
                        if (result == Scenario01Rules.StepResult.Waiting)
                        {
                            if (Scenario01Rules.AdvanceDialogue(definition, state, out var page)) state = page;
                            else Scenario01Rules.AdvanceTime(state, 1, 6);
                        }
                        else { state = next; inventory = items; }
                        var save = new NativeSaveData { format = NativeSaveStore.Format, version = NativeSaveStore.ScenarioVersion,
                            mapScene = "Brugos01_Map", runtimeScene = "Brugos01_Runtime", hasScenario01 = true, scenario01 = state,
                            hasInventory = true, inventory = inventory, hasCharacter = true,
                            character = new NativeCharacterProfile { name = "검증", jobId = 100 },
                            clearedScenarios = state.phase == Scenario01Progress.Complete ? new[] { "01" } : Array.Empty<string>() };
                        Assert.That(store.TrySave(save, out var reason), Is.True, reason);
                        Assert.That(store.TryLoad(out var loaded, out reason), Is.True, reason);
                        state = loaded.scenario01; inventory = loaded.inventory;
                        Assert.That(state.Matches(definition), Is.True);
                    }
                    Assert.That(limit, Is.LessThan(1000));
                }
                Run(); Assert.That(state.phase, Is.EqualTo(Scenario01Progress.Playing));
                // This is an explicit boss-death fixture, not a simulated fight.
                state.wave = 2; state.pendingBattle = 700;
                Assert.That(Scenario01Rules.RecordBattleReturn(definition, state, 700, true), Is.True);
                Assert.That(Scenario01Rules.TryActivate(definition, state, inventory, 19, 1, out state, out inventory), Is.True);
                Assert.That(state.activeEvent, Is.EqualTo(99));
                Run(); Assert.That(state.phase, Is.EqualTo(Scenario01Progress.Complete));
                Assert.That(inventory.Count(398), Is.EqualTo(1));
                Assert.That(state.actors.Exists(a => a.id == 801), Is.True);
                Assert.That(state.actors.Exists(a => a.id == 301), Is.False);
                Assert.That(store.TryLoad(out var completed, out _), Is.True);
                Assert.That(completed.clearedScenarios, Does.Contain("01"));
                Assert.That(Scenario01Rules.TryActivate(definition, state, inventory, 19, 1, out _, out _), Is.False);
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void SourceGold399DoesNotBecomeAKeyAndFailedConsumptionIsAtomic()
        {
            var definition = Content(); var state = Scenario01Progress.Create(definition); state.phase = Scenario01Progress.Playing;
            var inventory = new NativeInventory();
            Assert.That(Scenario01Rules.TryActivate(definition, state, inventory, 6, 21, out var next, out var items), Is.True);
            Assert.That(items.gold, Is.EqualTo(399)); Assert.That(items.Count(399), Is.Zero);
            Assert.That(inventory.gold, Is.Zero); Assert.That(state.activatedEvents, Is.Empty);
            state.insideEvents.Clear();
            Assert.That(Scenario01Rules.TryActivate(definition, state, inventory, 14, 5, out _, out _), Is.False);
            Assert.That(inventory.items, Is.Empty);
            Assert.That(next.activatedEvents, Does.Contain(51));
        }
    }
}
