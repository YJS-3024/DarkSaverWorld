using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
internal static class NativeMercenaryChecks
{
    static int checks;static string root;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
    static string Id()=>Guid.NewGuid().ToString("N");static string State(object value)=>JsonSerializer.Serialize(value,Json);
    static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    sealed class Memory:IEntryAccountStore
    {public LocalAccountDocument data=new LocalAccountDocument();public bool fail;public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}public bool TryWrite(LocalAccountDocument d){if(fail)return false;data=d.Copy();return true;}}
    sealed class Fixture
    {
        public readonly Memory store=new Memory();public EntryAccountService service;public EntryClient client;public LocalServerManager manager;public WorldDefinition definition;public int sends;public byte[] last;public NativeMercenaryRequest sent;public string npc;
        public WorldView View=>service.WorldSnapshot();public string Token=>View.facility.token;
        public Fixture(int job=-1,int count=0,bool known=true,bool money=true,int[] skills=null)
        {
            definition=JsonSerializer.Deserialize<WorldDefinition>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/World/KaosiaWorld.json")),Json);
            definition.growthCatalog=JsonSerializer.Deserialize<CharacterGrowthCatalog>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Data/Growth/CharacterGrowth.json")),Json);
            definition.mercenaryCatalog=JsonSerializer.Deserialize<NativeMercenaryCatalog>(File.ReadAllText(Path.Combine(root,"Assets/_Original/Resources/OriginalMercenarySkills.json")),Json);
            var map=definition.Map(2900);var staff=map.npcs.First();npc=staff.id;service=new EntryAccountService(store);client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"원본용병");
            var character=store.data.accounts[0].character;character.tutorialCompleted=true;
            character.world=new WorldRecord{map=2900,x=staff.counter[0].x,y=staff.counter[0].y,healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,goldKnown=money,gold=money?100000000UL:0,hiredJob=job,hiredCount=count,level=1,
                growth=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=Id(),skills=skills??new[]{20101}},
                roster=known?new CommanderRoster{revision=1,lastNotice=Id(),rows=new[]{new OwnedCommander{id=Id(),job=100,nativeRecord=37}}}:null};
            client.OpenLocalProfile();var begun=service.BeginWorld(definition);Check(begun.Accepted,"isolated actual mercenary world: "+begun.Failure);var opened=service.OpenWorldFacility(npc);Check(opened.Accepted,"isolated actual mercenary counter: "+opened.Failure);
            manager=new LocalServerManager(service);manager.Notify_MercenaryPacket+=bytes=>{sends++;last=(byte[])bytes.Clone();bytes[0]=0;};manager.Notify_MercenaryItemRequested+=request=>sent=request;
        }
        public void Approvals()=>manager.MercenaryAuthority=(r,v)=>new MercenaryNotice{id=Id(),request=r.id,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,commander=r.commander,job=r.job,hire=r.hire,approved=true,count=r.count+(r.hire?1:-1),expectedRevision=r.revision,currencyRevision=r.currencyRevision,amount=r.hire?r.price:0,gold=r.hire?v.record.gold-r.price.Value:v.record.gold};
    }
    static void NativeCases(JsonElement fixture)
    {
        foreach(var row in fixture.GetProperty("actions").EnumerateArray())
        {
            int commander=row.GetProperty("commander").GetInt32(),job=row.GetProperty("hired").GetInt32(),count=row.GetProperty("count").GetInt32(),selected=row.GetProperty("selectedJob").GetInt32();
            var offers=row.GetProperty("offers").EnumerateArray().Select(r=>new WorldHireOffer{job=r[0].GetInt32(),limit=r[1].GetInt32()}).ToArray();
            var view=new WorldView{characterId="owner",currencySession="session",record=new WorldRecord{map=2900,hiredJob=job,hiredCount=count},facility=new WorldFacilityView{token="window",npc="counter",kind=FacilityKind.Mercenary},commanders=new CommanderRoster{rows=new[]{new OwnedCommander{id="first",nativeRecord=commander}}}};
            var window=NativeMercenaryWindow.Begin(view,offers,"window");Check(window!=null,"valid native action fixture");var actual=window.Act(row.GetProperty("hire").GetBoolean(),selected,Id());var expected=row.GetProperty("expected");var packets=expected.GetProperty("packets");
            Check(actual==null?packets.GetArrayLength()==0:packets.GetArrayLength()==1&&Convert.ToHexString(actual.packet).ToLowerInvariant()==packets[0].GetString(),"470070/4701F0 packet parity");
            Check(window.PreviewJob==expected.GetProperty("previewJob").GetInt32()&&window.PreviewCount==expected.GetProperty("previewCount").GetInt32(),"native private preview parity");
            Check(view.record.hiredJob==job&&view.record.hiredCount==count,"preview cannot mutate authority");
        }
        foreach(var row in fixture.GetProperty("labels").EnumerateArray())
        {
            var unit=JsonSerializer.Deserialize<NativeHireUnit>(row.GetProperty("unit").GetRawText(),Json);var expected=row.GetProperty("expected");int x=row.GetProperty("x").GetInt32(),y=row.GetProperty("y").GetInt32();
            var count=expected[0];Check(count.GetProperty("text").GetString()==NativeMercenaryLayout.Count(row.GetProperty("count").GetInt32())&&count.GetProperty("rect")[0].GetInt32()==x+NativeMercenaryLayout.CountX&&count.GetProperty("rect")[1].GetInt32()==y+NativeMercenaryLayout.CountY&&count.GetProperty("rect")[2].GetInt32()==x+NativeMercenaryLayout.CountX+NativeMercenaryLayout.CountWidth&&count.GetProperty("rect")[3].GetInt32()==y+NativeMercenaryLayout.CountY+NativeMercenaryLayout.CountHeight,"native count text/rectangle");
            var details=expected[1];Check(details.GetProperty("text").GetString()==NativeMercenaryLayout.Details(unit,row.GetProperty("limit").GetInt32()),"actual native six-line unit description");
            Check(details.GetProperty("rect")[0].GetInt32()==x+NativeMercenaryLayout.TextX&&details.GetProperty("rect")[1].GetInt32()==y+NativeMercenaryLayout.TextY&&details.GetProperty("rect")[2].GetInt32()==x+NativeMercenaryLayout.TextX+NativeMercenaryLayout.TextWidth&&details.GetProperty("rect")[3].GetInt32()==y+NativeMercenaryLayout.TextY+NativeMercenaryLayout.TextHeight,"native details rectangle");
            var gold=expected[2];Check(gold.GetProperty("text").GetString()==NativeMercenaryLayout.Gold(row.GetProperty("gold").GetUInt64())&&gold.GetProperty("x").GetInt32()==x+NativeMercenaryLayout.GoldX&&gold.GetProperty("y").GetInt32()==y+NativeMercenaryLayout.GoldY&&gold.GetProperty("alignment").GetInt32()==2,"native32bit wallet label/right anchor");
            Check(count.GetProperty("color").GetInt32()==0&&details.GetProperty("color").GetInt32()==0&&gold.GetProperty("color").GetInt32()==(row.GetProperty("depth").GetInt32()==16?65535:255),"original label colors");
        }
    }
    static void ManagedCases()
    {
        var f=new Fixture();string before=State(f.View.record),disk=State(f.store.data);var id=Id();Check(f.manager.Request_MercenaryItem(true,200,f.Token,id).Accepted,"original raw hire without a fabricated approval");
        Check(f.sends==1&&f.last.SequenceEqual(new byte[]{0x4d,37,0,200,0,2,0})&&f.sent.packet[0]==0x4d&&f.sent.commander==37,"native WORD identity and isolated observers");
        Check(State(f.View.record)==before&&State(f.store.data)==disk,"unapproved request cannot change wallet/ownership/save");
        Check(!f.manager.Request_MercenaryItem(true,200,f.Token,id).Accepted&&f.sends==1,"duplicate request is not resent");Check(!f.manager.Request_MercenaryItem(true,291,f.Token,Id()).Accepted&&f.sends==1,"unlearned unit not class-granted");
        f.sent.packet[0]=0;Check(f.last[0]==0x4d,"metadata byte clone");Check(f.manager.MercenaryClient(f.Token).PreviewJob==-1&&f.manager.MercenaryClient(f.Token).PreviewCount==0,"new hire branch does not predict initial ownership");
        var owned=new Fixture(200,1);before=State(owned.View.record);Check(owned.manager.Request_MercenaryItem(true,291,owned.Token,Id()).Accepted&&owned.sent.job==200,"existing group ignores a different selected unit");
        Check(owned.manager.MercenaryClient(owned.Token).PreviewCount==2&&owned.View.record.hiredCount==1,"existing hire predicts only private count");Check(!owned.manager.Request_MercenaryItem(true,200,owned.Token,Id()).Accepted&&owned.sends==1,"native current group limit");
        Check(owned.manager.Request_OpenMercenaryClient("old-window").Failure==WorldFailure.StaleRequest&&owned.manager.MercenaryClient(owned.Token).PreviewCount==2,"old open request cannot discard the current private window");
        Check(owned.manager.Request_MercenaryItem(false,-1,owned.Token,Id()).Accepted&&owned.manager.Request_MercenaryItem(false,-1,owned.Token,Id()).Accepted,"private dismiss decrements to empty");Check(owned.manager.MercenaryClient(owned.Token).PreviewJob==-1&&State(owned.View.record)==before,"dismiss preview not an owned/refund change");
        Check(!owned.manager.Request_CancelMercenaryClient("wrong")&&owned.manager.MercenaryClient(owned.Token)!=null,"another window cannot cancel current client");Check(owned.manager.Request_CancelMercenaryClient(owned.Token)&&owned.manager.Request_OpenMercenaryClient(owned.Token).Accepted&&owned.manager.MercenaryClient(owned.Token).PreviewCount==1,"reopen restores authoritative private snapshot");
        var unknown=new Fixture(known:false);before=State(unknown.View.record);Check(unknown.manager.Request_MercenaryItem(true,200,unknown.Token,Id()).Failure==WorldFailure.RulesUnavailable&&unknown.sends==0&&State(unknown.View.record)==before,"missing native record is not inferred from job/slot");
        var poor=new Fixture(money:false);Check(poor.manager.Request_MercenaryItem(true,200,poor.Token,Id()).Accepted&&poor.sends==1&&!poor.View.record.goldKnown,"wire send independent of unknown financial policy");
        var approved=new Fixture();approved.Approvals();ulong gold=approved.View.record.gold;Check(approved.manager.Request_MercenaryItem(true,200,approved.Token,Id()).Accepted&&approved.View.record.hiredCount==1&&approved.View.record.gold==gold-(ulong)approved.definition.mercenaryCatalog.units.Single(u=>u.job==200).price,"explicit authority owns real hire and wallet");
        var trained=new Fixture(skills:new[]{21707});trained.Approvals();Check(trained.manager.Request_MercenaryItem(true,291,trained.Token,Id()).Accepted&&trained.View.record.hiredJob==291&&trained.sent.limit==4,"learned source skill controls authority eligibility beyond initial class stock");
        Check(approved.manager.Request_MercenaryItem(false,-1,approved.Token,Id()).Accepted&&approved.View.record.hiredCount==0,"explicit zero-refund fixture approval; no default refund");
        var failed=new Fixture(200,1);failed.Approvals();before=State(failed.View.record);failed.store.fail=true;Check(!failed.manager.Request_MercenaryItem(true,200,failed.Token,Id()).Accepted&&State(failed.View.record)==before,"failed persistence conserves real state");Check(failed.manager.MercenaryClient(failed.Token).PreviewCount==1,"explicit failed approval resets prediction");
        var closed=new Fixture();string old=closed.Token;closed.manager.Notify_MercenaryPacket+=_=>closed.service.CloseWorldFacility();Check(closed.manager.Request_MercenaryItem(true,200,old,Id()).Failure==WorldFailure.StaleRequest&&closed.View.facility==null,"synchronous close cannot apply old authority");
        var logout=new Fixture();string oldToken=logout.Token;logout.manager.Notify_MercenaryPacket+=_=>logout.client.Logout();Check(logout.manager.Request_MercenaryItem(true,200,oldToken,Id()).Failure==WorldFailure.StaleRequest,"wire observer logout cannot use old approval context");
        foreach(bool typedPhase in new[]{false,true})foreach(string change in new[]{"close","reopen","logout","reconnect","disable"})
        {
            var g=new Fixture(200,1);g.Approvals();string saved=State(g.store.data),token=g.Token;int typed=0;
            void Change(){if(change=="close"||change=="reopen"){g.manager.Request_CancelMercenaryClient(token);if(change=="reopen")Check(g.manager.Request_OpenMercenaryClient(token).Accepted,"replacement mercenary window fixture");}else if(change=="disable")g.manager.isActiveAndEnabled=false;else{g.client.Logout();if(change=="reconnect"){g.client.OpenLocalProfile();g.service.BeginWorld(g.definition);g.service.OpenWorldFacility(g.npc);}}}
            g.manager.Notify_MercenaryPacket+=_=>{if(!typedPhase)Change();};g.manager.Notify_MercenaryItemRequested+=_=>{typed++;if(typedPhase)Change();};
            Check(g.manager.Request_MercenaryItem(true,200,token,Id()).Failure==WorldFailure.StaleRequest&&typed==(typedPhase?1:0),"old mercenary window/connection cannot continue typed dispatch or approval");
            Check(State(g.store.data)==saved,"cancelled hire cannot spend, hire or save even with configured authority");
        }
        foreach(bool typedPhase in new[]{false,true})foreach(bool hire in new[]{false,true})
        {
            var g=new Fixture(200,1);g.Approvals();string token=g.Token,request=Id();ulong wallet=g.View.record.gold;int typed=0;
            void Approved(){var result=hire?g.manager.Request_HireMercenary(200,token,request):g.manager.Request_DismissMercenary(token,request);Check(result.Accepted,"explicit synchronous response fixture applies once");Check(g.manager.Request_OpenMercenaryClient(token).Accepted,"authoritative response refreshes mercenary projection");}
            g.manager.Notify_MercenaryPacket+=_=>{if(!typedPhase)Approved();};g.manager.Notify_MercenaryItemRequested+=_=>{typed++;if(typedPhase)Approved();};
            Check(g.manager.Request_MercenaryItem(hire,hire?200:-1,token,request).Accepted&&typed==(typedPhase?1:0),"synchronous response remains successful and cannot dispatch/approve a second time");
            ulong cost=(ulong)g.definition.mercenaryCatalog.units.Single(u=>u.job==200).price;Check(g.View.record.hiredCount==(hire?2:0)&&g.View.record.gold==(hire?wallet-cost:wallet),"synchronous hire/dismiss changes wallet and ownership exactly once");
        }
        var stopped=new Fixture();stopped.manager.isActiveAndEnabled=false;Check(stopped.manager.Request_MercenaryItem(true,200,stopped.Token,Id()).Failure==WorldFailure.Unauthenticated&&stopped.sends==0,"disabled component sends nothing");
    }
    static void Main(string[] args){root=args[0];using var fixture=JsonDocument.Parse(File.ReadAllText(args[1]));NativeCases(fixture.RootElement);ManagedCases();Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,mismatches=0,unityPlay=false}));}
}
