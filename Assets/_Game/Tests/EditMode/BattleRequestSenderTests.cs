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
    public sealed class BattleRequestSenderTests
    {
        [Test]
        public void TemporaryLoopbackResolvesOnceWithoutHandlerAndCanBeDisabledRemainsDisabledWithoutServerHandler()
        {
            WithSender((sender,server,receiver,battle)=>{
                battle.SetTemporaryLocalRules(true);Assert.IsFalse(battle.TemporaryLocalRulesEnabled);
                Assert.IsTrue(battle.ConfigureActionPointCosts(2,6));Assert.IsTrue(battle.ConfigureMagicApCost(101,6));NativeCombatStateFixture.ReceiveFullTicks(battle);
                var before=(battle.PlayerHp,battle.PlayerMp,battle.PlayerAp,battle.EnemyHp);
                Assert.IsFalse(sender.TrySendAttack(new AttackRequest(800,100,0,0),out _));
                Assert.AreEqual(before,(battle.PlayerHp,battle.PlayerMp,battle.PlayerAp,battle.EnemyHp));Assert.IsNull(battle.LastAttackPresentation);
            });
        }

        [Test]
        public void TemporaryMagicLoopbackSpendsApMpOnceAndRemovesDeadUnitImmediatelyRemainsDisabledWithoutServerHandler()
        {
            WithSender((sender,server,receiver,battle)=>{
                battle.SetTemporaryLocalRules(true);Assert.IsFalse(battle.TemporaryLocalRulesEnabled);
                Assert.IsTrue(battle.ConfigureActionPointCosts(2,6));Assert.IsTrue(battle.ConfigureMagicApCost(101,6));NativeCombatStateFixture.ReceiveFullTicks(battle);
                var before=(battle.PlayerHp,battle.PlayerMp,battle.PlayerAp,battle.EnemyHp);
                Assert.IsFalse(sender.TrySendMagic(new MagicRequest(101,100,0,0,7,new short[]{800}),out _));
                Assert.AreEqual(before,(battle.PlayerHp,battle.PlayerMp,battle.PlayerAp,battle.EnemyHp));Assert.IsNull(battle.LastAttackPresentation);
            });
        }

        [Test]
        public void DispatchStartsAnticipationBeforeImmediateResponseWithoutLocalDamage()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                var order = new List<string>();
                var packet = new AttackRequest(800, 100, -1, short.MaxValue);
                battle.AttackPresentationStarted += p => order.Add(p.IsAnticipation ? "start" : "effect");
                server.RequestReceived += value =>
                {
                    order.Add("server"); Assert.That(value, Is.Not.SameAs(packet));
                    var decoded = (AttackRequest)value;
                    Assert.That((decoded.TargetUnitId, decoded.AttackerUnitId, decoded.TargetTileAttribute, decoded.AttackerTileAttribute),
                        Is.EqualTo((packet.TargetUnitId, packet.AttackerUnitId, packet.TargetTileAttribute, packet.AttackerTileAttribute)));
                    Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 2));
                    Assert.That(battle.EnemyHp, Is.EqualTo(75));
                    Assert.That(packet.TargetTileAttribute, Is.EqualTo(-1));
                    Assert.That(packet.AttackerTileAttribute, Is.EqualTo(short.MaxValue));
                    Assert.That(server.SendToClient(new AttackEffectNotification(800, 100, -3)), Is.True);
                };
                Assert.That(sender.TrySendAttack(packet, out _), Is.True);
                Assert.That(order, Is.EqualTo(new[] { "start", "server", "effect" }));
                Assert.That(battle.LastAttackPresentation.Value.IsAnticipation, Is.False);
                Assert.That(battle.LastAttackPresentation.Value.Effect, Is.EqualTo(-3));
                Assert.That((battle.PlayerHp, battle.PlayerMp, battle.EnemyHp, battle.PlayerExperience), Is.EqualTo((120, 30, 75, 0u)));
            });
        }

        [Test]
        public void MissingOrDisabledHandlerDoesNotSpendApOrStartPresentation()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                var packet = new AttackRequest(800, 100, 0, 0);
                Assert.That(sender.TrySendAttack(packet, out var reason), Is.False); Assert.That(reason, Is.Not.Empty);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp)); Assert.That(battle.LastAttackPresentation, Is.Null);
                server.RequestReceived += _ => Assert.Fail("Disabled server received request");
                server.enabled = false;
                Assert.That(sender.TrySendAttack(packet, out _), Is.False);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp)); Assert.That(battle.LastAttackPresentation, Is.Null);
            });
        }

        [Test]
        public void InvalidOrUnreadyRequestsNeverReachServer()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                NativeCombatStateFixture.Initialize(battle);
                var requests = 0; server.RequestReceived += _ => requests++;
                var valid = new AttackRequest(800, 100, 0, 0);
                Assert.That(sender.TrySendAttack(valid, out _), Is.False); // Costs unknown.
                Assert.IsTrue(battle.ConfigureActionPointCosts(2, 6));
                Assert.That(sender.TrySendAttack(valid, out _), Is.False); // Received AP0.
                NativeCombatStateFixture.SetAp(battle,100,20,20);
                foreach (var packet in new[] { null, new AttackRequest(999, 100, 0, 0),
                    new AttackRequest(800, 701, 0, 0), new AttackRequest(100, 100, 0, 0) })
                    Assert.That(sender.TrySendAttack(packet, out _), Is.False);
                sender.enabled = false; Assert.That(sender.TrySendAttack(valid, out _), Is.False);
                sender.enabled = true;
                battle.ApplyDeath(800, true); Assert.That(sender.TrySendAttack(valid, out _), Is.False);
                Assert.That(requests, Is.Zero); Assert.That(battle.PlayerAp, Is.EqualTo(20));
                Assert.That(battle.LastAttackPresentation, Is.Null);
            });
        }

        [Test]
        public void ReentrantRequestIsRejectedAndNoResponseLeavesOnlyAnticipation()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                var calls = 0; var packet = new AttackRequest(800, 100, 0, 0);
                server.RequestReceived += request =>
                {
                    calls++; Assert.That(sender.TrySendAttack(packet, out _), Is.False);
                };
                Assert.That(sender.TrySendAttack(packet, out _), Is.True);
                Assert.That(calls, Is.EqualTo(1)); Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 2));
                Assert.That(battle.LastAttackPresentation.Value.IsAnticipation, Is.True);
                Assert.That(receiver.LastAttackEffect, Is.Null); Assert.That(battle.EnemyHp, Is.EqualTo(75));
            });
        }

        [Test]
        public void HandlerFailurePropagatesWithoutRetryRefundOrLocalDamage()
        {
            WithSender((sender, server, receiver, battle) =>
            {
                Ready(battle);
                var calls = 0; var packet = new AttackRequest(800, 100, 0, 0);
                Action<ClientPacket> fail = _ => { calls++; throw new InvalidOperationException("fixture"); };
                server.RequestReceived += fail;
                Assert.Throws<InvalidOperationException>(() => sender.TrySendAttack(packet, out _));
                Assert.That(calls, Is.EqualTo(1)); Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 2));
                Assert.That(battle.EnemyHp, Is.EqualTo(75)); Assert.That(receiver.LastAttackEffect, Is.Null);
                server.RequestReceived -= fail;
                Assert.That(battle.AdvanceActionPoints(2f), Is.False);
                Assert.That(NativeCombatStateFixture.ReceiveFullTicks(battle), Is.True);
                Assert.That(sender.TrySendAttack(packet, out _), Is.False); // Guard reset, no handler.
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
            });
        }

        private static void Ready(BattleSceneRoot battle)
        {
            Assert.That(battle.ConfigureActionPointCosts(2, 6), Is.True); // Fixture only.
            Assert.That(NativeCombatStateFixture.ReceiveFullTicks(battle), Is.True);
        }

        private static void WithSender(Action<BattleRequestSender, ServerManager, BattlePacketReceiver, BattleSceneRoot> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Battle request check"); SceneManager.MoveGameObjectToScene(root, scene);
                var sender = root.AddComponent<BattleRequestSender>();
                var receiver = root.AddComponent<BattlePacketReceiver>(); NativeCombatStateFixture.Initialize(root.GetComponent<BattleSceneRoot>(),6); Assert.That(receiver.Bind(), Is.True);
                check(sender, root.GetComponent<ServerManager>(), receiver, root.GetComponent<BattleSceneRoot>());
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
