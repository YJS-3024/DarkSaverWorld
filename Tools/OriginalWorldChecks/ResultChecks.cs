using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class ResultChecks
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
            definition=Read<WorldDefinition>("Data/World/KaosiaWorld.json");var campaign=Read<CampaignDefinition>("Data/Campaign/CampaignCatalog.json");
            var index=ReadPaths();foreach(var map in campaign.maps)
            {
                map.intro=map.introIndex<0?"":File.ReadAllText(Path.Combine(root,index[map.introIndex]));
                map.outro=map.outroIndex<0?"":File.ReadAllText(Path.Combine(root,index[map.outroIndex]));
            }
            var spawn=campaign.Map(26).entries.First(e=>e.unit==0);
            // Isolated authoring fixture, not a fabricated production field entrance.
            campaign.connections=new[]{new CampaignConnection{fromMap=1000,toMap=26,x=spawn.x,y=spawn.y,evidence="result test fixture only",cells=new[]{new WorldPoint{x=21,y=21}}}};
            definition=campaign.Merge(definition);server=new EntryAccountService(store);var client=new EntryClient(server);client.OpenLocalProfile();client.Create(100,"결과검사");
            var c=store.data.accounts.Single().character;c.tutorialCompleted=true;c.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=65,goldKnown=true,gold=123};
            client.OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"isolated world fixture begins");
        }
        private string[] ReadPaths()
        {
            using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Tools/OriginalWorldChecks/campaign-assets.json")));
            return doc.RootElement.GetProperty("scripts").EnumerateArray().Select(p=>p.GetString()).ToArray();
        }
        public void Enter()
        {Check(server.EnterCampaignMap(26).Accepted,"production scenario entry");FinishIntro();}
        public void FinishIntro()
        {for(int i=0;i<10000&&View.presentation?.completed==false;i++){if(View.presentation.waitingMessage)server.ContinueCampaignPresentation();server.AdvanceWorld(1000);}server.AdvanceWorld(0);Check(View.record.campaignPhase=="explore","source intro completes");}
        public void Clear()
        {Check(server.ReceiveCampaignClear(new CampaignClearNotice{approved=true,id=Id(),map=View.record.map,session=View.campaignSession}).Accepted,"approved source ending starts");}
        public void FinishEnding()
        {for(int i=0;i<10000&&View.presentation?.completed==false;i++){if(View.presentation.waitingMessage)server.ContinueCampaignPresentation();server.AdvanceWorld(1000);}server.AdvanceWorld(0);Check(View.record.campaignPhase=="complete","source ending completes");}
        public CampaignResultNotice Notice()=>new CampaignResultNotice{approved=true,character=View.characterId,session=View.campaignSession,
            result=new CampaignResultRecord{id=Id(),visit=View.record.scenarioVisit.id,map=26,victory=true,experience=407840,kills=100,spoils=10405446,eventCompleted=1,eventTotal=1,
                allies=new[]{new CampaignResultUnit{unit=0,job=100,name="결과검사",experience=407840}}}};
        public void Reload()
        {server=new EntryAccountService(store);new EntryClient(server).OpenLocalProfile();Check(server.BeginWorld(definition).Accepted,"real profile reconnect");}
    }
    private static void Main(string[] args)
    {
        root=args[0];var fixture=new Fixture();var f=fixture;var before=State(f.View.record);f.store.fail=true;
        Check(f.server.EnterCampaignMap(26).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"failed entry does not leave a partial visit");
        f.store.fail=false;f.Enter();var visit=f.View.record.scenarioVisit;
        Check(visit.sourceMap==1000&&visit.x==21&&visit.y==21&&visit.map==26&&Guid.TryParseExact(visit.id,"N",out _),"actual pre-entry map/tile and distinct visit saved");
        var notice=f.Notice();Check(f.server.ReceiveScenarioResult(notice).Failure==WorldFailure.InvalidStage,"result cannot fabricate scenario victory before approved ending");
        Check(f.server.ConfirmScenarioResult(notice.result.id,Id()).Failure==WorldFailure.StaleRequest,"no result to confirm before authority");
        f.Clear();notice=f.Notice();f.store.fail=true;before=State(f.View.record);
        Check(f.server.ReceiveScenarioResult(notice).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"failed result save keeps report unacknowledged");
        f.store.fail=false;Check(f.server.ReceiveScenarioResult(notice).Accepted,"result accepted while source ending is still running");
        Check(f.View.record.hp==17&&f.View.record.mp==3&&f.View.record.gold==123&&f.View.record.growth==null&&f.View.record.inventory.Length==0,"report totals do not grant XP/gold/items or heal");
        Check(f.server.ConfirmScenarioResult(notice.result.id,Id()).Failure==WorldFailure.InvalidStage,"confirmation cannot skip source ending");
        notice.result.allies[0].name="mutated";notice.result.experience=0;
        Check(f.View.record.scenarioVisit.result.allies[0].name=="결과검사"&&f.View.record.scenarioVisit.result.experience==407840,"received report rows copied");
        f.FinishEnding();var resultId=f.View.record.scenarioVisit.result.id;
        Check(f.server.MoveWorld(4,37).Failure==WorldFailure.Busy,"completed scenario waits for report confirmation");
        Check(!f.server.ReceiveScenarioResult(f.Notice()).Accepted,"second result id cannot replace the same visit report");
        var exposed=f.View;exposed.record.scenarioVisit.result.allies[0].name="mutated view";
        Check(f.View.record.scenarioVisit.result.allies[0].name=="결과검사","view does not alias report");
        before=State(f.View.record);f.Reload();Check(State(f.View.record)==before&&f.View.record.campaignPhase=="complete","pending result survives actual reconnect without replaying ending");
        f.store.fail=true;string confirm=Id();before=State(f.View.record);
        Check(f.server.ConfirmScenarioResult(resultId,confirm).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before,"failed return retains report/position for retry");
        f.store.fail=false;Check(f.server.ConfirmScenarioResult(resultId,confirm).Accepted,"same confirmation can retry after storage recovers");
        Check(f.View.record.map==1000&&f.View.record.x==21&&f.View.record.y==21&&f.View.record.direction==visit.direction&&f.View.record.scenarioVisit==null,"confirm returns to exact source tile, without guessed exit");
        Check(f.View.pendingPortal==null&&f.View.record.lastScenarioResult.id==resultId&&f.View.record.gold==123&&f.View.record.hp==17,"return records last display result and preserves unrelated state");
        before=State(f.View.record);Check(!f.server.ConfirmScenarioResult(resultId,confirm).Accepted&&State(f.View.record)==before,"double confirmation cannot duplicate result or relocation");
        f.Reload();Check(f.View.record.lastScenarioResult.experience==407840&&f.View.record.scenarioVisit==null,"confirmed report remains historical and does not reopen on reconnect");
        f.Enter();Check(f.View.record.scenarioVisit.id!=visit.id,"reentry owns a different visit identity");f.Clear();f.FinishEnding();
        var fresh=f.Notice();fresh.result.experience=null;fresh.result.spoils=null;fresh.result.kills=null;fresh.result.eventCompleted=null;fresh.result.eventTotal=null;fresh.result.allies=Array.Empty<CampaignResultUnit>();
        Check(f.server.ReceiveScenarioResult(fresh).Accepted&&!f.View.record.scenarioVisit.result.experience.HasValue,"unknown report numbers stay absent instead of invented zero");
        var validation=new Fixture();validation.Enter();validation.Clear();validation.FinishEnding();
        foreach(string problem in new[]{"owner","session","approval","visit","map","id","kills","event-pair","event-range","job","duplicate","name","defeat"})
        {
            var invalid=validation.Notice();switch(problem)
            {
                case "owner":invalid.character=Id();break;case "session":invalid.session=Id();break;case "approval":invalid.approved=false;break;case "visit":invalid.result.visit=Id();break;
                case "map":invalid.result.map=27;break;case "id":invalid.result.id="invalid";break;case "kills":invalid.result.kills=-1;break;case "event-pair":invalid.result.eventTotal=null;break;
                case "event-range":invalid.result.eventCompleted=2;break;case "job":invalid.result.allies[0].job=999999;break;case "duplicate":invalid.result.enemies=new[]{invalid.result.allies[0].Copy()};break;
                case "name":invalid.result.allies[0].name="";break;case "defeat":invalid.result.victory=false;break;
            }
            before=State(validation.View.record);Check(!validation.server.ReceiveScenarioResult(invalid).Accepted&&State(validation.View.record)==before,"invalid report preserves live state: "+problem);
        }
        var stale=validation.Notice();validation.Reload();Check(validation.server.ReceiveScenarioResult(stale).Failure==WorldFailure.StaleRequest,"pre-reconnect result session cannot replay");
        var latest=validation.Notice();Check(validation.server.ReceiveScenarioResult(latest).Accepted,"fresh-session approved report accepted");
        validation.definition.Map(1000).npcs=validation.definition.Map(1000).npcs.Concat(new[]{new WorldNpc{id="fixture-block",x=21,y=21,counter=Array.Empty<WorldPoint>()}}).ToArray();
        before=State(validation.View.record);Check(validation.server.ConfirmScenarioResult(latest.result.id,Id()).Failure==WorldFailure.InvalidInput&&State(validation.View.record)==before,"changed blocked origin cannot silently relocate elsewhere");
        var concurrent=new Fixture();concurrent.Enter();concurrent.Clear();concurrent.FinishEnding();
        var oldServer=concurrent.server;var oldNotice=concurrent.Notice();concurrent.Reload();
        var current=concurrent.Notice();Check(concurrent.server.ReceiveScenarioResult(current).Accepted,"current service stores report");
        string diskState=State(concurrent.store.data);before=State(oldServer.WorldSnapshot().record);
        Check(oldServer.ReceiveScenarioResult(oldNotice).Failure==WorldFailure.StaleRequest&&State(oldServer.WorldSnapshot().record)==before&&State(concurrent.store.data)==diskState,"stale service cannot replace another service's persisted report");
        oldServer=concurrent.server;concurrent.Reload();Check(concurrent.server.ConfirmScenarioResult(current.result.id,Id()).Accepted,"current service confirms persisted report");
        diskState=State(concurrent.store.data);Check(oldServer.ConfirmScenarioResult(current.result.id,Id()).Failure==WorldFailure.StaleRequest&&State(concurrent.store.data)==diskState,"stale service cannot undo an already persisted return");
        var phases=new Fixture();phases.Enter();phases.Clear();oldServer=phases.server;var oldPhaseResult=phases.Notice();phases.Reload();phases.FinishEnding();
        diskState=State(phases.store.data);Check(oldServer.ReceiveScenarioResult(oldPhaseResult).Failure==WorldFailure.StaleRequest&&State(phases.store.data)==diskState,"stale ending phase cannot rewind a completed ending while storing a report");
        // Real DataContract file persistence, including full unsigned display numbers.
        string temp=Path.Combine(Path.GetTempPath(),"ds-result-"+Id(),"accounts.json");
        try
        {
            var disk=new LocalAccountFileStore(temp);var document=f.store.data.Copy();var persisted=document.accounts[0].character.world;
            persisted.scenarioVisit.result.experience=ulong.MaxValue;
            Check(disk.TryWrite(document)&&disk.TryRead(out var restored)&&restored.accounts[0].character.world.scenarioVisit.result.experience==ulong.MaxValue,"real disk round-trip retains pending display data without numeric truncation");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(temp)))Directory.Delete(Path.GetDirectoryName(temp),true);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Production scenario result/return services with isolated routes and approved display fixtures. No rewards, victory calculation or rendered Play."}));
    }
}
