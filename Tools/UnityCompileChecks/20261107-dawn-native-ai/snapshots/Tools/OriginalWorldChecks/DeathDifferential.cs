using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class DeathDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public string name,operation;public int unit=0,job=100,ticks=48;public bool numeric;public TrainingUnit[] units;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);var output=new List<object>();
        foreach(var c in cases)
        {
            var actor=c.units.Single(u=>u.id==c.unit);var before=actor.Copy();var trace=new List<object>();var kind=c.operation=="death"?BattleLifecycleKind.Death:BattleLifecycleKind.Remove;
            if(!OriginalBattleLifecycle.TryApply(actor,kind,out actor))throw new Exception("case metadata");
            BattleDeathEffect death=kind==BattleLifecycleKind.Death||((before.nativeState??0)&128)!=0?new BattleDeathEffect{unit=c.unit,life=actor.life,job=c.job,held=c.numeric}:null;
            var sound=new DeathSoundPlayback();double numeric=0;
            for(int i=0;i<=c.ticks;i++)
            {
                trace.Add(new{active=actor.nativeActive==true,enabled=actor.nativeEnabled==true,dying=((actor.nativeState??0)&128)!=0,held=c.numeric&&numeric<1080,counter=death?.Counter??0,occupied=OriginalCommandRange.OccupiesTile(actor),sounds=sound.Update(death==null?Array.Empty<BattleDeathEffect>():new[]{death}).Select(x=>x.stop?"stop":"play").ToArray()});
                if(i<c.ticks&&actor.nativeActive==true&&actor.nativeEnabled==true)
                {
                    death?.Advance(40,c.numeric?Math.Max(0,1080-numeric):0);numeric+=40;
                    if(death?.Expired==true){actor.nativeState=0;actor.nativeEnabled=false;}
                }
            }
            output.Add(new{name=c.name,trace});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output));
    }
}
