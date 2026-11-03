using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
static class NativeShopChecks
{
 static int checks;static string root;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};static string Id()=>Guid.NewGuid().ToString("N");static string State(object v)=>JsonSerializer.Serialize(v,Json);static void Check(bool v,string label){checks++;if(!v)throw new Exception(label);}
 sealed class Memory:IEntryAccountStore{public LocalAccountDocument data=new LocalAccountDocument();public bool fail;public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}public bool TryWrite(LocalAccountDocument d){if(fail)return false;data=d.Copy();return true;}}
 sealed class Fixture
 {
  public Memory store=new Memory();public EntryAccountService service;public EntryClient client;public LocalServerManager manager;public WorldDefinition definition;public int sends;public byte[] last;public NativeShopRequest sent;public string npc;public WorldView View=>service.WorldSnapshot();public string Token=>View.facility.token;
  public Fixture(int count=1,bool funds=false)
  {
   definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Json);var map=definition.Map(2000);var staff=map.npcs.First();npc=staff.id;
   service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"원본상점");var c=store.data.accounts[0].character;c.tutorialCompleted=true;c.world=new WorldRecord{map=map.id,x=staff.counter[0].x,y=staff.counter[0].y,healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,goldKnown=funds,gold=funds?20000UL:0,inventory=Enumerable.Range(0,count).Select(i=>new WorldOwnedItem{id=Id(),item=1,slot=i,remainingUses=i==0?(int?)3:null}).ToArray()};
   client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted&&service.OpenWorldFacility(npc).Accepted,"isolated real shop facility");manager=new LocalServerManager(service);manager.Notify_ShopPacket+=p=>{sends++;last=(byte[])p.Clone();p[0]=0;};manager.Notify_ShopItemRequested+=r=>sent=r;
  }
  public NativeItemDragContext Drag(bool buy)=>NativeItemDragContext.Facility(View,Token,0,buy,0);
  public NativeShopInventoryNotice Inventory()=>new NativeShopInventoryNotice{id=Id(),character=View.characterId,session=View.currencySession,token=Token,npc=npc,map=2000,approved=true,nativeBase=0,inventory=View.record.inventory.Take(1).Select(i=>new NativeOwnedItemToken{owned=i.id,item=i.item,token=65535}).ToArray()};
  public void Approvals()
  {
   manager.ShopQuoteAuthority=(r,v)=>new ShopQuote{id=Id(),request=r.Copy(),approved=true,price=r.buy?(ulong)definition.Item(r.item).price:123};
   manager.ShopTradeAuthority=(r,v)=>new ShopNotice{id=Id(),request=r.Copy(),approved=true,price=v.facility.shopQuote.price,gold=r.buy?v.record.gold-v.facility.shopQuote.price.Value:v.record.gold+v.facility.shopQuote.price.Value,acquired=r.buy?new WorldOwnedItem{id=Id(),item=r.item,slot=r.slot}:null};
  }
 }
 static void Main(string[] args)
 {
  root=args[0];using(var doc=JsonDocument.Parse(File.ReadAllText(args[1])))
  {
   Check(NativeShopPackets.FullBagMessage==doc.RootElement.GetProperty("full").GetProperty("messages")[0].GetString()&&NativeShopPackets.CategoryMessage==doc.RootElement.GetProperty("wrong").GetProperty("messages")[0].GetString(),"production client rejection words match original messages");
   foreach(var row in doc.RootElement.GetProperty("packets").EnumerateArray())
   {bool buy=row.GetProperty("buy").GetBoolean();int item=row.GetProperty("item").GetInt32();var actual=buy?NativeShopPackets.Buy(item):NativeShopPackets.Sell(item,row.GetProperty("token").GetUInt16());Check(actual.SequenceEqual(Convert.FromHexString(row.GetProperty("packet").GetString())),"native4B/4C serialization");}
   foreach(var row in doc.RootElement.GetProperty("allow").EnumerateArray())Check(NativeShopPackets.Allows(row.GetProperty("item").GetInt32(),row.GetProperty("base").GetInt32(),null)==row.GetProperty("allowed").GetBoolean(),"native resolved category range");
  }
  var f=new Fixture();string before=State(f.store.data),request=Id();Check(f.manager.Request_ShopItem(f.Drag(true),true,f.Token,request).Accepted&&f.sends==1&&f.last.SequenceEqual(new byte[]{0x4b,1,0}),"drop immediately sends4B, without a client price confirmation or funds default");Check(State(f.store.data)==before&&f.View.shopQuoteRequest==null&&f.View.facility.shopQuote==null,"unconfigured sender does not fabricate approval, quote or inventory");
  Check(!f.manager.Request_ShopItem(f.Drag(true),true,f.Token,request).Accepted&&f.sends==1,"request cannot replay");Check(!f.manager.Request_ShopItem(f.Drag(true),false,f.Token,Id()).Accepted&&f.sends==1,"right offer cannot impersonate owned sale");
  Check(!f.manager.Request_ShopItem(f.Drag(false),false,f.Token,Id()).Accepted&&f.sends==1,"missing opaque token cannot be replaced with bag slot");
  var metadata=f.Inventory();Check(f.manager.Notify_ShopInventory(metadata).Accepted,"explicit native category and GUID/token association");metadata.inventory[0].token=0;metadata.nativeBase=100;
  Check(f.manager.Request_ShopItem(f.Drag(false),false,f.Token,Id()).Accepted&&f.last.SequenceEqual(new byte[]{0x4c,1,0,255,255})&&State(f.store.data)==before,"sale sends copied native token and no invented sale price");
  var detached=f.sent;detached.packet[0]=0;Check(f.last[0]==0x4c,"observers cannot mutate prior wire packet");
  foreach(string mode in new[]{"owner","session","token","npc","base","both","missing-token","duplicate","unknown"})
  {
   var bad=f.Inventory();switch(mode){case "owner":bad.character=Id();break;case "session":bad.session=Id();break;case "token":bad.token=Id();break;case "npc":bad.npc="wrong";break;case "base":bad.nativeBase=50;break;case "both":bad.allowedItems=new[]{1};break;case "missing-token":bad.inventory[0].token=null;break;case "duplicate":bad.inventory=new[]{bad.inventory[0],bad.inventory[0]};break;case "unknown":bad.nativeBase=null;break;}
   Check(!f.manager.Notify_ShopInventory(bad).Accepted,"invalid client metadata: "+mode);
  }
  var wrong=f.Inventory();wrong.nativeBase=100;Check(f.manager.Notify_ShopInventory(wrong).Accepted&&f.manager.Request_ShopItem(f.Drag(false),false,f.Token,Id()).Failure==WorldFailure.ShopCategoryMismatch,"explicit other native shop category blocks sale");
  var custom=f.Inventory();custom.nativeBase=null;custom.allowedItems=new[]{1};Check(f.manager.Notify_ShopInventory(custom).Accepted&&f.manager.Request_ShopItem(f.Drag(false),false,f.Token,Id()).Accepted,"explicit resolved custom predicate, no inferred stock/category");custom.allowedItems[0]=100;Check(f.manager.Request_ShopItem(f.Drag(false),false,f.Token,Id()).Accepted,"custom predicate copied");
  Check(!f.manager.Request_CancelShopClient("other")&&f.manager.Request_CancelShopClient(f.Token)&&!f.manager.Request_ShopItem(f.Drag(false),false,f.Token,Id()).Accepted,"closing only own token clears opaque mapping");
  var full=new Fixture(28);Check(full.manager.Request_ShopItem(full.Drag(true),true,full.Token,Id()).Failure==WorldFailure.InventoryFull&&full.sends==0,"native28-item limit before4B");
  var stale=new Fixture();var drag=stale.Drag(true);stale.service.CloseWorldFacility();Check(!stale.manager.Request_ShopItem(drag,true,"old",Id()).Accepted&&stale.sends==0,"closed facility cannot send captured gesture");
  var disabled=new Fixture();disabled.manager.isActiveAndEnabled=false;Check(!disabled.manager.Request_ShopItem(disabled.Drag(true),true,disabled.Token,Id()).Accepted&&disabled.sends==0,"disabled component sends nothing");
  var logout=new Fixture();logout.manager.Notify_ShopPacket+=_=>logout.client.Logout();Check(!logout.manager.Request_ShopItem(logout.Drag(true),true,logout.Token,Id()).Accepted,"wire callback logout cannot invoke later approval on a new session");
  var approved=new Fixture(1,true);approved.Approvals();Check(approved.manager.Request_ShopItem(approved.Drag(true),true,approved.Token,Id()).Accepted&&approved.View.record.gold==12000&&approved.View.record.inventory.Length==2&&approved.sends==1,"one drop may be priced/committed only by both explicit server authorities");
  Check(approved.manager.Notify_ShopInventory(approved.Inventory()).Accepted&&approved.manager.Request_ShopItem(approved.Drag(false),false,approved.Token,Id()).Accepted&&approved.View.record.gold==12123&&approved.View.record.inventory.Length==1,"explicit approved sale123 is fixture-only and not a half-price policy");
  var failure=new Fixture(1,true);failure.Approvals();failure.store.fail=true;before=State(failure.store.data);Check(!failure.manager.Request_ShopItem(failure.Drag(true),true,failure.Token,Id()).Accepted&&State(failure.store.data)==before,"approval save failure preserves all funds and items");
  foreach(bool typedPhase in new[]{false,true})foreach(string change in new[]{"logout","disable","close"})
  {
   var g=new Fixture(1,true);g.Approvals();string saved=State(g.store.data);int typed=0;
   void Change(){if(change=="logout")g.client.Logout();else if(change=="disable")g.manager.isActiveAndEnabled=false;else g.service.CloseWorldFacility();}
   g.manager.Notify_ShopPacket+=_=>{if(!typedPhase)Change();};g.manager.Notify_ShopItemRequested+=_=>{typed++;if(typedPhase)Change();};
   Check(g.manager.Request_ShopItem(g.Drag(true),true,g.Token,Id()).Failure==WorldFailure.StaleRequest&&typed==(typedPhase?1:0),"expired shop callback cannot dispatch the old typed request or authority");
   Check(State(g.store.data)==saved,"configured approval cannot spend or grant after shop callback cancellation");
  }
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Original ordinary-shop4B/4C bytes and category predicate; production drag/account/manager guarded dispatch, explicit opaque associations and optional explicit local approvals. No client confirmation window, no houseD8, no default sale price/funds or physical mouse/Unity/server delivery claim."}));
 }
}
