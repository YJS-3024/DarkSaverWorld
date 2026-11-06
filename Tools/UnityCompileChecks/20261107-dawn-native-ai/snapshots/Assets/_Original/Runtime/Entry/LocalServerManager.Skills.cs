using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Request_BattleSkill(int skill,int target)=>SendBattleAction(TrainingAction.UseSkill,target,0,0,skill);
        public WorldReply Request_TrainingSkill(int skill,int target)=>SendTrainingAction(TrainingAction.UseSkill,target,0,0,skill);
        public WorldReply Request_BattleSkillAt(int skill,int x,int y)=>SendBattleAction(TrainingAction.UseSkill,-1,x,y,skill);
        public WorldReply Request_TrainingSkillAt(int skill,int x,int y)=>SendTrainingAction(TrainingAction.UseSkill,-1,x,y,skill);
    }
}
