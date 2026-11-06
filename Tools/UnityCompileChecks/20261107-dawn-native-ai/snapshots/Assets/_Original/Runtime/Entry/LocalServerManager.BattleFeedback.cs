using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Notify_BattleFeedback(BattleFeedbackNotice notice)=>BattleResult(s=>s.ReceiveBattleFeedback(notice));
        public WorldReply Notify_TrainingFeedback(BattleFeedbackNotice notice)=>TrainingResult(s=>s.ReceiveTrainingFeedback(notice));
    }
}
