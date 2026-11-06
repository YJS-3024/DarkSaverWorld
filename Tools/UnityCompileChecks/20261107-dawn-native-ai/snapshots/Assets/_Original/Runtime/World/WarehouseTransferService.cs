using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkSaver.Original.Entry
{
    using DarkSaver.Original.World;
    public sealed partial class EntryAccountService
    {
        private WorldFailure WarehouseReady(string token)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(world.Moving)return WorldFailure.Moving;
            if(world.Kind!=FacilityKind.Warehouse||token==null||world.Token!=token||world.Draft==null)return WorldFailure.StaleRequest;
            if(world.Combat!=null||world.PresentationLocked||world.PendingPortal!=null)return WorldFailure.Busy;
            var npc=world.Map(world.Record.map).npcs.FirstOrDefault(n=>n.id==world.Npc&&n.kind==(int)FacilityKind.Warehouse);
            return world.CanInteract(npc)?WorldFailure.None:WorldFailure.TooFar;
        }
        private static bool SameOwned(WorldOwnedItem[] a,WorldOwnedItem[] b)=>a!=null&&b!=null&&a.Length==b.Length&&a.All(i=>i!=null&&b.Any(j=>j!=null&&i.id==j.id&&i.item==j.item&&i.slot==j.slot&&i.remainingUses==j.remainingUses))
            &&a.Select(i=>i.id).Distinct().Count()==a.Length;
        private static bool ValidOwned(WorldOwnedItem[] items,int capacity)=>items!=null&&items.All(i=>i!=null&&Guid.TryParseExact(i.id,"N",out _)&&i.item>=0&&i.slot>=0&&i.slot<capacity&&(!i.remainingUses.HasValue||i.remainingUses>=0))
            &&items.Select(i=>i.id).Distinct().Count()==items.Length&&items.Select(i=>i.slot).Distinct().Count()==items.Length;
        private bool ValidWarehouseArrays(WorldOwnedItem[] inventory,WorldOwnedItem[] storage,int capacity)
        {
            if(capacity<0||!ValidOwned(inventory,28)||!ValidOwned(storage,capacity))return false;
            var ids=inventory.Concat(storage).Concat(world.Record.Equipment).Select(i=>i.id).ToArray();return ids.Distinct().Count()==ids.Length;
        }
        private WorldFailure WarehouseFeeReady()
        {
            var terms=world.WarehouseTerms;
            if(terms?.fee==null||!terms.feeSource.HasValue)return WorldFailure.RulesUnavailable;
            bool wallet=terms.feeSource==BankFeeSource.Wallet;
            if(!(wallet?world.Record.goldKnown:world.Record.bankKnown))return WorldFailure.RulesUnavailable;
            return (wallet?world.Record.gold:world.Record.bank)<terms.fee.Value?WorldFailure.InsufficientFunds:WorldFailure.None;
        }
        public WorldReply RequestWarehouseOperation(WarehouseOperation operation,bool deposit,string owned,string token,string request)
        {
            var f=WarehouseReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(!Enum.IsDefined(typeof(WarehouseOperation),operation))return WorldResult(WorldFailure.InvalidInput);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingWarehouse!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.warehouse?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            int slot=-1;
            if(operation!=WarehouseOperation.State)
            {
                if(world.WarehouseTerms==null)return WorldResult(WorldFailure.RulesUnavailable);
                if(operation==WarehouseOperation.Move)
                {
                    if(world.StorageDraftRevision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
                    var source=deposit?world.Draft.inventory:world.Draft.storage;var target=deposit?world.Draft.storage:world.Draft.inventory;
                    if(source.All(i=>i.id!=owned))return WorldResult(WorldFailure.NotOwned);
                    int capacity=deposit?world.WarehouseTerms.capacity:28;var used=new HashSet<int>(target.Select(i=>i.slot));slot=0;
                    while(slot<capacity&&used.Contains(slot))slot++;
                    if(slot>=capacity)return WorldResult(WorldFailure.InventoryFull);
                }
                else{f=WarehouseFeeReady();if(f!=WorldFailure.None)return WorldResult(f);}
            }
            world.WarehouseRequestState=world.Record.Copy();
            world.PendingWarehouse=new WarehouseRequest{id=request,character=worldCharacter,session=world.CurrencySession,token=token,npc=world.Npc,map=world.Record.map,
                operation=operation,deposit=deposit,owned=owned,slot=slot,revision=revision,draftRevision=world.StorageDraftRevision,currencyRevision=world.Record.currency?.revision??0,
                inventory=world.Draft.inventory.Select(i=>i.Copy()).ToArray(),storage=world.Draft.storage.Select(i=>i.Copy()).ToArray()};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveWarehouseOperation(WarehouseNotice notice)
        {
            if(notice==null)return WorldResult(WorldFailure.InvalidInput);
            var f=WarehouseReady(notice.token);if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingWarehouse;var baseline=world.WarehouseRequestState;
            if(pending==null||baseline==null||pending.id!=notice.request||notice.character!=worldCharacter||notice.session!=world.CurrencySession||notice.npc!=world.Npc||notice.map!=world.Record.map
                ||!Guid.TryParseExact(notice.id,"N",out _)||world.WarehouseNotices.Contains(notice.id)||notice.operation!=pending.operation
                ||notice.expectedRevision!=pending.revision||notice.expectedRevision!=(world.Record.warehouse?.revision??0)
                ||notice.draftRevision!=pending.draftRevision||notice.draftRevision!=world.StorageDraftRevision||notice.currencyRevision!=pending.currencyRevision
                ||notice.currencyRevision!=(world.Record.currency?.revision??0))return WorldResult(WorldFailure.StaleRequest);
            if(!TempleRevivalOffer.SameState(world.Record,baseline)){world.ClearWarehouseRequest();return WorldResult(WorldFailure.StaleRequest);}
            if(!notice.approved){world.ClearWarehouseRequest();return WorldResult(WorldFailure.InvalidInput);}
            if(!notice.capacity.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            if(notice.capacity<0)return WorldResult(WorldFailure.InvalidInput);
            if(notice.fee.HasValue!=notice.feeSource.HasValue||notice.feeSource.HasValue&&!Enum.IsDefined(typeof(BankFeeSource),notice.feeSource.Value))return WorldResult(WorldFailure.InvalidInput);
            int capacity=notice.capacity.Value;
            if(pending.operation==WarehouseOperation.State)
            {
                if(!ValidWarehouseArrays(world.Draft.inventory,world.Draft.storage,capacity)||!ValidOwned(world.Record.storage,capacity)
                    ||notice.inventory!=null&&!SameOwned(notice.inventory,world.Draft.inventory)||notice.storage!=null&&!SameOwned(notice.storage,world.Draft.storage))return WorldResult(WorldFailure.InvalidInput);
                world.WarehouseTerms=new WarehouseTerms{capacity=capacity,fee=notice.fee,feeSource=notice.feeSource};world.PendingStorageFee=notice.fee??0;
            }
            else
            {
                if(world.WarehouseTerms==null||capacity!=world.WarehouseTerms.capacity||!notice.fee.HasValue||!notice.feeSource.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
                if(!ValidWarehouseArrays(notice.inventory,notice.storage,capacity))return WorldResult(WorldFailure.InvalidInput);
                if(pending.operation==WarehouseOperation.Move)
                {
                    var from=pending.deposit?pending.inventory:pending.storage;var to=pending.deposit?pending.storage:pending.inventory;
                    var item=from.FirstOrDefault(i=>i.id==pending.owned);if(item==null)return WorldResult(WorldFailure.NotOwned);
                    var moved=item.Copy();moved.slot=pending.slot;
                    var source=from.Where(i=>i.id!=item.id).ToArray();var target=to.Concat(new[]{moved}).ToArray();
                    if(!SameOwned(notice.inventory,pending.deposit?source:target)||!SameOwned(notice.storage,pending.deposit?target:source))return WorldResult(WorldFailure.InvalidInput);
                    world.Draft.inventory=notice.inventory.Select(i=>i.Copy()).ToArray();world.Draft.storage=notice.storage.Select(i=>i.Copy()).ToArray();
                    world.StorageDraftRevision++;world.PendingStorageFee=notice.fee.Value;
                    world.WarehouseTerms.fee=notice.fee;world.WarehouseTerms.feeSource=notice.feeSource;
                }
                else
                {
                    if(!SameOwned(notice.inventory,pending.inventory)||!SameOwned(notice.storage,pending.storage)||notice.fee!=world.WarehouseTerms.fee||notice.feeSource!=world.WarehouseTerms.feeSource)return WorldResult(WorldFailure.InvalidInput);
                    f=WarehouseFeeReady();if(f!=WorldFailure.None)return WorldResult(f);
                    bool wallet=notice.feeSource==BankFeeSource.Wallet;
                    if(wallet&&(!notice.gold.HasValue||notice.gold.Value!=world.Record.gold-notice.fee.Value||notice.bank.HasValue&&(!world.Record.bankKnown||notice.bank.Value!=world.Record.bank))
                        ||!wallet&&(!notice.bank.HasValue||notice.bank.Value!=world.Record.bank-notice.fee.Value||notice.gold.HasValue&&(!world.Record.goldKnown||notice.gold.Value!=world.Record.gold)))return WorldResult(WorldFailure.InvalidInput);
                    var next=world.Record.Copy();next.inventory=notice.inventory.Select(i=>i.Copy()).ToArray();next.storage=notice.storage.Select(i=>i.Copy()).ToArray();
                    if(wallet)next.gold=notice.gold.Value;else next.bank=notice.bank.Value;
                    next.warehouse=new WarehouseReceipt{id=notice.id,request=notice.request,revision=notice.expectedRevision+1,draftRevision=world.StorageDraftRevision,capacity=capacity,fee=notice.fee.Value,feeSource=notice.feeSource.Value};
                    f=CommitWorld(next,baseline);if(f!=WorldFailure.None)return WorldResult(f);
                    world.Record=next;world.WarehouseNotices.Add(notice.id);if(notice.fee.Value>0)world.CurrencyNotices.Add(notice.id);world.Close();return WorldResult(WorldFailure.None);
                }
            }
            world.WarehouseNotices.Add(notice.id);world.ClearWarehouseRequest();return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteWarehouseOperation(WarehouseOperation operation,bool deposit,string owned,string token,string request,Func<WarehouseRequest,WorldView,WarehouseNotice> authority)
        {
            var f=WarehouseReady(token);if(f!=WorldFailure.None)return WorldResult(f);
            if(authority==null)return WorldResult(WorldFailure.RulesUnavailable);
            var queued=RequestWarehouseOperation(operation,deposit,owned,token,request);if(!queued.Accepted)return queued;
            var origin=world;
            try
            {
                var notice=authority(queued.View.warehouseRequest.Copy(),queued.View);
                if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());
                if(notice!=null)
                {
                    if(notice.request!=request){if(world.PendingWarehouse?.id==request)world.ClearWarehouseRequest();return WorldResult(WorldFailure.StaleRequest);}
                    var result=ReceiveWarehouseOperation(notice);if(!result.Accepted&&world.PendingWarehouse?.id==request)world.ClearWarehouseRequest();return WorldResult(result.Failure);
                }
            }
            catch(Exception){/* Failed local approval must allow a fresh operation or cancel. */}
            if(!ReferenceEquals(world,origin))return new WorldReply(WorldFailure.StaleRequest,world?.Snapshot());
            if(world.PendingWarehouse?.id==request)world.ClearWarehouseRequest();return WorldResult(WorldFailure.RulesUnavailable);
        }
    }
}
