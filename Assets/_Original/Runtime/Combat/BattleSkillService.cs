using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply RequestBattleSkill(int skill,int target,string request)=>RequestBattleAction(TrainingAction.UseSkill,target,0,0,request,skill);
        public WorldReply RequestBattleSkillAt(int skill,int x,int y,string request)=>RequestBattleAction(TrainingAction.UseSkill,-1,x,y,request,skill);
    }
}
