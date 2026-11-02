using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Request_BattleItem(string owned,int unit)=>SendBattleAction(TrainingAction.UseItem,unit,0,0,-1,owned);
        public WorldReply Request_TrainingItem(string owned,int unit)=>SendTrainingAction(TrainingAction.UseItem,unit,0,0,-1,owned);
        public WorldReply Request_BattleItemAt(string owned,int x,int y)=>SendBattleAction(TrainingAction.UseItem,-1,x,y,-1,owned);
        public WorldReply Request_TrainingItemAt(string owned,int x,int y)=>SendTrainingAction(TrainingAction.UseItem,-1,x,y,-1,owned);
    }
}
