using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal sealed class NativeTickCase
{
    public string name,operation;public int member=1,apInput,menuInput;public TrainingUnit[] units;
}
internal static class BattleTickDifferential
{
    public static void Main(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};var cases=JsonSerializer.Deserialize<NativeTickCase[]>(File.ReadAllText(args[0]),options);
        var results=cases.Select(c=>
        {
            TrainingUnit[] units;
            if(c.operation=="pulse"){if(!OriginalBattleTicks.TryPulse(c.units,out units))throw new Exception(c.name);}
            else{units=c.units.Select(u=>u.Copy()).ToArray();int index=Array.FindIndex(units,u=>u.id==c.member);if(!OriginalBattleTicks.TryMenu(units[index],c.apInput,c.menuInput,out var next))throw new Exception(c.name);units[index]=next;}
            return new{c.name,units=units.Select(u=>new{u.id,u.ap,state=u.nativeState??0,commandFlags=u.nativeMenuMask.HasValue?OriginalBattleTicks.CommandFlags(u.nativeMenuMask.Value):0})};
        }).ToArray();File.WriteAllText(args[1],JsonSerializer.Serialize(results,options));
    }
}
