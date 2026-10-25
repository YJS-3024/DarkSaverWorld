using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class BattleMagicRequestTests
    {
        [Test]
        public void TemporaryMultiTargetDeathsReleaseAllCellsWithOneCastCost()
        {
            WithSender((sender, server, receiver, battle) => {
                Assert.That(battle.TryRegisterGroup(new[] { ExtraUnit(810, true, 90) }), Is.True);
                Assert.That(battle.TryConfigureGrid(4, 4, new[] {
                    new CombatGridPlacement(100, new CombatGridCell(0, 0)),
                    new CombatGridPlacement(800, new CombatGridCell(1, 0)),
                    new CombatGridPlacement(810, new CombatGridCell(2, 0)) }, (_, _) => true,
                    (a, b) => Math.Abs(a.X-b.X) + Math.Abs(a.Y-b.Y) == 1), Is.True);
                battle.SetTemporaryLocalRules(true); battle.ConfigureTemporaryDamageMagic(201, 7, 4, 1000, 1); Ready(battle);
                Assert.That(sender.TrySendMagic(new MagicRequest(201, 100, 0, 0, 7, new short[] { 800, 810 }), out _), Is.True);
                foreach (short id in new short[] { 800, 810 }) {
                    Assert.That(battle.TryGetUnit(id, out var unit), Is.True);
                    Assert.That(unit.LifeState, Is.EqualTo(UnitLifeState.Removed));
                }
                Assert.That(battle.TryGetGridOccupant(new CombatGridCell(1, 0), out _), Is.False);
                Assert.That(battle.TryGetGridOccupant(new CombatGridCell(2, 0), out _), Is.False);
                Assert.That(battle.PlayerMp, Is.EqualTo(23)); Assert.That(battle.PlayerAp, Is.EqualTo(7));
            });
        }

        [Test]
        public void TemporaryMultiTargetDamageChargesOnceAndCommitsBeforeCallbacks()
        {
            WithSender((sender, server, receiver, battle) => {
                Assert.That(battle.TryRegisterGroup(new[] { ExtraUnit(810, true, 90) }), Is.True);
                battle.SetTemporaryLocalRules(true);
                Assert.That(battle.ConfigureTemporaryDamageMagic(201, 7, 4, 10, 2), Is.True); Ready(battle);
                int results = 0, manaUpdates = 0, castStarts = 0, legacyStarts = 0;
                MagicRequest presentation = null;
                battle.MagicCastPresentationStarted += cast => { castStarts++; presentation = cast; };
                battle.MagicPresentationStarted += _ => legacyStarts++;
                battle.VitalsDisplayChanged += (id, value, mp) => { if (id == 100 && mp) manaUpdates++; };
                battle.CombatResultApplied += result => {
                    results++;
                    Assert.That(battle.EnemyHp, Is.LessThan(75));
                    Assert.That(battle.TryGetUnit(810, out var other), Is.True);
                    Assert.That(other.Hp, Is.LessThan(90));
                };
                var packet = new MagicRequest(201, 100, 2, 0, 7, new short[] { 800, 810, 800 });
                Assert.That(sender.TrySendMagic(packet, out _), Is.True);
                Assert.That(castStarts, Is.EqualTo(1)); Assert.That(legacyStarts, Is.Zero);
                Assert.That(presentation, Is.SameAs(packet));
                Assert.That((presentation.X, presentation.Y), Is.EqualTo(((short)2, (short)0)));
                Assert.That(sender.TrySendMagic(packet, out _), Is.False); // AP is not ready.
                Assert.That(castStarts, Is.EqualTo(1));
                Assert.That(results, Is.EqualTo(2)); Assert.That(manaUpdates, Is.EqualTo(1));
                Assert.That(battle.PlayerMp, Is.EqualTo(23)); Assert.That(battle.PlayerAp, Is.EqualTo(7));
                Assert.That(sender.LastMagicRequest.TargetUnitIds, Is.EqualTo(new short[] { 800, 810, 800 }));
            });
        }

        [Test]
        public void TemporaryPartyHealingAndBuffsShareOneCastCost()
        {
            WithSender((sender, server, receiver, battle) => {
                Assert.That(battle.TryRegisterGroup(new[] { ExtraUnit(110, false, 100) }), Is.True);
                battle.SetTemporaryLocalRules(true); battle.ApplyHp(100, 20); battle.ApplyHp(110, 10);
                Assert.That(battle.ConfigureTemporaryHealingMagic(2501, 5, 4, 12), Is.True);
                Assert.That(battle.ConfigureTemporaryBuffMagic(2502, 4, 4, 8, 3, 10), Is.True);
                battle.ConfigureMagicApCost(2501, 2); battle.ConfigureMagicApCost(2502, 2);
                battle.AdvanceActionPoints(14);
                Assert.That(sender.TrySendMagic(new MagicRequest(2501, 100, 0, 0, 5, new short[] { 100, 110, 110 }), out _), Is.True);
                Assert.That(battle.PlayerHp, Is.EqualTo(32));
                battle.TryGetUnit(110, out var healed); Assert.That(healed.Hp, Is.EqualTo(22));
                Assert.That(battle.PlayerMp, Is.EqualTo(25)); Assert.That(battle.PlayerAp, Is.EqualTo(8));
                battle.AdvanceActionPoints(2);
                Assert.That(sender.TrySendMagic(new MagicRequest(2502, 100, 0, 0, 4, new short[] { 100, 110 }), out _), Is.True);
                Assert.That(battle.PlayerMp, Is.EqualTo(21)); Assert.That(battle.PlayerAp, Is.EqualTo(8));
                Assert.That(battle.TryGetEffectiveStats(100, out int attack, out int defense), Is.True);
                battle.TryGetUnit(100, out var caster);
                Assert.That(attack, Is.EqualTo(caster.Attack + 8)); Assert.That(defense, Is.EqualTo(caster.Defense + 3));
                battle.TryGetEffectiveStats(110, out attack, out defense);
                Assert.That(attack, Is.EqualTo(healed.Attack + 8)); Assert.That(defense, Is.EqualTo(healed.Defense + 3));
            });
        }

        [Test]
        public void InvalidMultiTargetCastNeverAppliesAPartialEffectOrCost()
        {
            WithSender((sender, server, receiver, battle) => {
                var distant = ExtraUnit(810, true, 90); distant.X = distant.Y = 20;
                Assert.That(battle.TryRegisterGroup(new[] { distant }), Is.True);
                battle.SetTemporaryLocalRules(true); battle.ConfigureTemporaryDamageMagic(201, 7, 4, 10, 2); Ready(battle);
                int results = 0; battle.CombatResultApplied += _ => results++;
                foreach (var targets in new[] { new short[] { 800, 810 }, new short[] { 800, 999 }, new short[] { 800, 100 } })
                    Assert.That(sender.TrySendMagic(new MagicRequest(201, 100, 0, 0, 7, targets), out _), Is.False);
                Assert.That(battle.EnemyHp, Is.EqualTo(75)); Assert.That(battle.PlayerHp, Is.EqualTo(120));
                Assert.That(battle.PlayerMp, Is.EqualTo(30)); Assert.That(battle.PlayerAp, Is.EqualTo(10));
                Assert.That(results, Is.Zero); Assert.That(sender.LastMagicRequest, Is.Null);
            });
        }

        private static CombatUnit ExtraUnit(short id, bool enemy, int hp) => new CombatUnit {
            Id = id, Enemy = enemy, Name = "Explicit batch fixture", X = 2, Y = 0,
            Hp = hp, MaxHp = hp, Mp = 0, MaxMp = 0, Attack = 1, Defense = 2
        };

        [Test]
        public void NativeReferenceSentinelsChargeActualCasterAndRetainOriginalReference()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                var calls = 0;
                server.RequestReceived += value => {
                    var request = (MagicRequest)value;
                    Assert.That(request.CasterUnitId, Is.EqualTo(-1));
                    Assert.That(request.ActingUnitId, Is.EqualTo(100)); calls++;
                };
                var packet = new MagicRequest(201, -1, 0, 0, -123, new short[] { 800 }, 0x92, null, 100);
                Assert.That(sender.TrySendMagic(packet, out _), Is.True);
                Assert.That(calls, Is.EqualTo(1)); Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3));
                Assert.That(battle.EnemyAp, Is.EqualTo(GameConstants.BattleEnemyMaximumAp)); Assert.That(battle.PlayerMp, Is.EqualTo(30));
                Assert.That(sender.LastMagicRequest.CasterUnitId, Is.EqualTo(-1));
            });
        }

        [Test]
        public void MultiTargetMagicRetainsRawPacketAndChargesConfiguredApOnly()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                var targets = new short[] { 800, 100, 800 };
                var packet = new MagicRequest(201, 100, -2, short.MaxValue, int.MinValue, targets, 0x92, 7);
                targets[0] = 999;
                var order = new List<string>();
                sender.MagicRequestDispatched += value =>
                {
                    order.Add("start"); Assert.That(value, Is.SameAs(packet));
                    Assert.That(sender.LastMagicRequest, Is.SameAs(value));
                    Assert.That(value.TargetUnitIds, Is.EqualTo(new short[] { 800, 100, 800 }));
                };
                server.RequestReceived += value =>
                {
                    order.Add("server"); Assert.That(value, Is.Not.SameAs(packet));
                    var decoded = (MagicRequest)value;
                    Assert.That((decoded.MagicId, decoded.ActingUnitId, decoded.X, decoded.Y, decoded.ComputedCost),
                        Is.EqualTo((packet.MagicId, packet.ActingUnitId, packet.X, packet.Y, packet.ComputedCost)));
                    Assert.That(decoded.TargetUnitIds, Is.EqualTo(packet.TargetUnitIds));
                    Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3)); Assert.That(battle.PlayerMp, Is.EqualTo(30));
                    Assert.That(server.SendToClient(new MagicEffectNotification(201, 100, -2, short.MaxValue, int.MaxValue, new short[] { 800, 100, 800 })), Is.True);
                };
                receiver.MagicEffectReceived += _ => order.Add("effect");
                Assert.That(sender.TrySendMagic(packet, out _), Is.True);
                Assert.That(order, Is.EqualTo(new[] { "start", "server", "effect" }));
                Assert.That((packet.ComputedCost, packet.OriginalOpcode, packet.Mode), Is.EqualTo((int.MinValue, (byte?)0x92, (byte?)7)));
                Assert.That((battle.PlayerHp, battle.PlayerMp, battle.EnemyHp, battle.PlayerExperience), Is.EqualTo((120, 30, 75, 0u)));
                Assert.That(battle.LastAttackPresentation, Is.Null);
            });
        }

        [Test]
        public void MissingHandlerAndInvalidMagicDoNotSpendOrPublish()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                var valid = new MagicRequest(201, 100, 0, 0, 0, Array.Empty<short>());
                Assert.That(sender.TrySendMagic(valid, out _), Is.False);
                var calls = 0; server.RequestReceived += _ => calls++;
                foreach (var packet in new[] { null,
                    new MagicRequest(0, 100, 0, 0, 0, Array.Empty<short>()),
                    new MagicRequest(202, 100, 0, 0, 0, Array.Empty<short>()),
                    new MagicRequest(201, 999, 0, 0, 0, Array.Empty<short>()),
                    new MagicRequest(201, 100, 0, 0, 0, new short[] { 800, 999 }) })
                    Assert.That(sender.TrySendMagic(packet, out _), Is.False);
                Assert.That(calls, Is.Zero); Assert.That(sender.LastMagicRequest, Is.Null);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                battle.ApplyDeath(800, true);
                Assert.That(sender.TrySendMagic(new MagicRequest(201, 100, 0, 0, 0, new short[] { 800 }), out _), Is.False);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
            });
        }

        [Test]
        public void PositionRequestsAllowEmptyTargetsAndBlockReentryWithoutInventingResponse()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                var calls = 0; var packet = new MagicRequest(201, 100, -4, -5, int.MaxValue, Array.Empty<short>());
                server.RequestReceived += request =>
                {
                    calls++; Assert.That(sender.TrySendMagic(packet, out _), Is.False);
                    Assert.That(sender.TrySendAttack(new AttackRequest(800, 100, 0, 0), out _), Is.False);
                };
                Assert.That(sender.TrySendMagic(packet, out _), Is.True);
                Assert.That(calls, Is.EqualTo(1)); Assert.That(sender.LastMagicRequest.TargetCount, Is.Zero);
                Assert.That(receiver.LastMagicEffect, Is.Null); Assert.That(battle.PlayerMp, Is.EqualTo(30));
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3));
                Assert.That(sender.TrySendMagic(packet, out _), Is.False); // No AP reservation/queue.
            });
        }

        [Test]
        public void MpIsOnlyChangedBySeparateAbsoluteResponse()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                server.RequestReceived += packet =>
                {
                    Assert.That(battle.PlayerMp, Is.EqualTo(30));
                    Assert.That(server.SendToClient(new MpSetNotification(100, 7)), Is.True);
                };
                Assert.That(sender.TrySendMagic(new MagicRequest(201, 100, 0, 0, -123, new short[] { 100 }), out _), Is.True);
                Assert.That(battle.PlayerMp, Is.EqualTo(7));
                Assert.That(battle.PlayerHp, Is.EqualTo(120)); Assert.That(battle.PlayerExperience, Is.Zero);
            });
        }

        [Test]
        public void HandlerFailureDoesNotRetryOrRefundAndDisabledSenderDoesNotStart()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                var calls = 0; var packet = new MagicRequest(201, 100, 0, 0, 0, Array.Empty<short>());
                server.RequestReceived += _ => { calls++; throw new InvalidOperationException("fixture"); };
                sender.enabled = false; Assert.That(sender.TrySendMagic(packet, out _), Is.False);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp)); Assert.That(sender.LastMagicRequest, Is.Null);
                sender.enabled = true;
                Assert.Throws<InvalidOperationException>(() => sender.TrySendMagic(packet, out _));
                Assert.That(calls, Is.EqualTo(1)); Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3));
                Assert.That(battle.PlayerMp, Is.EqualTo(30)); Assert.That(receiver.LastMagicEffect, Is.Null);
            });
        }

        private static void Ready(BattleSceneRoot battle)
        {
            Assert.That(battle.ConfigureMagicApCost(201, 3), Is.True); // Explicit fixture, not original cost.
            Assert.That(battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp), Is.True);
        }

        private static void WithSender(Action<BattleRequestSender, ServerManager, BattlePacketReceiver, BattleSceneRoot> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Magic request check"); SceneManager.MoveGameObjectToScene(root, scene);
                var sender = root.AddComponent<BattleRequestSender>();
                var receiver = root.AddComponent<BattlePacketReceiver>(); Assert.That(receiver.Bind(), Is.True);
                check(sender, root.GetComponent<ServerManager>(), receiver, root.GetComponent<BattleSceneRoot>());
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
