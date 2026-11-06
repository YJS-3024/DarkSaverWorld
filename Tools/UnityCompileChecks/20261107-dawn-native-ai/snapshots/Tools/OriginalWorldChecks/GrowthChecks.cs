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
        public void Train()
        {
            definition.combatCatalog=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");
            Check(server.ReceiveCharacterGrowth(Notice()).Accepted,"battle fixture received initial growth");
            var gate=definition.Map(1000).portals.First(p=>p.targetMap==101);Walk(gate.cells[0].x,gate.cells[0].y);Check(server.EnterWorldPortal(gate.id,true).Accepted,"battle fixture enters training");
            var actor=new TrainingUnit{id=0,job=100,x=10,y=7,life=1,hp=17,maxHp=200,mp=3,maxMp=65,ap=7,maxAp=20,canMove=true,canAttack=true,canRest=true,movementBudget=5,attackRange=1,effectiveStats=View.record.growth.stats.Copy()};
            var enemy=new TrainingUnit{id=400,job=401,x=11,y=7,life=1,hp=100,maxHp=100,mp=0,maxMp=0,ap=20,maxAp=20};
            Check(server.ReceiveTrainingUnits(new TrainingUnitsNotice{approved=true,id=Id(),character=View.characterId,session=View.campaignSession,expectedRevision=0,units=new[]{actor,enemy}}).Accepted,"battle fixture receives explicit units");
        }
    }
    private static void BattleLevelResults()
    {
        var f=new Fixture();f.Train();string enemy=JsonSerializer.Serialize(f.View.training.Unit(400),Options);
        var level=f.Notice(100,2,2,CharacterGrowthOperation.LevelChanged);level.state.nextExperience=1000;level.state.stats.attack=75;level.state.nativeLevels=new short[]{2,2};
        Check(f.server.ReceiveCharacterGrowth(level).Accepted,"received same-class level result applies during training");
        Check(f.View.record.level==2&&f.View.record.growth.classLevel==2&&f.View.record.growth.nativeLevels.SequenceEqual(new short[]{2,2})&&f.View.training.Commander.effectiveStats.attack==75,"record and active commander update together");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&f.View.training.Commander.ap==7&&JsonSerializer.Serialize(f.View.training.Unit(400),Options)==enemy,"level result without health cannot heal or change enemies/AP");
        string before=State(f.View.record);long battleRevision=f.View.training.revision;
        Check(!f.server.ReceiveCharacterGrowth(level).Accepted&&State(f.View.record)==before&&f.View.training.revision==battleRevision,"duplicate level result cannot replay");
        var next=f.Notice(100,3,3,CharacterGrowthOperation.LevelChanged);next.health=new CharacterGrowthHealth{hp=19,maxHp=210,mp=4,maxMp=70};next.state.stats.attack=80;
        f.store.fail=true;Check(f.server.ReceiveCharacterGrowth(next).Failure==WorldFailure.StorageUnavailable,"battle level save failure reported");f.store.fail=false;
        Check(State(f.View.record)==before&&f.View.training.revision==battleRevision&&f.View.training.Commander.effectiveStats.attack==75&&f.View.training.Commander.hp==17,"failed level save preserves active stats/health and record atomically");
        Check(f.server.ReceiveCharacterGrowth(next).Accepted&&f.View.record.level==3&&f.View.training.Commander.hp==19&&f.View.training.Commander.effectiveStats.attack==80,"same explicit level result can retry after storage recovers");
        Check(f.server.EscapeTraining().Accepted,"leveled fixture escapes");f.Reconnect();Check(f.View.record.level==3&&f.View.record.growth.stats.attack==80&&f.View.record.hp==19,"received battle level survives return and reload");
        foreach(string problem in new[]{"class-change","snapshot","pending","stale","session","dead-revive"})
        {
            var bad=new Fixture();bad.Train();
            if(problem=="dead-revive")
            {
                var current=bad.View;var dead=current.training.units.Select(u=>u.Copy()).ToArray();dead[0].hp=0;
                Check(bad.server.ReceiveTrainingUnits(new TrainingUnitsNotice{approved=true,id=Id(),character=current.characterId,session=current.campaignSession,expectedRevision=current.training.revision,units=dead}).Accepted,"dead commander fixture");
            }
            var notice=bad.Notice(100,2,2,CharacterGrowthOperation.LevelChanged);
            if(problem=="class-change"){notice.operation=CharacterGrowthOperation.ClassChanged;notice.state.job=105;notice.state.classLevel=1;}
            if(problem=="snapshot")notice.operation=CharacterGrowthOperation.Snapshot;
            if(problem=="pending")Check(bad.server.RequestTrainingAction(TrainingAction.Rest,-1,0,0,Id()).Accepted,"pending battle action fixture");
            if(problem=="stale")notice.expectedRevision--;
            if(problem=="session")notice.session=Id();
            if(problem=="dead-revive")notice.health=new CharacterGrowthHealth{hp=200,maxHp=200,mp=65,maxMp=65};
            string old=State(bad.View.record);string unitsBefore=JsonSerializer.Serialize(bad.View.training.units,Options);long revision=bad.View.training.revision;
            Check(!bad.server.ReceiveCharacterGrowth(notice).Accepted&&State(bad.View.record)==old&&JsonSerializer.Serialize(bad.View.training.units,Options)==unitsBefore&&bad.View.training.revision==revision,"battle growth rejection preserves state "+problem);
        }
        foreach(string problem in new[]{"none","missing-xp","xp-mismatch","stats-mismatch","job","level"})
        {
            var combined=new Fixture();combined.Train();Check(combined.server.RequestTrainingAction(TrainingAction.Attack,400,0,0,Id()).Accepted,"combined kill request");
            var current=combined.View;string id=Id();var units=current.training.units.Select(u=>u.Copy()).ToArray();units.Single(u=>u.id==400).hp=0;
            var state=current.record.growth.Copy();state.classLevel=2;state.totalLevel=2;state.experience=23;state.revision++;state.lastNotice=id;
            var notice=new TrainingUnitsNotice{approved=true,id=id,character=current.characterId,session=current.campaignSession,expectedRevision=current.training.revision,request=current.training.pending.id,units=units,experience=23,expectedGrowthRevision=current.record.growth.revision,growthState=state};
            if(problem=="missing-xp")notice.experience=null;
            if(problem=="xp-mismatch")state.experience=24;
            if(problem=="stats-mismatch")state.stats.attack=999;
            if(problem=="job"){state.job=105;state.classLevel=1;}
            if(problem=="level"){state.classLevel=1;state.totalLevel=1;}
            string old=State(combined.View.record);long revision=combined.View.training.revision;var result=combined.server.ReceiveTrainingUnits(notice);
            if(problem=="none")Check(result.Accepted&&combined.View.training.Unit(400).hp==0&&combined.View.record.growth.experience==23&&combined.View.record.level==2,"death XP and explicit level commit in one battle result");
            else Check(!result.Accepted&&State(combined.View.record)==old&&combined.View.training.revision==revision&&combined.View.training.Unit(400).hp==100&&combined.View.training.pending?.id==notice.request,"inconsistent combined growth cannot partly kill or level "+problem);
        }
    }
    private static void MovingBattleGrowth()
    {
        var f=new Fixture();f.Train();
        Check(f.server.ExecuteTrainingAction(TrainingAction.Move,-1,9,7,(request,view)=>
        {
            var units=view.units.Select(u=>u.Copy()).ToArray();var commander=units.First(u=>u.id==0);commander.x=request.x;commander.y=request.y;
            return new TrainingUnitsNotice{approved=true,id=Id(),request=request.id,character=request.character,session=request.session,expectedRevision=request.revision,units=units};
        }).Accepted&&f.View.moving&&f.View.training.commanderMotion!=null,"explicit movement approval starts an actual combat motion");
        var motion=f.View.training.commanderMotion;
        Check(f.server.ReceiveCharacterExperience(f.Experience(456)).Accepted&&f.View.moving&&f.View.training.Commander.nativeExperience==456,"absolute XP can arrive during combat motion without cancelling it");
        var current=f.View;
        Check(f.server.ReceiveBattlePacket(new NativeBattleNotice{approved=true,id=Id(),character=current.characterId,session=current.campaignSession,
            expectedRevision=current.training.revision,expectedGrowthRevision=current.record.growth.revision,unitLife=current.training.Commander.life,
            packet=OriginalBattleExperience.Packet(0,789)}).Accepted&&f.View.moving&&f.View.training.Commander.nativeExperience==789,"native5C53 XP update also preserves a running combat motion");
        var level=f.Notice(100,2,2,CharacterGrowthOperation.LevelChanged);level.state.experience=789;level.state.stats.attack=75;
        f.store.fail=true;Check(f.server.ReceiveCharacterGrowth(level).Failure==WorldFailure.StorageUnavailable&&f.View.moving&&f.View.record.growth.classLevel==1,"failed moving level save preserves motion and old growth");
        f.store.fail=false;Check(f.server.ReceiveCharacterGrowth(level).Accepted&&f.View.moving&&f.View.record.growth.classLevel==2&&f.View.training.Commander.effectiveStats.attack==75,"approved same-class level result applies without interrupting movement");
        var after=f.View.training.commanderMotion;
        Check(after!=null&&after.x==motion.x&&after.y==motion.y&&after.direction==motion.direction&&f.View.record.hp==17&&f.View.record.mp==3&&f.View.training.Commander.ap==7,"growth preserves current rendered path position, direction and HP/MP/AP");
        f.server.AdvanceWorld(100000);
        Check(!f.View.moving&&f.View.record.x==9&&f.View.record.y==7&&f.View.record.growth.experience==789&&f.View.record.growth.classLevel==2,"same accepted route completes at its original destination with received growth intact");
    }
    private static void LocalProjectionChecks(CharacterGrowthCatalog catalog)
    {
        var current=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=Id(),points=7,magicLevel=2,magic=new[]{101},skills=new[]{10101},nativeLevels=new short[]{1,1},stats=new CharacterGrowthStats{attack=65,defense=45}};
        string before=JsonSerializer.Serialize(current,Options);
        Check(!LocalLevelProgression.TryGrant(catalog,current,500,LocalExperienceBasis.Unspecified,out var unknown)&&unknown==null,"missing XP basis never defaults to an inferred server rule");
        foreach(var basis in new[]{LocalExperienceBasis.SpendPerLevel,LocalExperienceBasis.Cumulative})
        {
            Check(LocalLevelProgression.TryGrant(catalog,current,499,basis,out var below)&&below.classLevel==1&&below.experience==499,"threshold minus one stays at same level "+basis);
            Check(LocalLevelProgression.TryGrant(catalog,current,500,basis,out var edge)&&edge.classLevel==2&&edge.totalLevel==2&&edge.experience==(basis==LocalExperienceBasis.SpendPerLevel?0ul:500ul)&&edge.nextExperience==1000,"exact first threshold uses explicit basis "+basis);
            Check(LocalLevelProgression.TryGrant(catalog,current,1500,basis,out var multi)&&multi.classLevel==(basis==LocalExperienceBasis.SpendPerLevel?3:4)&&multi.experience==(basis==LocalExperienceBasis.SpendPerLevel?0ul:1500ul),"multi-level grant follows selected basis "+basis);
            Check(multi.job==100&&multi.points==7&&multi.magicLevel==2&&multi.magic.SequenceEqual(current.magic)&&multi.skills.SequenceEqual(current.skills)&&multi.stats.attack==65&&multi.nativeLevels==null,"projection cannot promote, reward or change stats/abilities "+basis);
            Check(LocalLevelProgression.TryGrant(catalog,current,100000,basis,out var capped)&&capped.job==100&&capped.classLevel==5&&capped.totalLevel==5,"huge grant stops at class cap instead of following nextJob "+basis);
            Check(capped.revision==current.revision&&capped.lastNotice==current.lastNotice&&JsonSerializer.Serialize(current,Options)==before,"projection is detached and does not publish/save revisions "+basis);
            capped.magic[0]=999;Check(current.magic[0]==101,"projected arrays cannot mutate current ownership");
        }
        foreach(var job in catalog.jobs)
        {
            var state=current.Copy();state.job=job.id;state.magic=Array.Empty<int>();state.skills=Array.Empty<int>();
            Check(LocalLevelProgression.TryGrant(catalog,state,1,LocalExperienceBasis.SpendPerLevel,out var a)&&a.job==job.id&&a.classLevel==1,"all source class tables support explicit per-level projection "+job.id);
            Check(LocalLevelProgression.TryGrant(catalog,state,1,LocalExperienceBasis.Cumulative,out var b)&&b.job==job.id&&b.classLevel==1,"all source class tables support explicit cumulative projection "+job.id);
        }
        var overflow=current.Copy();overflow.experience=ulong.MaxValue;Check(!LocalLevelProgression.TryGrant(catalog,overflow,1,LocalExperienceBasis.SpendPerLevel,out _),"XP overflow rejected without partial projection");
        overflow=current.Copy();overflow.totalLevel=int.MaxValue;Check(!LocalLevelProgression.TryGrant(catalog,overflow,500,LocalExperienceBasis.Cumulative,out _),"level overflow rejected without partial projection");
        var malformed=new CharacterGrowthCatalog{jobs=catalog.jobs,magicIds=catalog.magicIds,skillIds=catalog.skillIds,rows=catalog.rows.Where(r=>r.job!=100||r.classLevel!=2).ToArray()};
        Check(!LocalLevelProgression.TryGrant(malformed,current,500,LocalExperienceBasis.SpendPerLevel,out _),"missing middle source row is not mistaken for a class cap");
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
        BattleLevelResults();MovingBattleGrowth();LocalProjectionChecks(catalog);
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
