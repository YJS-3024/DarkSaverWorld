using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class ExitChecks
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
        public Memory store=new Memory();public EntryAccountService server;public WorldDefinition definition;
        public WorldView View=>server.WorldSnapshot();
        public Fixture(bool enter=true)
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.combatCatalog=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");
            var campaign=Read<CampaignDefinition>("Data/Campaign/CampaignCatalog.json");
            using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Tools/OriginalWorldChecks/campaign-assets.json")));
            var scripts=manifest.RootElement.GetProperty("scripts").EnumerateArray().Select(p=>p.GetString()).ToArray();
            foreach(var map in campaign.maps)
            {map.intro=map.introIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.introIndex]));map.outro=map.outroIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.outroIndex]));}
            var spawn=campaign.Map(26).entries.First(e=>e.unit==0);
            campaign.connections=new[]{new CampaignConnection{fromMap=1000,toMap=26,x=spawn.x,y=spawn.y,evidence="isolated exit fixture, not production route",cells=new[]{new WorldPoint{x=21,y=21}}}};
            definition=campaign.Merge(definition);server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"복귀검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=65,goldKnown=true,gold=123};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world begins");if(enter)Enter();
        }
        public void Enter()
        {
            Check(server.EnterCampaignMap(26).Accepted,"scenario enters real service");
            for(int i=0;i<10000&&View.presentation?.completed==false;i++){if(View.presentation.waitingMessage)server.ContinueCampaignPresentation();server.AdvanceWorld(1000);}server.AdvanceWorld(0);
            Check(View.record.campaignPhase=="explore","source intro completes");
            var job=definition.campaign.Map(26).units.First(u=>u.unit==400).job;
            var n=BattleNotice(new[]{new TrainingUnit{id=0,job=100,x=3,y=37,life=1,hp=17,maxHp=200,mp=3,maxMp=65,ap=20,maxAp=20,canMove=true,canAttack=true,canRest=true,movementBudget=5,attackRange=1},
                new TrainingUnit{id=400,job=job,x=4,y=37,life=1,hp=50,maxHp=50,ap=20,maxAp=20}});
            Check(server.ReceiveBattleUnits(n).Accepted,"explicit isolated authoritative battle fixture");
        }
        public TrainingUnitsNotice BattleNotice(TrainingUnit[] units=null)=>new TrainingUnitsNotice{approved=true,id=Id(),character=View.characterId,session=View.campaignSession,
            expectedRevision=View.battle?.revision??0,units=units??View.battle.units.Select(u=>u.Copy()).ToArray()};
        public CampaignExitNotice Notice(CampaignExitKind kind=CampaignExitKind.Defeat)
        {
            var id=Id();return new CampaignExitNotice{approved=true,id=id,character=View.characterId,session=View.campaignSession,visit=View.record.scenarioVisit.id,kind=kind,
                expectedBattleRevision=View.record.scenarioBattle?.revision??0,request=kind==CampaignExitKind.Retreat?View.pendingRetreat?.id:null,
                result=kind==CampaignExitKind.Defeat?new CampaignResultRecord{id=id,visit=View.record.scenarioVisit.id,map=26,victory=false,outcome="defeat"}:null};
        }
        public void Queue()=>Check(server.RequestScenarioRetreat(View.record.scenarioVisit.id,Id()).Accepted,"retreat request queued");
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"real service reconnect");}
        public void Walk(int x,int y)
        {Check(server.MoveWorld(x,y).Accepted,"normal world walk");server.AdvanceWorld(100000);Check(View.record.x==x&&View.record.y==y,"world walk arrives");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();var before=State(f.View.record);var visit=f.View.record.scenarioVisit.id;
        Check(f.server.ExecuteScenarioRetreat(visit,null).Failure==WorldFailure.RulesUnavailable&&f.View.pendingRetreat==null&&State(f.View.record)==before,"missing retreat authority neither exits nor locks input");
        Check(f.server.RequestScenarioRetreat(Id(),Id()).Failure==WorldFailure.StaleRequest,"UI from an older visit cannot retreat current visit");
        f.Queue();var request=f.View.pendingRetreat.id;
        Check(State(f.View.record)==before&&f.View.record.hp==17&&f.View.record.map==26,"retreat request alone spends no HP/MP/money and stays in scenario");
        var exposed=f.View;exposed.pendingRetreat.visit=Id();Check(f.View.pendingRetreat.visit==visit,"pending request view is detached");
        Check(f.server.RequestBattleAction(TrainingAction.Rest,-1,0,0,Id()).Failure==WorldFailure.Busy&&f.server.MoveWorld(3,38).Failure==WorldFailure.Busy,"pending retreat blocks actions and walking");
        var denied=f.Notice(CampaignExitKind.Retreat);denied.approved=false;
        Check(!f.server.ReceiveScenarioExit(denied).Accepted&&f.View.pendingRetreat==null&&State(f.View.record)==before,"denied retreat unlocks with no mutation");
        Check(f.server.RequestScenarioRetreat(visit,request).Failure==WorldFailure.StaleRequest,"same request identity cannot be reused");
        foreach(string failure in new[]{"null","throw","wrong-request"})
        {
            var reply=f.server.ExecuteScenarioRetreat(visit,(r,v)=>{
                if(failure=="null")return null;if(failure=="throw")throw new Exception("fixture failure");var n=f.Notice(CampaignExitKind.Retreat);n.request=Id();return n;});
            Check(!reply.Accepted&&f.View.pendingRetreat==null&&State(f.View.record)==before,"authority failure unlocks without mutation: "+failure);
        }
        f.Queue();var outdated=f.Notice(CampaignExitKind.Retreat);var update=f.BattleNotice();update.units[0].ap=19;
        Check(f.server.ReceiveBattleUnits(update).Accepted&&f.View.pendingRetreat==null,"incoming battle update is not dropped while retreat waits and invalidates older decision");
        Check(f.server.ReceiveScenarioExit(outdated).Failure==WorldFailure.StaleRequest,"outdated retreat approval cannot overwrite newer battle revision");
        f.Queue();var retreat=f.Notice(CampaignExitKind.Retreat);before=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveScenarioExit(retreat).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.pendingRetreat!=null,"failed retreat persistence leaves battle/position/request for retry");
        f.store.fail=false;Check(f.server.ReceiveScenarioExit(retreat).Accepted,"same retreat notice retries after storage recovery");
        Check(f.View.record.map==1000&&f.View.record.x==21&&f.View.record.y==21&&f.View.battle==null&&f.View.pendingRetreat==null&&f.View.pendingPortal==null,"approved retreat returns exact entered tile with no active battle or auto-reentry");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&!f.View.record.ghost&&f.View.record.gold==123&&f.View.record.lastScenarioResult==null&&f.View.record.lastScenarioExit.kind==CampaignExitKind.Retreat,"alive retreat preserves HP/MP/gold and does not fabricate victory/rewards/report");
        before=State(f.View.record);Check(!f.server.ReceiveScenarioExit(retreat).Accepted&&State(f.View.record)==before,"retreat replay cannot mutate returned state");f.Reload();
        Check(f.View.record.lastScenarioExit.id==retreat.id&&f.View.record.map==1000,"retreat marker persists without replay");
        var reported=new Fixture();reported.Queue();var reportExit=reported.Notice(CampaignExitKind.Retreat);
        reportExit.result=new CampaignResultRecord{id=reportExit.id,visit=reportExit.visit,map=26,outcome="retreat",victory=false};
        Check(reported.server.ReceiveScenarioExit(reportExit).Accepted&&reported.View.record.scenarioVisit.result.IsRetreat&&reported.View.presentation==null&&reported.View.record.map==26,"server-supplied retreat report displays native retreat outcome without victory outro or premature return");
        reported.Reload();Check(reported.View.record.scenarioVisit.result.IsRetreat,"retreat report survives reconnect");
        Check(reported.server.ConfirmScenarioResult(reportExit.id,Id()).Accepted&&reported.View.record.map==1000&&reported.View.record.lastScenarioResult.IsRetreat,"native retreat report confirms through shared return path");
        // Defeat is a server result, not an HP==0 automatic-clear rule.
        var dead=new Fixture();var hp0=dead.BattleNotice();hp0.units[0].hp=0;dead.server.ReceiveBattleUnits(hp0);
        Check(dead.View.record.campaignPhase=="explore"&&!dead.View.record.ghost,"zero HP alone neither ends scenario nor creates field ghost");
        var defeat=dead.Notice();before=State(dead.View.record);dead.store.fail=true;
        Check(dead.server.ReceiveScenarioExit(defeat).Failure==WorldFailure.StorageUnavailable&&State(dead.View.record)==before&&dead.View.battle!=null,"failed defeat save keeps original battle");
        dead.store.fail=false;Check(dead.server.ReceiveScenarioExit(defeat).Accepted,"approved defeat stored");
        Check(dead.View.record.campaignPhase=="complete"&&dead.View.presentation==null&&dead.View.battle==null&&dead.View.record.scenarioVisit.result.IsDefeat
            &&!dead.View.record.ghost&&dead.View.record.hp==0,"defeat opens report without victory outro; ghost waits for return");
        Check(dead.server.DrainCampaignEffects().Length==0,"defeat emits no victory presentation effects");
        Check(!dead.server.ReceiveScenarioExit(defeat).Accepted,"duplicate defeat refused");dead.Reload();
        Check(dead.View.record.scenarioVisit.result.IsDefeat&&!dead.View.record.ghost&&dead.View.record.hp==0,"pending defeat report reloads without premature ghost or winning outro");
        before=State(dead.View.record);dead.store.fail=true;
        Check(dead.server.ConfirmScenarioResult(defeat.id,Id()).Failure==WorldFailure.StorageUnavailable&&State(dead.View.record)==before&&!dead.View.record.ghost,"failed defeat confirmation does not partially apply ghost/return");
        dead.store.fail=false;Check(dead.server.ConfirmScenarioResult(defeat.id,Id()).Accepted,"defeat confirmation returns");
        Check(dead.View.record.map==1000&&dead.View.record.x==21&&dead.View.record.y==21&&dead.View.record.ghost&&dead.View.record.hp==0&&dead.View.record.mp==3&&dead.View.record.gold==123
            &&dead.View.record.lastScenarioResult.IsDefeat,"dead normal-combat return sets user-confirmed ghost without heal/reward/penalty");
        dead.Reload();Check(dead.View.record.ghost&&dead.View.record.lastScenarioResult.IsDefeat,"ghost and historical defeat survive reconnect");
        Check(dead.server.EnterCampaignMap(26).Failure==WorldFailure.InvalidStage,"ghost cannot reenter scenario");
        var gate=dead.definition.Map(1000).portals.First(p=>p.targetMap==101);dead.Walk(gate.cells[0].x,gate.cells[0].y);
        Check(dead.server.EnterWorldPortal(gate.id,true).Failure==WorldFailure.InvalidStage,"ghost cannot enter training combat through a second route");
        // Existing temple operation remains held; this feature does not invent a free resurrection.
        dead.Reload();var temple=dead.definition.maps.Single(m=>m.npcs.Any(n=>n.kind==(int)FacilityKind.Temple));gate=dead.definition.Map(1000).portals.First(p=>p.targetMap==temple.id);
        dead.Walk(gate.cells[0].x,gate.cells[0].y);Check(dead.server.EnterWorldPortal(gate.id,false).Accepted,"ghost can enter temple");
        var staff=temple.npcs.Single();dead.Walk(staff.counter[0].x,staff.counter[0].y);Check(dead.server.OpenWorldFacility(staff.id).Accepted,"ghost can interact with temple NPC");
        before=State(dead.View.record);Check(dead.server.ReviveWorld(dead.View.facility.token,Id()).Failure==WorldFailure.RulesUnavailable&&State(dead.View.record)==before,"unknown resurrection cost/health policy stays held");
        // Authority may declare defeat while an attack or retreat request is unresolved.
        var racing=new Fixture();racing.server.RequestBattleAction(TrainingAction.Attack,400,0,0,Id());var oldHit=racing.BattleNotice();oldHit.request=racing.View.battle.pending.id;
        var loss=racing.Notice();loss.health=new CharacterGrowthHealth{hp=0,maxHp=200,mp=2,maxMp=65};
        Check(racing.server.ReceiveScenarioExit(loss).Accepted&&racing.View.battle==null&&racing.View.record.hp==0,"authoritative defeat supersedes pending attack and supplies explicit health");
        Check(racing.server.ReceiveBattleUnits(oldHit).Failure==WorldFailure.Busy,"late attack reply cannot revive finished battle");
        var retreatRace=new Fixture();retreatRace.Queue();var lateFlee=retreatRace.Notice(CampaignExitKind.Retreat);var loss2=retreatRace.Notice();
        Check(retreatRace.server.ReceiveScenarioExit(loss2).Accepted&&retreatRace.View.pendingRetreat==null,"defeat supersedes pending retreat");
        Check(!retreatRace.server.ReceiveScenarioExit(lateFlee).Accepted,"late flee reply cannot replace defeat");
        Check(retreatRace.server.ConfirmScenarioResult(loss2.id,Id()).Accepted&&!retreatRace.View.record.ghost&&retreatRace.View.record.hp==17,"party defeat with living main commander does not invent personal death");
        var winRace=new Fixture();winRace.Queue();var late=winRace.Notice(CampaignExitKind.Retreat);
        Check(winRace.server.ReceiveCampaignClear(new CampaignClearNotice{approved=true,id=Id(),map=26,session=winRace.View.campaignSession}).Accepted&&winRace.View.pendingRetreat==null,"approved victory supersedes pending retreat");
        Check(!winRace.server.ReceiveScenarioExit(late).Accepted,"late retreat cannot overwrite approved winning outro");
        var moving=new Fixture();moving.server.ExecuteBattleAction(TrainingAction.Move,-1,3,38,(r,v)=>{var n=moving.BattleNotice();n.request=r.id;n.units[0].y=38;return n;});
        Check(moving.View.moving&&moving.server.RequestScenarioRetreat(moving.View.record.scenarioVisit.id,Id()).Failure==WorldFailure.Busy,"moving commander cannot request retreat");
        Check(moving.server.ReceiveScenarioExit(moving.Notice()).Accepted&&!moving.View.moving&&moving.View.record.y==38,"approved defeat stops presentation at the already-confirmed grid endpoint");
        foreach(string problem in new[]{"owner","session","visit","revision","approval","id","kind","result","health","win"})
        {
            var invalid=new Fixture();var n=invalid.Notice();switch(problem)
            {
                case "owner":n.character=Id();break;case "session":n.session=Id();break;case "visit":n.visit=Id();break;case "revision":n.expectedBattleRevision++;break;
                case "approval":n.approved=false;break;case "id":n.id="bad";break;case "kind":n.kind=(CampaignExitKind)99;break;case "result":n.result.visit=Id();break;
                case "health":n.health=new CharacterGrowthHealth{hp=201,maxHp=200};break;case "win":n.result.victory=true;break;
            }
            before=State(invalid.View.record);Check(!invalid.server.ReceiveScenarioExit(n).Accepted&&State(invalid.View.record)==before,"invalid exit preserves state: "+problem);
        }
        var callback=new Fixture();Check(callback.server.ExecuteScenarioRetreat(callback.View.record.scenarioVisit.id,(r,v)=>callback.Notice(CampaignExitKind.Retreat)).Accepted&&callback.View.record.map==1000,"configured retreat authority executes the actual request/approval/return path");
        var reconnect=new Fixture();reconnect.Queue();var old=reconnect.Notice(CampaignExitKind.Retreat);reconnect.Reload();
        Check(reconnect.View.pendingRetreat==null&&reconnect.server.ReceiveScenarioExit(old).Failure==WorldFailure.StaleRequest,"unapproved retreat is not persisted and old session approval rejected");
        var origin=new Fixture();origin.Queue();var blocked=origin.Notice(CampaignExitKind.Retreat);origin.definition.Map(1000).npcs=origin.definition.Map(1000).npcs.Concat(new[]{new WorldNpc{id="blocked-origin",x=21,y=21,counter=Array.Empty<WorldPoint>()}}).ToArray();
        before=State(origin.View.record);Check(origin.server.ReceiveScenarioExit(blocked).Failure==WorldFailure.InvalidInput&&State(origin.View.record)==before,"blocked original tile cannot be replaced by a guessed destination");
        var concur=new Fixture(false);var townOld=concur.server;concur.Reload();concur.Enter();var deathNotice=concur.Notice();deathNotice.health=new CharacterGrowthHealth{hp=0,maxHp=200,mp=3,maxMp=65};concur.server.ReceiveScenarioExit(deathNotice);concur.server.ConfirmScenarioResult(deathNotice.id,Id());
        var growthId=Id();var staleGrowth=new CharacterGrowthNotice{approved=true,id=growthId,character=townOld.WorldSnapshot().characterId,session=townOld.WorldSnapshot().growthSession,
            state=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=growthId}};
        var persisted=State(concur.store.data);Check(townOld.ReceiveCharacterGrowth(staleGrowth).Failure==WorldFailure.StaleRequest&&State(concur.store.data)==persisted,"pre-visit service cannot erase a later ghost/exit marker after return");
        // Real serialization of the terminal state, not just in-memory copies.
        var durable=new Fixture();var durableLoss=durable.Notice();durableLoss.health=new CharacterGrowthHealth{hp=0,maxHp=200,mp=3,maxMp=65};durable.server.ReceiveScenarioExit(durableLoss);
        string path=Path.Combine(Path.GetTempPath(),"ds-scenario-exit-"+Id(),"accounts.json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(durable.store.data)&&disk.TryRead(out var restored)&&restored.accounts[0].character.world.scenarioVisit.result.IsDefeat,"defeat result/terminal marker round-trip through real file format");
            var server=new EntryAccountService(disk);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(durable.definition).Accepted&&server.ConfirmScenarioResult(durableLoss.id,Id()).Accepted&&server.WorldSnapshot().record.ghost,"disk-backed pending defeat confirms to persistent field ghost");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Scenario retreat/defeat/return and user-confirmed ghost contracts with isolated approval fixtures; no server escape/defeat formulas or resurrection pricing."}));
    }
}
