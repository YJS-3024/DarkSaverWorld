using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Editor;
using DarkSaver.Events;

// Compiles the production helper unchanged, using the actual compiled runtime unit model.
internal static class EditorTroopChecks
{
    private static int assertions;
    private static void Check(bool condition,string message)
    {assertions++;if(!condition)throw new Exception(message);}
    private static Scenario01Unit Unit(int id,int x=0,int y=0,int wave=0)
        =>new Scenario01Unit{id=id,x=x,y=y,wave=wave};
    public static void Main(string[] args)
    {
        string root=args[0];
        Check(ScenarioPlacementTroops.CommanderId(401)==400,"member commander");
        Check(ScenarioPlacementTroops.CommanderId(409)==400,"ninth slot");
        Check(ScenarioPlacementTroops.CommanderId(410)==410,"next commander");
        var missing=new[]{Unit(401)};
        Check(ScenarioPlacementTroops.Commander(missing,missing[0])==null,"orphan must not become leader");
        Check(ScenarioPlacementTroops.FormationName(missing)=="지휘관 없음","orphan shape");
        var waveMismatch=new[]{Unit(400,wave:1),Unit(401)};
        Check(ScenarioPlacementTroops.Commander(waveMismatch,waveMismatch[1])==null,"cross-wave commander");
        Check(ScenarioPlacementTroops.NextMemberId(waveMismatch,waveMismatch[1])<0,"cannot add to orphan wave");
        Check(ScenarioPlacementTroops.NextMemberId(missing,null)<0,"no selection");
        Check(ScenarioPlacementTroops.NextCommanderId(missing)==410,"skip orphan-occupied group");
        Check(ScenarioPlacementTroops.NextCommanderId(Array.Empty<Scenario01Unit>())==400,"first enemy commander");
        Check(ScenarioPlacementTroops.NextCommanderId(Enumerable.Range(40,40).Select(n=>Unit(n*10)))<0,"enemy slots exhausted");
        var full=Enumerable.Range(400,10).Select(n=>Unit(n)).ToArray();
        Check(ScenarioPlacementTroops.NextMemberId(full,full[3])<0,"no spill into next commander");
        Check(ScenarioPlacementTroops.NextMemberId(new[]{Unit(400),Unit(402)},Unit(400))==401,"reuse free slot");
        Check(ScenarioPlacementTroops.FormationName(new[]{Unit(400),Unit(401),Unit(402)})=="편집 대형 (3인)","authored trio is not source five");
        for(int i=0;i<4;i++)
        {
            var shape=ScenarioPlacementTroops.Formation(i);
            var group=new[]{Unit(700,20,30)}.Concat(shape.Select((o,n)=>Unit(701+n,20+o.x,30+o.y))).ToArray();
            Check(ScenarioPlacementTroops.FormationName(group).StartsWith(ScenarioPlacementTroops.FormationNames[i]),"translated formation "+i);
            Check(ScenarioPlacementTroops.FormationName(group.Reverse()).StartsWith(ScenarioPlacementTroops.FormationNames[i]),"source ordering "+i);
            group[4].x=group[3].x;group[4].y=group[3].y;
            Check(ScenarioPlacementTroops.FormationName(group)=="편집 대형 (5인)","duplicate cells not native shape "+i);
        }
        var rules=ScenarioPlacementTroops.ParseAi("400 ODNORMAL AREA ( 3, 8, 7) ATTACK(100)\r\n480 ODATTACK SOILDER (700) REST(100)\n; 410 ODNORMAL AREA(1,2,3)\n999\n410 ODNORMAL AREA(1,bad,2)\n420 ODNORMAL AREA(1,2,-1)");
        Check(rules.Length==2,"spaced AREA / SOILDER / comment / invalid inputs");
        Check(rules[0].x==3 && rules[0].y==8 && rules[0].radius==7 && !rules[0].followsUnit,"AREA operands");
        Check(rules[1].target==700 && rules[1].followsUnit,"SOILDER reference");
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"분석/Saver_Late/scenario_monster_placements.json")));
        var histogram=new Dictionary<string,int>();int nativeUnits=0,nativeGroups=0;
        foreach(var scenario in document.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            var units=scenario.GetProperty("units").EnumerateArray().Select(u=>Unit(u.GetProperty("unit").GetInt32(),u.GetProperty("x").GetInt32(),u.GetProperty("y").GetInt32())).ToArray();
            foreach(var unit in units)
            {
                var leader=ScenarioPlacementTroops.Commander(units,unit);
                Check(leader!=null && leader.id/10==unit.id/10,"native commander "+unit.id);
                Check(ScenarioPlacementTroops.Members(units,unit).Length==5,"native member count "+unit.id);
            }
            foreach(var group in units.GroupBy(u=>ScenarioPlacementTroops.CommanderId(u.id)))
            {
                var name=ScenarioPlacementTroops.FormationName(group);histogram.TryGetValue(name,out int count);histogram[name]=count+1;nativeGroups++;
            }
            nativeUnits+=units.Length;
            if(scenario.GetProperty("scenario").GetString()=="26")
                Check(ScenarioPlacementTroops.Commander(units,units.First(u=>u.id==491)).id==490,"26: ignore nearer AREA700");
        }
        Check(nativeUnits==610 && nativeGroups==122,"complete native corpus");
        int[] expectedShapes={65,44,12,1};
        for(int i=0;i<4;i++)Check(histogram[ScenarioPlacementTroops.FormationNames[i]+" (형태 일치)"]==expectedShapes[i],"source shape count "+i);
        int areaCount=0,targetCount=0;
        foreach(var path in Directory.GetFiles(Path.Combine(root,"분석/Saver_Late/decoded/map"),"*.ai",SearchOption.AllDirectories))
        {
            var parsed=ScenarioPlacementTroops.ParseAi(File.ReadAllText(path));
            areaCount+=parsed.Count(r=>!r.followsUnit);targetCount+=parsed.Count(r=>r.followsUnit);
        }
        Check(areaCount==1099 && targetCount==22,"complete AI corpus");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,nativeUnits,nativeGroups,areaCount,targetCount,shapes=histogram,
            scope="Actual editor helper and runtime unit type; managed checks, not Unity GUI/Play"}));
    }
}
