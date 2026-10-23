using System;
using DarkSaver.Combat;
using UnityEngine;

namespace DarkSaver.Network
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BattleSceneRoot), typeof(ServerManager))]
    public sealed class BattleRequestSender : MonoBehaviour
    {
        private bool sending;
        public MagicRequest LastMagicRequest { get; private set; }
        // Full packet retained for position/multi-target anticipation. Not actual playback.
        public event Action<MagicRequest> MagicRequestDispatched;

        public bool TrySendMagic(MagicRequest packet, out string reason)
        {
            reason = string.Empty;
            if (!isActiveAndEnabled || sending || packet == null)
            { reason = "요청이 없거나 전송기가 비활성/전송 중입니다."; return false; }
            var battle = GetComponent<BattleSceneRoot>();
            var server = GetComponent<ServerManager>();
            if (battle == null || server == null)
            { reason = "로컬 전투 연결이 없습니다."; return false; }
            if (!battle.CanStartMagicRequest(packet.MagicId, packet.ActingUnitId, packet.TargetUnitIds, out reason)) return false;
            sending = true;
            try
            {
                var delivered = server.SendToServer(packet, () =>
                {
                    if (!battle.TryStartMagicRequest(packet, out var startReason))
                        throw new InvalidOperationException(startReason);
                    LastMagicRequest = packet;
                    MagicRequestDispatched?.Invoke(packet);
                });
                if (!delivered) reason = "로컬 서버 처리기가 없거나 비활성 상태입니다.";
                return delivered;
            }
            finally { sending = false; }
        }

        // Caller supplies both raw tile attributes; do not invent them from missing map data.
        // True means delivered, not approved. Server responses remain independent packets.
        public bool TrySendAttack(AttackRequest packet, out string reason)
        {
            reason = string.Empty;
            if (!isActiveAndEnabled || sending || packet == null)
            { reason = "요청이 없거나 전송기가 비활성/전송 중입니다."; return false; }
            var battle = GetComponent<BattleSceneRoot>();
            var server = GetComponent<ServerManager>();
            if (battle == null || server == null)
            { reason = "로컬 전투 연결이 없습니다."; return false; }
            if (!battle.CanStartAttackRequest(packet.TargetUnitId, packet.AttackerUnitId, out reason)) return false;
            sending = true;
            try
            {
                var delivered = server.SendToServer(packet, () =>
                {
                    if (!battle.TryStartAttackRequest(packet.TargetUnitId, packet.AttackerUnitId, out var startReason))
                        throw new InvalidOperationException(startReason);
                });
                if (!delivered) reason = "로컬 서버 처리기가 없거나 비활성 상태입니다.";
                return delivered;
            }
            finally { sending = false; }
            // Callback exceptions propagate; already-started AP is not refunded/retried
            // without a separately defined cancellation/acknowledgement rule.
        }
    }
}
