using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private WorldFailure PrepareBattleItem(TrainingUnit actor,string owned,int target,ref int x,ref int y,out WorldOwnedItem item,out int magic,out int mpCost,out int apCost,out BattleMagicTarget[] targets)
        {
            item=null;magic=-1;mpCost=apCost=0;targets=null;
            if(!Guid.TryParseExact(owned,"N",out _))return WorldFailure.InvalidInput;
            item=world.Record.inventory.FirstOrDefault(i=>i.id==owned);if(item==null)return WorldFailure.NotOwned;
            var link=world.Definition.battleItems?.Find(item.item);
            if(link==null||world.Definition.Item(item.item)?.equipType!=7)return WorldFailure.RulesUnavailable;
            if(item.remainingUses==0)return WorldFailure.InsufficientResource;
            int itemId=item.item;var rule=actor.itemRules?.FirstOrDefault(r=>r.item==itemId);if(rule==null)return WorldFailure.RulesUnavailable;
            magic=link.magic;
            return PrepareBattleMagic(actor,magic,target,ref x,ref y,out mpCost,out apCost,out targets,false,rule.ForMagic(magic));
        }
        private WorldFailure ValidateBattleItemOutcome(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            bool usingItem=!string.IsNullOrEmpty(notice.request)&&request?.action==TrainingAction.UseItem;
            if(!usingItem)return notice.itemConsumed.HasValue||notice.itemRemainingUses.HasValue?WorldFailure.InvalidInput:WorldFailure.None;
            if(!notice.itemConsumed.HasValue)return WorldFailure.RulesUnavailable;
            if(notice.itemRemainingUses<0||notice.itemConsumed==true&&notice.itemRemainingUses.HasValue&&notice.itemRemainingUses!=0)return WorldFailure.InvalidInput;
            if(combat.ItemBaseline==null||!TempleRevivalOffer.SameState(world.Record,combat.ItemBaseline))return WorldFailure.StaleRequest;
            var item=world.Record.inventory.FirstOrDefault(i=>i.id==request.ownedItem);
            return item==null||item.item!=request.item||item.slot!=request.itemSlot||world.Definition.battleItems?.Find(item.item)?.magic!=request.magic?WorldFailure.StaleRequest:WorldFailure.None;
        }
        private WorldRecord BattleItemStoredBaseline(WorldRecord baseline)
        {
            var stored=baseline.Copy();
            // CommitWorld persists interrupted training at the town entrance. Match that representation.
            if(world.Map(stored.map).training)
            {var gate=TrainingEntrance(stored);stored.map=1000;stored.x=gate.returnX;stored.y=gate.returnY;stored.trainingEntrance=null;}
            return stored;
        }
        public WorldReply RequestBattleItem(string owned,int target,string request)=>RequestBattleAction(TrainingAction.UseItem,target,0,0,request,-1,owned);
        public WorldReply RequestBattleItemAt(string owned,int x,int y,string request)=>RequestBattleAction(TrainingAction.UseItem,-1,x,y,request,-1,owned);
    }
}
