using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event System.Action<byte[]> Notify_BattlePacketRequested;
        public event System.Action<NativeBattleFocus> Notify_BattleFocusRequested;
        public WorldReply Notify_BattleUnitPackets(NativeBattleUnitsNotice notice)=>BattleResult(s=>s.ReceiveBattleUnitPackets(notice));
        public WorldReply Notify_TrainingUnitPackets(NativeBattleUnitsNotice notice)=>TrainingResult(s=>s.ReceiveTrainingUnitPackets(notice));
        public WorldReply Notify_BattlePacket(NativeBattleNotice notice)=>ReceiveNativeBattlePacket(notice,false);
        public WorldReply Notify_TrainingPacket(NativeBattleNotice notice)=>ReceiveNativeBattlePacket(notice,true);
        private WorldReply ReceiveNativeBattlePacket(NativeBattleNotice notice,bool training)
        {
            var input=notice?.Copy();byte[] followup=null;NativeBattleFocus? focus=null;
            bool control=OriginalNativeMagic.TryLevelEffectNotice(input?.packet,out int unit)&&unit>=0;
            WorldReply Apply(EntryAccountService service)
            {
                var reply=training?service.ReceiveTrainingPacket(input,out followup):service.ReceiveBattlePacket(input,out followup);
                if(reply.Accepted&&control)focus=NativeBattleFocus.Capture(reply.View,unit);
                return reply;
            }
            var result=training?TrainingResult(Apply):BattleResult(Apply);
            if(focus.HasValue&&focus.Value.Matches(WorldSnapshot))Notify_BattleFocusRequested?.Invoke(focus.Value);
            if(followup!=null&&focus.HasValue&&focus.Value.Matches(WorldSnapshot))Notify_BattlePacketRequested?.Invoke((byte[])followup.Clone());
            return result;
        }
    }
}
