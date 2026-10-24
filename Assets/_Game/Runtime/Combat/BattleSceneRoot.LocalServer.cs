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
        private ServerManager EnsureLocalServer()
        {
            if (localServer != null) return localServer;
            localServer=GetComponent<ServerManager>();
            if(localServer==null)localServer=gameObject.AddComponent<ServerManager>();
            var receiver=GetComponent<BattlePacketReceiver>();
            if(receiver==null)receiver=gameObject.AddComponent<BattlePacketReceiver>();
            receiver.Bind();
            LocalCommandResponse Handle(Func<LocalCommandResponse> execute)
            {
                if(!isActiveAndEnabled || !TemporaryLocalRulesEnabled)return new LocalCommandResponse(false,"로컬 전투 서버가 비활성 상태입니다.");
                processingServerRequest=true;
                try{return execute();}finally{processingServerRequest=false;}
            }
            serverBindings.Add(localServer.RegisterRequestHandler<AttackRequest>(p=>Handle(()=>
                new LocalCommandResponse(TryStartAttackRequest(p.TargetUnitId,p.AttackerUnitId,out var reason),reason))));
            serverBindings.Add(localServer.RegisterRequestHandler<BattleAttackCommand>(p=>Handle(()=>
                new LocalCommandResponse(TryStartAttackRequest(p.TargetId,p.ActorId,out var reason),reason))));
            serverBindings.Add(localServer.RegisterRequestHandler<MagicRequest>(p=>Handle(()=>
                new LocalCommandResponse(TryStartMagicRequest(p,out var reason),reason))));
            serverBindings.Add(localServer.RegisterRequestHandler<BattleMoveRequest>(p=>Handle(()=>
                new LocalCommandResponse(TryMoveTo(p.UnitId,new CombatGridCell(p.X,p.Y),out var reason),reason))));
            serverBindings.Add(localServer.RegisterRequestHandler<BattleRestRequest>(p=>Handle(()=>
                new LocalCommandResponse(TryTemporaryRestUnit(p.UnitId),""))));
            serverBindings.Add(localServer.RegisterRequestHandler<BattleClockRequest>(p=>Handle(()=>
                new LocalCommandResponse(AdvanceActionPoints(p.Seconds),""))));
            serverBindings.Add(localServer.RegisterRequestHandler<BattleSimulationTickRequest>(p=>Handle(()=>
            {
                if(float.IsNaN(p.Seconds) || float.IsInfinity(p.Seconds) || p.Seconds<0 || p.Seconds>60
                    || float.IsNaN(p.MovementSeconds) || float.IsInfinity(p.MovementSeconds) || p.MovementSeconds<0 || p.MovementSeconds>60)
                    return new LocalCommandResponse(false,"잘못된 전투 시간입니다.");
                AdvanceActionPoints(p.Seconds);AdvanceMovement(p.MovementSeconds);AdvanceTemporaryMonsterAi(p.Seconds);
                var units=new List<CombatUnitSnapshot>();foreach(var unit in world.Units)if(world.TryGetSnapshot(unit.Id,out var snapshot))units.Add(snapshot);
                SendLocalNotification(new BattleStateNotification(units.ToArray()));
                return new LocalCommandResponse(true);
            })));
            return localServer;
        }
        public bool TrySubmitLocalRequest(ClientPacket packet,out string reason)
        {
            var accepted=EnsureLocalServer().TryRequest(packet,out var response);
            reason=response?.Reason ?? "로컬 서버 처리기가 없습니다.";
            return accepted;
        }
        private void SendLocalNotification(ServerPacket packet)=>EnsureLocalServer().SendToClient(packet);
        // Server state is already committed. This message publishes client presentation only;
        // applying damage/rewards a second time here would duplicate an authoritative result.
        internal bool ReceiveLocalCombatResult(CombatResult result)
        { PresentCombatResult(result);return true; }
        internal bool ReceiveLocalVitals(short unit,int delta,bool mp)
        { VitalsDisplayChanged?.Invoke(unit,delta,mp);return true; }
        internal bool ReceiveLocalMagic(CombatRequest action,MagicRequest cast)
        {
            if(cast!=null)MagicCastPresentationStarted?.Invoke(cast);
            else MagicPresentationStarted?.Invoke(action);
            return true;
        }
        private void OnDestroy()
        {foreach(var binding in serverBindings)binding.Dispose();serverBindings.Clear();}
    }
}
