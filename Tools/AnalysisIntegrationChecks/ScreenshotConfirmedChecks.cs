using DarkSaver.Core;
using DarkSaver.Events;
using UnityEngine;

internal static class ScreenshotConfirmedChecks
{
    public static void Run(Action<bool,string> check)
    {
        var directory=Path.Combine(Path.GetTempPath(),"ds-write-diagnostic-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"preserve");
            var store=new NativeSaveStore(Path.Combine(blocked,"save.json"));
            var sample=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene="Kaosia_Map",runtimeScene="Town_Runtime",clearedScenarios=Array.Empty<string>()};
            check(!store.TrySave(sample,out _) && store.CanRetryLastWrite,"I/O failure marked retryable without replacing data");
            check(store.LastWriteDiagnostic.Contains("IOException") && store.LastWriteDiagnostic.Contains("0x") && !store.LastWriteDiagnostic.Contains(directory),"diagnostic includes phase/type/code without private path");
            check(File.ReadAllText(blocked)=="preserve" && Directory.GetFiles(directory).Length==1,"failed write leaves existing files and no temporary files");
            File.Delete(blocked);Directory.CreateDirectory(blocked);
            check(store.TrySave(sample,out _) && !store.CanRetryLastWrite && store.LastWriteDiagnostic=="","same save retries after I/O recovery and clears diagnostic");
            check(store.TryLoad(out var restored,out _) && restored.mapScene==sample.mapScene,"recovered save remains readable");
            sample.hp=-1;sample.hasVitals=true;
            check(!store.TrySave(sample,out _) && !store.CanRetryLastWrite,"invalid data never offered as a retryable I/O failure");
        }
        finally{Directory.Delete(directory,true);}
        var lateItems=DarkSaver.Editor.ItemCatalogConverter.Parse(File.ReadAllText("분석/Saver_Late/unity_catalogs/items.csv"));
        check(lateItems.Length==1212 && lateItems.Single(r=>r.Id==1245).GetNumericValue(16)==80000000000L,"late prices parse all rows without rounding");
        check(lateItems.Count(r=>r.PriceNotation=="1.00E+11")==9
            && lateItems.Where(r=>r.PriceNotation=="1.00E+11").All(r=>r.GetNumericValue(16)==100000000000L),"all nine decimal-exponent rows preserved");
        foreach(var fixture in new[]{("1.23E+2",123L),("9.223372036854775807E+18",long.MaxValue),("-9.223372036854775808E+18",long.MinValue)})
        {
            var fields=Enumerable.Repeat("0",45).ToArray();fields[16]=fixture.Item1;
            check(DarkSaver.Editor.ItemCatalogConverter.Parse(DarkSaver.Editor.ItemCatalogConverter.Header+"\n"+string.Join(",",fields))[0].GetNumericValue(16)==fixture.Item2,"exact exponent integer "+fixture.Item1);
        }
        foreach(string price in new[]{"1.5E+0","1E-50","9.223372036854775808E+18","0.999999999999999999999999999999999999E+18"})
        {
            var fields=Enumerable.Repeat("0",45).ToArray();fields[16]=price;bool rejected=false;
            try{DarkSaver.Editor.ItemCatalogConverter.Parse(DarkSaver.Editor.ItemCatalogConverter.Header+"\n"+string.Join(",",fields));}
            catch(InvalidDataException){rejected=true;}
            check(rejected,"noninteger/overflow exponent rejects without rounding "+price);
        }
        var definition=JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/01/Content.json"));
        var boss=definition.units.Single(u=>u.id==700);
        check(boss.hp==300 && boss.CombatHp==450,"source Kisra row preserved; screenshot combat maximum applied");
        check(OriginalEnemyOverrides.MaximumHp(702,300)==300 && OriginalEnemyOverrides.MaximumHp(701,600)==600,"observed Kisra override is not a global multiplier");
        foreach(int hp in new[]{50,150,300,400,450})
        {
            var state=Scenario01Progress.Create(definition);state.phase=Scenario01Progress.Playing;state.wave=boss.wave;state.pendingBattle=700;
            var snapshot=new Scenario01EnemyState {id=700,hp=hp,mp=140,x=boss.x,y=boss.y};
            var live=state.Clone();
            check(Scenario01Rules.RecordBattleReturn(definition,state,700,false,snapshot),"remaining HP snapshot accepted "+hp);
            check(state.Matches(definition) && state.enemies.Single(e=>e.id==700).hp==hp,"old/current remaining HP preserved without refill "+hp);
            check(live.pendingBattle==700 && live.enemies.Count==0 && !state.defeated.Contains(700),"detached checkpoint does not finalize live encounter or create victory "+hp);
            var loaded=JsonUtility.FromJson<Scenario01Progress>(JsonUtility.ToJson(state));
            check(loaded.Matches(definition) && loaded.sourceHash==definition.sourceHash && loaded.enemies[0].hp==hp,"unchanged content identity and HP roundtrip "+hp);
        }
        var invalid=Scenario01Progress.Create(definition);invalid.phase=Scenario01Progress.Playing;invalid.wave=boss.wave;invalid.pendingBattle=700;
        check(!Scenario01Rules.RecordBattleReturn(definition,invalid,700,false,new Scenario01EnemyState {id=700,hp=451,mp=140,x=boss.x,y=boss.y})
            && invalid.pendingBattle==700 && invalid.enemies.Count==0,"over-maximum snapshot rejects without mutation");
    }
}
