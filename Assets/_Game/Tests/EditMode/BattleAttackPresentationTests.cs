using System;
using DarkSaver.Combat;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class BattleAttackPresentationTests
    {
        [Test]
        public void ReceivedAttackRecordsBothDirectionsBeforeReceiverEventWithoutChangingStats()
        {
            WithBattle((receiver, battle, enemy) =>
            {
                var calls = 0;
                battle.AttackPresentationStarted += p =>
                {
                    calls++; Assert.That(p.IsAnticipation, Is.False);
                    Assert.That(p.AttackerFacing, Is.EqualTo(new Vector2Int(1, -1)));
                    Assert.That(p.TargetFacing, Is.EqualTo(new Vector2Int(-1, 1)));
                };
                receiver.AttackEffectReceived += packet =>
                    Assert.That(battle.LastAttackPresentation.Value.Effect, Is.EqualTo(packet.Effect));
                var ap = battle.PlayerAp;
                Assert.That(receiver.TryApplyPacket(new AttackEffectNotification(700, 100, -3)), Is.True);
                var value = battle.LastAttackPresentation.Value;
                Assert.That((value.AttackerId, value.TargetId, value.Effect), Is.EqualTo(((short)100, (short)700, (short)-3)));
                Assert.That(calls, Is.EqualTo(1)); Assert.That(value.HasDirection, Is.True);
                Assert.That((battle.PlayerHp, battle.PlayerMp, battle.EnemyHp, battle.PlayerAp), Is.EqualTo((120, 30, 450, ap)));
                Assert.That(battle.PlayerExperience, Is.Zero);
                Assert.That(battle.TryGetLatestAttacker(700, out var attacker), Is.True);
                Assert.That(attacker, Is.EqualTo(100));
            });
        }

        [Test]
        public void DispatchedAttackAnticipationPrecedesIndependentHpResponse()
        {
            WithBattle((receiver,battle,enemy)=>{
                var sender=battle.gameObject.AddComponent<BattleRequestSender>();var server=battle.GetComponent<ServerManager>();
                int calls=0,hpAtStart=0;battle.AttackPresentationStarted+=p=>{calls++;hpAtStart=battle.EnemyHp;Assert.IsTrue(p.IsAnticipation);};
                Assert.IsFalse(sender.TrySendAttack(new AttackRequest(700,100,0,0),out _));Assert.IsNull(battle.LastAttackPresentation);
                Assert.IsTrue(battle.ConfigureActionPointCosts(2,6));NativeCombatStateFixture.ReceiveFullTicks(battle);
                Assert.IsTrue(battle.ApplyHp(700,1));
                server.RequestReceived+=packet=>{Assert.IsInstanceOf<AttackRequest>(packet);Assert.AreEqual(1,calls);server.SendToClient(new HpSetNotification(700,0));};
                Assert.IsTrue(sender.TrySendAttack(new AttackRequest(700,100,0,0),out _));
                Assert.AreEqual(1,calls);Assert.AreEqual(1,hpAtStart);Assert.AreEqual(0,battle.EnemyHp);
                Assert.AreEqual(UnitLifeState.Active,battle.EnemyLifeState);Assert.IsNull(receiver.LastAttackEffect);
            });
        }

        [Test]
        public void SameTileHasUnknownDirectionAndReverseAttackReversesFacing()
        {
            WithBattle((receiver, battle, enemy) =>
            {
                Assert.That(battle.ApplyAttackPresentation(100, 100, 0), Is.True);
                Assert.That(battle.TryGetLatestAttacker(100, out _), Is.False);
                Assert.That(battle.LastAttackPresentation.Value.HasDirection, Is.False);
                Assert.That(battle.ApplyAttackPresentation(700, 100, 1), Is.True);
                var forward = battle.LastAttackPresentation.Value.AttackerFacing;
                Assert.That(battle.ApplyAttackPresentation(100, 700, 1), Is.True);
                Assert.That(battle.LastAttackPresentation.Value.AttackerFacing, Is.EqualTo(-forward));
                Assert.That(battle.TryGetLatestAttacker(100, out var attacker), Is.True);
                Assert.That(attacker, Is.EqualTo(700));
            });
        }

        [Test]
        public void InvalidOrRemovedReferencesDoNotReplacePresentationOrResurrect()
        {
            WithBattle((receiver, battle, enemy) =>
            {
                Assert.That(battle.ApplyAttackPresentation(700, 100, 5), Is.True);
                var prior = battle.LastAttackPresentation;
                var calls = 0; battle.AttackPresentationStarted += _ => calls++;
                Assert.That(battle.ApplyAttackPresentation(701, 100, 1), Is.False);
                Assert.That(battle.ApplyDeath(700, false), Is.True);
                Assert.That(battle.ApplyAttackPresentation(700, 100, 1), Is.False);
                Assert.That(battle.LastAttackPresentation, Is.EqualTo(prior));
                Assert.That(calls, Is.Zero);
                Assert.That(battle.EnemyHp, Is.EqualTo(450),"removal alone does not contain an HP update");
                Assert.That(battle.EnemyLifeState, Is.EqualTo(UnitLifeState.Removed));
                Assert.That(battle.TryGetLatestAttacker(700, out _), Is.False);
            });
        }

        private static void WithBattle(Action<BattlePacketReceiver, BattleSceneRoot, BattleEnemyDefinition> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var enemy = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>("Assets/_Game/Runtime/Combat/Brugos01Kisra.asset");
            try
            {
                var root = new GameObject("Attack presentation check"); SceneManager.MoveGameObjectToScene(root, scene);
                var receiver = root.AddComponent<BattlePacketReceiver>(); NativeCombatStateFixture.Initialize(root.GetComponent<BattleSceneRoot>(),6);
                Assert.That(receiver.Bind(), Is.True);
                var battle = root.GetComponent<BattleSceneRoot>();
                Assert.That(battle.ConfigureEnemy(enemy), Is.True);
                NativeCombatStateFixture.SetAp(battle,700,20,6);
                check(receiver, battle, enemy);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
