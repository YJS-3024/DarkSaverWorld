using System;
using System.Linq;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.Tutorial;
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private bool ReturnStoneMap(int map)=>world.Map(map)!=null&&!world.Map(map).training&&world.Definition.campaign?.Map(map)?.kind!="scenario";
        private WorldFailure ItemUseReady()
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(world.Moving||world.Kind!=FacilityKind.None||world.PresentationLocked||world.PendingPortal!=null||world.Combat!=null||world.PendingClassChange!=null||world.PendingClassOptions!=null||world.PendingStatAdjustment!=null||world.StatDraft?.Dirty==true||world.PendingMagicLoadout!=null||world.MagicDraft?.Dirty==true)return WorldFailure.Busy;
            // Combat, training and scenario exits have their own approval/state lifecycles.
            return ReturnStoneMap(world.Record.map)&&world.Record.scenarioVisit==null&&world.Record.scenarioBattle==null?WorldFailure.None:WorldFailure.RulesUnavailable;
        }
        public WorldReply RequestItemUse(string owned,string request)
        {
            var f=ItemUseReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingItemUse!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.itemUse?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var item=world.Record.inventory.FirstOrDefault(i=>i.id==owned);if(item==null)return WorldResult(WorldFailure.NotOwned);
            if(world.Definition.Item(item.item)?.equipType!=10)return WorldResult(WorldFailure.InvalidInput);
            if(item.item!=324&&item.item!=361)return WorldResult(WorldFailure.RulesUnavailable);
            world.ItemUseState=world.Record.Copy();
            world.PendingItemUse=new ItemUseRequest{id=request,character=worldCharacter,session=world.CurrencySession,owned=owned,map=world.Record.map,item=item.item,slot=item.slot,revision=revision};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveItemUse(ItemUseNotice notice)
        {
            if(notice?.request==null)return WorldResult(WorldFailure.InvalidInput);
            var f=ItemUseReady();if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingItemUse;var baseline=world.ItemUseState;
            if(!ItemUseRequest.Same(pending,notice.request)||baseline==null||!Guid.TryParseExact(notice.id,"N",out _)||world.ItemUseNotices.Contains(notice.id)
                ||pending.character!=worldCharacter||pending.session!=world.CurrencySession||pending.map!=world.Record.map||pending.revision!=(world.Record.itemUse?.revision??0))return WorldResult(WorldFailure.StaleRequest);
            if(!TempleRevivalOffer.SameState(world.Record,baseline)){world.ClearItemUse();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.ClearItemUse();return WorldResult(WorldFailure.InvalidInput);}
            if(!notice.consumed.HasValue||notice.destination==null)return WorldResult(WorldFailure.RulesUnavailable);
            var to=notice.destination;
            if(!ReturnStoneMap(to.map)||to.direction<0||to.direction>3)return WorldResult(WorldFailure.InvalidInput);
            var map=world.Map(to.map);
            if(!map.map.Contains(new TutorialTile(to.x,to.y))||map.npcs.Any(n=>n.x==to.x&&n.y==to.y))return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();next.map=to.map;next.x=to.x;next.y=to.y;next.direction=to.direction;
            if(notice.consumed.Value)next.inventory=next.inventory.Where(i=>i.id!=pending.owned).ToArray();
            next.itemUse=new ItemUseReceipt{id=notice.id,request=pending.id,owned=pending.owned,revision=pending.revision+1,item=pending.item,sourceMap=pending.map,
                map=to.map,x=to.x,y=to.y,direction=to.direction,consumed=notice.consumed.Value};
            f=CommitWorld(next,baseline);if(f!=WorldFailure.None)return WorldResult(f);
            world.ItemUseNotices.Add(notice.id);world.Relocate(next);return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteItemUse(string owned,string request,Func<ItemUseRequest,WorldView,ItemUseNotice> authority)
        {
            var f=ItemUseReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestItemUse(owned,request);if(!queued.Accepted)return queued;
            try
            {
                var notice=authority(queued.View.itemUseRequest.Copy(),queued.View);
                if(notice?.request?.id!=request)
                {if(world.PendingItemUse?.id==request)world.ClearItemUse();return WorldResult(notice==null?WorldFailure.RulesUnavailable:WorldFailure.StaleRequest);}
                var reply=ReceiveItemUse(notice);if(!reply.Accepted&&world.PendingItemUse?.id==request)world.ClearItemUse();return WorldResult(reply.Failure);
            }
            catch(Exception){if(world.PendingItemUse?.id==request)world.ClearItemUse();return WorldResult(WorldFailure.RulesUnavailable);}
        }
    }
}
