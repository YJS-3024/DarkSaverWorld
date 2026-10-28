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
    public sealed class BattleEffectPacketTests
    {
        [Test]
        public void AttackPublishesRawEffectBeforeHandledAndNeverAppliesDamage()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var hp = battle.PlayerHp; var mp = battle.PlayerMp; var enemyHp = battle.EnemyHp;
                var ap = battle.PlayerAp; var enemyAp = battle.EnemyAp;
                var order = new List<string>();
                var packet = new AttackEffectNotification(800, 100, short.MinValue);
                receiver.AttackEffectReceived += value =>
                {
                    Assert.That((value.TargetUnitId,value.AttackerUnitId,value.Effect),Is.EqualTo((packet.TargetUnitId,packet.AttackerUnitId,packet.Effect)));
                    Assert.That(receiver.LastAttackEffect, Is.SameAs(value));
                    order.Add("effect");
                };
                receiver.PacketHandled += (_, applied) => { Assert.That(applied, Is.True); order.Add("handled"); };
                Assert.That(server.SendToClient(packet), Is.True);
                Assert.That(server.SendToClient(packet), Is.True); // No invented sequence-ID deduplication.
                Assert.That(order, Is.EqualTo(new[] { "effect", "handled", "effect", "handled" }));
                Assert.That(receiver.LastAttackEffect.Effect, Is.EqualTo(short.MinValue));
                Assert.That(receiver.LastMagicEffect, Is.Null);
                Assert.That((battle.PlayerHp, battle.PlayerMp, battle.EnemyHp, battle.PlayerAp, battle.EnemyAp),
                    Is.EqualTo((hp, mp, enemyHp, ap, enemyAp)));
                Assert.That(battle.PlayerExperience, Is.Zero);
                Assert.That(battle.VictoryAchieved, Is.False);
            });
        }

        [Test]
        public void MagicPreservesUninterpretedFieldsAndOwnedTargetOrderIncludingDuplicates()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var targets = new short[] { 800, 100, 800 };
                var packet = new MagicEffectNotification(short.MinValue, 100, -2, short.MaxValue, int.MinValue, targets);
                targets[0] = 999;
                var calls = 0;
                receiver.MagicEffectReceived += value =>
                {
                    calls++;
                    Assert.That(value, Is.SameAs(packet));
                    Assert.That(receiver.LastMagicEffect, Is.SameAs(value));
                    Assert.That(value.TargetUnitIds, Is.EqualTo(new short[] { 800, 100, 800 }));
                };
                Assert.That(server.SendToClient(packet), Is.True);
                Assert.That(calls, Is.EqualTo(1));
                Assert.That((packet.MagicId, packet.X, packet.Y, packet.Value), Is.EqualTo((short.MinValue, (short)-2, short.MaxValue, int.MinValue)));
                Assert.That((battle.PlayerHp, battle.PlayerMp, battle.EnemyHp), Is.EqualTo((120, 30, 75)));
                Assert.That(receiver.LastAttackEffect, Is.Null);
            });
        }

        [Test]
        public void InvalidReferencesRejectWholeNotificationWithoutReplacingLastEffect()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var attack = new AttackEffectNotification(800, 100, 5);
                var magic = new MagicEffectNotification(101, 100, 0, 0, 8, new short[] { 800 });
                Assert.That(receiver.TryApplyPacket(attack), Is.True);
                Assert.That(receiver.TryApplyPacket(magic), Is.True);
                var calls = 0;
                receiver.AttackEffectReceived += _ => calls++; receiver.MagicEffectReceived += _ => calls++;
                foreach (var packet in new ServerPacket[] {
                    new AttackEffectNotification(999, 100, 1), new AttackEffectNotification(800, 701, 1),
                    new MagicEffectNotification(101, 999, 0, 0, 8, Array.Empty<short>()),
                    new MagicEffectNotification(101, 100, 0, 0, 8, new short[] { 800, 999, 100 }) })
                    Assert.That(receiver.TryApplyPacket(packet), Is.False);
                Assert.That(calls, Is.Zero);
                Assert.That(receiver.LastAttackEffect, Is.SameAs(attack));
                Assert.That(receiver.LastMagicEffect, Is.SameAs(magic));
            });
        }

        [Test]
        public void PositionalMagicAllowsEmptyTargetsAndDelayedEffectsNeverReviveUnits()
        {
            WithReceiver((receiver, server, battle) =>
            {
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(101, 100, -2, -3, int.MaxValue, Array.Empty<short>())), Is.True);
                Assert.That(receiver.LastMagicEffect.TargetCount, Is.Zero);
                Assert.That(receiver.TryApplyPacket(new HpSetNotification(800, 0)), Is.True);
                Assert.That(receiver.TryApplyPacket(new AttackEffectNotification(800, 100, 1)), Is.True);
                Assert.That(battle.VictoryAchieved, Is.False);
                Assert.That(receiver.TryApplyPacket(new UnitCorpseNotification(800)), Is.True);
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(101, 100, 0, 0, 1, new short[] { 800 })), Is.True);
                Assert.That(battle.EnemyLifeState, Is.EqualTo(UnitLifeState.Corpse));
                Assert.That(receiver.TryApplyPacket(new UnitRemoveNotification(800)), Is.True);
                Assert.That(receiver.TryApplyPacket(new AttackEffectNotification(800, 100, 1)), Is.False);
                Assert.That(receiver.TryApplyPacket(new AttackEffectNotification(100, 800, 1)), Is.False);
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(101, 100, 0, 0, 1, new short[] { 800 })), Is.False);
                Assert.That(receiver.TryApplyPacket(new MagicEffectNotification(101, 800, 0, 0, 1, Array.Empty<short>())), Is.False);
                Assert.That(battle.EnemyHp, Is.Zero);
                Assert.That(battle.EnemyLifeState, Is.EqualTo(UnitLifeState.Removed));
                Assert.That(battle.PlayerExperience, Is.Zero);
            });
        }

        [Test]
        public void UnboundOrDisabledReceiverDoesNotPublishOrReplayEffects()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var calls = 0; receiver.AttackEffectReceived += _ => calls++;
                var packet = new AttackEffectNotification(800, 100, 3);
                receiver.Unbind();
                Assert.That(server.SendToClient(packet), Is.False);
                receiver.enabled = false;
                Assert.That(receiver.TryApplyPacket(packet), Is.False);
                Assert.That(receiver.LastAttackEffect, Is.Null);
                receiver.enabled = true;
                Assert.That(receiver.Bind(), Is.True);
                Assert.That(server.SendToClient(packet), Is.True);
                receiver.Unbind(); Assert.That(receiver.Bind(), Is.True);
                Assert.That(calls, Is.EqualTo(1)); // No replay on rebinding.
            });
        }

        [Test]
        public void EffectCallbackFailureIsNotAcknowledgedOrRetried()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var calls = 0; var handled = 0;
                receiver.AttackEffectReceived += _ => { calls++; throw new InvalidOperationException("fixture callback"); };
                receiver.PacketHandled += (_, _) => handled++;
                var packet = new AttackEffectNotification(800, 100, 3);
                Assert.Throws<InvalidOperationException>(() => server.SendToClient(packet));
                Assert.That(calls, Is.EqualTo(1)); Assert.That(handled, Is.Zero);
                Assert.That(receiver.LastAttackEffect.Effect, Is.EqualTo(packet.Effect));
                Assert.That(battle.EnemyHp, Is.EqualTo(75));
            });
        }

        private static void WithReceiver(Action<BattlePacketReceiver, ServerManager, BattleSceneRoot> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Battle effect check"); SceneManager.MoveGameObjectToScene(root, scene);
                var receiver = root.AddComponent<BattlePacketReceiver>(); NativeCombatStateFixture.Initialize(root.GetComponent<BattleSceneRoot>(),6);
                Assert.That(receiver.Bind(), Is.True);
                check(receiver, root.GetComponent<ServerManager>(), root.GetComponent<BattleSceneRoot>());
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
