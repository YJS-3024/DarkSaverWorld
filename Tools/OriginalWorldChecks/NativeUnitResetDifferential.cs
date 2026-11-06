using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class NativeUnitResetDifferential
{
    public sealed class Case {public string name,operation;public int unit,source=0,target=400,value,newHp,newJob,beforeTicks,ticks;public int? directionBeforeReset;public TrainingUnit[] units;}
    public static void Main(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),options);var output=new List<object>();
        var reset=typeof(EntryAccountService).GetMethod("ResetNativeUnitPresentation",BindingFlags.NonPublic|BindingFlags.Static);
        foreach(var c in cases)
        {
            var combat=new TrainingCombatSession{Units=c.units.Select(u=>u.Copy()).ToArray()};var actor=combat.Units.Single(u=>u.id==c.unit);
            if(c.operation=="attack")
            {
                foreach(var e in BattleAttackEffect.Create(combat.Units.Single(u=>u.id==c.source),combat.Units.Single(u=>u.id==c.target),1))
                {combat.AttackEffects[e.unit]=e;combat.Units.Single(u=>u.id==e.unit).direction=e.direction;}
                combat.AttackFlags[c.source]=(combat.Units.Single(u=>u.id==c.source).life,1);
            }
            else if(c.operation=="hp"||c.operation=="mp"||c.operation=="miss"||c.operation=="hp-death")
            {
                bool hp=c.operation=="hp"||c.operation=="hp-death",mp=c.operation=="mp";int before=hp?actor.hp:actor.mp;
                if(hp)actor.hp=c.value;else if(mp)actor.mp=c.value;
                combat.NumberEffects[actor.id]=new BattleNumberEffect{unit=actor.id,life=actor.life,kind=hp?BattleFeedbackKind.Health:mp?BattleFeedbackKind.Mana:BattleFeedbackKind.Miss,hp=actor.hp,maxHp=actor.maxHp,delta=hp||mp?c.value-before:0};
            }
            if(c.operation=="death"||c.operation=="hp-death")
            {actor.nativeState|=0x80u;combat.Deaths[actor.id]=new BattleDeathEffect{unit=actor.id,life=actor.life,job=actor.job,held=c.operation=="hp-death"};}
            for(int step=0;step<c.beforeTicks;step++)combat.Advance(40);
            if(c.directionBeforeReset.HasValue)actor.direction=c.directionBeforeReset.Value;
            var units=combat.Units.Select(u=>u.Copy()).ToArray();var after=units.Single(u=>u.id==actor.id);after.hp=c.newHp;after.maxHp=200;after.mp=3;after.maxMp=65;after.job=c.newJob;after.nativeAppearanceJob=c.newJob;
            if(actor.nativeEnabled!=true){after.nativeState=0x30;after.troopOrder=1;}after.nativeActive=after.nativeEnabled=true;
            combat.AiHints[actor.id]=(actor.life,99);
            reset.Invoke(null,new object[]{combat,units,new[]{actor.id}});
            if(combat.AiHints.ContainsKey(actor.id))throw new Exception("B0A0 must clear the1FC AI hint on every native refresh");combat.Units=units;
            var trace=new List<object>();
            for(int step=0;step<=c.ticks;step++)
            {
                combat.NumberEffects.TryGetValue(actor.id,out var number);combat.AttackEffects.TryGetValue(actor.id,out var attack);combat.Deaths.TryGetValue(actor.id,out var death);
                var frames=new List<BattleFeedbackFrame>();double x=after.x*32,y=after.y*32;
                if(death!=null&&!death.Expired)frames.AddRange(death.Frames(x,y));
                else if(attack!=null&&!attack.Expired)frames.AddRange(BattleFeedbackFrames.Attack(attack,x,y));
                if(number!=null&&!number.Expired)frames.AddRange(BattleFeedbackFrames.Numbers(number,x,y));
                trace.Add(new{numeric=number==null||number.Expired?null:new{counter=number.Counter,number.delta,kind=(int)number.kind,number.hp,number.maxHp},
                    attack=attack==null||attack.Expired?null:new{counter=attack.Counter,attack.partner,attack.offsetX,attack.offsetY,attack.flag,attack.direction},
                    death=death==null||death.Expired?null:new{counter=death.Counter,death.held},frames=frames.Select(f=>new{surface=(int)f.surface,x=(int)f.x,y=(int)f.y,f.atlasX,f.atlasY,f.width,f.height}).ToArray()});
                if(step<c.ticks)combat.Advance(40);
            }
            output.Add(trace);
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output));
    }
}
