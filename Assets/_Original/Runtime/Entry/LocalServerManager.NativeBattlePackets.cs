using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Notify_BattlePacket(NativeBattleNotice notice)=>BattleResult(s=>s.ReceiveBattlePacket(notice));
        public WorldReply Notify_TrainingPacket(NativeBattleNotice notice)=>TrainingResult(s=>s.ReceiveTrainingPacket(notice));
    }
}
