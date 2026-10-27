using System;
using System.Reflection;
using DarkSaver.Combat;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class OriginalCombatBoundaryTests
    {
        private static void WithBattle(Action<BattleSceneRoot,ServerManager,BattlePacketReceiver> test)
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("original combat protocol fixture");
                SceneManager.MoveGameObjectToScene(go,scene);
                var battle=go.AddComponent<BattleSceneRoot>();
                battle.BindOriginalProtocol();
                // Explicit server-state fixture, never production initial stats.
                var state=(CombatWorld)typeof(BattleSceneRoot).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle);
                var actor=state.Get(100);actor.Hp=actor.MaxHp=120;actor.Mp=actor.MaxMp=30;
                actor.ActionPoints.ConfigureMaximum(10);
                var enemy=state.Get(800);enemy.Hp=enemy.MaxHp=75;enemy.ActionPoints.ConfigureMaximum(20);
                test(battle,go.GetComponent<ServerManager>(),go.GetComponent<BattlePacketReceiver>());
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }

        [Test]
        public void DefaultHasNoSimulatorAndCannotManufactureCombatOrRewards()
        {
            WithBattle((battle,server,receiver)=>{
                Assert.IsFalse(GameConstants.BattleUseTemporaryLocalRules);
                Assert.IsFalse(battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(server.LocalServer.HasHandler(new AttackRequest(800,100,0,0)));
                var before=(battle.PlayerHp,battle.PlayerMp,battle.EnemyHp,battle.PlayerExperience);
                battle.PlayerAttack();battle.PlayerMagic();
                Assert.IsFalse(battle.TrySubmitLocalRequest(new BattleSimulationTickRequest(1,1),out _));
                Assert.IsFalse(battle.TryAwardDefeatExperience());
                Assert.AreEqual(before,(battle.PlayerHp,battle.PlayerMp,battle.EnemyHp,battle.PlayerExperience));
                Assert.IsFalse(battle.IsPrototypeHudVisible);
            });
        }

        [Test]
        public void RemovalDoesNotInventHpExperienceVictoryOrScenarioCompletion()
        {
            WithBattle((battle,server,receiver)=>{
                int oldHp=battle.EnemyHp;
                Assert.IsTrue(receiver.TryApplyPacket(new UnitCorpseNotification(800)));
                Assert.AreEqual(oldHp,battle.EnemyHp);
                Assert.IsTrue(receiver.TryApplyPacket(new UnitRemoveNotification(800)));
                Assert.AreEqual(oldHp,battle.EnemyHp);
                Assert.AreEqual(0u,battle.PlayerExperience);
                Assert.IsFalse(battle.TryAwardDefeatExperience());
                Assert.IsFalse(battle.VictoryAchieved);Assert.IsFalse(battle.IsBattleFinished);
                Assert.IsFalse(battle.ShowsEncounterOutcome);Assert.IsEmpty(battle.ClearedScenarioKey);
                Assert.IsTrue(receiver.TryApplyPacket(new HpSetNotification(800,0)));
                Assert.AreEqual(0,battle.EnemyHp);
                Assert.IsTrue(receiver.TryApplyPacket(new ExperienceSetNotification(100,1234)));
                Assert.IsTrue(receiver.TryApplyPacket(new ExperienceSetNotification(100,1234)));
                Assert.AreEqual(1234u,battle.PlayerExperience);
            });
        }

        [Test]
        public void OriginalRequestCrossesServerManagerAndOnlyResponseChangesVitals()
        {
            WithBattle((battle,server,receiver)=>{
                // Explicit test AP cost. This is not a production or original-server formula.
                Assert.IsTrue(battle.ConfigureActionPointCosts(2,6));
                Assert.IsTrue(battle.ApplyTickMenu(100,10,0)); // Explicit server-tick fixture.
                int oldHp=battle.EnemyHp;int calls=0;
                server.RequestReceived+=packet=>{
                    calls++;Assert.IsInstanceOf<AttackRequest>(packet);
                    Assert.AreEqual(oldHp,battle.EnemyHp);Assert.AreEqual(0u,battle.PlayerExperience);
                    // Explicit recorded-result fixture, never computed damage or XP.
                    server.SendToClient(new HpSetNotification(800,0));
                    server.SendToClient(new UnitRemoveNotification(800));
                    server.SendToClient(new ExperienceSetNotification(100,456));
                };
                var sender=battle.gameObject.AddComponent<BattleRequestSender>();
                Assert.IsTrue(sender.TrySendAttack(new AttackRequest(800,100,-1,short.MaxValue),out _));
                Assert.AreEqual(1,calls);Assert.AreEqual(0,battle.EnemyHp);
                Assert.AreEqual(456u,battle.PlayerExperience);
                Assert.GreaterOrEqual(server.OriginalFrameCount,1);
                Assert.IsFalse(battle.IsBattleFinished);
            });
        }

        [Test]
        public void NativeReturnKeepsReceivedMpAndRejectsLegacySimulationPackets()
        {
            WithBattle((battle,server,receiver)=>{
                Assert.IsTrue(receiver.TryApplyPacket(new HpSetNotification(100,17)));
                Assert.IsTrue(receiver.TryApplyPacket(new MpSetNotification(100,3)));
                var session=battle.gameObject.AddComponent<BattleTransitionController>();
                Assert.IsTrue(session.CapturePlayerVitals(battle));
                Assert.AreEqual(17,session.PlayerHp);Assert.AreEqual(3,session.PlayerMp);
                Assert.IsFalse(receiver.TryApplyPacket(new LocalVitalsDisplayNotification(100,100,false)));
                Assert.IsFalse(receiver.TryApplyPacket(new BattleStateNotification(Array.Empty<CombatUnitSnapshot>())));
                Assert.AreEqual(17,battle.PlayerHp);Assert.AreEqual(3,battle.PlayerMp);
                Assert.IsTrue(receiver.TryApplyPacket(new UnitRemoveNotification(100)));
                Assert.IsFalse(session.CapturePlayerVitals(battle)); // Wait for actual HP0; no invalid save.
                Assert.AreEqual(17,session.PlayerHp);Assert.AreEqual(3,session.PlayerMp);
                Assert.IsTrue(receiver.TryApplyPacket(new HpSetNotification(100,0)));
                Assert.IsTrue(session.CapturePlayerVitals(battle));
                Assert.AreEqual(0,session.PlayerHp);Assert.AreEqual(3,session.PlayerMp);
            });
        }
    }
}
