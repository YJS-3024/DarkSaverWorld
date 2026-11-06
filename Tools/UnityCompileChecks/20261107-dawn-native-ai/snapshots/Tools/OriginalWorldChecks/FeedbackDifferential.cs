using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class FeedbackDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public string name,operation;public int unit=0,target=400,value,ticks=50;public TrainingUnit[] units;public int targetFlag;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);var output=new List<object>();
        foreach(var c in cases)
        {
            var actor=c.units.Single(u=>u.id==c.unit);var trace=new List<object>();
            if(c.operation=="attack")
            {
                var target=c.units.Single(u=>u.id==c.target);var pair=BattleAttackEffect.Create(actor,target,c.value,c.targetFlag);
                for(int i=0;i<=c.ticks;i++)
                {
                    foreach(var e in pair)e.age=i*40;
                    trace.Add(pair.Select(e=>new{unit=e.unit,counter=e.Expired?0:e.Counter,expired=e.Expired,offsetX=e.offsetX,offsetY=e.offsetY,nativeDirection=e.direction==0?3:e.direction==1?4:e.direction==2?2:1,flag=e.flag,partner=e.attacker?e.partner:-1}).ToArray());
                }
            }
            else
            {
                bool hp=c.operation=="hp",mp=c.operation=="mp";var e=new BattleNumberEffect{unit=c.unit,life=actor.life,kind=hp?BattleFeedbackKind.Health:mp?BattleFeedbackKind.Mana:BattleFeedbackKind.Miss,hp=hp?c.value:actor.hp,maxHp=actor.maxHp,delta=hp?c.value-actor.hp:mp?c.value-actor.mp:0};
                for(int i=0;i<=c.ticks;i++)
                {e.age=i*40;trace.Add(new{hp=hp?c.value:actor.hp,mp=mp?c.value:actor.mp,delta=e.Expired?0:e.delta,counter=e.Expired?0:e.Counter,kind=e.Expired?-1:hp?0:mp?1:2,expired=e.Expired});}
            }
            output.Add(new{name=c.name,trace});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output));
    }
}
