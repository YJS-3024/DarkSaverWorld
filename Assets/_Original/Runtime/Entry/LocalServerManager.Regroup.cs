using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public WorldReply Request_BattleRegroup(int member)=>BattleResult(s=>s.ExecuteBattleRegroup(member,BattleAuthority));
        public WorldReply Request_TrainingRegroup(int member)=>TrainingResult(s=>s.ExecuteTrainingRegroup(member,TrainingAuthority??BattleAuthority));
        private void PumpBattleRegroup()
        {
            if(!isActiveAndEnabled||service==null)return;
            bool training=service.WorldSnapshot()?.training!=null;
            var authority=AutomationAuthority(training);if(authority==null)return;
            var reply=service.AdvanceBattleAutomation(authority,SoldierTargetChoice,UnityEngine.Time.realtimeSinceStartupAsDouble*1000.0);
            if(reply==null)return;
            Notify_World?.Invoke(reply);
            if(reply.Accepted){Notify_BattleChanged?.Invoke(reply);if(training)Notify_TrainingChanged?.Invoke(reply);}
        }
    }
}
