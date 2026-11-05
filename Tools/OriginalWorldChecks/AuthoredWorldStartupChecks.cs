using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Campaign;
namespace DarkSaver.Original.World
{
    public sealed class NativeWorldArt
    {
        public bool IsConfigured=true;public Func<WorldDefinition> Factory;public WorldDefinition Last;
        public WorldDefinition Definition=>Last=Factory();
    }
}
namespace DarkSaver.Original.Campaign
{
    public sealed class TestTextAsset {public byte[] bytes;}
    public sealed partial class NativeCampaignArt
    {
        public bool IsConfigured=false; // Missing presentation resources, with valid map/settings data.
        public CampaignDefinition Definition;public TestTextAsset gatewaySettings;
        public GatewayMapCatalog GatewayMaps;public object gatewayWindow,GatewayWindow;
    }
}
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private readonly EntryAccountService service;private readonly NativeCampaignArt campaignArt;
        public LocalServerManager(EntryAccountService s,NativeCampaignArt c){service=s;campaignArt=c;}
        private WorldReply WorldRequest(Func<EntryAccountService,WorldReply> apply,bool entered=false)=>apply(service);
    }
}
internal static partial class AuthoredWorldStartupChecks
{
    private static int checks;
    private static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
    private sealed class Memory:IEntryAccountStore
    {
        public LocalAccountDocument data=new LocalAccountDocument();public bool fail;
        public bool TryRead(out LocalAccountDocument d){d=data.Copy();return true;}
        public bool TryWrite(LocalAccountDocument d){if(fail)return false;data=d.Copy();return true;}
    }
    public static void Main(string[] args)
    {
        string root=args[0];var options=new JsonSerializerOptions{IncludeFields=true};
        T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(root,path)),options);
        string settingsPath=Path.Combine(root,"Assets/_Original/Data/Campaign/UserGateways.json");byte[] bytes=File.ReadAllBytes(settingsPath);
        var settings=ScenarioGatewayData.Decode(bytes);
        var campaign=new NativeCampaignArt{Definition=Read<CampaignDefinition>("Assets/_Original/Data/Campaign/CampaignCatalog.json"),
            GatewayMaps=Read<GatewayMapCatalog>("Assets/_Original/Data/Campaign/GatewayMaps.json"),gatewaySettings=new TestTextAsset{bytes=bytes},gatewayWindow=new object(),GatewayWindow=new object()};
        var worldArt=new NativeWorldArt{Factory=()=>Read<WorldDefinition>("Assets/_Original/Data/World/KaosiaWorld.json")};
        var store=new Memory();var service=new EntryAccountService(store);var client=new EntryClient(service);client.OpenLocalProfile();client.Create(100,"시작입구검사");
        store.data.accounts[0].character.tutorialCompleted=true;client.OpenLocalProfile();
        var reply=new LocalServerManager(service,campaign).Request_BeginWorld(worldArt);Check(reply.Accepted,"real account service begins world even with incomplete presentation assets");
        var actual=worldArt.Last.Map(1000).portals;int applied=0;
#if BASELINE
        Check(actual.All(p=>string.IsNullOrEmpty(p.gateway)),"old IsConfigured guard silently drops user gateways");
#else
        foreach(var g in settings.gates.Where(g=>settings.applyToGame&&g.enabled&&g.IsWalk))
        {
            var portal=worldArt.Last.Map(g.fromMap).portals.Single(p=>p.gateway==g.id);
            Check(portal.targetMap==g.targetMap&&portal.cells.Select(c=>(c.x,c.y)).SequenceEqual(g.Cells().Select(c=>(c.x,c.y))),"actual saved gateway applies through production BeginWorld request: "+g.name);
            Check(portal.spawnX==g.spawnX&&portal.spawnY==g.spawnY,"saved arrival coordinates apply: "+g.name);applied++;
        }
        var original=worldArt.Factory();foreach(var native in original.Map(1000).portals)
        {
            var replacement=settings.gates.FirstOrDefault(g=>settings.applyToGame&&g.enabled&&g.fromMap==1000&&g.replacesPortal==native.id);
            var portal=actual.Single(p=>p.id==native.id);
            Check(replacement==null?portal.cells.Select(c=>(c.x,c.y)).SequenceEqual(native.cells.Select(c=>(c.x,c.y))):portal.gateway==replacement.id,"saved replacements override defaults while unedited doors remain");
        }
        var weapon=settings.gates.FirstOrDefault(g=>settings.applyToGame&&g.enabled&&g.fromMap==1000&&g.targetMap==2000&&g.IsWalk);
        if(weapon!=null)
        {
            Check(service.MoveWorld(weapon.x,weapon.y).Accepted,"walk to the actual user-edited weapon doorway");service.AdvanceWorld(10000);
            Check(service.WorldSnapshot().pendingPortal==weapon.replacesPortal,"new doorway cells trigger the replaced native ID");
            Check(service.EnterWorldPortal(weapon.replacesPortal,false).Accepted&&service.WorldSnapshot().record.map==2000,"actual user doorway enters the shop");
            var exit=settings.gates.FirstOrDefault(g=>g.enabled&&g.fromMap==2000&&g.targetMap==1000&&g.IsWalk);
            if(exit!=null)
            {
                Check(service.MoveWorld(exit.x,exit.y).Accepted,"walk to authored interior exit");service.AdvanceWorld(10000);
                Check(service.EnterWorldPortal(exit.replacesPortal,false).Accepted&&service.WorldSnapshot().record.map==1000,"authored interior exit returns to town");
                Check(service.WorldSnapshot().record.x==exit.spawnX&&service.WorldSnapshot().record.y==exit.spawnY,"return coordinate is exactly the saved user exit setting");
            }
        }
        var missing=new NativeCampaignArt{Definition=campaign.Definition,GatewayMaps=campaign.GatewayMaps};bool rejected=false;
        try{missing.Merge(worldArt.Factory());}catch(InvalidDataException){rejected=true;}Check(rejected,"missing user settings are explicit failures, not silent old-door fallback");
        CheckFieldTravel(settings,campaign,worldArt);
        CheckAuthoredReturnDrafts(settings,campaign,worldArt);
#endif
        Check(bytes.SequenceEqual(File.ReadAllBytes(settingsPath)),"real authored settings file preserved");
        Console.WriteLine(JsonSerializer.Serialize(new{checks,applied,kaosiaPortals=actual.Length,fieldTravel,returnDraftTravel,unityPlay=false,scope="Production BeginWorld request expression and NativeCampaignArt.Merge with real domain/account/config data. Current variant also walks every authored1000-to1010 entrance cell, retries failed entry saves, checks resource preservation and reconnects with real disk accounts. Reverse trips explicitly enable new drafts in isolated settings only. Asset/presentation boundaries isolated; no actual Unity import/Play or combat policy."}));
    }
}
