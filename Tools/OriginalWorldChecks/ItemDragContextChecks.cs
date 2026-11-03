using System;
using System.IO;
using System.Text.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class ItemDragContextChecks
{
 static int checks;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static WorldOwnedItem Item(string id,int slot,int code)=>new WorldOwnedItem{id=id,slot=slot,item=code,remainingUses=3};
 static WorldView Personal()=>new WorldView{characterId="actor",commanderSession="world1",currencySession="money1",record=new WorldRecord{map=1000,inventory=new[]{Item("bagA",0,1)},equipment=new[]{Item("worn",2,2)}},commanders=new CommanderRoster{revision=1,rows=new[]{new OwnedCommander{id="first",job=100},new OwnedCommander{id="second",job=101,profile=new CommanderProfile{inventory=new[]{Item("otherBag",0,3)}}}}}};
 static WorldView Facility(bool shop=true)
 {var v=Personal();v.facility=new WorldFacilityView{kind=shop?FacilityKind.Shop:FacilityKind.Warehouse,token="window1",inventory=new[]{Item("bagA",0,1)},storage=new[]{Item("storedA",3,2)},offers=new[]{1,2,3,4,5,6,7},storageDraftRevision=4};return v;}
 static void Main()
 {
  var v=Personal();var context=NativeItemDragContext.Personal(v,0,false);Check(context!=null&&context.Item==1&&context.Owned=="bagA"&&context.Matches(v),"start personal item identity");
  foreach(string mutation in new[]{"actor","session","map","moving","portal","facility","inspected","roster","identity","item","instance","slot","uses","removed","request","useRequest","battle","training","presentation"})
  {
   var changed=Personal();switch(mutation){case"actor":changed.characterId="new";break;case"session":changed.commanderSession="world2";break;case"map":changed.record.map=1010;break;case"moving":changed.moving=true;break;case"portal":changed.pendingPortal="door";break;case"facility":changed.facility=new WorldFacilityView();break;case"inspected":changed.inspectedCommander=1;break;case"roster":changed.commanders.revision++;break;case"identity":changed.commanders.rows[0].id="other";break;case"item":changed.record.inventory[0].item=2;break;case"instance":changed.record.inventory[0].id="different";break;case"slot":changed.record.inventory[0].slot=1;break;case"uses":changed.record.inventory[0].remainingUses--;break;case"removed":changed.record.inventory=Array.Empty<WorldOwnedItem>();break;case"request":changed.commanderRequest=new CommanderRequest();break;case"useRequest":changed.itemUseRequest=new ItemUseRequest();break;case"battle":changed.battle=new TrainingView();break;case"training":changed.training=new TrainingView();break;case"presentation":changed.presentation=new DarkSaver.Original.Campaign.CampaignPresentationView();break;}
   Check(!context.Matches(changed),"old personal drag cancelled after "+mutation);
  }
  var restored=Personal();Check(context.Matches(restored),"model can recognize current data; pointer component still requires a new begin after cancel");restored.record.goldKnown=true;restored.record.gold=999;restored.record.healthKnown=true;restored.record.hp=17;Check(context.Matches(restored),"unrelated money and wounds do not invalidate item ownership");
  var secondary=Personal();secondary.inspectedCommander=1;context=NativeItemDragContext.Personal(secondary,0,false);Check(context.Owned=="otherBag"&&context.Item==3,"secondary commander owns a distinct bag");secondary.commanders.rows[1].profile.inventory[0].id="changed";Check(!context.Matches(secondary),"secondary item exchange invalidates capture");
  var legacy=Personal();legacy.commanders=null;Check(NativeItemDragContext.Personal(legacy,0,false)?.Matches(legacy)==true,"legacy primary item identity requires no fabricated native index");Check(NativeItemDragContext.Personal(v,2,true)?.Owned=="worn","equipment capture");Check(NativeItemDragContext.Personal(v,19,false)==null,"empty cell is not a captured item");
  var shop=Facility();context=NativeItemDragContext.Facility(shop,"window1",0,true,1);Check(context?.Item==4&&context.Owned==null&&context.Matches(shop,1),"offer capture follows current page without inventing owned instance");Check(!context.Matches(shop,0),"changing offer page invalidates gesture");shop.facility.offers[3]=5;Check(!context.Matches(shop,1),"changed offer cannot replace the held product");
  var warehouse=Facility(false);context=NativeItemDragContext.Facility(warehouse,"window1",0,true,1);Check(context?.Owned=="storedA"&&context.Matches(warehouse,1),"warehouse captures native visible storage entry");warehouse.facility.storageDraftRevision++;Check(!context.Matches(warehouse,1),"approved draft change invalidates storage drag");
  context=NativeItemDragContext.Facility(Facility(),"window1",0,false,0);Check(context.Matches(Facility(),2),"unrelated right-hand page does not change owned left-hand item");
  foreach(string mutation in new[]{"window","session","actor","kind","inventory","use","draft","warehousePending","quotePending","tradePending","moving","map","closed"})
  {
   var changed=Facility();switch(mutation){case"window":changed.facility.token="newWindow";break;case"session":changed.currencySession="money2";break;case"actor":changed.characterId="newOwner";break;case"kind":changed.facility.kind=FacilityKind.Warehouse;break;case"inventory":changed.facility.inventory[0].id="newOwned";break;case"use":changed.facility.inventory[0].remainingUses--;break;case"draft":changed.facility.storageDraftRevision++;break;case"warehousePending":changed.warehouseRequest=new WarehouseRequest();break;case"quotePending":changed.shopQuoteRequest=new ShopRequest();break;case"tradePending":changed.shopTradeRequest=new ShopRequest();break;case"moving":changed.moving=true;break;case"map":changed.record.map=1010;break;case"closed":changed.facility=null;break;}
   Check(!context.Matches(changed),"old facility drag cancelled after "+mutation);
  }
  Check(NativeItemDragContext.Facility(Facility(),"oldWindow",0,true,0)==null,"old UI cannot begin a drag from a new server window");Check(NativeItemDragContext.Facility(Facility(),"window1",int.MaxValue,true,int.MaxValue)==null,"invalid page arithmetic cannot alias a real entry");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Production pointer-gesture item ownership snapshots: personal/secondary/legacy bags, equipment, shop pages, storage drafts, stale sessions/windows/instances and unrelated-state preservation. No transaction policy, Unity EventSystem or original native packet-token inference."}));
 }
}
