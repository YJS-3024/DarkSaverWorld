using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
static class NativeWarehouseChecks
{
    static int checks;static string root;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static string Id()=>Guid.NewGuid().ToString("N");static string State(object value)=>JsonSerializer.Serialize(value,Json);
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    sealed class Memory:IEntryAccountStore
    {public LocalAccountDocument data=new LocalAccountDocument();public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}public bool TryWrite(LocalAccountDocument value){data=value.Copy();return true;}}
    sealed class Fixture
    {
        public Memory store=new Memory();public EntryAccountService service;public EntryClient client;public LocalServerManager manager;public WorldDefinition definition;public string npc;
        public NativeWarehouseOpenNotice open;public int sends;public byte[] last;public NativeWarehouseRequest itemRequest;public WorldView View=>service.WorldSnapshot();public string Token=>View.facility.token;
        public Fixture(int count=2)
        {
            definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Json);var map=definition.maps.First(m=>m.npcs.Any(n=>n.kind==(int)FacilityKind.Warehouse));var staff=map.npcs.First(n=>n.kind==(int)FacilityKind.Warehouse);npc=staff.id;
            service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"원본창고");var c=store.data.accounts[0].character;c.tutorialCompleted=true;
            c.world=new WorldRecord{map=map.id,x=staff.counter[0].x,y=staff.counter[0].y,healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,
                inventory=Enumerable.Range(0,count).Select(i=>new WorldOwnedItem{id=Id(),item=1,slot=i,remainingUses=i==0?(int?)3:null}).ToArray()};
            client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted&&service.OpenWorldFacility(npc).Accepted,"isolated storage interior and real facility guard");manager=new LocalServerManager(service);
            manager.Notify_WarehousePacket+=p=>{sends++;last=(byte[])p.Clone();p[0]=0;};manager.Notify_WarehouseItemRequested+=r=>itemRequest=r;
            open=Open();Check(manager.Notify_WarehouseOpened(open).Accepted&&last.SequenceEqual(new byte[]{0x97,0x34,0x12}),"explicit received key causes native97 only");
        }
        public NativeWarehouseOpenNotice Open()=>new NativeWarehouseOpenNotice{id=Id(),character=View.characterId,session=View.currencySession,token=Token,npc=npc,map=View.record.map,key=0x1234,approved=true,
            inventory=View.record.inventory.Take(1).Select(i=>new NativeOwnedItemToken{owned=i.id,item=i.item,token=65535}).ToArray()};
        public NativeWarehouseContentsNotice Notice(byte[] packet)=>new NativeWarehouseContentsNotice{id=Id(),open=open.id,character=open.character,session=open.session,token=open.token,approved=true,packet=packet};
        public void Reconnect(){client.Logout();client.OpenLocalProfile();Check(service.BeginWorld(definition).Accepted&&service.OpenWorldFacility(npc).Accepted,"new session and window");}
    }
    static byte[] Contents(params NativeWarehouseRow[] rows)
    {var bytes=new byte[7+rows.Length*4];bytes[0]=0x98;bytes[1]=(byte)rows.Length;bytes[2]=(byte)(rows.Length>>8);for(int i=0;i<rows.Length;i++){int at=3+i*4;bytes[at]=(byte)rows[i].item;bytes[at+1]=(byte)(rows[i].item>>8);bytes[at+2]=(byte)rows[i].token;bytes[at+3]=(byte)(rows[i].token>>8);}bytes[bytes.Length-4]=7;return bytes;}
    static void Main(string[] args)
    {
        root=args[0];using(var document=JsonDocument.Parse(File.ReadAllText(args[1])))
        {
            Check(NativeWarehousePackets.FullBagMessage==document.RootElement.GetProperty("full").GetProperty("messages")[0].GetString(),"production full-bag wording matches original client");
            foreach(var row in document.RootElement.GetProperty("queries").EnumerateArray())Check(NativeWarehousePackets.Query(row.GetProperty("key").GetUInt16()).SequenceEqual(Convert.FromHexString(row.GetProperty("packet").GetString())),"native query comparison");
            foreach(var row in document.RootElement.GetProperty("moves").EnumerateArray())
            {var expected=row.GetProperty("packet").GetString();Check(NativeWarehousePackets.Move(row.GetProperty("deposit").GetBoolean(),row.GetProperty("key").GetUInt16(),row.GetProperty("item").GetInt32(),row.GetProperty("token").GetUInt16()).SequenceEqual(Convert.FromHexString(expected)),"native item/token bit pattern comparison");}
            foreach(var row in document.RootElement.GetProperty("contents").EnumerateArray())
            {
                var known=row.GetProperty("known").EnumerateArray().Select(v=>v.GetInt32()).ToArray();Check(NativeWarehousePackets.TryContents(Convert.FromHexString(row.GetProperty("packet").GetString()),id=>known.Contains(id),out var rows,out uint fee),"native98 decode");
                var expected=JsonSerializer.Deserialize<NativeWarehouseRow[]>(row.GetProperty("rows").GetRawText(),Json);Check(State(rows)==State(expected)&&fee==row.GetProperty("fee").GetUInt32(),"original signed item lookup, opaque token and fee");
            }
            foreach(var row in document.RootElement.GetProperty("labels").EnumerateArray())
            {
                bool fee=row.GetProperty("fee").GetBoolean();var origin=row.GetProperty("origin").EnumerateArray().Select(v=>v.GetInt32()).ToArray();uint value=row.GetProperty("value").GetUInt32();
                Check(row.GetProperty("x").GetInt32()==origin[0]+(fee?NativeShopStorageLayout.FeeX:NativeShopStorageLayout.WalletX)
                    &&row.GetProperty("y").GetInt32()==origin[1]+(fee?NativeShopStorageLayout.FeeY:NativeShopStorageLayout.WalletY),"production money-label anchors match original bounded render block");
                Check(row.GetProperty("text").GetString()==(fee?NativeShopStorageLayout.Fee(value):value.ToString(System.Globalization.CultureInfo.InvariantCulture)),"unsigned fee format matches original MFC boundary");
            }
        }
        foreach(var packet in new byte[][]{null,Array.Empty<byte>(),new byte[]{0x98},new byte[7],new byte[]{0x98,255,255,0,0,0,0},new byte[]{0x98,1,0,0,0,0,0},new byte[]{0x98,0,0,0,0,0,0,0}})
            Check(!NativeWarehousePackets.TryContents(packet,_=>true,out _,out _),"malformed payload rejected before window update");
        Check(NativeWarehousePackets.Move(true,0,-1,0)==null&&NativeWarehousePackets.Move(true,0,32768,0)==null,"valid native source IDs cannot silently narrow");
        var f=new Fixture();string original=State(f.store.data);var window=f.manager.WarehouseClient(f.Token);Check(window!=null&&!window.Loaded&&window.Count==0,"no inferred initial storage/fee/capacity");
        var deposit=f.manager.Request_BeginWarehouseDrag(f.Token,true,f.View.record.inventory[0].id,-1);Check(deposit!=null,"owned native token allows deposit before list arrives");
        Check(f.manager.Request_BeginWarehouseDrag(f.Token,true,f.View.record.inventory[1].id,-1)==null,"unknown token is not inferred from bag slot or GUID");
        string request=Id();Check(f.manager.Request_WarehouseItem(deposit,request).Accepted&&f.last.SequenceEqual(new byte[]{0x99,0x34,0x12,1,0,255,255}),"native99 sends explicit opaque token");
        int sends=f.sends;Check(!f.manager.Request_WarehouseItem(deposit,request).Accepted&&f.sends==sends,"local gesture request ID cannot replay");
        Check(State(f.store.data)==original,"query and deposit request never move/charge/save items");
        var payload=Contents(new NativeWarehouseRow{item=1,token=32768},new NativeWarehouseRow{item=32767,token=5},new NativeWarehouseRow{item=200,token=2});var receipt=f.Notice(payload);
        Check(f.manager.Notify_WarehouseContents(receipt).Accepted&&window.Loaded&&window.Fee==7&&window.Count==2,"98 filters only unknown items, preserves stored order");
        Check(window.Rows[0].token==32768&&window.Rows[1].item==200,"opaque signed WORD remains exact bits");var detached=window.Rows;detached[0].item=999;Check(window.ItemAt(0)==1,"window rows are detached");
        Check(!f.manager.Notify_WarehouseContents(receipt).Accepted,"same receipt cannot replay");
        var retrieve=f.manager.Request_BeginWarehouseDrag(f.Token,false,null,0);Check(f.manager.Request_WarehouseItem(retrieve,Id()).Accepted&&f.last.SequenceEqual(new byte[]{0x9a,0x34,0x12,1,0,0,128}),"native9A request does not need invented local storage GUID");
        Check(State(f.store.data)==original&&f.View.record.storage.Length==0,"raw98 is client presentation, not an invented owned-item or balance approval");
        var revision=window.Revision;var malformed=f.Notice(new byte[]{0x98,1,0});Check(!f.manager.Notify_WarehouseContents(malformed).Accepted&&window.Revision==revision&&window.Count==2,"malformed refresh is atomic");
        var display=window.Display();var freshPayload=Contents(new NativeWarehouseRow{item=200,token=9});
        Check(f.manager.Notify_WarehouseContents(f.Notice(freshPayload)).Accepted&&!f.manager.WarehouseDragCurrent(retrieve),"new original list invalidates a captured storage row");
        Check(display.ItemAt(0)==1&&display.Count==2&&!display.Matches(window)&&window.Display().ItemAt(0)==200,"old rendered icons cannot silently retarget to refreshed list");
        foreach(string fault in new[]{"owner","session","token","open","denied","id"})
        {
            var bad=f.Notice(payload);switch(fault){case "owner":bad.character=Id();break;case "session":bad.session=Id();break;case "token":bad.token=Id();break;case "open":bad.open=Id();break;case "denied":bad.approved=false;break;case "id":bad.id="bad";break;}
            revision=window.Revision;Check(!f.manager.Notify_WarehouseContents(bad).Accepted&&window.Revision==revision,"receipt ownership guard: "+fault);
        }
        var changed=f.View;changed.record.inventory[0].remainingUses=0;Check(!window.Matches(deposit,changed),"drag does not ignore approved use count");
        var open=f.Open();open.inventory[0].owned=Id();Check(!f.manager.Notify_WarehouseOpened(open).Accepted&&ReferenceEquals(f.manager.WarehouseClient(f.Token),window),"bad association cannot replace current window");
        open=f.Open();open.inventory=new[]{open.inventory[0],open.inventory[0]};Check(!f.manager.Notify_WarehouseOpened(open).Accepted,"duplicate owned association rejected");
        open=f.Open();open.key=null;Check(!f.manager.Notify_WarehouseOpened(open).Accepted,"unknown storage key is not converted to zero");
        open=f.Open();open.inventory[0].token=null;Check(!f.manager.Notify_WarehouseOpened(open).Accepted,"unknown item token is not converted to zero");
        f.open.inventory[0].token=0;var still=f.manager.Request_BeginWarehouseDrag(f.Token,true,f.View.record.inventory[0].id,-1);Check(still.ItemToken==65535,"open notice association copied");
        var token=f.Token;Check(!f.manager.Request_CancelWarehouseClient("wrong")&&f.manager.WarehouseClient(token)!=null,"foreign close ignored");Check(f.manager.Request_CancelWarehouseClient(token)&&!f.manager.WarehouseDragCurrent(still),"closing window invalidates old pointer gesture");
        f.Reconnect();Check(!f.manager.Notify_WarehouseContents(receipt).Accepted,"old receipt after reconnect cannot recreate window");
        var full=new Fixture(28);Check(full.manager.Notify_WarehouseContents(full.Notice(Contents(new NativeWarehouseRow{item=1,token=0}))).Accepted,"full bag still shows storage");sends=full.sends;var blocked=full.manager.Request_BeginWarehouseDrag(full.Token,false,null,0);Check(full.manager.Request_WarehouseItem(blocked,Id()).Failure==WorldFailure.InventoryFull&&full.sends==sends,"native28-item bag limit blocks retrieval before9A");
        var disabled=new Fixture();disabled.manager.isActiveAndEnabled=false;Check(disabled.manager.WarehouseClient(disabled.Token)==null&&!disabled.manager.Notify_WarehouseContents(disabled.Notice(payload)).Accepted,"disabled component rejects raw receipts");
        Check(disabled.manager.Request_CancelWarehouseClient(null),"component cleanup can cancel while disabled");
        var reentrant=new Fixture();reentrant.manager.Notify_WarehousePacket+=_=>reentrant.client.Logout();Check(!reentrant.manager.Notify_WarehouseOpened(reentrant.Open()).Accepted,"query observer replacing session cannot report old window ready");
        foreach(bool typedPhase in new[]{false,true})foreach(string change in new[]{"logout","reconnect","disable","close","replace"})
        {
            var g=new Fixture();var capturedToken=g.Token;var drag=g.manager.Request_BeginWarehouseDrag(capturedToken,true,g.View.record.inventory[0].id,-1);string saved=State(g.store.data);int typed=0;bool callbackChanged=false;
            void Change(){if(change=="logout")g.client.Logout();else if(change=="reconnect")g.Reconnect();else if(change=="disable")g.manager.isActiveAndEnabled=false;else if(change=="close")g.manager.Request_CancelWarehouseClient(capturedToken);else Check(g.manager.Notify_WarehouseOpened(g.Open()).Accepted,"replacement query accepts its own live window");}
            g.manager.Notify_WarehousePacket+=_=>{if(!typedPhase&&!callbackChanged){callbackChanged=true;Change();}};g.manager.Notify_WarehouseItemRequested+=_=>{typed++;if(typedPhase)Change();};
            Check(g.manager.Request_WarehouseItem(drag,Id()).Failure==WorldFailure.StaleRequest&&typed==(typedPhase?1:0),"expired storage window cannot continue captured item request callbacks");
            Check(State(g.store.data)==saved,"cancelled warehouse request cannot move items, charge a fee or save");
        }
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Native97/98/99/9A comparisons plus production account/manager/UI-context model: explicit opaque associations, detached raw presentation, revision/replay/ownership/full bag and lifecycle guards. Component host boundary isolated. No automatic inventory/currency approval, warehouse capacity/fee-source, local storage GUID invention or physical drag/Unity rendering claim."}));
    }
}
