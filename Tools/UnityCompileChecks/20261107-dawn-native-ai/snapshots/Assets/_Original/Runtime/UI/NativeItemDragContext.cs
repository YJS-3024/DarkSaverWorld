using System;
using System.Linq;
using DarkSaver.Original.World;
namespace DarkSaver.Original.UI
{
    // One pointer gesture stays bound to the item/window it started on. These are
    // local UI identities, not inferred original packet tokens or trading rules.
    public sealed class NativeItemDragContext
    {
        private readonly string character,session,facility,commander;
        private readonly int map,slot,offset,inspecting;private readonly long roster,draft;
        private readonly bool personal,right,equipped;private readonly FacilityKind kind;private readonly int? uses;
        public string Owned{get;}public int Item{get;}
        private NativeItemDragContext(WorldView view,bool own,int source,bool fromRight,bool worn,int rowOffset,WorldOwnedItem item,int itemId)
        {
            personal=own;slot=source;right=fromRight;equipped=worn;offset=fromRight?rowOffset:0;character=view.characterId;session=own?view.commanderSession:view.currencySession;
            map=view.record.map;facility=view.facility?.token;kind=view.facility?.kind??FacilityKind.None;inspecting=view.inspectedCommander;roster=view.commanders?.revision??0;
            commander=view.commanders?.rows?.ElementAtOrDefault(inspecting)?.id;draft=view.facility?.storageDraftRevision??0;Owned=item?.id;Item=itemId;uses=item?.remainingUses;
        }
        private static bool Available(WorldView view)=>view?.record!=null&&!string.IsNullOrEmpty(view.characterId)&&!view.moving&&view.pendingPortal==null&&view.battle==null&&view.training==null&&(view.presentation==null||view.presentation.completed);
        public static WorldOwnedItem PersonalItem(WorldView view,int slot,bool equipped)
        {
            if(view?.record==null||view.inspectedCommander<0)return null;
            var profile=view.commanders?.rows?.ElementAtOrDefault(view.inspectedCommander)?.profile;
            var items=view.inspectedCommander==0?(equipped?view.record.Equipment:view.record.inventory):(equipped?profile?.equipment:profile?.inventory);
            return items?.FirstOrDefault(item=>item.slot==slot);
        }
        public static NativeItemDragContext Personal(WorldView view,int slot,bool equipped)
        {
            if(!Available(view)||string.IsNullOrEmpty(view.commanderSession)||view.facility!=null||view.training!=null||view.battle!=null||view.commanderRequest!=null||view.itemUseRequest!=null)return null;
            var item=PersonalItem(view,slot,equipped);return item==null?null:new NativeItemDragContext(view,true,slot,false,equipped,0,item,item.item);
        }
        public static NativeItemDragContext Facility(WorldView view,string token,int slot,bool right,int rowOffset)
        {
            if(!Available(view)||string.IsNullOrEmpty(view.currencySession)||string.IsNullOrEmpty(token)||view.facility?.token!=token||slot<0||rowOffset<0
                ||view.warehouseRequest!=null||view.shopQuoteRequest!=null||view.shopTradeRequest!=null)return null;
            var facility=view.facility;if(facility.kind!=FacilityKind.Shop&&facility.kind!=FacilityKind.Warehouse)return null;
            long index=(long)slot+(right?(long)rowOffset*3:0);if(index>int.MaxValue)return null;
            if(right&&facility.kind==FacilityKind.Shop)
            {var offers=facility.offers;return offers==null||index>=offers.Length||offers[index]<0?null:new NativeItemDragContext(view,false,slot,true,false,rowOffset,null,offers[index]);}
            var item=(right?facility.storage:facility.inventory)?.FirstOrDefault(row=>row.slot==(int)index);
            return item==null?null:new NativeItemDragContext(view,false,slot,right,false,rowOffset,item,item.item);
        }
        public bool Matches(WorldView view,int currentOffset=0)
        {
            var other=personal?Personal(view,slot,equipped):Facility(view,facility,slot,right,currentOffset);
            return other!=null&&other.character==character&&other.session==session&&other.map==map&&other.facility==facility&&other.kind==kind
                &&other.Owned==Owned&&other.Item==Item&&other.uses==uses
                &&(personal?other.inspecting==inspecting&&other.roster==roster&&other.commander==commander:other.offset==offset&&other.draft==draft);
        }
    }
}
