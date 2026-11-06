using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply RequestBattleOrder(int choice,string request)=>RequestBattleAction(TrainingAction.TroopOrder,-1,0,0,request,-1,null,choice);
        // GameCtl's5F9 popup calls UserUnit.setsolai directly; it has no wire builder.
        public WorldReply ApplyBattleOrder(int choice)=>ExecuteBattleAction(TrainingAction.TroopOrder,-1,0,0,(request,view)=>new TrainingUnitsNotice
        {
            id=Guid.NewGuid().ToString("N"),request=request.id,character=request.character,session=request.session,expectedRevision=request.revision,approved=true,
            units=view.units.Select(u=>{var copy=u.Copy();if(copy.id==0)copy.troopOrder=request.nativeOrder;return copy;}).ToArray()
        },-1,null,choice);
        public WorldReply ApplyTrainingOrder(int choice)
        {var ready=ReadyTraining();return ready==WorldFailure.None?ApplyBattleOrder(choice):WorldResult(ready);}
        private WorldFailure ValidateTroopOrderOutcome(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            if(string.IsNullOrEmpty(notice.request)||request?.action!=TrainingAction.TroopOrder)return WorldFailure.None;
            if(!OriginalTroopOrders.ValidChoice(request.orderChoice)||request.nativeOrder!=OriginalTroopOrders.NativeMode(request.orderChoice)
                ||notice.experience.HasValue||notice.units.Length!=combat.Units.Length)return WorldFailure.InvalidInput;
            foreach(var before in combat.Units)
            {
                var after=notice.units.SingleOrDefault(u=>u.id==before.id);
                if(after==null||!OriginalTroopOrders.SameExceptOrder(before,after)
                    ||(before.id==0?after.troopOrder!=request.nativeOrder:after.troopOrder!=before.troopOrder))return WorldFailure.InvalidInput;
            }
            return WorldFailure.None;
        }
    }
}
