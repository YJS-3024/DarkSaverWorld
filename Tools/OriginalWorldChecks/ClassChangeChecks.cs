using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class ClassChangeChecks
{
    private static int assertions;private static string root;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static string Id()=>Guid.NewGuid().ToString("N");
    private static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
    private static string State(object value)=>JsonSerializer.Serialize(value,Options);
    private static T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",path)),Options);
    private sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();public bool fail;
        public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}
    }
    private sealed class Fixture
    {
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(bool growth=true)
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"전직검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;
            c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45,
                inventory=new[]{new WorldOwnedItem{id=Id(),item=1,slot=0},new WorldOwnedItem{id=Id(),item=361,slot=1}},hiredJob=200,hiredCount=1};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world without invented growth/funds");
            if(growth)
            {
                string id=Id();Check(server.ReceiveCharacterGrowth(new CharacterGrowthNotice{approved=true,id=id,character=View.characterId,session=View.growthSession,
                    state=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=id,experience=12345,points=7,magic=new[]{101},skills=new[]{10101}}}).Accepted,"isolated explicit initial growth snapshot");
            }
        }
        public ClassOptionsNotice Notice(ClassOptionsRequest r=null,int? points=2)=>new ClassOptionsNotice{id=Id(),request=(r??View.classOptionsRequest).Copy(),approved=true,classPoints=points,
            left=new ClassCandidate{job=101,description="fixture left"},right=new ClassCandidate{job=103,description="fixture right"},options=new[]{new ClassCandidate{job=110,description="fixture lower0"},new ClassCandidate{job=120,description="fixture lower1"}}};
        public void QueueOptions()=>Check(server.RequestClassOptions(Id()).Accepted,"candidate query queued");
        public void Info(int? points=2)=>Check(server.ExecuteClassOptions(Id(),(r,v)=>Notice(r,points)).Accepted,"explicit candidate/point response");
        public void QueueChange(int slot=2,int index=1)=>Check(server.RequestClassChange(slot,index,View.classOptions.id,Id()).Accepted,"received slot/index change requested");
        public ClassChangeNotice Approval(ClassChangeRequest r=null)
        {
            r=r??View.classChangeRequest;string id=Id();var state=View.record.growth.Copy();state.job=r.job;state.classLevel=1;state.totalLevel=2;state.revision=r.revision+1;state.lastNotice=id;
            state.points=4;state.experience=456;state.magic=new[]{2001,2101};state.skills=new[]{10101,10201};state.stats=new CharacterGrowthStats{attack=25,defense=10,magicAttack=8,magicDefense=6};
            return new ClassChangeNotice{id=id,request=r.Copy(),approved=true,state=state};
        }
        public void Approve(int slot=2,int index=1)=>Check(server.ExecuteClassChange(slot,index,View.classOptions.id,Id(),(r,v)=>Approval(r)).Accepted,"approved explicit class result");
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"grid walk request");server.AdvanceWorld(100000);Check(View.record.x==x&&View.record.y==y,"grid arrival");}
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"profile reconnect");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var before=State(f.View.record);
        Check(f.server.ExecuteClassOptions(Id(),null).Failure==WorldFailure.RulesUnavailable&&f.View.classOptionsRequest==null,"no authority creates no candidates or points");
        Check(new Fixture(false).server.RequestClassOptions(Id()).Failure==WorldFailure.RulesUnavailable,"no received growth means no assumed level/eligibility");
        Check(f.server.RequestClassChange(0,0,Id(),Id()).Failure==WorldFailure.StaleRequest,"no class change from static table alone");
        f.QueueOptions();var options=f.Notice();Check(State(f.View.record)==before&&f.View.classOptions==null,"query does not spend points or change class");
        Check(f.server.RequestClassOptions(Id()).Failure==WorldFailure.Busy,"one query pending");
        Check(f.server.ReceiveClassOptions(options).Accepted&&f.View.classOptions.classPoints==2&&State(f.View.record)==before,"received candidates are session display only");
        options.options[1].job=100;options.right.description="changed";Check(f.View.classOptions.options[1].job==120&&f.View.classOptions.right.description=="fixture right","candidate input deeply detached");
        var copy=f.View.classOptions;copy.options[0].description="changed";Check(f.View.classOptions.options[0].description=="fixture lower0","candidate view deeply detached");
        foreach(var choice in new[]{(-1,0),(3,0),(0,1),(1,-1),(2,2)})Check(f.server.RequestClassChange(choice.Item1,choice.Item2,f.View.classOptions.id,Id()).Failure==WorldFailure.InvalidInput,"invalid slot/index rejected");
        Check(f.server.RequestClassChange(0,0,Id(),Id()).Failure==WorldFailure.StaleRequest,"old candidate set cannot be selected");
        f.QueueChange();var n=f.Approval();Check(f.View.classChangeRequest.job==120&&f.View.classChangeRequest.slot==2&&f.View.classChangeRequest.index==1,"bottom selection maps exact candidate");
        Check(State(f.View.record)==before&&f.View.record.growth.points==7,"request does not spend generic growth points or change profile");
        Check(f.server.RequestClassOptions(Id()).Failure==WorldFailure.Busy&&f.server.RequestClassChange(0,0,f.View.classOptions.id,Id()).Failure==WorldFailure.Busy,"query and change cannot overlap approval");
        f.store.fail=true;Check(f.server.ReceiveClassChange(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.classChangeRequest!=null,"failed save preserves original growth and pending retry");
        f.store.fail=false;Check(f.server.ReceiveClassChange(n).Accepted,"same approved result retries");
        Check(f.View.record.growth.job==120&&f.View.record.level==2&&f.View.record.growth.classLevel==1&&f.View.record.growth.experience==456&&f.View.record.growth.points==4,"only explicit whole growth result applied");
        Check(f.View.record.growth.magic.SequenceEqual(new[]{2001,2101})&&f.View.record.growth.skills.SequenceEqual(new[]{10101,10201})&&!f.View.record.growth.magic.Contains(101),"server whole lists replace old spells/skills without automatic union");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&!f.View.record.goldKnown&&f.View.record.inventory.Length==2&&f.View.record.hiredJob==200&&f.View.record.hiredCount==1,"absent health leaves injury; money/items/hire preserved");
        Check(f.View.record.classChange.growthRevision==2&&f.View.record.classChange.previousJob==100&&f.View.record.classChange.slot==2&&f.View.classOptions==null&&f.View.classChangeRequest==null,"receipt saved; old candidates/approval cleared");
        n.state.magic[0]=101;Check(f.View.record.growth.magic[0]==2001,"received state deeply detached");before=State(f.View.record);Check(!f.server.ReceiveClassChange(n).Accepted&&State(f.View.record)==before,"duplicate approval cannot apply twice");
        f.Reload();Check(f.View.record.growth.job==120&&f.View.record.classChange.job==120&&f.View.classOptions==null&&f.View.classOptionsRequest==null,"class/spells/receipt reconnect without stale options");
        var zero=new Fixture();zero.Info(0);Check(zero.server.RequestClassChange(1,0,zero.View.classOptions.id,Id()).Failure==WorldFailure.InvalidInput&&zero.View.record.growth.points==7,"native class counter zero blocks despite generic points7");
        var empty=new Fixture();empty.QueueOptions();var e=empty.Notice();e.left=e.right=null;e.options=Array.Empty<ClassCandidate>();Check(empty.server.ReceiveClassOptions(e).Accepted&&empty.server.RequestClassChange(2,0,empty.View.classOptions.id,Id()).Failure==WorldFailure.InvalidInput,"empty received candidates remain empty");
        foreach(string field in new[]{"id","request","character","session","map","revision","missing-points","negative","overflow","missing-list","null-option","unknown-job"})
        {
            var bad=new Fixture();bad.QueueOptions();var notice=bad.Notice();var state=State(bad.View.record);
            switch(field)
            {
                case "id":notice.id="bad";break;case "request":notice.request.id=Id();break;case "character":notice.request.character=Id();break;case "session":notice.request.session=Id();break;case "map":notice.request.map++;break;case "revision":notice.request.revision++;break;
                case "missing-points":notice.classPoints=null;break;case "negative":notice.classPoints=-1;break;case "overflow":notice.classPoints=65536;break;case "missing-list":notice.options=null;break;case "null-option":notice.options[0]=null;break;case "unknown-job":notice.left.job=999999;break;
            }
            Check(!bad.server.ReceiveClassOptions(notice).Accepted&&bad.View.classOptions==null&&State(bad.View.record)==state,"invalid options leave profile unchanged: "+field);
        }
        foreach(string field in new[]{"id","request","options","character","session","map","revision","slot","index","job","missing-state","target","state-revision","notice-id","magic","health","ghost-health"})
        {
            var bad=new Fixture();if(field=="ghost-health"){var r=bad.store.data.accounts[0].character.world;r.ghost=true;r.hp=0;bad.Reload();}bad.Info();bad.QueueChange();var notice=bad.Approval();var state=State(bad.View.record);
            switch(field)
            {
                case "id":notice.id="bad";break;case "request":notice.request.id=Id();break;case "options":notice.request.options=Id();break;case "character":notice.request.character=Id();break;case "session":notice.request.session=Id();break;case "map":notice.request.map++;break;
                case "revision":notice.request.revision++;break;case "slot":notice.request.slot=0;break;case "index":notice.request.index=0;break;case "job":notice.request.job=110;break;case "missing-state":notice.state=null;break;
                case "target":notice.state.job=110;break;case "state-revision":notice.state.revision++;break;case "notice-id":notice.state.lastNotice=Id();break;case "magic":notice.state.magic=new[]{999999};break;
                case "health":notice.health=new CharacterGrowthHealth{hp=999,maxHp=100,mp=0,maxMp=1};break;case "ghost-health":notice.health=new CharacterGrowthHealth{hp=1,maxHp=200,mp=0,maxMp=45};break;
            }
            Check(!bad.server.ReceiveClassChange(notice).Accepted&&State(bad.View.record)==state,"invalid approval leaves profile unchanged: "+field);
        }
        foreach(int slot in new[]{0,1})
        {var side=new Fixture();side.Info();side.Approve(slot,0);Check(side.View.record.growth.job==(slot==0?101:103)&&side.View.record.classChange.slot==slot,"left/right mapping follows received slot");}
        var health=new Fixture();health.Info();health.QueueChange();var h=health.Approval();h.health=new CharacterGrowthHealth{hp=19,maxHp=220,mp=4,maxMp=50};Check(health.server.ReceiveClassChange(h).Accepted&&health.View.record.hp==19&&health.View.record.maxHp==220,"explicit validated health may accompany class result");
        var closed=new Fixture();closed.Info();closed.QueueChange();var old=closed.Approval();Check(closed.server.CloseClassOptions().Accepted&&!closed.server.ReceiveClassChange(old).Accepted&&closed.View.record.growth.job==100,"closing class UI invalidates old approval");
        var walked=new Fixture();walked.Info();walked.QueueChange();old=walked.Approval();walked.Walk(21,22);Check(walked.View.classOptions==null&&walked.View.classChangeRequest==null&&!walked.server.ReceiveClassChange(old).Accepted,"new walking invalidates previous options/change");
        var xp=new Fixture();xp.Info();xp.QueueChange();old=xp.Approval();Check(xp.server.ReceiveCharacterExperience(new CharacterExperienceNotice{id=Id(),approved=true,character=xp.View.characterId,session=xp.View.growthSession,expectedRevision=1,experience=555}).Accepted,"new authoritative XP revision");
        Check(xp.View.classOptions==null&&xp.View.classChangeRequest==null&&!xp.server.ReceiveClassChange(old).Accepted&&xp.View.record.growth.experience==555,"new XP invalidates candidate approval");
        var interrupted=new Fixture();interrupted.Info();interrupted.QueueChange();old=interrupted.Approval();interrupted.Reload();Check(!interrupted.server.ReceiveClassChange(old).Accepted&&interrupted.View.record.growth.job==100&&interrupted.View.classOptions==null,"interrupted unapproved change not saved");
        foreach(string mode in new[]{"null","throw","wrong-request","denied"})
        {
            var provider=new Fixture();var state=State(provider.View.record);
            var reply=provider.server.ExecuteClassOptions(Id(),(r,v)=>{if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var options=provider.Notice(r);if(mode=="wrong-request")options.request.id=Id();else options.approved=false;return options;});
            Check(!reply.Accepted&&provider.View.classOptionsRequest==null&&State(provider.View.record)==state,"option provider failure unlocks: "+mode);
            provider.Info();reply=provider.server.ExecuteClassChange(2,1,provider.View.classOptions.id,Id(),(r,v)=>{if(mode=="null")return null;if(mode=="throw")throw new Exception("fixture");var notice=provider.Approval(r);if(mode=="wrong-request")notice.request.id=Id();else notice.approved=false;return notice;});
            Check(!reply.Accepted&&provider.View.classChangeRequest==null&&State(provider.View.record)==state,"change provider failure unlocks: "+mode);
        }
        var concurrent=new Fixture();concurrent.Info();concurrent.QueueChange();var earlier=concurrent.Approval();
        var other=new EntryAccountService(concurrent.store);new EntryClient(other).OpenLocalProfile();Check(other.BeginWorld(concurrent.definition).Accepted,"second service sees same growth");
        Check(other.ReceiveCharacterExperience(new CharacterExperienceNotice{id=Id(),approved=true,character=other.WorldSnapshot().characterId,session=other.WorldSnapshot().growthSession,expectedRevision=1,experience=999}).Accepted,"second service newer growth stored");
        Check(concurrent.server.ReceiveClassChange(earlier).Failure==WorldFailure.StaleRequest&&concurrent.store.data.accounts[0].character.world.growth.experience==999,"older class approval cannot overwrite newer growth");
        var unrelated=new Fixture();unrelated.Info();unrelated.QueueChange();old=unrelated.Approval();unrelated.store.data.accounts[0].character.world.inventory[0].item=2;Check(unrelated.server.ReceiveClassChange(old).Failure==WorldFailure.StaleRequest&&unrelated.store.data.accounts[0].character.world.inventory[0].item==2,"newer unrelated inventory preserved");
        var use=new Fixture();use.Info();use.QueueChange();Check(use.server.RequestItemUse(use.View.record.inventory[1].id,Id()).Failure==WorldFailure.Busy&&use.server.EquipWorldItem(use.View.record.inventory[0].id,2,Id()).Failure==WorldFailure.Busy,"pending class approval owns conflicting item/equipment changes");
        var path=Path.Combine(Path.GetTempPath(),"ds-class-change-"+Id()+".json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(f.store.data)&&disk.TryRead(out var doc)&&doc.accounts[0].character.world.classChange.growthRevision==2,"class receipt actual DataContract roundtrip");
            var server=new EntryAccountService(disk);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(f.definition).Accepted&&server.WorldSnapshot().record.growth.job==120&&server.WorldSnapshot().record.growth.magic.SequenceEqual(new[]{2001,2101})&&server.WorldSnapshot().classOptions==null,"file-backed reconnect restores approved class and spells only");
        }
        finally{if(File.Exists(path))File.Delete(path);}
        var invalid=f.View.record.Copy();invalid.classChange.growthRevision=3;Check(!WorldRecord.Valid(invalid),"receipt cannot be ahead of saved growth");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual received-candidate class selection, explicit approved growth/optional health and isolated persistence contracts; no inferred eligibility/cost, stat/magic editor or recovered native wire claim."}));
    }
}
