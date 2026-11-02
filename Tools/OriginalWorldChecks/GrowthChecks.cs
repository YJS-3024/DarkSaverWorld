using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class GrowthChecks
{
    private static int assertions;private static string root;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static string Id()=>Guid.NewGuid().ToString("N");
    private static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
    private static T Read<T>(string name)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",name)),Options);
    private static string State(WorldRecord record)=>JsonSerializer.Serialize(record,Options);
    private sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();public bool fail;
        public bool TryRead(out LocalAccountDocument document){document=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument document){if(fail)return false;data=document.Copy();return true;}
    }
    private sealed class Fixture
    {
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(bool ghost=false)
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"성장검사");
            var character=store.data.accounts.Single().character;character.tutorialCompleted=true;
            character.world=new WorldRecord{goldKnown=true,gold=100000,healthKnown=true,hp=ghost?0:17,maxHp=200,mp=3,maxMp=65,ghost=ghost};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated completed-profile fixture");
        }
        public CharacterGrowthNotice Notice(int job=100,int classLevel=1,int totalLevel=1,CharacterGrowthOperation operation=CharacterGrowthOperation.Snapshot)
        {
            string id=Id();return new CharacterGrowthNotice{approved=true,character=View.characterId,session=View.growthSession,id=id,expectedRevision=View.record.growth?.revision??0,operation=operation,
                state=new CharacterGrowthRecord{job=job,classLevel=classLevel,totalLevel=totalLevel,lastNotice=id,revision=(View.record.growth?.revision??0)+1,
                    experience=12,points=4,magic=new[]{101},skills=new[]{10101,21101},stats=new CharacterGrowthStats{attack=65,defense=45,magicAttack=10,magicDefense=8}}};
        }
        public CharacterExperienceNotice Experience(ulong value)=>new CharacterExperienceNotice{approved=true,character=View.characterId,session=View.growthSession,id=Id(),expectedRevision=View.record.growth?.revision??0,experience=value};
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"fixture walking request");for(int i=0;i<10000&&View.moving;i++)server.AdvanceWorld(80);Check(!View.moving&&View.record.x==x&&View.record.y==y,"fixture walking arrival");}
        public void Reconnect()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"real service/profile reload");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var catalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");
        Check(catalog.jobs.Length==73&&catalog.rows.Length==2780,"all source player classes and unique growth rows");
        Check(catalog.rows.Sum(r=>r.sourceLines.Length)==2781&&catalog.Row(163,35).sourceLines.Length==2,"identical native duplicate retains both source lines");
        Check(catalog.Row(100,1).requiredExperience==500&&catalog.Row(100,5).nextJob==105,"native threshold and next class metadata");
        Check(catalog.Row(103,1).magic.SequenceEqual(new[]{101,301})&&catalog.Row(105,1).skills.SequenceEqual(new[]{10102,10301,21102}),"native class-entry magic and skill lists");
        Check(catalog.rows.All(r=>r.abilitiesResolved)&&catalog.rows.Any(r=>r.rawSkills.EndsWith(",")),"native trailing delimiters retained with resolved source references");
        var f=new Fixture();Check(f.View.record.growth==null&&!f.View.record.level.HasValue,"legacy profile does not receive invented growth or XP");
        Check(f.server.ReceiveCharacterExperience(f.Experience(100)).Failure==WorldFailure.RulesUnavailable,"XP without known character growth is held");
        var first=f.Notice();Check(f.server.ReceiveCharacterGrowth(first).Accepted,"approved complete initial growth snapshot applied");
        Check(f.View.record.level==1&&f.View.record.growth.experience==12&&f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.gold==100000,"growth snapshot preserves unrelated health and funds");
        first.state.magic[0]=301;first.state.stats.attack=999;Check(f.View.record.growth.magic[0]==101&&f.View.record.growth.stats.attack==65,"incoming arrays/stats do not alias stored state");
        var exposed=f.View;exposed.record.growth.skills[0]=999;Check(f.View.record.growth.skills[0]==10101,"public view cannot mutate skills");
        var xp=f.Experience(1234);Check(f.server.ReceiveCharacterExperience(xp).Accepted&&f.View.record.growth.experience==1234,"absolute experience replaces received value");
        Check(f.View.record.level==1&&f.View.record.growth.points==4&&f.View.record.hp==17&&f.View.record.mp==3,"crossing table value cannot invent level, points or healing");
        string before=State(f.View.record);Check(f.server.ReceiveCharacterExperience(xp).Failure==WorldFailure.StaleRequest&&State(f.View.record)==before,"duplicate XP does not add rewards");
        Check(f.server.ReceiveCharacterExperience(f.Experience(ulong.MaxValue)).Accepted&&f.View.record.growth.experience==ulong.MaxValue,"unsigned absolute XP preserves full value without arithmetic overflow");
        Check(f.server.ReceiveCharacterExperience(f.Experience(7)).Accepted&&f.View.record.growth.experience==7,"authoritative lower absolute XP is not converted into a delta");
        var level=f.Notice(100,2,2,CharacterGrowthOperation.LevelChanged);level.state.points=8;level.state.experience=20;
        Check(f.server.ReceiveCharacterGrowth(level).Accepted&&f.View.record.level==2&&f.View.record.growth.classLevel==2&&f.View.record.growth.points==8,"approved level result supplies its own XP and points");
        Check(f.View.record.hp==17&&f.View.record.mp==3,"level result without health cannot refill HP/MP");
        var change=f.Notice(105,1,6,CharacterGrowthOperation.ClassChanged);change.state.magic=new[]{101,2401};change.state.skills=new[]{10102,10301,21102};
        Check(f.server.ReceiveCharacterGrowth(change).Accepted&&f.View.record.growth.job==105&&f.View.record.level==6,"class result applies class-local and total levels separately");
        Check(f.View.record.growth.magic.SequenceEqual(new[]{101,2401})&&f.View.record.growth.skills.SequenceEqual(change.state.skills),"received complete ability ownership applied without inferred merge policy");
        Check(f.store.data.accounts[0].character.job==100&&LocalEntryPolicy.IsDocument(f.store.data),"starting identity and old account format remain valid after promotion");
        var dagger=f.definition.items.First(i=>i.name=="대거");var owned=new WorldOwnedItem{id=Id(),item=dagger.id,slot=0};
        // Separate synthetic owned item fixture, never a production account injection.
        f.store.data.accounts[0].character.world.inventory=new[]{owned};f.Reconnect();
        Check(f.server.EquipWorldItem(owned.id,2,Id()).Accepted&&f.View.record.Equipment.Single().item==dagger.id,"promoted class equips using original item family flags");
        Check(f.View.record.hp==17&&f.View.record.mp==3,"promoted-class equip does not heal");
        string saved=State(f.View.record);f.Reconnect();Check(State(f.View.record)==saved&&f.View.record.growth.job==105,"growth, class, skills and equipment survive real reconnect");
        var oldSession=f.Experience(200);f.Reconnect();Check(f.server.ReceiveCharacterExperience(oldSession).Failure==WorldFailure.StaleRequest,"old session notice cannot replay after reconnect");
        var retry=f.Experience(300);before=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveCharacterExperience(retry).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"failed growth save keeps live state and revision");
        f.store.fail=false;Check(f.server.ReceiveCharacterExperience(retry).Accepted&&f.View.record.growth.experience==300,"same notice can retry after failed save");
        foreach(string test in new[]{"owner","session","approval","id","revision","job","level","magic","skills","points","health","operation"})
        {
            var notice=f.Notice(105,1,6);
            switch(test)
            {
                case "owner":notice.character=Id();break;case "session":notice.session=Id();break;case "approval":notice.approved=false;break;case "id":notice.id="invalid";break;
                case "revision":notice.state.revision++;break;case "job":notice.state.job=999;break;case "level":notice.state.classLevel=999;notice.state.totalLevel=999;break;
                case "magic":notice.state.magic=new[]{int.MaxValue};break;case "skills":notice.state.skills=new[]{10101,10101};break;case "points":notice.state.points=-1;break;
                case "health":notice.health=new CharacterGrowthHealth{hp=300,maxHp=200};break;case "operation":notice.operation=CharacterGrowthOperation.ClassChanged;break;
            }
            before=State(f.View.record);Check(!f.server.ReceiveCharacterGrowth(notice).Accepted&&State(f.View.record)==before,"invalid growth notice preserves state: "+test);
        }
        var moving=f.Experience(400);f.server.MoveWorld(22,21);Check(f.server.ReceiveCharacterExperience(moving).Failure==WorldFailure.Busy,"moving growth save is held");f.server.AdvanceWorld(10000);
        var stale=f.Experience(500);f.store.data.accounts[0].character.world.growth.revision++;f.store.data.accounts[0].character.world.growth.lastNotice=Id();
        string fresh=State(f.store.data.accounts[0].character.world);Check(f.server.ReceiveCharacterExperience(stale).Failure==WorldFailure.StaleRequest&&State(f.store.data.accounts[0].character.world)==fresh,"fresh stored revision cannot be overwritten by old service state");
        var ghost=new Fixture(true);var resurrection=ghost.Notice();resurrection.health=new CharacterGrowthHealth{hp=200,maxHp=200,mp=65,maxMp=65};
        Check(!ghost.server.ReceiveCharacterGrowth(resurrection).Accepted&&ghost.View.record.ghost&&ghost.View.record.hp==0,"growth cannot bypass temple resurrection policy");
        var healthy=new Fixture();var health=healthy.Notice();health.health=new CharacterGrowthHealth{hp=19,maxHp=210,mp=4,maxMp=70};
        Check(healthy.server.ReceiveCharacterGrowth(health).Accepted&&healthy.View.record.hp==19&&healthy.View.record.maxHp==210,"explicit approved health accompanies growth atomically");
        var warehouse=new Fixture();warehouse.server.ReceiveCharacterGrowth(warehouse.Notice());warehouse.definition.rules.storageRuleKnown=true;warehouse.definition.rules.storageSlots=48;
        var gate=warehouse.definition.Map(1000).portals.First(p=>p.targetMap==2510);warehouse.Walk(gate.cells[0].x,gate.cells[0].y);warehouse.server.EnterWorldPortal(gate.id,false);
        var npc=warehouse.definition.Map(2510).npcs.Single();warehouse.Walk(npc.counter[0].x,npc.counter[0].y);warehouse.server.OpenWorldFacility(npc.id);
        var token=warehouse.View.facility.token;Check(warehouse.server.ReceiveCharacterExperience(warehouse.Experience(999)).Accepted,"XP can update during warehouse draft");
        Check(warehouse.server.ConfirmWorldStorage(token,Id()).Accepted&&warehouse.View.record.growth.experience==999,"confirming warehouse draft cannot roll growth back");
        string folder=Path.Combine(Path.GetTempPath(),"growth-disk-"+Id()),path=Path.Combine(folder,"accounts.json");
        try
        {
            var disk=new LocalAccountFileStore(path);var document=f.store.data.Copy();document.accounts[0].character.world.growth.experience=ulong.MaxValue;
            Check(disk.TryWrite(document)&&disk.TryRead(out var loaded)&&loaded.accounts[0].character.world.growth.experience==ulong.MaxValue,"real DataContract file roundtrip preserves full XP");
            disk.TryRead(out loaded);Check(loaded.accounts[0].character.world.growth.skills.SequenceEqual(document.accounts[0].character.world.growth.skills)&&loaded.accounts[0].character.world.growth.job==105,"actual file restores promoted class and owned skills");
        }
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,sourceJobs=catalog.jobs.Length,sourceRows=catalog.rows.Length,unityPlay=false,
            scope="Authoritative growth/absolute XP application, profile persistence and source metadata only. No inferred server XP/stat/promotion formula."}));
    }
}
