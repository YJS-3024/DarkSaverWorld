using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        // User-approved local training test policy; not a recovered original server value.
        // Delay starts at confirmed monster death, not at corpse animation retirement.
        // Initial health/AP and authoritative respawn results still require a configured authority.
        public const double TrainingMonsterRespawnDelaySeconds = 30d;
        public const int TrainingApMaximum = 20;
        public const double TrainingApRecoveryPerSecond = 2d;

        // No fallback damage/AP/spawn rules: a configured local authority supplies the complete result.
        public Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> TrainingAuthority { get; set; }
        public Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> BattleAuthority { get; set; }
        public event Action<WorldReply> Notify_TrainingChanged;
        public event Action<WorldView> Notify_TrainingInitialRequested;
        public event Action<WorldReply> Notify_BattleChanged;
        public bool HasBattleAuthority(bool training)=>training?TrainingAuthority!=null||BattleAuthority!=null:BattleAuthority!=null;
        public WorldReply Notify_BattleUnits(TrainingUnitsNotice notice)=>BattleResult(s=>s.ReceiveBattleUnits(notice));
        public WorldReply Request_BattleMove(int x,int y)=>SendBattleAction(TrainingAction.Move,-1,x,y);
        public WorldReply Request_BattleAttack(int unit)=>SendBattleAction(TrainingAction.Attack,unit,0,0);
        public WorldReply Request_BattleMagic(int magic,int unit)=>SendBattleAction(TrainingAction.CastMagic,unit,0,0,magic);
        public WorldReply Request_TrainingMagic(int magic,int unit)=>SendTrainingAction(TrainingAction.CastMagic,unit,0,0,magic);
        public WorldReply Request_BattleMagicAt(int magic,int x,int y)=>SendBattleAction(TrainingAction.CastMagic,-1,x,y,magic);
        public WorldReply Request_TrainingMagicAt(int magic,int x,int y)=>SendTrainingAction(TrainingAction.CastMagic,-1,x,y,magic);
        public WorldReply Request_BattleRest()=>SendBattleAction(TrainingAction.Rest,-1,0,0);
        public WorldReply Notify_TrainingUnits(TrainingUnitsNotice notice)=>TrainingResult(s=>s.ReceiveTrainingUnits(notice));
        public WorldReply Request_TrainingMove(int x,int y)=>SendTrainingAction(TrainingAction.Move,-1,x,y);
        public WorldReply Request_TrainingAttack(int unit)=>SendTrainingAction(TrainingAction.Attack,unit,0,0);
        public WorldReply Request_TrainingRest()=>SendTrainingAction(TrainingAction.Rest,-1,0,0);
        private WorldReply TrainingResult(Func<EntryAccountService,WorldReply> apply)
        {var reply=WorldRequest(apply);if(IsCurrentBattleReply(reply)){Notify_TrainingChanged?.Invoke(reply);if(IsCurrentBattleReply(reply))Notify_BattleChanged?.Invoke(reply);}return reply;}
        private WorldReply BattleResult(Func<EntryAccountService,WorldReply> apply)
        {var reply=WorldRequest(apply);if(IsCurrentBattleReply(reply)){Notify_BattleChanged?.Invoke(reply);if(reply.View?.training!=null&&IsCurrentBattleReply(reply))Notify_TrainingChanged?.Invoke(reply);}return reply;}
        private bool IsCurrentBattleReply(WorldReply reply)
        {
            if(!IsCurrentWorldReply(reply))return false;var current=WorldSnapshot;
            return current!=null&&current.battle?.revision==reply.View.battle?.revision&&current.battleTeamRevision==reply.View.battleTeamRevision
                &&current.resultKillRevision==reply.View.resultKillRevision;
        }
        private WorldReply SendBattleAction(TrainingAction action,int target,int x,int y,int magic=-1,string owned=null)
            =>BattleResult(s=>action==TrainingAction.CastMagic||action==TrainingAction.UseSkill||action==TrainingAction.UseItem
                ?s.ExecuteNativeBattleCast(action,target,x,y,ActionAuthority(false,action),magic,owned):s.ExecuteBattleAction(action,target,x,y,ActionAuthority(false,action),magic,owned));
        private WorldReply SendTrainingAction(TrainingAction action,int target,int x,int y,int magic=-1,string owned=null)
            =>TrainingResult(s=>action==TrainingAction.CastMagic||action==TrainingAction.UseSkill||action==TrainingAction.UseItem
                ?s.ExecuteNativeTrainingCast(action,target,x,y,ActionAuthority(true,action),magic,owned):s.ExecuteTrainingAction(action,target,x,y,ActionAuthority(true,action),magic,owned));
    }
}
