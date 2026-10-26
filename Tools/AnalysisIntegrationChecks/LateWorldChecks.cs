using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Network;
using UnityEngine;

internal static class LateWorldChecks
{
    public static void Run(Action<bool,string> check)
    {
        var content=JsonUtility.FromJson<WorldLayoutResumeData>(File.ReadAllText("Assets/_Game/Resources/World/LateLayoutResume.json"));
        check(content.version==1 && content.maps.Length==3,"three migrated layouts have recovery data");
        foreach(var map in content.maps)
        {
            check(map.IsValid,"sorted unique native connected recovery cells "+map.mapScene);
            check(!map.TryResolve(float.NaN,0,out _,out _) && !map.TryResolve(0,float.PositiveInfinity,out _,out _)
                && !map.TryResolve(-1,0,out _,out _) && !map.TryResolve(map.width,0,out _,out _),"invalid saved coordinates fail closed");
            foreach(var cell in map.safeCells)
            {
                float x=cell%map.width+.5f,y=map.height-cell/map.width-.5f;
                check(map.TryResolve(x,y,out var nx,out var ny) && nx==x && ny==y,"safe saved position preserved exactly");
            }
        }
        var kaosia=content.maps.Single(m=>m.mapScene=="Kaosia_Map");
        check(kaosia.TryResolve(21,21,out var kx,out var ky) && kx==21 && ky==21,"user Kaosia21,21 remains exact");
        var field=content.maps.Single(m=>m.mapScene=="World1010_Map");
        check(field.TryResolve(23,34,out var fx,out var fy) && (fx!=23 || fy!=34),"blocked old field spawn is recovered");
        check(field.TryResolve(fx,fy,out var fx2,out var fy2) && fx==fx2 && fy==fy2,"recovered position is stable");
        var cleared=new HashSet<string>();
        foreach(var key in new[]{"33","330","331","332","333","404","405","406"})
            check(!ScenarioCatalog.CanEnter(key,cleared.Contains),"late branch cannot skip prerequisite "+key);
        cleared.Add("17");
        check(new[]{"33","330","404"}.All(k=>ScenarioCatalog.CanEnter(k,cleared.Contains)),"three local side branches open after17");
        foreach(var pair in new[]{("330","331"),("331","332"),("332","333"),("404","405"),("405","406")})
        {
            cleared.Add(pair.Item1);
            check(ScenarioCatalog.CanEnter(pair.Item2,cleared.Contains) && !ScenarioCatalog.CanEnter(pair.Item1,cleared.Contains),"late branch order and replay guard "+pair.Item2);
        }
        check(!ScenarioCatalog.CanEnter("18",cleared.Contains) && !ScenarioCatalog.CanEnter("033",cleared.Contains),"unimplemented and noncanonical keys stay blocked");
        foreach(var key in new[]{"33","330","331","332","333","404","405","406"})
        {
            var definition=JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/"+key+"/Content.json"));
            var state=Scenario01Progress.Create(definition);state.phase=Scenario01Progress.Playing;
            var items=new NativeInventory();int revision=0;bool fail=false;
            bool Save(Scenario01Progress next,NativeInventory nextItems,out string reason)
            {reason=fail?"injected isolated failure":null;if(fail)return false;state=next.Clone();items=nextItems.Clone();revision++;return true;}
            var server=new LocalScenarioServer(definition,()=>state,()=>items,()=>revision,()=>100,()=>false,Save);
            ScenarioStateNotification Send(ScenarioOperation op,int x=0,int y=0)=>
                (ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,revision,op,x,y));
            check(!Send(ScenarioOperation.BeginEncounter,9999).Accepted && state.pendingBattle==-1,"unknown unit request rejected "+key);
            var unit=definition.units.First();fail=true;
            check(!Send(ScenarioOperation.BeginEncounter,unit.id).Accepted && state.pendingBattle==-1 && revision==0,"encounter save failure stays atomic "+key);
            fail=false;check(Send(ScenarioOperation.BeginEncounter,unit.id).Accepted && state.pendingBattle==unit.id,"known unit accepted "+key);
            check(!Send(ScenarioOperation.BeginEncounter,unit.id).Accepted,"duplicate pending battle rejected "+key);
            check(Send(ScenarioOperation.CancelEncounter).Accepted && state.pendingBattle==-1 && state.defeated.Count==0,"cancel never grants victory "+key);
            var stale=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,0,ScenarioOperation.Probe,0,0));
            check(!stale.Accepted,"stale scenario revision rejected "+key);
        }
    }
}
