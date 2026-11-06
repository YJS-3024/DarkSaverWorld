using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Request_BattleMemberMove(int member,int x,int y)=>BattleResult(s=>s.ExecuteBattleMemberAction(member,TrainingAction.Move,-1,x,y,ActionAuthority(false,TrainingAction.Move)));
        public WorldReply Request_BattleMemberAttack(int member,int target)=>BattleResult(s=>s.ExecuteBattleMemberAction(member,TrainingAction.Attack,target,0,0,ActionAuthority(false,TrainingAction.Attack)));
        public WorldReply Request_BattleMemberRest(int member)=>BattleResult(s=>s.ExecuteBattleMemberAction(member,TrainingAction.Rest,-1,0,0,ActionAuthority(false,TrainingAction.Rest)));
        public WorldReply Request_TrainingMemberMove(int member,int x,int y)=>TrainingResult(s=>s.ExecuteTrainingMemberAction(member,TrainingAction.Move,-1,x,y,ActionAuthority(true,TrainingAction.Move)));
        public WorldReply Request_TrainingMemberAttack(int member,int target)=>TrainingResult(s=>s.ExecuteTrainingMemberAction(member,TrainingAction.Attack,target,0,0,ActionAuthority(true,TrainingAction.Attack)));
        public WorldReply Request_TrainingMemberRest(int member)=>TrainingResult(s=>s.ExecuteTrainingMemberAction(member,TrainingAction.Rest,-1,0,0,ActionAuthority(true,TrainingAction.Rest)));
    }
}
