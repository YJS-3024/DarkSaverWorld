using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static partial class AuthoredWorldStartupChecks
{
    private static readonly System.Collections.Generic.List<object> fieldTravel=new System.Collections.Generic.List<object>();
    private static readonly System.Collections.Generic.List<object> returnDraftTravel=new System.Collections.Generic.List<object>();
    private static string TravelState(object value)=>JsonSerializer.Serialize(value,new JsonSerializerOptions{IncludeFields=true});
    private static string TravelResources(WorldRecord record)=>TravelState(new{record.healthKnown,record.hp,record.maxHp,record.mp,record.maxMp,
        record.ghost,record.goldKnown,record.gold,record.growth,record.inventory,record.equipment,record.roster,record.clearedScenarios});
    private static EntryAccountService TravelAccount(NativeCampaignArt art,NativeWorldArt worldArt,out Memory store)
    {
        store=new Memory();var service=new EntryAccountService(store);var client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"필드이동검사");
        var character=store.data.accounts[0].character;character.tutorialCompleted=true;
        // Injured resources are an isolated account fixture, not new production defaults.
        character.world=new WorldRecord{healthKnown=true,hp=17,maxHp=200,mp=3,maxMp=45};
        client.OpenLocalProfile();Check(new LocalServerManager(service,art).Request_BeginWorld(worldArt).Accepted,"field fixture uses real saved account/world startup");return service;
    }
    private static void FinishWalk(EntryAccountService service)
    {
        for(int i=0;i<2000&&service.WorldSnapshot().moving;i++)service.AdvanceWorld(160);
        Check(!service.WorldSnapshot().moving,"walking finishes without teleporting the fixture");
    }
    private static void CheckFieldTravel(ScenarioGatewaySettings settings,NativeCampaignArt campaign,NativeWorldArt worldArt)
    {
        foreach(var gate in settings.gates.Where(g=>settings.applyToGame&&g.enabled&&g.IsWalk&&g.fromMap==1000&&g.targetMap==1010))
        {
            var cells=gate.Cells();
            for(int index=0;index<cells.Length;index++)
            {
                var service=TravelAccount(campaign,worldArt,out var store);string resources=TravelResources(service.WorldSnapshot().record);
                var point=cells[index];string label=gate.name+" ("+point.x+","+point.y+")";
                Check(service.MoveWorld(point.x,point.y).Accepted,"walk from town21,21 to actual saved field gate "+label);
                Check(!service.EnterCampaignMap(1010).Accepted,"field transition cannot skip an in-flight walk "+label);FinishWalk(service);
                var view=service.WorldSnapshot();var portal=worldArt.Last.Map(1000).portals.Single(p=>p.gateway==gate.id);
                Check(view.pendingPortal==portal.id&&view.record.map==1000&&view.record.x==point.x&&view.record.y==point.y,"actual gate cell triggers correct pending portal "+label);
                string before=TravelState(view.record);store.fail=true;
                Check(service.EnterCampaignMap(1010).Failure==WorldFailure.StorageUnavailable&&TravelState(service.WorldSnapshot().record)==before
                    &&service.WorldSnapshot().pendingPortal==portal.id,"failed entry save keeps town/position/pending gate for retry "+label);
                store.fail=false;Check(service.EnterCampaignMap(1010).Accepted,"same pending field entry retries after storage recovery "+label);
                view=service.WorldSnapshot();Check(view.record.map==1010&&view.pendingPortal==null&&view.presentation==null&&view.battle==null
                    &&gate.SpawnContains(view.record.x,view.record.y),"field arrival uses saved destination area without starting a scenario/battle "+label);
                if(index==0)Check(view.record.x==gate.spawnX&&view.record.y==gate.spawnY,"first entrance cell uses first arrival cell "+label);
                if(index==cells.Length-1)Check(view.record.x==gate.spawnX+gate.SpawnWidth-1&&view.record.y==gate.spawnY+gate.SpawnHeight-1,"last entrance cell uses last arrival cell "+label);
                Check(TravelResources(view.record)==resources,"field entry cannot heal/grant XP/money/items "+label);
                Check(store.data.accounts[0].character.world.map==1010,"field arrival is committed to the isolated account "+label);
                if(index==0)
                {
                    string path=Path.Combine(Path.GetTempPath(),"ds-field-travel-"+Guid.NewGuid().ToString("N"),"accounts.json");
                    try
                    {
                        var file=new LocalAccountFileStore(path);Check(file.TryWrite(store.data)&&file.TryRead(out var restored)
                            &&TravelState(restored.accounts[0].character.world)==TravelState(view.record),"actual DataContract file preserves the field arrival "+label);
                        var diskService=new EntryAccountService(file);new EntryClient(diskService).OpenLocalProfile();
                        Check(new LocalServerManager(diskService,campaign).Request_BeginWorld(worldArt).Accepted&&TravelState(diskService.WorldSnapshot().record)==TravelState(view.record),"actual disk account reconnect restores field map/position/resources "+label);
                    }
                    finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
                }
                service=new EntryAccountService(store);new EntryClient(service).OpenLocalProfile();
                Check(new LocalServerManager(service,campaign).Request_BeginWorld(worldArt).Accepted&&TravelState(service.WorldSnapshot().record)==TravelState(view.record),"field map/position/resources restore on real service reconnect "+label);
                var current=service.WorldSnapshot();before=TravelState(current.record);
                bool authoredReturn=worldArt.Last.Map(1010).portals.Any(p=>p.targetMap==1000);
                fieldTravel.Add(new{gate=gate.name,x=point.x,y=point.y,target=view.record.map,arrivalX=view.record.x,arrivalY=view.record.y,authoredReturn});
                if(!authoredReturn)Check(!service.EnterCampaignMap(1000).Accepted&&TravelState(service.WorldSnapshot().record)==before,"unconfigured return route is not silently fabricated "+label);
                var map=worldArt.Last.Map(1010);var adjacent=new[]{new WorldPoint{x=current.record.x+1,y=current.record.y},new WorldPoint{x=current.record.x-1,y=current.record.y},
                    new WorldPoint{x=current.record.x,y=current.record.y+1},new WorldPoint{x=current.record.x,y=current.record.y-1}}
                    .First(p=>map.map.Contains(p.Tile)&&map.map.Terrain(p.Tile,2)>0&&map.map.Terrain(p.Tile,2)!=99);
                Check(service.MoveWorld(adjacent.x,adjacent.y).Accepted,"walking remains available after field reconnect "+label);FinishWalk(service);
                Check(service.WorldSnapshot().record.x==adjacent.x&&service.WorldSnapshot().record.y==adjacent.y&&TravelResources(service.WorldSnapshot().record)==resources,"normal field walking preserves resources "+label);
            }
        }
    }
    private static void CheckAuthoredReturnDrafts(ScenarioGatewaySettings settings,NativeCampaignArt campaign,NativeWorldArt worldArt)
    {
        foreach(var source in settings.gates.Where(g=>settings.applyToGame&&g.enabled&&g.IsWalk&&g.fromMap==1000&&g.targetMap==1010))
        {
            var draft=ScenarioGatewayData.CreateReturnDraft(source,campaign.GatewayMaps);Check(!draft.enabled,"real field return draft starts disabled "+source.name);
            draft.enabled=true;var isolated=settings.Copy();isolated.gates=isolated.gates.Concat(new[]{draft}).ToArray();
            var art=new NativeCampaignArt{Definition=campaign.Definition,GatewayMaps=campaign.GatewayMaps,gatewaySettings=new TestTextAsset{bytes=ScenarioGatewayData.Encode(isolated)},gatewayWindow=new object(),GatewayWindow=new object()};
            var service=TravelAccount(art,worldArt,out var store);string resources=TravelResources(service.WorldSnapshot().record);
            Check(service.MoveWorld(source.x,source.y).Accepted,"walk to real source with explicitly enabled test return "+source.name);FinishWalk(service);
            Check(service.EnterCampaignMap(1010).Accepted,"real source enters field at drafted return area "+source.name);
            var arrived=service.WorldSnapshot();Check(draft.Contains(arrived.record.x,arrived.record.y)&&arrived.pendingPortal==null,"arrival on reverse area is suppressed until leaving "+source.name);
            var map=worldArt.Last.Map(1010);var outside=new[]{new WorldPoint{x=arrived.record.x+1,y=arrived.record.y},new WorldPoint{x=arrived.record.x-1,y=arrived.record.y},
                new WorldPoint{x=arrived.record.x,y=arrived.record.y+1},new WorldPoint{x=arrived.record.x,y=arrived.record.y-1}}
                .First(p=>!draft.Contains(p.x,p.y)&&map.map.Contains(p.Tile)&&map.map.Terrain(p.Tile,2)>0&&map.map.Terrain(p.Tile,2)!=99);
            Check(service.MoveWorld(outside.x,outside.y).Accepted,"leave real field return area "+source.name);FinishWalk(service);
            var back=draft.Cells().Last();Check(service.MoveWorld(back.x,back.y).Accepted,"walk back into real reverse entrance "+source.name);FinishWalk(service);
            bool multiple=draft.Cells().Length>1;
            Check(service.EnterWorldPortal(service.WorldSnapshot().pendingPortal,false).Accepted&&service.WorldSnapshot().record.map==1000
                &&service.WorldSnapshot().record.x==source.x+(multiple?source.Width-1:0)&&service.WorldSnapshot().record.y==source.y+(multiple?source.Height-1:0),"drafted return uses first cell for one source or last cell for multiple sources "+source.name);
            Check(TravelResources(service.WorldSnapshot().record)==resources,"real map roundtrip preserves injured HP/MP and all resources "+source.name);
            service=new EntryAccountService(store);new EntryClient(service).OpenLocalProfile();
            Check(new LocalServerManager(service,art).Request_BeginWorld(worldArt).Accepted&&service.WorldSnapshot().record.map==1000&&service.WorldSnapshot().pendingPortal==null,"town-edge reconnect does not bounce to field "+source.name);
            Check(service.MoveWorld(21,21).Accepted,"walk away from the town-edge return "+source.name);FinishWalk(service);
            Check(service.WorldSnapshot().record.x==21&&service.WorldSnapshot().record.y==21&&TravelResources(service.WorldSnapshot().record)==resources,"returned character can reach town centre "+source.name);
            returnDraftTravel.Add(new{source=source.name,draft.enabled,draft.fromMap,draft.targetMap,x=draft.x,y=draft.y,passed=true});
        }
    }
}
