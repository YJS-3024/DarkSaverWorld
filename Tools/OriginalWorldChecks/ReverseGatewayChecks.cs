using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;

internal static class ReverseGatewayEntry{public static void Main(string[] args)=>GatewayChecks.RunReverse(args[0]);}
internal static partial class GatewayChecks
{
    internal static void RunReverse(string path)
    {
        root=path;var catalog=Catalog();var forward=Walk(1000,1010,22,22,6,1,10,12);forward.spawnWidth=2;forward.spawnHeight=1;forward.group="검증 그룹";
        byte[] original=ScenarioGatewayData.Encode(Areas(forward));var draft=ScenarioGatewayData.CreateReturnDraft(forward,catalog);
        Check(original.SequenceEqual(ScenarioGatewayData.Encode(Areas(forward))),"return authoring never mutates the source gateway");
        Check(!draft.enabled&&draft.IsWalk&&Guid.TryParseExact(draft.id,"N",out _)&&draft.id!=forward.id,"new return has a distinct ID and is disabled");
        Check(draft.fromMap==1010&&draft.targetMap==1000&&draft.fromHash==forward.targetHash&&draft.targetHash==forward.fromHash,"return swaps map IDs and exact source hashes");
        Check(draft.x==10&&draft.y==12&&draft.Width==2&&draft.Height==1&&draft.spawnX==22&&draft.spawnY==22&&draft.SpawnWidth==6&&draft.SpawnHeight==1,"asymmetric6-to2 areas swap without losing dimensions");
        Check(draft.group==forward.group&&draft.name.Contains(forward.name)&&string.IsNullOrEmpty(draft.replacesPortal)&&draft.returnX==-1&&draft.returnY==-1,"metadata does not inherit a source replacement/retreat policy");
        var replacement=forward.Copy();replacement.replacesPortal="native-forward";Check(string.IsNullOrEmpty(ScenarioGatewayData.CreateReturnDraft(replacement,catalog).replacesPortal),"source native replacement cannot leak into return");
        var legacy=forward.Copy();legacy.spawnWidth=legacy.spawnHeight=0;Check(ScenarioGatewayData.CreateReturnDraft(legacy,catalog).Width==1,"missing old destination size normalizes to one cell");
        var oneWay=FlatWorld();ScenarioGatewayData.Apply(Areas(forward,draft),catalog,oneWay);
        Check(oneWay.Map(1010).portals.Length==0,"merely creating/serializing a disabled draft never enables return travel");
        foreach(int target in new[]{26,101,201,301,-1,1000})
        {
            var bad=forward.Copy();bad.targetMap=target;
            Check(!ScenarioGatewayData.CanDraftReturn(bad,catalog),"scenario/training/duel/tutorial/missing/self destination has no ordinary reverse tool "+target);
            Reject(()=>ScenarioGatewayData.CreateReturnDraft(bad,catalog),"unsupported route rejected "+target);
        }
        Reject(()=>ScenarioGatewayData.CreateReturnDraft(Settings().gates[0],catalog),"NPC scenario room is not converted to an automatic walking entrance");
        foreach(string invalid in new[]{"hash","negative","size"})
        {
            var bad=forward.Copy();if(invalid=="hash")bad.fromHash="stale";if(invalid=="negative")bad.spawnX=-1;if(invalid=="size")bad.spawnWidth=int.MaxValue;
            Reject(()=>ScenarioGatewayData.CreateReturnDraft(bad,catalog),"invalid draft source rejected before area allocation "+invalid);
        }
        var second=ScenarioGatewayData.CreateReturnDraft(forward,catalog);Check(second.id!=draft.id,"each deliberate draft has a fresh ID");
        Reject(()=>ScenarioGatewayData.Validate(Areas(forward,draft,second),catalog),"overlapping return drafts still obey existing authoring validation");
        draft.enabled=true;
        foreach(int slot in new[]{0,1})
        {
            var world=FlatWorld();ScenarioGatewayData.Apply(Areas(forward,draft),catalog,world);var fixture=Service(world);var service=fixture.server;
            Check(service.MoveWorld(22,22).Accepted,"walk to explicit source area");service.AdvanceWorld(10000);
            Check(service.EnterCampaignMap(1010).Accepted&&service.WorldSnapshot().record.x==10&&service.WorldSnapshot().record.y==12,"outgoing portal arrives on newly enabled return area");
            Check(!service.EnterCampaignMap(1000).Accepted&&service.WorldSnapshot().pendingPortal==null,"arrival does not immediately bounce to source");
            Check(service.MoveWorld(12,12).Accepted,"leave full return area to rearm it");service.AdvanceWorld(10000);
            Check(service.MoveWorld(10+slot,12).Accepted,"walk back into selected return cell");service.AdvanceWorld(10000);
            string pending=service.WorldSnapshot().pendingPortal;fixture.store.fail=true;
            Check(service.EnterWorldPortal(pending,false).Failure==DarkSaver.Original.World.WorldFailure.StorageUnavailable&&service.WorldSnapshot().record.map==1010
                &&service.WorldSnapshot().pendingPortal==pending,"failed return save preserves field and pending portal");fixture.store.fail=false;
            Check(service.EnterWorldPortal(pending,false).Accepted&&service.WorldSnapshot().record.map==1000&&service.WorldSnapshot().record.x==(slot==0?22:27)
                &&service.WorldSnapshot().record.y==22,"two return cells map to first/last source arrival cells");
            Check(service.WorldSnapshot().pendingPortal==null&&!service.EnterCampaignMap(1010).Accepted,"return arrival has the same anti-bounce guard");
        }
        string folder=Path.Combine(Path.GetTempPath(),"ds-reverse-gateway-"+Guid.NewGuid().ToString("N"));
        try
        {
            var settings=Areas(forward,draft);string file=Path.Combine(folder,"gateways.json");ScenarioGatewayData.Save(file,settings,catalog,null);
            var restored=ScenarioGatewayData.Decode(File.ReadAllBytes(file));Check(ScenarioGatewayData.Encode(restored).SequenceEqual(ScenarioGatewayData.Encode(settings)),"explicit pair survives actual authoring file round-trip");
        }
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Explicit return-draft helper and real account/service travel on isolated flat maps; disabled-by-default authoring, independent areas, validation, anti-bounce and save retry. No production gateways changed, automatic return policy or Unity Editor GUI/Play."}));
    }
}
