using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;

internal static class TrainingChecks
{
    private static int assertions;private static string root;
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    private static string Id()=>Guid.NewGuid().ToString("N");
    private static void Check(bool ok,string text){assertions++;if(!ok)throw new Exception(text);}
    private static T Read<T>(string file)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,"Assets/_Original",file)),Options);
    private static string State(object v)=>JsonSerializer.Serialize(v,Options);
    private sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();public bool fail;
        public bool TryRead(out LocalAccountDocument value){value=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument value){if(fail)return false;data=value.Copy();return true;}
    }
    private sealed class Fixture
    {
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition def;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(bool seed=true)
        {
            def=Read<WorldDefinition>("Data/World/KaosiaWorld.json");def.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");def.combatCatalog=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");
            server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"전투검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=200,maxHp=200,mp=45,maxMp=65};
            client.OpenLocalProfile();Check(server.BeginWorld(def).Accepted,"isolated native world fixture");
            var id=Id();Check(server.ReceiveCharacterGrowth(new CharacterGrowthNotice{approved=true,id=id,character=View.characterId,session=View.growthSession,
                state=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=id}}).Accepted,"isolated approved growth snapshot");
            var gate=def.Map(1000).portals.First(p=>p.targetMap==101);Check(server.MoveWorld(gate.cells[0].x,gate.cells[0].y).Accepted,"native entrance path");server.AdvanceWorld(100000);
            Check(server.EnterWorldPortal(gate.id,true).Accepted&&View.record.map==101,"actual service portal entry");
            if(seed)
            {
                // Explicit synthetic authoritative fixture; this is not a default game roster/rule set.
                Check(server.ReceiveTrainingUnits(Notice(new[]{Actor(0,100,10,7),Actor(400,302,11,7)})).Accepted,"receive fixture units through production service");
            }
        }
        public TrainingUnit Actor(int id,int job,int x,int y)=>new TrainingUnit{id=id,job=job,x=x,y=y,life=1,hp=id==0?200:50,maxHp=id==0?200:50,mp=id==0?45:0,maxMp=id==0?65:0,
            ap=20,maxAp=20,canMove=true,canAttack=true,canRest=true,movementBudget=5,attackRange=1};
        public TrainingUnitsNotice Notice(TrainingUnit[] units=null)=>new TrainingUnitsNotice{approved=true,id=Id(),character=View.characterId,session=View.campaignSession,
            expectedRevision=View.training?.revision??0,units=units??View.training.units.Select(u=>u.Copy()).ToArray()};
        public TrainingUnitsNotice Result()=>Result(View.training.pending);
        public TrainingUnitsNotice Result(TrainingActionRequest request)
        {var notice=Notice();notice.request=request.id;return notice;}
        public void Queue(TrainingAction action,int target=-1,int x=0,int y=0)
        {Check(server.RequestTrainingAction(action,target,x,y,Id()).Accepted,"valid request queued: "+action);}
    }
    private static void Main(string[] args)
    {
        root=args[0];var data=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");
        Check(data.jobs.Length==542&&data.jobs.Count(j=>j.category=="monster")==337,"all native unit job facts");
        Check(data.Job(302).baseExperience==45&&data.Job(701).baseExperience==240&&data.Job(498).baseExperience==800,"original per-monster XP values preserved");
        Check(data.Job(100).rawAttackRange==0&&data.Job(100).rawAttackTick==7,"raw range/tick not silently converted to general combat cost");
        var empty=new Fixture(false);
        Check(empty.View.training==null&&empty.server.RequestTrainingAction(TrainingAction.Attack,400,0,0,Id()).Failure==WorldFailure.RulesUnavailable,"no fabricated training roster");
        var f=new Fixture();var before=State(f.View.record);var initial=f.View.training;
        f.server.AdvanceWorld(30000);Check(f.View.training.Commander.ap==20&&State(f.View.record)==before,"elapsed time cannot invent AP regeneration, XP or AI damage");
        var exposed=f.View.training;exposed.units[0].hp=1;Check(f.View.training.Commander.hp==200,"public snapshot does not alias live actors");
        Check(f.server.MoveWorld(10,8).Failure==WorldFailure.Busy,"ordinary walk cannot bypass battle action requests");
        Check(!f.server.RequestTrainingAction(TrainingAction.Attack,0,0,0,Id()).Accepted,"self attack rejected");
        Check(!f.server.RequestTrainingAction(TrainingAction.Move,-1,10,7,Id()).Accepted,"own tile excluded from movement");
        Check(!f.server.RequestTrainingAction(TrainingAction.Move,-1,11,7,Id()).Accepted,"occupied enemy tile excluded");
        Check(!f.server.RequestTrainingAction(TrainingAction.Move,-1,int.MaxValue,int.MaxValue,Id()).Accepted,"out of bounds move rejected");
        var original=f.Notice();original.units[1].x=11;original.units[1].y=8;
        Check(f.server.ReceiveTrainingUnits(original).Accepted&&!f.server.RequestTrainingAction(TrainingAction.Attack,400,0,0,Id()).Accepted,"range one is four cardinal cells, excludes diagonals");
        original=f.Notice();original.units[1].y=7;f.server.ReceiveTrainingUnits(original);
        before=State(f.View.record);var enemyHp=f.View.training.Unit(400).hp;f.Queue(TrainingAction.Attack,400);
        var pending=f.View.training.pending;Check(State(f.View.record)==before&&f.View.training.Commander.ap==20&&f.View.training.Unit(400).hp==enemyHp,"request alone does not predict HP, AP or XP");
        Check(f.server.RequestTrainingAction(TrainingAction.Rest,-1,0,0,Id()).Failure==WorldFailure.Busy,"only one unresolved command");
        Check(f.server.EscapeTraining().Failure==WorldFailure.Busy,"flee cannot race an unresolved result");
        var denied=f.Result();denied.approved=false;Check(!f.server.ReceiveTrainingUnits(denied).Accepted&&f.View.training.pending==null&&State(f.View.record)==before,"denied command unlocks without resource mutation");
        Check(f.server.RequestTrainingAction(TrainingAction.Attack,400,0,0,pending.id).Failure==WorldFailure.StaleRequest,"retired command identifier cannot be replayed");
        f.Queue(TrainingAction.Attack,400);var hit=f.Result();hit.units[0].ap=13;hit.units[1].hp=0;hit.experience=45;hit.expectedGrowthRevision=f.View.record.growth.revision;
        f.store.fail=true;var roster=State(f.View.training);before=State(f.View.record);
        Check(f.server.ReceiveTrainingUnits(hit).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&State(f.View.training)==roster,"save failure is atomic across death, AP and XP");
        f.store.fail=false;Check(f.server.ReceiveTrainingUnits(hit).Accepted,"same approved result can retry after storage recovery");
        Check(f.View.training.Unit(400).hp==0&&f.View.training.Commander.ap==13&&f.View.record.growth.experience==45&&f.View.record.growth.totalLevel==1,"approved kill applies absolute XP without automatic level formula");
        Check(f.View.record.map==101&&!f.View.record.ghost,"last monster death neither clears training nor creates ghost");
        Check(f.View.training.impactX==11&&f.View.training.impactY==7,"contact remains captured at killed enemy tile");
        before=State(f.View.record);Check(f.server.ReceiveTrainingUnits(hit).Failure==WorldFailure.StaleRequest&&State(f.View.record)==before,"duplicate kill result cannot award twice");
        hit.units[0].ap=0;Check(f.View.training.Commander.ap==13,"incoming result arrays are copied");
        f.server.AdvanceWorld(160);Check(f.View.training.impact==null,"native contact presentation completes");
        var respawn=f.Notice();respawn.units[1].hp=50;
        Check(!f.server.ReceiveTrainingUnits(respawn).Accepted,"dead enemy cannot reuse old life token");
        respawn.units[1].life=2;Check(f.server.ReceiveTrainingUnits(respawn).Accepted&&f.View.training.Unit(400).hp==50,"authoritative new life respawns enemy");
        f.Queue(TrainingAction.Attack,400);var oldHit=f.Result();var update=f.Notice();update.units[1].life=3;
        Check(f.server.ReceiveTrainingUnits(update).Accepted&&f.View.training.pending==null,"new roster invalidates command using older roster");
        Check(f.server.ReceiveTrainingUnits(oldHit).Failure==WorldFailure.StaleRequest,"old target-life hit cannot damage respawn");
        f.Queue(TrainingAction.Move,-1,10,8);var move=f.Result();move.units[0].y=8;move.units[0].ap=9;
        Check(f.server.ReceiveTrainingUnits(move).Accepted&&f.View.moving&&f.View.renderX==10&&f.View.renderY==7&&f.View.record.y==8,"approved move stores target but begins rendering at source");
        f.server.AdvanceWorld(80);Check(f.View.renderY==7.5&&f.View.record.direction==TutorialGeometry.FacingRow(0,1),"native stepped interpolation and facing");
        var duringMove=f.Notice();duringMove.units[0].ap=8;
        Check(f.server.ReceiveTrainingUnits(duringMove).Accepted&&f.View.moving&&f.View.renderY==7.5&&f.View.training.Commander.ap==8,"incoming state updates are not dropped during approved movement presentation");
        Check(!f.server.EscapeTraining().Accepted&&!f.server.RequestTrainingAction(TrainingAction.Rest,-1,0,0,Id()).Accepted,"moving actor cannot flee or issue overlapping action");
        f.server.AdvanceWorld(80);Check(!f.View.moving&&f.View.renderY==8&&f.View.training.Commander.ap==8,"approved movement completes on grid without rolling back newer AP");
        before=State(f.View.record);
        Check(f.server.ExecuteTrainingAction(TrainingAction.Rest,-1,0,0,null).Failure==WorldFailure.RulesUnavailable&&f.View.training.pending==null,"missing authority cannot lock input");
        Check(f.server.ExecuteTrainingAction(TrainingAction.Rest,-1,0,0,(r,v)=>null).Failure==WorldFailure.RulesUnavailable&&f.View.training.pending==null,"empty authority response unlocks input");
        Check(f.server.ExecuteTrainingAction(TrainingAction.Rest,-1,0,0,(r,v)=>throw new Exception("fixture")).Failure==WorldFailure.RulesUnavailable&&f.View.training.pending==null&&State(f.View.record)==before,"authority exception unlocks and preserves resources");
        Check(f.server.ExecuteTrainingAction(TrainingAction.Rest,-1,0,0,(r,v)=>{v.units[0].hp=1;return new TrainingUnitsNotice{id="bad"};}).Failure==WorldFailure.StaleRequest&&f.View.training.pending==null&&State(f.View.record)==before,"malformed provider response and mutated provider view cannot lock/corrupt client");
        Check(f.server.ExecuteTrainingAction(TrainingAction.Rest,-1,0,0,(r,v)=>{var n=f.Notice();n.units[0].hp=1;return n;}).Failure==WorldFailure.StaleRequest&&f.View.training.pending==null&&State(f.View.record)==before,"uncorrelated provider result cannot be mistaken for command approval");
        Check(f.server.ExecuteTrainingAction(TrainingAction.Rest,-1,0,0,(r,v)=>{var n=f.Result(r);n.units[0].ap=11;return n;}).Accepted&&f.View.training.Commander.ap==11&&f.View.record.mp==45,"approved rest uses supplied result, no inferred HP/MP refill");
        var reduced=f.Notice();reduced.units=reduced.units.Where(u=>u.id==0).ToArray();
        Check(f.server.ReceiveTrainingUnits(reduced).Accepted&&f.View.record.map==101,"removing final enemy does not automatically complete training");
        var readd=f.Notice(new[]{f.View.training.Commander.Copy(),f.Actor(400,302,11,7)});readd.units[1].life=3;
        Check(!f.server.ReceiveTrainingUnits(readd).Accepted,"removed unit cannot return with a retired life token");
        readd.units[1].life=4;Check(f.server.ReceiveTrainingUnits(readd).Accepted,"removed unit can return with authoritative new life token");
        var missing=f.Notice();missing.units[0].attackRange=null;missing.units[0].movementBudget=null;
        Check(f.server.ReceiveTrainingUnits(missing).Accepted&&f.server.RequestTrainingAction(TrainingAction.Attack,400,0,0,Id()).Failure==WorldFailure.RulesUnavailable
            &&f.server.RequestTrainingAction(TrainingAction.Move,-1,10,9,Id()).Failure==WorldFailure.RulesUnavailable,"missing server range/budget never uses guessed native defaults");
        var restored=f.Notice();restored.units[0].attackRange=1;restored.units[0].movementBudget=5;f.server.ReceiveTrainingUnits(restored);
        foreach(string problem in new[]{"owner","session","revision","duplicate","job","position","hp","ap","life","commander","nullable","portal","terrain","xp"})
        {
            var invalid=f.Notice();switch(problem)
            {
                case "owner":invalid.character=Id();break;case "session":invalid.session=Id();break;case "revision":invalid.expectedRevision++;break;
                case "duplicate":invalid.units=new[]{invalid.units[0],invalid.units[0]};break;case "job":invalid.units[1].job=999999;break;
                case "position":invalid.units[1].x=-1;break;case "hp":invalid.units[1].hp=51;break;case "ap":invalid.units[0].ap=21;break;
                case "life":invalid.units[1].life=1;break;case "commander":invalid.units[0].job=101;break;case "nullable":invalid.units[0].attackRange=0;break;
                case "portal":f.def.Map(101).portals=new[]{new WorldPortal{id="fixture",cells=new[]{new WorldPoint{x=11,y=7}}}};break;
                case "terrain":invalid.units[0].x=0;invalid.units[0].y=0;break;case "xp":invalid.experience=999;invalid.expectedGrowthRevision=0;break;
            }
            before=State(f.View.record);var snapshot=State(f.View.training);Check(!f.server.ReceiveTrainingUnits(invalid).Accepted&&State(f.View.record)==before&&State(f.View.training)==snapshot,"invalid full result is atomic: "+problem);
            if(problem=="portal")f.def.Map(101).portals=Array.Empty<WorldPortal>();
        }
        var death=f.Notice();death.units[0].hp=0;Check(f.server.ReceiveTrainingUnits(death).Accepted&&!f.View.record.ghost&&f.View.record.hp==0,"training death records zero HP without ghost");
        Check(!f.server.RequestTrainingAction(TrainingAction.Attack,400,0,0,Id()).Accepted,"dead commander cannot attack");
        var stale=f.Notice();Check(f.server.EscapeTraining().Accepted&&f.View.record.map==1000&&f.View.record.x==8&&f.View.record.y==21&&f.View.training==null,"flee clears roster and returns actual training entrance");
        Check(!f.server.ReceiveTrainingUnits(stale).Accepted,"late training result rejected outside training");
        var g=new Fixture();var old=g.Notice();g.server.EscapeTraining();var gate=g.def.Map(1000).portals.First(p=>p.targetMap==101);g.server.MoveWorld(gate.cells[0].x,gate.cells[0].y);g.server.AdvanceWorld(100000);g.server.EnterWorldPortal(gate.id,true);
        Check(g.server.ReceiveTrainingUnits(old).Failure==WorldFailure.StaleRequest,"previous visit notice rejected after reentry");
        var reload=new EntryAccountService(g.store);new EntryClient(reload).OpenLocalProfile();Check(reload.BeginWorld(g.def).Accepted&&reload.WorldSnapshot().record.map==1000&&reload.WorldSnapshot().training==null,"interrupted training reconnects at town without fabricated enemies");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,originalJobs=data.jobs.Length,unityPlayExecuted=false,scope="Production service contracts with isolated synthetic authority, not original spawn/AI/damage/AP/XP-distribution policy or rendered Play."}));
    }
}
