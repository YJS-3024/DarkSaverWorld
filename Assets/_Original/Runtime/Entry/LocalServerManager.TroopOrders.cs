using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Request_BattleOrder(int choice)=>BattleResult(s=>s.ApplyBattleOrder(choice));
        public WorldReply Request_TrainingOrder(int choice)=>TrainingResult(s=>s.ApplyTrainingOrder(choice));
    }
}
