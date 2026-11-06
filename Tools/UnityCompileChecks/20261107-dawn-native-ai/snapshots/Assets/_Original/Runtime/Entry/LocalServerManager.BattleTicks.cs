using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Notify_BattleTicks(BattleTickNotice notice)=>BattleResult(s=>s.ReceiveBattleTicks(notice));
        public WorldReply Notify_TrainingTicks(BattleTickNotice notice)=>TrainingResult(s=>s.ReceiveTrainingTicks(notice));
    }
}
