using System;
using System.Linq;
using System.IO;
using DarkSaver.Original.Campaign;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class TrainingTierChecks
{
 static int checks;static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
 static void Main(string[] args)
 {
  foreach(var pair in new[]{(100,101),(103,101),(104,102),(114,102),(115,103),(124,103),(125,104),(135,105),(145,106),(155,107),(163,107)})
  {
   var f=CommanderNameChecks.CreateFixture(args[0]);var v=f.View;string id=Guid.NewGuid().ToString("N");
   var state=new CharacterGrowthRecord{job=pair.Item1,classLevel=1,totalLevel=1,revision=1,lastNotice=id};
   Check(f.service.ReceiveCharacterGrowth(new CharacterGrowthNotice{character=v.characterId,session=v.growthSession,id=id,approved=true,expectedRevision=0,state=state,operation=CharacterGrowthOperation.Snapshot}).Accepted,"explicit fixture class snapshot");
   var town=f.definition.Map(1000);town.npcs=Array.Empty<WorldNpc>();town.map.attributes=new int[town.map.width*town.map.height];
   town.portals=new[]{new WorldPortal{id="tier-entry",targetMap=101,spawnX=10,spawnY=7,returnX=21,returnY=21,cells=new[]{new WorldPoint{x=22,y=21}}}};
   int expectedX=10,expectedY=7;
   if(pair.Item1==100)
   {
    var maps=JsonSerializer.Deserialize<GatewayMapCatalog>(File.ReadAllText(Path.Combine(args[0],"Assets/_Original/Data/Campaign/GatewayMaps.json")),new JsonSerializerOptions{IncludeFields=true});
    var target=f.definition.Map(pair.Item2);string hash=maps.maps.First(m=>m.id==pair.Item2).hash;
    var point=Enumerable.Range(0,target.map.width*target.map.height).Select(n=>new ScenarioEntryPoint{map=pair.Item2,mapHash=hash,x=n%target.map.width,y=n/target.map.width,enabled=true})
        .First(p=>(p.x!=10||p.y!=7)&&ScenarioGatewayData.EntryLocationAllowed(f.definition,p));
    ScenarioGatewayData.Apply(new ScenarioGatewaySettings{entryPoints=new[]{point}},maps,f.definition);expectedX=point.x;expectedY=point.y;
   }
   Check(f.service.MoveWorld(22,21).Accepted,"walk to isolated entrance");f.service.AdvanceWorld(10000);
   Check(f.service.WorldSnapshot().pendingPortal=="tier-entry","entrance reached");
   if(pair.Item1==100)
   {
    f.store.fail=true;Check(f.service.EnterWorldPortal("tier-entry",true).Failure==WorldFailure.StorageUnavailable&&f.View.record.map==1000,"failed entry save preserves town state");f.store.fail=false;
   }
   Check(f.service.EnterWorldPortal("tier-entry",true).Accepted,"training enter");v=f.View;
   Check(v.record.map==pair.Item2&&v.record.x==expectedX&&v.record.y==expectedY&&v.training==null,"tier map selected; no invented combat state");
   Check(f.service.MoveWorld(11,7).Failure==WorldFailure.InvalidStage,"battle waiting does not permit town walking");
   Check(f.service.EscapeTraining().Accepted&&f.View.record.map==1000&&f.View.record.x==21,"tier training escape returns through original entrance");
   f.service.MoveWorld(22,21);f.service.AdvanceWorld(10000);Check(f.service.EnterWorldPortal("tier-entry",true).Accepted,"reenter tier training");
   f.client.Logout();f.client.OpenLocalProfile();Check(f.service.BeginWorld(f.definition).Accepted&&f.View.record.map==1000&&f.View.record.x==21,"reconnect exits every tier to same source entrance");
  }
  Console.WriteLine(JsonSerializer.Serialize(new{checks,classes=11,unityPlay=false,scope="Actual account/service tier entry, battle-wait movement rejection, escape and reconnect with isolated flat source entrance and explicit class snapshots. Native training map geometry retained; no monster/AP/HP authority invented."}));
 }
}
