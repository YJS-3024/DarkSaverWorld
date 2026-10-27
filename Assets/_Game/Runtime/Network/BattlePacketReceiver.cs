using System;
using DarkSaver.Combat;
using UnityEngine;

namespace DarkSaver.Network
{
    // Client endpoint on the same battle object; never searches another additive battle.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BattleSceneRoot), typeof(ServerManager))]
    public sealed class BattlePacketReceiver : MonoBehaviour
    {
        private ServerManager server;
        private BattleSceneRoot battle;
        private bool bound;
        // Delivery and successful application are different. Unsupported packets report false.
        public event Action<ServerPacket, bool> PacketHandled;
        // Bounded, immutable latest notifications. Acceptance is not visual playback.
        // Effect/Value are uninterpreted; never feed them into damage or AP/MP costs.
        public LocalCommandResponse LastCommandResponse {get;private set;}
        public BattleStateNotification LastServerState {get;private set;}
        public AttackEffectNotification LastAttackEffect { get; private set; }
        public MagicEffectNotification LastMagicEffect { get; private set; }
        public event Action<AttackEffectNotification> AttackEffectReceived;
        public event Action<MagicEffectNotification> MagicEffectReceived;

        private void OnEnable() => Bind();
        private void OnDisable() => Unbind();
        private void OnDestroy() => Unbind();

        public bool Bind()
        {
            if (!isActiveAndEnabled) return false;
            if (bound) return true;
            server = GetComponent<ServerManager>();
            battle = GetComponent<BattleSceneRoot>();
            if (server == null || battle == null) return false;
            server.Sender.ResponseSent += Receive;
            bound = true;
            return true;
        }

        public void Unbind()
        {
            if (bound && server != null) server.Sender.ResponseSent -= Receive;
            bound = false;
        }

        public bool TryApplyPacket(ServerPacket packet)
        {
            if (!isActiveAndEnabled || packet == null) return false;
            if (battle == null) battle = GetComponent<BattleSceneRoot>();
            if (battle == null) return false;
            if (packet is BattleStateNotification snapshot)
            { if(!battle.TemporaryLocalRulesEnabled)return false;LastServerState=snapshot;return true; }
            if (packet is LocalCombatResolvedNotification resolved) return battle.ReceiveLocalCombatResult(resolved.Result);
            if (packet is LocalVitalsDisplayNotification display) return battle.ReceiveLocalVitals(display.UnitId,display.Delta,display.Mp);
            if (packet is LocalMagicPresentationNotification cast) return battle.ReceiveLocalMagic(cast.Action,cast.Cast);
            if (packet is LocalCommandResponse acknowledgement) { LastCommandResponse=acknowledgement;return acknowledgement.Accepted; }
            // PGSNEWEXP is an absolute uint32 assignment. No reward/level/AP inference.
            if (packet is ExperienceSetNotification experience)
                return battle.ApplyExperience(new CombatExperienceResult(experience.UnitId, experience.AbsoluteExperience));
            if (packet is HpSetNotification hp)
                return battle.ApplyHp(hp.UnitId, hp.AbsoluteHp);
            if (packet is MpSetNotification mp)
                return battle.ApplyMp(mp.UnitId, mp.AbsoluteMp);
            if (packet is TickMenuNotification tick)
                return tick.HasPayload && battle.ApplyTickMenu(tick.UnitId, tick.AbsoluteTick, tick.RawFlags);
            if (packet is UnitCorpseNotification corpse)
                return battle.ApplyDeath(corpse.UnitId, true);
            if (packet is UnitRemoveNotification removed)
                return battle.ApplyDeath(removed.UnitId, false);
            if (packet is AttackEffectNotification attack)
            {
                if (!battle.ApplyAttackPresentation(attack.TargetUnitId, attack.AttackerUnitId, attack.Effect)) return false;
                LastAttackEffect = attack;
                AttackEffectReceived?.Invoke(attack);
                return true;
            }
            if (packet is MagicEffectNotification magic)
            {
                if (!battle.CanReceiveEffect(magic.CasterUnitId)) return false;
                // Check the complete list before publishing; no partial target application.
                foreach (var target in magic.TargetUnitIds)
                    if (!battle.CanReceiveEffect(target)) return false;
                LastMagicEffect = magic;
                MagicEffectReceived?.Invoke(magic);
                return true;
            }
            // Actual effect playback and unknown packet families remain unimplemented boundaries.
            return false;
        }

        private void Receive(ServerPacket packet)
        {
            var applied = TryApplyPacket(packet);
            PacketHandled?.Invoke(packet, applied);
        }
    }
}
