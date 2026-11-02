using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class FeedbackFrameDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public string name,operation;public int unit=0,target=400,value,counter,job=100;public TrainingUnit[] units;public int targetFlag;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);var output=new List<object>();
        foreach(var c in cases)
        {
            var actor=c.units.Single(u=>u.id==c.unit);BattleFeedbackFrame[] frames;
            if(c.operation=="death")
            {var e=new BattleDeathEffect{unit=c.unit,life=actor.life,job=c.job,age=c.counter*40};frames=e.Frames(actor.x*32,actor.y*32);}
            else if(c.operation=="attack")
            {var target=c.units.Single(u=>u.id==c.target);var pair=BattleAttackEffect.Create(actor,target,c.value,c.targetFlag);foreach(var e in pair)e.age=c.counter*40;frames=pair.SelectMany(e=>{var u=c.units.Single(u=>u.id==e.unit);return BattleFeedbackFrames.Attack(e,u.x*32,u.y*32);}).ToArray();}
            else
            {bool hp=c.operation=="hp",mp=c.operation=="mp";var e=new BattleNumberEffect{unit=c.unit,life=actor.life,kind=hp?BattleFeedbackKind.Health:mp?BattleFeedbackKind.Mana:BattleFeedbackKind.Miss,hp=hp?c.value:actor.hp,maxHp=actor.maxHp,delta=hp?c.value-actor.hp:mp?c.value-actor.mp:0,age=c.counter*40};frames=BattleFeedbackFrames.Numbers(e,actor.x*32,actor.y*32);}
            output.Add(frames.Select(f=>new{surface=(int)f.surface,x=(int)f.x,y=(int)f.y,f.atlasX,f.atlasY,f.width,f.height}).ToArray());
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output));
    }
}
