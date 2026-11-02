using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class RevivalChecks
{
    private static int assertions;private static string root;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static string Id()=>Guid.NewGuid().ToString("N");
    private static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
    private static T Read<T>(string file)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",file)),Options);
    private static string State(object value)=>JsonSerializer.Serialize(value,Options);
    private sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();public bool fail;
        public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}
    }
    private sealed class Fixture
    {
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;public int temple;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(bool ghost=true,bool knownGold=true,ulong gold=500)
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");
            temple=definition.maps.Single(m=>m.npcs.Any(n=>n.kind==(int)FacilityKind.Temple)).id;
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"부활검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=0,maxHp=200,mp=3,maxMp=65,ghost=ghost,goldKnown=knownGold,gold=knownGold?gold:0};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated ghost profile begins without a policy injection");OpenTemple();
        }
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"native world walking request");server.AdvanceWorld(100000);Check(View.record.x==x&&View.record.y==y,"native world walking arrival");}
        public void OpenTemple()
        {
            if(View.record.map!=temple)
            {var gate=definition.Map(View.record.map).portals.First(p=>p.targetMap==temple);Walk(gate.cells[0].x,gate.cells[0].y);Check(server.EnterWorldPortal(gate.id,false).Accepted,"actual temple portal entered");}
            var npc=definition.Map(temple).npcs.Single();
            if(View.record.x!=npc.counter[0].x||View.record.y!=npc.counter[0].y)Walk(npc.counter[0].x,npc.counter[0].y);
            Check(server.OpenWorldFacility(npc.id).Accepted,"actual counter and temple NPC interaction");
        }
        public RevivalQuote Quote(ulong? fee=100,RevivalQuoteRequest r=null)
        {
            r=r??View.revivalQuoteRequest;return new RevivalQuote{approved=true,id=Id(),request=r.id,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,revision=r.revision,fee=fee};
        }
        public void Offer(ulong? fee=100)
        {Check(server.ExecuteRevivalQuote(View.facility.token,Id(),(r,v)=>Quote(fee,r)).Accepted,"approved quote received through the real quote request/provider path");}
        public void Queue()=>Check(server.RequestTempleRevival(View.facility.token,Id()).Accepted,"revival request queued");
        public RevivalNotice Notice(RevivalRequest r=null)
        {
            r=r??View.revivalRequest;return new RevivalNotice{approved=true,id=Id(),request=r.id,quote=r.quote,character=r.character,session=r.session,token=r.token,npc=r.npc,map=r.map,revision=r.revision,
                gold=View.record.gold-View.facility.revivalQuote.fee.Value,health=new CharacterGrowthHealth{hp=100,maxHp=200,mp=7,maxMp=65}};
        }
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"real service profile reconnect");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var before=State(f.View.record);string token=f.View.facility.token;
        Check(!f.definition.rules.reviveRuleKnown&&f.server.ReviveWorld(token,Id()).Failure==WorldFailure.RulesUnavailable,"production resurrection policy remains unknown/held");
        Check(f.server.ExecuteRevivalQuote(token,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.revivalQuoteRequest==null&&State(f.View.record)==before,"missing quote authority does not invent a fee or lock controls");
        Check(f.server.RequestTempleRevival(token,Id()).Failure==WorldFailure.RulesUnavailable,"no revival without a displayed approved quote");
        Check(f.server.RequestRevivalQuote(token,Id()).Accepted,"quote request queued");var pending=f.View.revivalQuoteRequest;
        Check(State(f.View.record)==before&&f.View.facility.revivalQuote==null,"quote request alone changes no health or balance");
        Check(f.server.RequestRevivalQuote(token,Id()).Failure==WorldFailure.Busy,"duplicate pending quote rejected");
        var missing=f.Quote(null);Check(f.server.ReceiveRevivalQuote(missing).Failure==WorldFailure.RulesUnavailable&&State(f.View.record)==before,"missing fee is not treated as free resurrection");
        var quote=f.Quote();Check(f.server.ReceiveRevivalQuote(quote).Accepted&&f.View.facility.revivalQuote.fee==100,"explicit quote displayed");
        quote.fee=0;var exposed=f.View;exposed.facility.revivalQuote.fee=0;
        Check(f.View.facility.revivalQuote.fee==100,"provider/view mutation cannot alter confirmed price");
        Check(f.server.ExecuteTempleRevival(token,Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.revivalRequest==null,"missing approval authority cannot charge quote");
        f.Queue();var request=f.View.revivalRequest.id;var notice=f.Notice();before=State(f.View.record);
        Check(f.View.record.ghost&&f.View.record.hp==0&&f.View.record.gold==500,"request does not predict healing or debit");
        Check(f.server.RequestTempleRevival(token,Id()).Failure==WorldFailure.Busy,"one unresolved revival command");
        f.store.fail=true;
        Check(f.server.ReceiveTempleRevival(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.revivalRequest!=null,"failed save atomically preserves ghost, health, currency and pending request");
        f.store.fail=false;Check(f.server.ReceiveTempleRevival(notice).Accepted,"same approval can retry after storage recovery");
        Check(!f.View.record.ghost&&f.View.record.hp==100&&f.View.record.mp==7&&f.View.record.gold==400&&f.View.record.maxHp==200&&f.View.record.maxMp==65,"explicit partial HP/MP and exact quoted remaining balance applied, no automatic full refill");
        Check(f.View.record.revival.revision==1&&f.View.record.revival.fee==100&&f.View.record.revival.request==request&&f.View.facility.revivalQuote==null&&f.View.revivalRequest==null,"durable receipt and cleared request/quote");
        Check(f.store.data.accounts[0].character.job==100&&f.View.record.growth==null&&f.View.record.inventory.Length==0,"revival preserves character identity, growth and items");
        before=State(f.View.record);Check(!f.server.ReceiveTempleRevival(notice).Accepted&&State(f.View.record)==before,"duplicate approval cannot spend/heal twice");
        Check(f.server.RequestRevivalQuote(token,Id()).Failure==WorldFailure.InvalidStage,"healthy character cannot request another paid resurrection");
        var location=(f.View.record.map,f.View.record.x,f.View.record.y);f.Reload();
        Check(!f.View.record.ghost&&f.View.record.hp==100&&f.View.record.gold==400&&f.View.record.revival.id==notice.id&&(f.View.record.map,f.View.record.x,f.View.record.y)==location&&f.View.facility==null,"reconnect restores paid health/receipt at the same location with no stale open quote");
        var denied=new Fixture();denied.server.RequestRevivalQuote(denied.View.facility.token,Id());var deny=denied.Quote();deny.approved=false;
        before=State(denied.View.record);Check(!denied.server.ReceiveRevivalQuote(deny).Accepted&&denied.View.revivalQuoteRequest==null&&State(denied.View.record)==before,"denied quote unlocks without mutation");
        denied.Offer();denied.Queue();var no=denied.Notice();no.approved=false;
        Check(!denied.server.ReceiveTempleRevival(no).Accepted&&denied.View.revivalRequest==null&&denied.View.record.gold==500,"denied revival unlocks without charge");
        var insufficient=new Fixture(gold:50);insufficient.Offer();
        Check(insufficient.server.RequestTempleRevival(insufficient.View.facility.token,Id()).Failure==WorldFailure.InsufficientFunds&&insufficient.View.record.gold==50,"insufficient funds leave ghost and balance intact");
        var unknown=new Fixture(knownGold:false);unknown.Offer(0);
        Check(unknown.server.RequestTempleRevival(unknown.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable&&!unknown.View.record.goldKnown,"unknown wallet is not fabricated as zero, even with explicit zero quote");
        var trainingDeath=new Fixture(ghost:false);Check(trainingDeath.server.RequestRevivalQuote(trainingDeath.View.facility.token,Id()).Failure==WorldFailure.InvalidStage,"HP0 without ghost does not get misclassified as normal-combat death");
        foreach(string error in new[]{"owner","session","token","npc","map","request","revision","id","fee"})
        {
            var invalid=new Fixture();invalid.server.RequestRevivalQuote(invalid.View.facility.token,Id());var q=invalid.Quote();
            switch(error)
            {case "owner":q.character=Id();break;case "session":q.session=Id();break;case "token":q.token=Id();break;case "npc":q.npc="invalid";break;case "map":q.map=1000;break;case "request":q.request=Id();break;case "revision":q.revision++;break;case "id":q.id="bad";break;case "fee":q.fee=null;break;}
            before=State(invalid.View.record);Check(!invalid.server.ReceiveRevivalQuote(q).Accepted&&State(invalid.View.record)==before,"invalid quote preserves state: "+error);
        }
        foreach(string error in new[]{"owner","session","token","npc","map","request","quote","revision","id","hp-zero","hp-range","mp-range","max-change","gold","gold-unknown"})
        {
            var invalid=new Fixture();invalid.Offer();invalid.Queue();var n=invalid.Notice();
            switch(error)
            {case "owner":n.character=Id();break;case "session":n.session=Id();break;case "token":n.token=Id();break;case "npc":n.npc="bad";break;case "map":n.map=1000;break;case "request":n.request=Id();break;case "quote":n.quote=Id();break;case "revision":n.revision++;break;case "id":n.id="bad";break;case "hp-zero":n.health.hp=0;break;case "hp-range":n.health.hp=201;break;case "mp-range":n.health.mp=66;break;case "max-change":n.health.maxHp=201;break;case "gold":n.gold=401;break;case "gold-unknown":n.gold=null;break;}
            before=State(invalid.View.record);Check(!invalid.server.ReceiveTempleRevival(n).Accepted&&State(invalid.View.record)==before,"invalid approval preserves state: "+error);
        }
        var closed=new Fixture();closed.server.RequestRevivalQuote(closed.View.facility.token,Id());var oldQuote=closed.Quote();closed.server.CloseWorldFacility();closed.OpenTemple();
        Check(!closed.server.ReceiveRevivalQuote(oldQuote).Accepted,"closing/reopening NPC invalidates old quote token");
        closed.Offer();closed.Queue();var oldApproval=closed.Notice();closed.server.CloseWorldFacility();closed.OpenTemple();Check(!closed.server.ReceiveTempleRevival(oldApproval).Accepted,"closing/reopening invalidates in-flight approval");
        var resumed=new Fixture();resumed.Offer();resumed.Queue();oldApproval=resumed.Notice();resumed.Reload();resumed.OpenTemple();
        Check(!resumed.server.ReceiveTempleRevival(oldApproval).Accepted&&resumed.View.record.ghost&&resumed.View.record.gold==500,"pending revival not saved; pre-reconnect approval cannot charge on a new session");
        foreach(string changed in new[]{"gold","items","receipt"})
        {
            var stale=new Fixture();stale.Offer();stale.Queue();var response=stale.Notice();var disk=stale.store.data.accounts[0].character.world;
            if(changed=="gold")disk.gold=450;
            if(changed=="items")disk.inventory=new[]{new WorldOwnedItem{id=Id(),item=200,slot=0}};
            if(changed=="receipt")disk.revival=new RevivalReceipt{id=Id(),request=Id(),quote=Id(),revision=1,fee=0};
            var stored=State(stale.store.data);before=State(stale.View.record);
            Check(stale.server.ReceiveTempleRevival(response).Failure==WorldFailure.StaleRequest&&State(stale.store.data)==stored&&State(stale.View.record)==before,"fresh persisted state cannot be overwritten by stale revival: "+changed);
        }
        var growth=new Fixture();growth.Offer();var growthId=Id();growth.server.ReceiveCharacterGrowth(new CharacterGrowthNotice{approved=true,id=growthId,character=growth.View.characterId,session=growth.View.growthSession,state=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=growthId}});
        Check(growth.View.facility.revivalQuote==null&&growth.server.RequestTempleRevival(growth.View.facility.token,Id()).Failure==WorldFailure.StaleRequest,"changed growth context invalidates previously displayed fee");
        var distant=new Fixture();distant.Offer();distant.Queue();var distantNotice=distant.Notice();var staff=distant.definition.Map(distant.temple).npcs.Single();staff.x=18;staff.y=1;staff.counter=Array.Empty<WorldPoint>();
        before=State(distant.View.record);Check(distant.server.ReceiveTempleRevival(distantNotice).Failure==WorldFailure.TooFar&&State(distant.View.record)==before,"approval cannot bypass current NPC distance/counter rules");
        foreach(string problem in new[]{"null","throw","wrong-request"})
        {
            var failure=new Fixture();before=State(failure.View.record);
            Check(!failure.server.ExecuteRevivalQuote(failure.View.facility.token,Id(),(r,v)=>{if(problem=="null")return null;if(problem=="throw")throw new Exception("fixture");var q=failure.Quote(r:r);q.request=Id();return q;}).Accepted
                &&failure.View.revivalQuoteRequest==null&&State(failure.View.record)==before,"quote callback failure unlocks without mutation: "+problem);
            failure.Offer();before=State(failure.View.record);
            Check(!failure.server.ExecuteTempleRevival(failure.View.facility.token,Id(),(r,v)=>{if(problem=="null")return null;if(problem=="throw")throw new Exception("fixture");var n=failure.Notice(r);n.request=Id();return n;}).Accepted
                &&failure.View.revivalRequest==null&&State(failure.View.record)==before,"approval callback failure unlocks without mutation: "+problem);
        }
        var paid=new Fixture();paid.Offer();Check(paid.server.ExecuteTempleRevival(paid.View.facility.token,Id(),(r,v)=>paid.Notice(r)).Accepted&&!paid.View.record.ghost,"configured provider executes full quote/confirm/approve path");
        var free=new Fixture();free.Offer(0);Check(free.server.ExecuteTempleRevival(free.View.facility.token,Id(),(r,v)=>free.Notice(r)).Accepted&&free.View.record.gold==500,"explicit zero-cost server quote works without a default free policy");
        // Rejoin a scenario after revival, die, return as ghost, then pay a second independent quote.
        var campaign=Read<CampaignDefinition>("Data/Campaign/CampaignCatalog.json");using(var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Tools/OriginalWorldChecks/campaign-assets.json"))))
        {
            var scripts=manifest.RootElement.GetProperty("scripts").EnumerateArray().Select(p=>p.GetString()).ToArray();var scene=campaign.Map(26);
            scene.intro=scene.introIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[scene.introIndex]));scene.outro=scene.outroIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[scene.outroIndex]));
            var spawn=scene.entries.First(e=>e.unit==0);campaign.connections=new[]{new CampaignConnection{fromMap=1000,toMap=26,x=spawn.x,y=spawn.y,evidence="isolated post-revival scenario route",cells=new[]{new WorldPoint{x=21,y=21}}}};campaign.Merge(paid.definition);
        }
        Check(paid.server.CloseWorldFacility().Accepted,"close temple modal before walking away");
        var exit=paid.definition.Map(paid.temple).portals.Single();paid.Walk(exit.cells[0].x,exit.cells[0].y);paid.server.EnterWorldPortal(exit.id,false);paid.Walk(21,21);
        Check(paid.server.EnterCampaignMap(26).Accepted,"revived commander can enter combat again");paid.server.AdvanceWorld(0);
        var defeatId=Id();var defeat=new CampaignExitNotice{approved=true,id=defeatId,character=paid.View.characterId,session=paid.View.campaignSession,visit=paid.View.record.scenarioVisit.id,kind=CampaignExitKind.Defeat,
            health=new CharacterGrowthHealth{hp=0,maxHp=200,mp=3,maxMp=65},result=new CampaignResultRecord{id=defeatId,visit=paid.View.record.scenarioVisit.id,map=26,outcome="defeat"}};
        Check(paid.server.ReceiveScenarioExit(defeat).Accepted&&paid.server.ConfirmScenarioResult(defeat.id,Id()).Accepted&&paid.View.record.ghost,"later approved death creates a new ghost through actual return path");
        paid.OpenTemple();paid.Offer(50);Check(paid.server.ExecuteTempleRevival(paid.View.facility.token,Id(),(r,v)=>paid.Notice(r)).Accepted&&paid.View.record.revival.revision==2&&paid.View.record.gold==350,"independent later resurrection pays once with a new receipt revision");
        string path=Path.Combine(Path.GetTempPath(),"ds-revival-"+Id(),"accounts.json");
        try
        {
            var file=new LocalAccountFileStore(path);Check(file.TryWrite(paid.store.data)&&file.TryRead(out var saved)&&saved.accounts[0].character.world.revival.revision==2,"real disk format preserves paid revival receipt");
            var reloaded=new EntryAccountService(file);new EntryClient(reloaded).OpenLocalProfile();Check(reloaded.BeginWorld(paid.definition).Accepted&&!reloaded.WorldSnapshot().record.ghost&&reloaded.WorldSnapshot().record.gold==350&&reloaded.WorldSnapshot().record.hp==100,"file-backed resurrection health/balance survives login");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual temple quote/approval/persistence services with isolated explicit fees and health; no default resurrection economy or original wire protocol claim."}));
    }
}
