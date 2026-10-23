using System;
using DarkSaver.Combat;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class BattlePacketReceiverTests
    {
        [Test]
        public void TickMenuPacketsClampAbsoluteApAndReplaceNativeBitsWithoutInferringActions()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var hp = battle.PlayerHp; var mp = battle.PlayerMp; var xp = battle.PlayerExperience;
                var inputs = new short[] { 1, 2, 4, 8, 16, 32, 64, 128 };
                var mapped = new byte[] { 2, 16, 8, 32, 1, 4, 64, 128 };
                var renderMapped = new byte[] { 16, 0, 0, 0, 32, 0, 0, 0 };
                Assert.That(battle.TryGetTickMenuRenderBits(100, out _), Is.False);
                for (var i = 0; i < inputs.Length; i++)
                {
                    Assert.That(receiver.TryApplyPacket(new TickMenuNotification(100, 3, inputs[i])), Is.True);
                    Assert.That(battle.PlayerAp, Is.EqualTo(3));
                    Assert.That(battle.TryGetTickMenu(100, out var raw, out var bits), Is.True);
                    Assert.That(raw, Is.EqualTo(inputs[i])); Assert.That(bits, Is.EqualTo(mapped[i]));
                    Assert.That(battle.TryGetTickMenuRenderBits(100, out var render), Is.True);
                    Assert.That(render, Is.EqualTo(renderMapped[i]));
                }
                Assert.That(server.SendToClient(new TickMenuNotification(100, short.MaxValue, -1)), Is.True);
                Assert.That(battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                Assert.That(battle.TryGetTickMenu(100, out var flags, out var menu), Is.True);
                Assert.That(flags, Is.EqualTo(-1)); Assert.That(menu, Is.EqualTo(255));
                Assert.That(battle.TryGetTickMenuRenderBits(100, out var both), Is.True);
                Assert.That(both, Is.EqualTo(0x30));
                Assert.That(receiver.TryApplyPacket(new TickMenuNotification(100, 10, 0x1100)), Is.True);
                Assert.That(battle.TryGetTickMenuRenderBits(100, out var highOnly), Is.True);
                Assert.That(highOnly, Is.Zero);
                Assert.That(receiver.TryApplyPacket(new TickMenuNotification(100, short.MinValue, 0)), Is.True);
                Assert.That(battle.PlayerAp, Is.Zero);
                Assert.That(battle.TryGetTickMenu(100, out flags, out menu), Is.True);
                Assert.That(menu, Is.Zero);
                Assert.That(battle.TryGetTickMenuRenderBits(100, out var cleared), Is.True);
                Assert.That(cleared, Is.Zero);
                Assert.That(receiver.TryApplyPacket(new TickMenuNotification()), Is.False);
                Assert.That(receiver.TryApplyPacket(new TickMenuNotification(999, 10, 1)), Is.False);
                Assert.That(battle.PlayerHp, Is.EqualTo(hp)); Assert.That(battle.PlayerMp, Is.EqualTo(mp));
                Assert.That(battle.PlayerExperience, Is.EqualTo(xp)); Assert.That(battle.DefeatAchieved, Is.False);
                Assert.That(receiver.TryApplyPacket(new UnitRemoveNotification(100)), Is.True);
                Assert.That(receiver.TryApplyPacket(new TickMenuNotification(100, 10, 1)), Is.False);
                Assert.That(battle.TryGetTickMenu(100, out _, out _), Is.False);
                Assert.That(battle.TryGetTickMenuRenderBits(100, out _), Is.False);
                Assert.That(battle.PlayerAp, Is.Zero);
            });
        }

        [Test]
        public void ExperiencePacketsReplaceAbsoluteValueWithoutChangingVitals()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var hp = battle.PlayerHp; var mp = battle.PlayerMp; var ap = battle.PlayerAp;
                foreach (var xp in new uint[] { 3477000000, 3477000000, 1, 0, uint.MaxValue })
                {
                    Assert.That(server.SendToClient(new ExperienceSetNotification(100, xp)), Is.True);
                    Assert.That(battle.PlayerExperience, Is.EqualTo(xp));
                }
                Assert.That(battle.PlayerHp, Is.EqualTo(hp)); Assert.That(battle.PlayerMp, Is.EqualTo(mp));
                Assert.That(battle.PlayerAp, Is.EqualTo(ap)); Assert.That(battle.VictoryAchieved, Is.False);
            });
        }

        [Test]
        public void DeliveryDoesNotAcknowledgeInvalidOrUnsupportedPacketApplication()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var notified = 0; var applied = true; var hp = battle.PlayerHp;
                receiver.PacketHandled += (_, success) => { notified++; applied = success; };
                Assert.That(server.SendToClient(new ExperienceSetNotification(999, 50)), Is.True);
                Assert.That(applied, Is.False);
                Assert.That(server.SendToClient(new HpSetNotification(100, -1)), Is.True);
                Assert.That(applied, Is.False); Assert.That(battle.PlayerHp, Is.EqualTo(hp));
                Assert.That(server.SendToClient(new UnknownServerPacket()), Is.True);
                Assert.That(applied, Is.False); Assert.That(notified, Is.EqualTo(3));
                Assert.That(receiver.TryApplyPacket(null), Is.False);
                Assert.That(battle.PlayerExperience, Is.EqualTo(0u));
            });
        }

        [Test]
        public void BindingIsIdempotentAndUnbindingStopsDelivery()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var handled = 0; receiver.PacketHandled += (_, _) => handled++;
                Assert.That(receiver.Bind(), Is.True); Assert.That(receiver.Bind(), Is.True);
                Assert.That(server.SendToClient(new ExperienceSetNotification(100, 100)), Is.True);
                Assert.That(handled, Is.EqualTo(1));
                receiver.Unbind(); receiver.Unbind();
                Assert.That(server.SendToClient(new ExperienceSetNotification(100, 200)), Is.False);
                Assert.That(battle.PlayerExperience, Is.EqualTo(100u));
                Assert.That(receiver.Bind(), Is.True);
                Assert.That(server.SendToClient(new ExperienceSetNotification(100, 200)), Is.True);
                Assert.That(handled, Is.EqualTo(2)); Assert.That(battle.PlayerExperience, Is.EqualTo(200u));
            });
        }

        [Test]
        public void DisabledReceiverRejectsDirectApplication()
        {
            WithReceiver((receiver, server, battle) =>
            {
                receiver.Unbind(); receiver.enabled = false;
                Assert.That(receiver.Bind(), Is.False);
                Assert.That(receiver.TryApplyPacket(new ExperienceSetNotification(100, 100)), Is.False);
                Assert.That(server.SendToClient(new ExperienceSetNotification(100, 100)), Is.False);
                Assert.That(battle.PlayerExperience, Is.EqualTo(0u));
            });
        }

        [Test]
        public void VitalsAreAbsoluteAndDoNotInferDeathOrChangeExperienceOrAp()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var ap = battle.PlayerAp;
                Assert.That(receiver.TryApplyPacket(new ExperienceSetNotification(100, 123)), Is.True);
                foreach (var hp in new short[] { 37, 37, 0, 120 })
                {
                    Assert.That(server.SendToClient(new HpSetNotification(100, hp)), Is.True);
                    Assert.That(battle.PlayerHp, Is.EqualTo(hp));
                    Assert.That(battle.PlayerLifeState, Is.EqualTo(UnitLifeState.Active));
                }
                foreach (var mp in new short[] { 7, 7, 0, 30 })
                {
                    Assert.That(receiver.TryApplyPacket(new MpSetNotification(100, mp)), Is.True);
                    Assert.That(battle.PlayerMp, Is.EqualTo(mp));
                }
                Assert.That(receiver.TryApplyPacket(new HpSetNotification(800, 0)), Is.True);
                Assert.That(battle.EnemyHp, Is.Zero);
                Assert.That(battle.VictoryAchieved, Is.False);
                Assert.That(battle.PlayerExperience, Is.EqualTo(123u));
                Assert.That(battle.PlayerAp, Is.EqualTo(ap));
            });
        }

        [Test]
        public void InvalidVitalsReportFailureWithoutClampingOrChangingState()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var handled = 0; var applied = true;
                receiver.PacketHandled += (_, success) => { handled++; applied = success; };
                foreach (var packet in new ServerPacket[] {
                    new HpSetNotification(100, -1), new HpSetNotification(100, 121),
                    new MpSetNotification(100, -1), new MpSetNotification(100, 31),
                    new HpSetNotification(701, 1), new MpSetNotification(999, 1),
                    new UnitCorpseNotification(999), new UnknownServerPacket() })
                {
                    Assert.That(server.SendToClient(packet), Is.True);
                    Assert.That(applied, Is.False);
                }
                Assert.That(handled, Is.EqualTo(8));
                Assert.That(battle.PlayerHp, Is.EqualTo(120));
                Assert.That(battle.PlayerMp, Is.EqualTo(30));
                Assert.That(battle.PlayerLifeState, Is.EqualTo(UnitLifeState.Active));
            });
        }

        [Test]
        public void HpAssignmentsCannotResurrectConfirmedDeadUnits()
        {
            var world = new CombatWorld();
            var unit = new CombatUnit { Id = 100, MaxHp = 120, MaxMp = 30, Hp = 0, LifeState = UnitLifeState.Corpse };
            world.Add(unit);
            var flow = new CombatFlow(world, new LocalCombatAuthority());
            foreach (var state in new[] { UnitLifeState.Corpse, UnitLifeState.Removed })
            {
                unit.LifeState = state;
                Assert.That(flow.TryApplyHp(100, 1), Is.False);
                Assert.That(flow.TryApplyHp(100, 0), Is.True);
                Assert.That(flow.TryApplyMp(100, 7), Is.True);
                Assert.That(unit.Hp, Is.Zero);
                Assert.That(unit.LifeState, Is.EqualTo(state));
            }
        }

        [Test]
        public void CorpseThenRemovalConfirmsVictoryWithoutAutomaticRewards()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var hp = battle.PlayerHp; var mp = battle.PlayerMp; var ap = battle.PlayerAp;
                Assert.That(receiver.TryApplyPacket(new HpSetNotification(800, 0)), Is.True);
                Assert.That(battle.EnemyLifeState, Is.EqualTo(UnitLifeState.Active));
                Assert.That(battle.VictoryAchieved, Is.False);
                foreach (var packet in new ServerPacket[] { new UnitCorpseNotification(800),
                    new UnitCorpseNotification(800), new UnitRemoveNotification(800), new UnitRemoveNotification(800) })
                {
                    var applied = false;
                    void Handled(ServerPacket _, bool success) => applied = success;
                    receiver.PacketHandled += Handled;
                    Assert.That(server.SendToClient(packet), Is.True);
                    receiver.PacketHandled -= Handled;
                    Assert.That(applied, Is.True);
                    Assert.That(battle.EnemyLifeState, Is.EqualTo(packet is UnitCorpseNotification ? UnitLifeState.Corpse : UnitLifeState.Removed));
                    Assert.That(battle.VictoryAchieved, Is.True);
                }
                Assert.That(receiver.TryApplyPacket(new UnitCorpseNotification(800)), Is.False);
                Assert.That(battle.EnemyLifeState, Is.EqualTo(UnitLifeState.Removed));
                Assert.That(receiver.TryApplyPacket(new HpSetNotification(800, 1)), Is.False);
                Assert.That(battle.PlayerExperience, Is.Zero);
                Assert.That(battle.PlayerHp, Is.EqualTo(hp));
                Assert.That(battle.PlayerMp, Is.EqualTo(mp));
                Assert.That(battle.PlayerAp, Is.EqualTo(ap));
            });
        }

        [Test]
        public void DeathBeforeHpCreatesConsistentStateWithoutRestoringRemovedUnits()
        {
            WithReceiver((receiver, server, battle) =>
            {
                Assert.That(receiver.TryApplyPacket(new UnitRemoveNotification(100)), Is.True);
                Assert.That(battle.PlayerHp, Is.Zero);
                Assert.That(battle.PlayerLifeState, Is.EqualTo(UnitLifeState.Removed));
                Assert.That(receiver.TryApplyPacket(new HpSetNotification(100, 0)), Is.True);
                Assert.That(receiver.TryApplyPacket(new HpSetNotification(100, 120)), Is.False);
                Assert.That(receiver.TryApplyPacket(new UnitCorpseNotification(100)), Is.False);
                Assert.That(receiver.TryApplyPacket(new UnitCorpseNotification(999)), Is.False);
                Assert.That(receiver.TryApplyPacket(new UnitRemoveNotification(701)), Is.False);
                Assert.That(battle.VictoryAchieved, Is.False);
                Assert.That(battle.PlayerMp, Is.EqualTo(30));
                Assert.That(battle.PlayerExperience, Is.Zero);
            });
        }

        [Test]
        public void DeathDoesNotPublishCombatResultOrExperienceAndStopsApRecovery()
        {
            var world = new CombatWorld();
            var unit = new CombatUnit { Id = 100, Hp = 120, MaxHp = 120, Mp = 7, MaxMp = 30,
                Attack = 26, Defense = 7, Experience = 123 };
            world.Add(unit);
            var flow = new CombatFlow(world, new LocalCombatAuthority(), true);
            var events = 0;
            flow.ActionStarted += _ => events++; flow.ResultApplied += _ => events++;
            flow.ExperienceApplied += _ => events++;
            var ap = unit.ActionPoints.Current;
            Assert.That(flow.TryApplyDeath(100, true), Is.True);
            Assert.That(unit.LifeState, Is.EqualTo(UnitLifeState.Corpse));
            Assert.That(unit.Hp, Is.Zero);
            Assert.That(flow.AdvanceActionPoints(10f), Is.True);
            Assert.That(unit.ActionPoints.Current, Is.EqualTo(ap));
            Assert.That((unit.Mp, unit.Attack, unit.Defense, unit.Experience), Is.EqualTo((7, 26, 7, 123u)));
            Assert.That(events, Is.Zero);
            Assert.That(world.TryGet(100, out var retained), Is.True);
            Assert.That(retained, Is.SameAs(unit));
        }

        [Test]
        public void PlayerDeathSnapshotRetainsLifeStateAndBlocksBattleReentry()
        {
            WithReceiver((receiver, server, battle) =>
            {
                var root = new GameObject("Death session");
                SceneManager.MoveGameObjectToScene(root, battle.gameObject.scene);
                var session = root.AddComponent<BattleTransitionController>();
                foreach (var packet in new ServerPacket[] { new UnitCorpseNotification(100), new UnitRemoveNotification(100) })
                {
                    Assert.That(receiver.TryApplyPacket(packet), Is.True);
                    Assert.That(session.CapturePlayerVitals(battle), Is.True);
                    var data = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", new Vector2(1.5f, 2.5f));
                    Assert.That(DarkSaver.Core.NativeSaveStore.IsValid(data), Is.True);
                    Assert.That(data.hp, Is.Zero);
                    Assert.That(data.lifeState, Is.EqualTo((int)battle.PlayerLifeState));
                    Assert.That(data.mp, Is.EqualTo(30)); // Existing return-to-world recovery rule.
                    var copy = new GameObject("Restored death session");
                    SceneManager.MoveGameObjectToScene(copy, battle.gameObject.scene);
                    var restored = copy.AddComponent<BattleTransitionController>();
                    Assert.That(restored.RestoreSaveState(data), Is.True);
                    Assert.That(restored.CanEnterBattle, Is.False);
                    Assert.That(restored.RestorePlayerVitals(battle), Is.False);
                    Assert.That(battle.PlayerHp, Is.Zero);
                    Assert.That(battle.PlayerLifeState, Is.EqualTo(packet is UnitCorpseNotification ? UnitLifeState.Corpse : UnitLifeState.Removed));
                }
            });
        }

        private static void WithReceiver(Action<BattlePacketReceiver, ServerManager, BattleSceneRoot> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Battle packet check"); SceneManager.MoveGameObjectToScene(root, scene);
                var receiver = root.AddComponent<BattlePacketReceiver>();
                Assert.That(receiver.Bind(), Is.True);
                check(receiver, root.GetComponent<ServerManager>(), root.GetComponent<BattleSceneRoot>());
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
