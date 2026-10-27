using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using DarkSaver;
using DarkSaver.Combat;

internal static class BalanceChecks
{
    public static void Run(Dictionary<int,UnresolvedAbilityRule> rules,string fixture,string output)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixture));var rows=new List<object>();var issues=new List<object>();int cases=0,executed=0,blocked=0,quickNoGain=0;
        foreach(var job in document.RootElement.EnumerateArray())
        {
            int id=job.GetProperty("id").GetInt32(),mp=job.GetProperty("mp").GetInt32();
            string name=job.GetProperty("name").GetString();
            foreach(int ability in job.GetProperty("abilities").EnumerateArray().Select(x=>x.GetInt32()).Distinct())
            {
                if(!rules.TryGetValue(ability,out var rule) || !rule.Combat)continue;cases++;
                var actor=new CombatUnit {Id=100,JobId=id,Name=name,Hp=job.GetProperty("hp").GetInt32(),MaxHp=job.GetProperty("hp").GetInt32(),Mp=mp,MaxMp=mp,
                    Attack=job.GetProperty("attack").GetInt32(),Defense=job.GetProperty("defense").GetInt32(),HasMagicStats=true,MagicAttack=job.GetProperty("magicAttack").GetInt32(),MagicDefense=job.GetProperty("magicDefense").GetInt32()};
                var target=new CombatUnit {Id=rule.Support?(short)101:(short)800,Name="스켈톤",Enemy=!rule.Support,Hp=500,MaxHp=1000,Mp=50,MaxMp=100,Attack=60,Defense=50,HasMagicStats=true,MagicAttack=60,MagicDefense=40};
                if(rule.Effect==AbilityEffect.Revive){target.Hp=0;target.LifeState=UnitLifeState.Removed;}
                var world=new CombatWorld();world.Add(actor);world.Add(target);var authority=new CombatServerBoundary(sample:()=>0);authority.TemporaryRulesEnabled=true;authority.ConfigureAbility(rule);
                if(rule.Effect==AbilityEffect.CurePoison)authority.ApplyTemporaryBuff(new CombatResult(100,target.Id,target.Hp,actor.Mp,0,false,statusEffect:TemporaryStatusEffect.Poison,statusPower:1,statusSeconds:20));
                if(rule.Effect==AbilityEffect.Wake)authority.ApplyTemporaryBuff(new CombatResult(100,target.Id,target.Hp,actor.Mp,0,false,statusEffect:TemporaryStatusEffect.Sleep,statusPower:1,statusSeconds:20));
                var flow=new CombatFlow(world,authority,true);flow.ConfigureMagicApCost(ability,rule.Ap);actor.ActionPoints.ApplyAbsolute(10);
                bool ok=flow.TryExecute(new CombatRequest(100,target.Id,CombatActionType.Magic,ability),out var result,out var reason);
                if(ok)executed++;else blocked++;
                if(!ok && rule.Mp<=mp)throw new Exception("Unexpected native-class rejection "+id+"/"+ability+": "+reason);
                if(!ok)issues.Add(new {kind="NativeMpExceedsClassBase",job=id,ability,mp,cost=rule.Mp});
                int baseAttackAp=GameConstants.GetTemporaryAttackApCost(id,false),afterQuick=baseAttackAp;
                if(rule.Effect==AbilityEffect.Quick && ok)
                {
                    afterQuick=authority.EffectiveActionCost(target,baseAttackAp);
                    if(afterQuick>=baseAttackAp && baseAttackAp>1){quickNoGain++;issues.Add(new {kind="QuickHasNoAttackApBenefit",job=id,ability,baseAttackAp,afterQuick,percent=rule.Power});}
                }
                int attackDamage=UnresolvedCombatDamage.Calculate(actor.Attack,target.Defense,false);
                double baseline=baseAttackAp>0?(double)attackDamage/baseAttackAp:0;
                double efficiency=result.Damage>0 && ok?((double)result.Damage/rule.Ap)/(baseline>0?baseline:1):0;
                if(efficiency>=5)issues.Add(new {kind="HighDamagePerAp",job=id,ability,ratio=Math.Round(efficiency,2),nativeMp=rule.Mp});
                rows.Add(new {job=id,name,ability,effect=rule.Effect.ToString(),accepted=ok,reason,baseMp=mp,mpCost=rule.Mp,castsFromFullMp=rule.Mp>0?mp/rule.Mp:-1,ap=rule.Ap,damage=result.Damage,healing=result.Healing,damagePerApVsBasic=Math.Round(efficiency,3),baseAttackAp,afterQuick});
            }
        }
        if(quickNoGain!=0)throw new Exception("Positive Quick/Rapid has no discrete AP benefit.");
        File.WriteAllText(output,JsonSerializer.Serialize(new {classes=document.RootElement.GetArrayLength(),cases,executed,blocked,quickNoGain,issues,rows,scope="Actual combat classes with native class-entry base stats and granted entry abilities; synthetic common opponent; no level growth, equipment, inherited spells, AI encounters or Unity playback",unityPlay=false},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"BALANCE native classes{document.RootElement.GetArrayLength()}, cases{cases}, executed{executed}, MP-blocked{blocked}, ineffective Quick{quickNoGain}");
    }
}
