using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Notify_BattleLifecycle(BattleLifecycleNotice notice)=>BattleResult(s=>s.ReceiveBattleLifecycle(notice));
        public WorldReply Notify_TrainingLifecycle(BattleLifecycleNotice notice)=>TrainingResult(s=>s.ReceiveTrainingLifecycle(notice));
    }
}
