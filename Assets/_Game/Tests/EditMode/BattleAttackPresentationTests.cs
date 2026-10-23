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
                Assert.That((battle.PlayerHp, battle.PlayerMp, battle.EnemyHp, battle.PlayerAp), Is.EqualTo((120, 30, 300, ap)));
                Assert.That(battle.PlayerExperience, Is.Zero);
                Assert.That(battle.TryGetLatestAttacker(700, out var attacker), Is.True);
                Assert.That(attacker, Is.EqualTo(100));
            });
        }

        [Test]
        public void AcceptedLocalAttackPublishesAnticipationBeforeApplyingDamage()
        {
            WithBattle((receiver, battle, enemy) =>
            {
                var calls = 0; var hpAtStart = 0;
                battle.AttackPresentationStarted += p =>
                {
                    calls++; hpAtStart = battle.EnemyHp;
                    Assert.That(p.IsAnticipation, Is.True); Assert.That(p.Effect, Is.Zero);
                    Assert.That((p.AttackerId, p.TargetId), Is.EqualTo(((short)100, (short)700)));
                };
                battle.PlayerAttack(); // Missing AP costs: no presentation.
                Assert.That(calls, Is.Zero); Assert.That(battle.LastAttackPresentation, Is.Null);
                battle.ConfigureActionPointCosts(2, 6); // Test-only opt-in, not production costs.
                battle.PlayerAttack(); // AP below maximum: still no presentation.
                Assert.That(calls, Is.Zero);
                Assert.That(battle.ApplyHp(700, 1), Is.True);
                battle.AdvanceActionPoints(GameConstants.BattleHighestMaximumAp - GameConstants.BattleStartingAp); battle.PlayerAttack();
                Assert.That(calls, Is.EqualTo(1)); Assert.That(hpAtStart, Is.EqualTo(1));
                Assert.That(battle.EnemyHp, Is.Zero);
                Assert.That(battle.LastAttackPresentation.Value.IsAnticipation, Is.True);
                Assert.That(receiver.LastAttackEffect, Is.Null); // Not a fabricated server notification.
                Assert.That(battle.TryGetLatestAttacker(700, out _), Is.False);
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
                Assert.That(battle.EnemyHp, Is.Zero);
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
                var receiver = root.AddComponent<BattlePacketReceiver>();
                Assert.That(receiver.Bind(), Is.True);
                var battle = root.GetComponent<BattleSceneRoot>();
                Assert.That(battle.ConfigureEnemy(enemy), Is.True);
                check(receiver, battle, enemy);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
