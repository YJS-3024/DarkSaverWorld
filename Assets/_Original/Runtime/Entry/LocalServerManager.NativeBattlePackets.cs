using DarkSaver.Original.World;
using DarkSaver.Original.Campaign;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event System.Action<byte[]> Notify_BattlePacketRequested;
        public event System.Action<NativeBattleFocus> Notify_BattleFocusRequested;
        private object nativeControlDelivery;
        private void PublishNativeScenarioReturn(NativeScenarioReturn delivery,bool requestLeave=false)
        {
            // Saver414640 writes root5B before restoring the saved outside map.
            // This is its host leave notification, not a guessed server exit result.
            if(requestLeave&&delivery.Matches(WorldSnapshot))Notify_BattlePacketRequested?.Invoke(new byte[]{0x5b});
            if(delivery.Matches(WorldSnapshot))Notify_MapEntered?.Invoke(new WorldReply(WorldFailure.None,WorldSnapshot));
            if(delivery.Matches(WorldSnapshot))Notify_ScenarioExited?.Invoke(new WorldReply(WorldFailure.None,WorldSnapshot));
        }
        public WorldReply Notify_BattleUnitPackets(NativeBattleUnitsNotice notice)=>BattleResult(s=>s.ReceiveBattleUnitPackets(notice));
        public WorldReply Notify_TrainingUnitPackets(NativeBattleUnitsNotice notice)=>TrainingResult(s=>s.ReceiveTrainingUnitPackets(notice));
        public WorldReply Notify_BattlePacket(NativeBattleNotice notice)=>ReceiveNativeBattlePacket(notice,false);
        public WorldReply Notify_TrainingPacket(NativeBattleNotice notice)=>ReceiveNativeBattlePacket(notice,true);
        private WorldReply ReceiveNativeBattlePacket(NativeBattleNotice notice,bool training)
        {
            var before=WorldSnapshot;
            var input=notice?.Copy();byte[] followup=null;NativeBattleFocus? focus=null;NativeScenarioReturn? returned=null;
            bool control=OriginalNativeMagic.TryLevelEffectNotice(input?.packet,out int unit)&&unit>=0;
            bool redirect=OriginalMagicRedirect.TryDecode(input?.packet,out _,out int caster,out _,out _);
            if(redirect)unit=caster>=0?caster:0;
            var delivery=new object();
            WorldReply Apply(EntryAccountService service)
            {
                var reply=training?service.ReceiveTrainingPacket(input,out followup):service.ReceiveBattlePacket(input,out followup);
                if(reply.Accepted&&OriginalScenarioEnd.TryDecode(input?.packet,out _)&&before?.record.scenarioVisit!=null) returned=NativeScenarioReturn.Capture(reply.View);
                if(reply.Accepted&&(control||redirect)){nativeControlDelivery=delivery;focus=NativeBattleFocus.Capture(reply.View,unit);}
                return reply;
            }
            var result=training?TrainingResult(Apply):BattleResult(Apply);
            if(returned.HasValue)PublishNativeScenarioReturn(returned.Value,true);
            if(control&&nativeControlDelivery==delivery&&focus.HasValue&&focus.Value.Matches(WorldSnapshot))Notify_BattleFocusRequested?.Invoke(focus.Value);
            if(followup!=null&&nativeControlDelivery==delivery&&focus.HasValue&&focus.Value.Matches(WorldSnapshot))Notify_BattlePacketRequested?.Invoke((byte[])followup.Clone());
            return result;
        }
    }
}
