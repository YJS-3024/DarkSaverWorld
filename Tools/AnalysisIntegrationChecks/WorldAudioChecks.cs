using DarkSaver.Core;
using System.Text.Json;
internal static class WorldAudioChecks
{
    public static void Run(Action<bool,string> check)
    {
        var data=JsonSerializer.Deserialize<WorldAudioData>(File.ReadAllText("Assets/_Game/Resources/World/Audio/Manifest.json"),new JsonSerializerOptions {IncludeFields=true});
        check(data.version==1 && data.regions.Length==44,"44 world audio regions loaded");
        foreach(var region in TownCatalog.All.Concat(FieldCatalog.All).Concat(NativeInteriorCatalog.All).Concat(KaosiaFacilityCatalog.All.Select(r=>r.Region)).Concat(ShopInteriorCatalog.ShopsWithFacilities.Select(r=>r.Region)))
            check(data.FindRegion(region.NativeMap)!=null,"all world and interior aliases resolve music");
        check(data.regions.Count(r=>r.authored)==4,"four fallback bindings explicitly authored");
        check(data.FindRegion("1000").clip.EndsWith("/game3") && data.FindRegion("1010").clip.EndsWith("/game2"),"native Kaosia and outside music");
        check(data.FindRegion("2600").clip.EndsWith("/game4"),"native temple music");
        check(data.FindRegion("unknown")==null && data.FindSound(999)==null,"unknown music/sound has no invented substitute");
        foreach(var expected in new[]{("1002","game9"),("1003","game10"),("1004","game3"),("1012","game12")})
            check(data.FindRegion(expected.Item1).clip.EndsWith("/"+expected.Item2),"explicit later MIDI mapping "+expected.Item1);
        check(data.jobs.Length==542,"later unit sound appearance catalog");
        check(data.sounds.Length==20 && data.FindSound(1)!=null,"all original sound files include death1");
        foreach(var job in data.jobs)
            check(data.HasDeathSound(job.job)==(job.appearance<700 || job.appearance>799),"death audio respects native appearance range");
        foreach(var mode in new[]{-1,0,1,2})
        {
            var definition=UnityEngine.JsonUtility.FromJson<DarkSaver.Events.Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/"+(mode==2?"02":"01")+"/Content.json"));
            definition.intro[0]=new DarkSaver.Events.Scenario01Command {name="PLAYMIDI",args=mode<0?new[]{"game3.mid"}:new[]{"game3.mid",mode.ToString()}};
            var state=DarkSaver.Events.Scenario01Progress.Create(definition);
            DarkSaver.Events.Scenario01Rules.Step(definition,state,new NativeInventory(),100,out var next,out _);
            check(next.musicSource=="game3.mid" && next.musicMode==(mode<0?1:mode),"script MIDI default1 and explicit modes preserved");
        }
        check(!data.HasDeathSound(-1234),"unknown job has no inferred death effect");
    }
}
