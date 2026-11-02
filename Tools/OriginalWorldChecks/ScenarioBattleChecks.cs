using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class ScenarioBattleChecks
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
        public Fixture()
        {
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");definition.combatCatalog=Read<OriginalCombatCatalog>("Data/Combat/OriginalCombat.json");definition.growthCatalog=Read<CharacterGrowthCatalog>("Data/Growth/CharacterGrowth.json");
            var campaign=Read<CampaignDefinition>("Data/Campaign/CampaignCatalog.json");
            using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Tools/OriginalWorldChecks/campaign-assets.json")));
            var scripts=manifest.RootElement.GetProperty("scripts").EnumerateArray().Select(p=>p.GetString()).ToArray();
            foreach(var map in campaign.maps)
            {map.intro=map.introIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.introIndex]));map.outro=map.outroIndex<0?"":File.ReadAllText(Path.Combine(root,scripts[map.outroIndex]));}
            var spawn=campaign.Map(26).entries.First(e=>e.unit==0);
            // Isolated route and health/AP fixture. Production connections and authority remain unconfigured.
            campaign.connections=new[]{new CampaignConnection{fromMap=1000,toMap=26,x=spawn.x,y=spawn.y,evidence="scenario combat check only",cells=new[]{new WorldPoint{x=21,y=21}}}};
            definition=campaign.Merge(definition);server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"전투검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=65,goldKnown=true,gold=123};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world begins");
            var id=Id();Check(server.ReceiveCharacterGrowth(new CharacterGrowthNotice{approved=true,id=id,character=View.characterId,session=View.growthSession,
                state=new CharacterGrowthRecord{job=100,classLevel=1,totalLevel=1,revision=1,lastNotice=id}}).Accepted,"isolated approved initial growth snapshot");
        }
        public void Enter(bool finish=true)
        {
            Check(server.EnterCampaignMap(26).Accepted,"native scenario entry at POS commander tile");
            if(finish)FinishIntro();
        }
        public void FinishIntro()
        {for(int i=0;i<10000&&View.presentation?.completed==false;i++){if(View.presentation.waitingMessage)server.ContinueCampaignPresentation();server.AdvanceWorld(1000);}server.AdvanceWorld(0);Check(View.record.campaignPhase=="explore","original intro completes before battle");}
        public TrainingUnit Unit(int id,int job,int x,int y)=>new TrainingUnit{id=id,job=job,x=x,y=y,direction=1,life=1,hp=id==0?17:50,maxHp=id==0?200:50,mp=id==0?3:0,maxMp=id==0?65:0,
            ap=20,maxAp=20,attackRange=1,movementBudget=5,canMove=true,canAttack=true,canRest=true};
        public TrainingUnitsNotice Notice(TrainingUnit[] units=null)=>new TrainingUnitsNotice{approved=true,id=Id(),character=View.characterId,session=View.campaignSession,
            expectedRevision=View.battle?.revision??0,units=units??View.battle.units.Select(u=>u.Copy()).ToArray()};
        public TrainingUnitsNotice Initial()
        {
            var commander=Unit(0,100,View.record.x,View.record.y);
            return Notice(new[]{commander}.Concat(definition.campaign.Map(26).units.Select(u=>Unit(u.unit,u.job,u.x,u.y))).ToArray());
        }
        public TrainingUnitsNotice Result(TrainingActionRequest request=null)
        {var n=Notice();n.request=(request??View.battle.pending).id;return n;}
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"real service reconnect restores approved scenario battle");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var f=new Fixture();
        Check(f.server.ReceiveBattleUnits(new TrainingUnitsNotice()).Failure==WorldFailure.InvalidStage,"town cannot receive a battle roster");
        Check(f.server.RequestBattleAction(TrainingAction.Rest,-1,0,0,Id()).Failure==WorldFailure.InvalidStage,"town cannot issue battle action");
        f.Enter(false);var initial=f.Initial();
        Check(f.server.ReceiveBattleUnits(initial).Failure==WorldFailure.Busy,"state cannot bypass the source intro phase");f.FinishIntro();
        Check(f.View.battle==null&&f.View.record.scenarioBattle==null&&f.View.units.Length==65,"original source roster alone does not invent HP/AP state");
        Check(f.server.RequestBattleAction(TrainingAction.Attack,400,0,0,Id()).Failure==WorldFailure.RulesUnavailable,"missing authoritative battle state remains held");
        Check(f.server.ReceiveTrainingUnits(initial).Failure==WorldFailure.InvalidStage,"legacy training-only notification cannot target scenario");
        Check(f.server.ReceiveBattleUnits(initial).Accepted&&f.View.battle.units.Length==66&&f.View.training==null,"one owner plus all 65 original units accepted through shared battle path");
        var source=f.definition.campaign.Map(26).units;var nav=f.definition.Map(26).map;
        Check(source.All(u=>{var actor=f.View.battle.Unit(u.unit);return actor.job==u.job&&actor.x==u.x&&actor.y==u.y;}),"all source jobs/coordinates retained, no supplemental formation");
        Check(source.Count(u=>nav.Terrain(new DarkSaver.Original.Tutorial.TutorialTile(u.x,u.y),2)==99)==25,"native 25 non-infantry-walkable positions are received, not rejected or relocated");
        Check(f.View.record.scenarioBattle.revision==1&&f.View.record.scenarioBattle.units.Length==66,"initial approved roster persisted with revision");
        var exposed=f.View;exposed.record.scenarioBattle.units[0].hp=1;exposed.battle.units[0].ap=1;
        Check(f.View.record.hp==17&&f.View.battle.Commander.ap==20,"public persisted/live snapshots are detached");
        Check(f.server.MoveWorld(4,37).Failure==WorldFailure.Busy,"ordinary movement cannot bypass scenario battle budget");
        var moveEnemy=f.Notice();moveEnemy.units.First(u=>u.id==400).x=4;moveEnemy.units.First(u=>u.id==400).y=37;
        Check(f.server.ReceiveBattleUnits(moveEnemy).Accepted,"authoritative enemy position updates independently of source placement reference");
        var before=State(f.View.record);f.server.AdvanceWorld(10000);
        Check(State(f.View.record)==before&&f.View.battle.Commander.ap==20,"elapsed time alone cannot invent general combat AI/AP/damage");
        Check(f.server.RequestTrainingAction(TrainingAction.Attack,400,0,0,Id()).Failure==WorldFailure.InvalidStage,"training-only command cannot enter scenario");
        Check(f.server.RequestBattleAction(TrainingAction.Attack,400,0,0,Id()).Accepted,"valid scenario attack queued");
        Check(State(f.View.record)==before&&f.View.battle.Unit(400).hp==50,"queued command has no predicted resource mutation");
        var clear=new CampaignClearNotice{approved=true,id=Id(),session=f.View.campaignSession,map=26};
        Check(f.server.ReceiveCampaignClear(clear).Failure==WorldFailure.Busy,"clear cannot race an unresolved attack");
        var hit=f.Result();hit.units[0].ap=13;hit.units.First(u=>u.id==400).hp=20;hit.experience=321;hit.expectedGrowthRevision=f.View.record.growth.revision;
        f.store.fail=true;Check(f.server.ReceiveBattleUnits(hit).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle.Unit(400).hp==50,"failed attack save is atomic across actor and persistent battle/growth state");
        f.store.fail=false;Check(f.server.ReceiveBattleUnits(hit).Accepted,"approved scenario attack result committed");
        Check(f.View.battle.Unit(400).hp==20&&f.View.record.scenarioBattle.units.First(u=>u.id==400).hp==20&&f.View.record.growth.experience==321,"live enemy damage and absolute XP persisted together");
        Check(f.View.record.growth.totalLevel==1&&f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.gold==123,"no level formula, implicit heal or money reward");
        before=State(f.View.record);Check(f.server.ReceiveBattleUnits(hit).Failure==WorldFailure.StaleRequest&&State(f.View.record)==before,"duplicate scenario result cannot award twice");
        var stale=f.Notice();f.Reload();
        Check(f.View.battle.Unit(400).hp==20&&f.View.battle.Commander.ap==13&&f.View.battle.revision==3,"reconnect restores remaining enemy HP/AP and revision, without source-reset healing");
        Check(f.server.ReceiveBattleUnits(stale).Failure==WorldFailure.StaleRequest,"pre-reconnect battle notification cannot replay");
        Check(f.server.ExecuteBattleAction(TrainingAction.Rest,-1,0,0,null).Failure==WorldFailure.RulesUnavailable&&f.View.battle.pending==null,"absent shared authority cannot lock controls");
        Check(f.server.ExecuteBattleAction(TrainingAction.Rest,-1,0,0,(r,v)=>{var n=f.Result(r);n.units[0].ap=15;return n;}).Accepted&&f.View.battle.Commander.ap==15,"configured shared authority drives real rest request/result service");
        Check(f.server.ExecuteBattleAction(TrainingAction.Move,-1,3,38,(r,v)=>{var n=f.Result(r);n.units[0].y=38;n.units[0].ap=9;return n;}).Accepted,"approved scenario movement committed");
        Check(f.View.moving&&f.View.renderY==37&&f.View.record.y==38&&f.View.record.scenarioBattle.units[0].y==38,"approved move presents from old tile and persists the accepted endpoint");
        f.Reload();Check(!f.View.moving&&f.View.record.y==38&&f.View.battle.Commander.y==38&&f.View.battle.Commander.ap==9,"mid-presentation reconnect restores confirmed target/AP without replaying animation");
        var kill=f.Notice();kill.units.First(u=>u.id==400).hp=0;Check(f.server.ReceiveBattleUnits(kill).Accepted,"approved enemy death received");
        var remove=f.Notice();remove.units=remove.units.Where(u=>u.id!=400).ToArray();Check(f.server.ReceiveBattleUnits(remove).Accepted,"removed enemy state received");f.Reload();
        var respawn=f.Notice(f.View.battle.units.Concat(new[]{f.Unit(400,source.First(u=>u.unit==400).job,4,37)}).ToArray());
        before=State(f.View.record);Check(f.server.ReceiveBattleUnits(respawn).Failure==WorldFailure.StaleRequest&&State(f.View.record)==before,"retired life remains protected after real reconnect");
        respawn.units.Last().life=2;Check(f.server.ReceiveBattleUnits(respawn).Accepted,"authoritative fresh life can respawn after reconnect");
        var allDead=f.Notice();foreach(var actor in allDead.units.Where(u=>u.id!=0))actor.hp=0;
        Check(f.server.ReceiveBattleUnits(allDead).Accepted&&f.View.record.campaignPhase=="explore"&&f.View.record.map==26,"zero living enemies alone does not fabricate clear/result/rewards");
        // A separate service remains on the prior accepted revision while the current one updates it.
        var oldServer=f.server;var oldNotice=f.Notice();f.Reload();var newer=f.Notice();newer.units[0].ap=8;f.server.ReceiveBattleUnits(newer);
        var saved=State(f.store.data);Check(oldServer.ReceiveBattleUnits(oldNotice).Failure==WorldFailure.StaleRequest&&State(f.store.data)==saved,"stale service cannot overwrite newer persistent battle revision");
        var interrupted=f.Notice();Check(f.server.RequestBattleAction(TrainingAction.Rest,-1,0,0,Id()).Accepted,"unresolved request before reconnect");f.Reload();
        Check(f.View.battle.pending==null&&f.View.battle.Commander.ap==8,"unapproved command is never saved or charged across reconnect");
        clear.session=f.View.campaignSession;clear.id=Id();
        f.store.fail=true;before=State(f.View.record);Check(f.server.ReceiveCampaignClear(clear).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle!=null,"failed clear preserves accepted battle snapshot");
        f.store.fail=false;Check(f.server.ReceiveCampaignClear(clear).Accepted&&f.View.battle==null&&f.View.record.scenarioBattle==null,"approved clear atomically freezes and removes active battle state");
        Check(f.server.ReceiveBattleUnits(interrupted).Failure==WorldFailure.Busy,"battle updates cannot alter source ending phase");
        f.server.AdvanceWorld(0);Check(f.View.record.campaignPhase=="complete","source ending still flows to result phase");
        var report=new CampaignResultNotice{approved=true,character=f.View.characterId,session=f.View.campaignSession,result=new CampaignResultRecord{id=Id(),visit=f.View.record.scenarioVisit.id,map=26,victory=true,experience=321,kills=65}};
        Check(f.server.ReceiveScenarioResult(report).Accepted&&f.server.ConfirmScenarioResult(report.result.id,Id()).Accepted,"battle closes through approved report and original entry return");
        Check(f.View.record.map==1000&&f.View.battle==null&&f.View.record.scenarioBattle==null&&f.View.record.growth.experience==321,"return does not keep a battle overlay or duplicate XP");
        // Persist and validate a real DataContract battle snapshot, including nullable ranges and life history.
        var diskFixture=new Fixture();diskFixture.Enter();diskFixture.server.ReceiveBattleUnits(diskFixture.Initial());
        string path=Path.Combine(Path.GetTempPath(),"ds-scenario-combat-"+Id(),"accounts.json");
        try
        {
            var disk=new LocalAccountFileStore(path);Check(disk.TryWrite(diskFixture.store.data)&&disk.TryRead(out _),"battle record round-trips through real file serializer");
            var reloaded=new EntryAccountService(disk);new EntryClient(reloaded).OpenLocalProfile();
            Check(reloaded.BeginWorld(diskFixture.definition).Accepted&&reloaded.WorldSnapshot().battle.units.Length==66,"real disk-backed world reconstructs the complete accepted battle state");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
        diskFixture.store.data.accounts[0].character.world.scenarioBattle.units[1].job=999999;
        var corrupted=State(diskFixture.store.data);var badRestore=new EntryAccountService(diskFixture.store);new EntryClient(badRestore).OpenLocalProfile();
        Check(!badRestore.BeginWorld(diskFixture.definition).Accepted&&State(diskFixture.store.data)==corrupted&&badRestore.WorldSnapshot()==null,"invalid saved job is rejected without overwriting stored evidence or leaving partial world state");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,originalScenario=26,originalEnemies=65,unityPlayExecuted=false,scope="Scenario battle services with native job/position roster and isolated HP/AP/authority fixtures; no production combat formulas or automatic clear."}));
    }
}
