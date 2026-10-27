using System;
using System.Collections.Generic;
using DarkSaver.Network;

namespace DarkSaver.Combat
{
    public sealed partial class BattleSceneRoot
    {
        private ServerManager localServer;
        private readonly List<IDisposable> serverBindings = new();
        private bool processingServerRequest;
        public void BindOriginalProtocol() => EnsureLocalServer();
        private ServerManager EnsureLocalServer()
        {
            if (localServer != null) return localServer;
            localServer=GetComponent<ServerManager>();
            if(localServer==null)localServer=gameObject.AddComponent<ServerManager>();
            var receiver=GetComponent<BattlePacketReceiver>();
            if(receiver==null)receiver=gameObject.AddComponent<BattlePacketReceiver>();
            receiver.Bind();
            return localServer;
        }
        public bool TrySubmitLocalRequest(ClientPacket packet,out string reason)
        {
            if (!TemporaryLocalRulesEnabled)
            { reason="원작 서버 판정이 등록되지 않았습니다.";return false; }
            var accepted=EnsureLocalServer().TryRequest(packet,out var response);
            reason=response?.Reason ?? "로컬 서버 처리기가 없습니다.";
            return accepted;
        }
        private void SendLocalNotification(ServerPacket packet)=>EnsureLocalServer().SendToClient(packet);
        // Server state is already committed. This message publishes client presentation only;
        // applying damage/rewards a second time here would duplicate an authoritative result.
        internal bool ReceiveLocalCombatResult(CombatResult result)
        { if(!TemporaryLocalRulesEnabled)return false;PresentCombatResult(result);return true; }
        internal bool ReceiveLocalVitals(short unit,int delta,bool mp)
        { if(!TemporaryLocalRulesEnabled)return false;VitalsDisplayChanged?.Invoke(unit,delta,mp);return true; }
        internal bool ReceiveLocalMagic(CombatRequest action,MagicRequest cast)
        {
            if(!TemporaryLocalRulesEnabled)return false;
            if(cast!=null)MagicCastPresentationStarted?.Invoke(cast);
            else MagicPresentationStarted?.Invoke(action);
            return true;
        }
        private void OnDestroy()
        {foreach(var binding in serverBindings)binding.Dispose();serverBindings.Clear();}
    }
}
