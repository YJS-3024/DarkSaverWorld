using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Magic;
using DarkSaver.Core;
using DarkSaver.Jobs;

internal static class Program
{
    private static int count;
    private static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
    private static CombatUnit Unit(short id,bool enemy)=>new CombatUnit {Id=id,JobId=100,Name=enemy?"스켈톤":"아군",Enemy=enemy,Hp=enemy?5000:50,MaxHp=enemy?5000:100,Mp=enemy?500:10000,MaxMp=enemy?500:10000,Attack=20,Defense=10,HasMagicStats=true,MagicAttack=20,MagicDefense=10};
    private static (CombatWorld,LocalCombatAuthority,CombatFlow) Setup(SinglePlayerAbilityRule rule)
    {
        var world=new CombatWorld();world.Add(Unit(100,false));world.Add(Unit(101,false));world.Add(Unit(800,true));world.Add(Unit(801,true));
        world.Get(101).Mp=50;world.Get(101).MaxMp=500;
        var authority=new LocalCombatAuthority(sample:()=>0);authority.TemporaryRulesEnabled=true;
        Check(authority.ConfigureAbility(rule),"ability registration "+rule.Id);
        var flow=new CombatFlow(world,authority,true);Check(flow.ConfigureMagicApCost(rule.Id,rule.Ap),"AP registration");world.Get(100).ActionPoints.ApplyAbsolute(world.Get(100).ActionPoints.Maximum);
        return(world,authority,flow);
    }
    public static void Main(string[] args)
    {
        Console.WriteLine("Beginning actual production CombatFlow/LocalCombatAuthority checks (no Unity scene execution).");
        // Exercise the actual enemy factory while bypassing Unity object construction/lifecycle.
        var kisra=new DarkSaver.Events.Scenario01Unit {id=700,job=701,name="키스라",hp=300,mp=150,attack=126,defense=55,x=19,y=10};
        var enemy=(BattleEnemyDefinition)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(BattleEnemyDefinition));
        enemy.InitializeScenarioUnit(kisra);Check(enemy.TryCreateUnit(out var freshKisra),"Kisra production factory");
        Check(freshKisra.Hp==450 && freshKisra.MaxHp==450 && freshKisra.Mp==150 && freshKisra.Attack==126 && freshKisra.Defense==55,"screenshot maximum with unchanged other combat stats");
        foreach(int hp in new[]{150,300,450}) {
            enemy.InitializeScenarioUnit(kisra,new DarkSaver.Events.Scenario01EnemyState {id=700,hp=hp,mp=140,x=19,y=10});
            Check(enemy.TryCreateUnit(out var resumedKisra) && resumedKisra.Hp==hp && resumedKisra.MaxHp==450,"production factory preserves saved remaining HP");
        }
        Check(kisra.hp==300,"production factory does not mutate baked source");
        var coverage=new List<object>();var rules=new Dictionary<int,SinglePlayerAbilityRule>();int combat=0,hiring=0,emotes=0;var unconfirmed=new List<int>();
        using var fixture=JsonDocument.Parse(File.ReadAllText(args[0]));
        foreach(var item in fixture.RootElement.EnumerateArray())
        {
            var values=item.GetProperty("values").EnumerateArray().Select(v=>v.GetInt64()).ToArray();
            var entry=new MagicCatalogEntry(values,item.GetProperty("name").GetString());
            if(!SinglePlayerAbilityRules.TryCreate(entry,out var rule))
            {Check(entry.Id==30901 && entry.Name=="큐리즘","unclassified late catalog ID "+entry.Id);unconfirmed.Add(entry.Id);continue;}
            Check(rule.Mp==entry.Mp && rule.Ap>0 && rule.Ap<=10,"native MP / bounded authored AP");rules.Add(entry.Id,rule);
            coverage.Add(new {id=entry.Id,name=entry.Name,effect=rule.Effect.ToString(),route=rule.Combat?"Combat":rule.Effect.ToString(),nativeRange=entry.Range,nativeArea=entry.Area,nativeTick=entry.RawTick,physical=rule.Physical,mp=rule.Mp,ap=rule.Ap,range=rule.Range,power=rule.Power,seconds=rule.Seconds});
            if(!rule.Combat){if(rule.Effect==AbilityEffect.Hiring)hiring++;else emotes++;continue;}combat++;
            if(entry.Unit!=8)
            {
                bool geometry=false;
                foreach(var clicked in new[]{new CombatGridCell(16,16),new CombatGridCell(16,15),new CombatGridCell(16,12)})
                    for(int x=0;x<32 && !geometry;x++)for(int y=0;y<32 && !geometry;y++)
                        if(CombatMagicArea.TryGetWeight(entry,new CombatGridCell(16,16),clicked,new CombatGridCell(x,y),32,32,out int weight) && weight>0)geometry=true;
                Check(geometry,"native geometry has selectable cells "+entry.Id);
            }
            var (world,authority,flow)=Setup(rule);var target=world.Get(rule.Support?(short)101:(short)800);var actor=world.Get(100);
            if(rule.Effect==AbilityEffect.Revive){target.Hp=0;target.LifeState=UnitLifeState.Removed;}
            if(rule.Effect==AbilityEffect.Wake)authority.ApplyTemporaryBuff(new CombatResult(800,target.Id,target.Hp,500,0,false,statusEffect:TemporaryStatusEffect.Sleep,statusPower:1,statusSeconds:20));
            if(rule.Effect==AbilityEffect.CurePoison)authority.ApplyTemporaryBuff(new CombatResult(800,target.Id,target.Hp,500,0,false,statusEffect:TemporaryStatusEffect.Poison,statusPower:1,statusSeconds:20));
            int beforeHp=target.Hp,beforeMp=actor.Mp,targetMp=target.Mp,actorHp=actor.Hp;
            Check(flow.TryExecute(new CombatRequest(100,target.Id,CombatActionType.Magic,rule.Id),out var result,out var reason),"execute "+entry.Id+" "+reason);
            Check(actor.ActionPoints.Current==actor.ActionPoints.Maximum-rule.Ap,"one AP receipt "+entry.Id);
            Check(actor.Mp>=0 && actor.Mp<=actor.MaxMp && target.Hp>=0 && target.Hp<=target.MaxHp,"bounded state");
            if(rule.Effect!=AbilityEffect.MpDrain)Check(actor.Mp==beforeMp-rule.Mp,"native MP spent once "+entry.Id);
            switch(rule.Effect)
            {
                case AbilityEffect.Damage:case AbilityEffect.TurnUndead:Check(target.Hp<beforeHp,"damage");break;
                case AbilityEffect.HpDrain:Check(target.Hp<beforeHp && actor.Hp>actorHp,"HP drain two-sided state");break;
                case AbilityEffect.MpDrain:Check(target.Mp<targetMp && actor.Mp>=beforeMp-rule.Mp,"MP drain two-sided state");break;
                case AbilityEffect.RestoreMp:Check(target.Mp>targetMp,"MP restoration");break;
                case AbilityEffect.Heal:Check(target.Hp>beforeHp,"healing");break;
                case AbilityEffect.Revive:Check(target.Alive && target.Hp>0,"revival from removed state");break;
                case AbilityEffect.AttackBuff:Check(authority.EffectiveAttack(target)>20,"attack buff");break;
                case AbilityEffect.DefenseBuff:Check(authority.EffectiveDefense(target)>10,"defense buff");break;
                case AbilityEffect.Quick:Check(authority.EffectiveActionCost(target,10)<10,"quick reduction");break;
                case AbilityEffect.Slow:Check(authority.EffectiveActionCost(target,2)>2,"slow action expenditure");break;
                case AbilityEffect.AttackDown:Check(authority.EffectiveAttack(target)<20,"attack down");break;
                case AbilityEffect.MagicAttackUp:Check(authority.EffectiveMagicAttack(target)>20,"magic attack up");break;
                case AbilityEffect.MagicDefenseUp:Check(authority.EffectiveMagicDefense(target)>10,"magic defense up");break;
                case AbilityEffect.MagicDefenseDown:Check(authority.EffectiveMagicDefense(target)<10,"magic defense down");break;
                case AbilityEffect.Poison:flow.AdvanceActionPoints(2);Check(target.Hp<beforeHp,"poison tick");break;
                case AbilityEffect.Regeneration:flow.AdvanceActionPoints(2);Check(target.Hp>beforeHp,"regeneration tick");break;
                case AbilityEffect.Sleep:Check(!authority.CanAct(target),"sleep blocks actions");break;
                case AbilityEffect.Silence:Check(!authority.CanCast(target),"silence blocks spells");break;
                case AbilityEffect.CurePoison:Check(!authority.TryGetTemporaryStatus(target,TemporaryStatusEffect.Poison,out _,out _),"cure poison");break;
                case AbilityEffect.Wake:Check(authority.CanAct(target),"wake restores action");break;
                case AbilityEffect.Berserk:Check(authority.EffectiveAttack(target)>20 && authority.EffectiveDefense(target)<10,"berserk tradeoff");break;
                case AbilityEffect.MovementReduction:Check(authority.EffectiveMovementBudget(target,100)<100,"movement reduction");break;
                default:Check(result.StatusEffect!=TemporaryStatusEffect.None,"explicit remaining buff status");break;
            }
            // Invalid requests must leave costs/state untouched.
            var denied=Setup(rule);denied.Item1.Get(100).Mp=0;var deniedTarget=denied.Item1.Get(rule.Support?(short)101:(short)800);
            if(rule.Effect==AbilityEffect.Revive){deniedTarget.Hp=0;deniedTarget.LifeState=UnitLifeState.Corpse;}
            if(rule.Mp>0)
            {
                int ap=denied.Item1.Get(100).ActionPoints.Current;
                Check(!denied.Item3.TryExecute(new CombatRequest(100,deniedTarget.Id,CombatActionType.Magic,rule.Id),out _,out _),"insufficient MP rejected");
                Check(denied.Item1.Get(100).Mp==0 && denied.Item1.Get(100).ActionPoints.Current==ap,"failed cast costs unchanged");
            }
        }
        Check(combat==374 && hiring==50 && emotes==15,"439 supported routes retained");
        // Multi-target cost atomicity, duplicated targets and caster drains.
        foreach(int id in new[]{1201,1301})
        {
            var (w,a,f)=Setup(rules[id]);w.Get(100).Mp=100;w.Get(100).MaxMp=10000;
            Check(f.TryExecuteMagicTargets(id,100,new short[]{800,801,800},out _),"multi drain casts");
            Check(w.Get(100).ActionPoints.Current==w.Get(100).ActionPoints.Maximum-rules[id].Ap,"area AP once");
            if(id==1201)Check(w.Get(100).Hp==Math.Min(100,50+(5000-w.Get(800).Hp)+(5000-w.Get(801).Hp)),"sum HP drain over unique targets");
            else Check(w.Get(100).Mp==100-rules[id].Mp+(500-w.Get(800).Mp)+(500-w.Get(801).Mp),"sum MP drain with one MP cost");
            var bad=Setup(rules[id]);int mp=bad.Item1.Get(100).Mp,hp=bad.Item1.Get(800).Hp;
            Check(!bad.Item3.TryExecuteMagicTargets(id,100,new short[]{800,999},out _),"invalid area target rejected");
            Check(bad.Item1.Get(100).Mp==mp && bad.Item1.Get(800).Hp==hp,"area candidate unchanged on failure");
        }
        var itemSpell=Setup(rules[101]);itemSpell.Item1.Get(100).Mp=0;
        Check(itemSpell.Item3.TryApplyItemMagic(100,800,101,3,2),"item-linked damage spell can execute without spell MP");
        Check(itemSpell.Item1.Get(100).Mp==0 && itemSpell.Item1.Get(800).Hp<5000,"item spell does not charge MP");
        var selfMana=Setup(rules[3201]);selfMana.Item1.Get(100).Mp=10;
        Check(selfMana.Item3.TryExecuteMagicTargets(3201,100,new short[]{100,101},out _),"area mana recovery includes caster");
        Check(selfMana.Item1.Get(100).Mp==50 && selfMana.Item1.Get(101).Mp==90,"self mana recovery counted once");
        var ward=Setup(rules[3901]);ward.Item3.TryExecute(new CombatRequest(100,101,CombatActionType.Magic,3901),out _,out _);
        ward.Item2.ConfigureAbility(rules[6901]);ward.Item3.ConfigureMagicApCost(6901,rules[6901].Ap);ward.Item1.Get(800).ActionPoints.ApplyAbsolute(20);
        Check(!ward.Item3.TryExecute(new CombatRequest(800,101,CombatActionType.Magic,6901),out _,out _),"sleep ward rejects sleep");
        var revived=Setup(rules[3701]);var fallen=revived.Item1.Get(101);fallen.Hp=0;fallen.LifeState=UnitLifeState.Removed;
        var grid=new CombatOccupancyGrid(revived.Item1,5,5,(_,__)=>true,(_,__)=>true);
        Check(grid.CanReviveAt(101,new CombatGridCell(0,0)),"vacant revival tile accepts fallen ally");
        Check(revived.Item3.TryExecute(new CombatRequest(100,101,CombatActionType.Magic,3701),out _,out _) && grid.TryPlace(101,new CombatGridCell(0,0)),"revival reclaims grid occupancy");
        revived.Item1.Get(800).Hp=0;revived.Item1.Get(800).LifeState=UnitLifeState.Removed;
        Check(!grid.CanReviveAt(800,new CombatGridCell(0,0)),"occupied revival cell rejected");
        var turn=Setup(rules[1101]);turn.Item1.Get(800).Name="와일드독";
        Check(!turn.Item3.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,1101),out _,out _),"turn undead rejects living-type target");
        var poison=Setup(rules[11101]);poison.Item3.TryExecute(new CombatRequest(100,101,CombatActionType.Magic,11101),out _,out _);
        poison.Item1.Get(101).ActionPoints.ApplyAbsolute(10);poison.Item3.ConfigureAttackApCost(101,4);
        Check(poison.Item3.TryExecute(new CombatRequest(101,800,CombatActionType.Attack),out _,out _),"poison-imbued physical hit");
        Check(poison.Item2.TryGetTemporaryStatus(poison.Item1.Get(800),TemporaryStatusEffect.Poison,out _,out _),"poison weapon applies poison");
        var expired=Setup(rules[1501]);expired.Item3.TryExecute(new CombatRequest(100,800,CombatActionType.Magic,1501),out _,out _);expired.Item3.AdvanceActionPoints(rules[1501].Seconds+1);
        Check(expired.Item2.EffectiveAttack(expired.Item1.Get(800))==20,"debuff expires");
        File.WriteAllText(args[1],JsonSerializer.Serialize(new {total=fixture.RootElement.GetArrayLength(),combat,hiring,emotes,unconfirmed,checks=count,unityPlay=false,scope="Actual production pure combat classes; no Unity scenes, imports or visual playback",entries=coverage},new JsonSerializerOptions {WriteIndented=true}));
        if(args.Length>3)
        {
            Check(unconfirmed.SequenceEqual(new[] {30901}),"unconfirmed later skill stays blocked");
        foreach(int job in new[] {170,171,172}) {
            var profile=new NativeCharacterProfile {name="후기검증",jobId=job,hasLevels=true,baseLevel=500,jobLevel=300,
                hasClassAbilities=true,learnedMagicIds=Array.Empty<int>(),usableSkillIds=Array.Empty<int>()};
            Check(profile.IsValid,"late profile level300 "+job);profile.jobLevel=301;
            Check(!profile.IsValid,"late profile rejects301 "+job);
            profile.jobId=169;profile.jobLevel=101;Check(!profile.IsValid,"legacy class cap remains100");
        }
        BalanceChecks.Run(rules,args[2],args[3]);
            ScenarioRewardChecks.Run(rules,args[2],args[4],Path.Combine(Path.GetDirectoryName(args[1]),"scenario_rewards_check.json"));
        }
        Console.WriteLine($"PASS {count} actual combat checks; {combat} combat abilities, {hiring} hiring, {emotes} emote entries classified.");
    }
}
