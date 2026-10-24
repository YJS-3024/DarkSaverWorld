using System;
using System.Collections.Generic;
namespace DarkSaver.Combat
{
    public sealed partial class LocalCombatAuthority
    {
        private readonly Dictionary<int,SinglePlayerAbilityRule> abilityRules=new();
        public bool ConfigureAbility(SinglePlayerAbilityRule rule)
        {
            if(rule==null || !Enum.IsDefined(typeof(AbilityEffect),rule.Effect) || !rule.Combat || rule.Id<=0 || rule.Id>short.MaxValue || rule.Mp<0 || rule.Range<0 || rule.Range>255 || rule.Power<1 || rule.Ap<1 || rule.Ap>10 || float.IsNaN(rule.Seconds) || float.IsInfinity(rule.Seconds) || rule.Seconds<=0)return false;
            abilityRules[rule.Id]=new SinglePlayerAbilityRule {Id=rule.Id,Mp=rule.Mp,Range=rule.Range,Power=rule.Power,Ap=rule.Ap,Seconds=rule.Seconds,Effect=rule.Effect,Physical=rule.Physical};magicRules.Remove(rule.Id);buffRules.Remove(rule.Id);quickRules.Remove(rule.Id);statusRules.Remove(rule.Id);return true;
        }
        public bool IsRevivalMagic(int id)=>TemporaryRulesEnabled && abilityRules.TryGetValue(id,out var rule) && rule.Effect==AbilityEffect.Revive;
        private bool ValidateAbility(CombatUnit actor,CombatUnit target,SinglePlayerAbilityRule rule,out string reason)
        {
            reason="";
            if(rule.Support?actor.Enemy!=target.Enemy:actor.Enemy==target.Enemy)reason="이 기술의 대상 진영이 아닙니다.";
            else if(rule.Effect==AbilityEffect.Revive && (target.Alive || target.Enemy!=actor.Enemy))reason="전투 불능 아군을 지정하세요.";
            else if(rule.Effect==AbilityEffect.RestoreMp && target.Mp>=target.MaxMp)reason="MP가 부족한 아군을 지정하세요.";
            else if(rule.Effect==AbilityEffect.CurePoison && !TryGetTemporaryStatus(target,TemporaryStatusEffect.Poison,out _,out _))reason="해제할 독 상태가 없습니다.";
            else if(rule.Effect==AbilityEffect.Wake && !TryGetTemporaryStatus(target,TemporaryStatusEffect.Sleep,out _,out _))reason="해제할 수면 상태가 없습니다.";
            else if(rule.Effect==AbilityEffect.MpDrain && target.Mp==0)reason="흡수할 MP가 없습니다.";
            else if(rule.Effect==AbilityEffect.TurnUndead && !SinglePlayerAbilityRules.IsUndead(target))reason="언데드 대상을 지정하세요.";
            else if(rule.Effect==AbilityEffect.Sleep && TryGetTemporaryStatus(target,TemporaryStatusEffect.SleepWard,out _,out _))reason="수면에 면역인 대상입니다.";
            return reason.Length==0;
        }
        public int EffectiveMagicAttack(CombatUnit unit)=>AbilityStat(unit,unit.HasMagicAttack?unit.MagicAttack:EffectiveAttack(unit),TemporaryStatusEffect.MagicAttackUp,TemporaryStatusEffect.None);
        public int EffectiveMagicDefense(CombatUnit unit)=>AbilityStat(unit,unit.HasMagicDefense?unit.MagicDefense:EffectiveDefense(unit),TemporaryStatusEffect.MagicDefenseUp,TemporaryStatusEffect.MagicDefenseDown);
        private int AbilityStat(CombatUnit unit,int value,TemporaryStatusEffect up,TemporaryStatusEffect down)
        {
            long result=value;
            if(TryGetTemporaryStatus(unit,up,out var bonus,out _))result+=bonus;
            if(down!=TemporaryStatusEffect.None && TryGetTemporaryStatus(unit,down,out var penalty,out _))result-=penalty;
            return (int)Math.Max(0,Math.Min(int.MaxValue,result));
        }
        private CombatResult ResolveAbility(CombatUnit actor,CombatUnit target,SinglePlayerAbilityRule rule,bool consumeMana=true)
        {
            int mana=actor.Mp-(consumeMana?rule.Mp:0);var effect=rule.Effect;int power=rule.Power;
            if(effect==AbilityEffect.Damage || effect==AbilityEffect.HpDrain || effect==AbilityEffect.TurnUndead)
            {
                int atk=rule.Physical?EffectiveAttack(actor):EffectiveMagicAttack(actor);
                int def=rule.Physical?EffectiveDefense(target):EffectiveMagicDefense(target);
                int damage=(int)Math.Min(target.Hp,Math.Max(1,Math.Min(int.MaxValue,(long)atk+power-def/2)));
                int healed=effect==AbilityEffect.HpDrain?(int)Math.Min(actor.MaxHp,(long)actor.Hp+damage):-1;
                return new CombatResult(actor.Id,target.Id,target.Hp-damage,mana,damage,false,actorHp:healed);
            }
            if(effect==AbilityEffect.MpDrain)
            {
                int drain=Math.Min(target.Mp,power);
                return new CombatResult(actor.Id,target.Id,target.Hp,(int)Math.Min(actor.MaxMp,(long)mana+drain),0,false,targetMp:target.Mp-drain);
            }
            if(effect==AbilityEffect.RestoreMp)
            {
                int restored=(int)Math.Min(target.MaxMp,(long)(actor.Id==target.Id?mana:target.Mp)+power);
                return new CombatResult(actor.Id,target.Id,target.Hp,actor.Id==target.Id?restored:mana,0,false,targetMp:restored);
            }
            if(effect==AbilityEffect.Revive)
            {
                int hp=(int)Math.Max(1,(long)target.MaxHp*Math.Min(100,power)/100);
                return new CombatResult(actor.Id,target.Id,hp,mana,0,false,healing:hp,revive:true);
            }
            if(effect==AbilityEffect.Heal)
            {
                int hp=(int)Math.Min(target.MaxHp,(long)target.Hp+power);
                return new CombatResult(actor.Id,target.Id,hp,mana,0,false,healing:hp-target.Hp);
            }
            if(effect==AbilityEffect.AttackBuff || effect==AbilityEffect.DefenseBuff)
                return new CombatResult(actor.Id,target.Id,target.Hp,mana,0,false,attackBonus:effect==AbilityEffect.AttackBuff?power:0,defenseBonus:effect==AbilityEffect.DefenseBuff?power:0,buffSeconds:rule.Seconds);
            if(effect==AbilityEffect.Quick)return new CombatResult(actor.Id,target.Id,target.Hp,mana,0,false,buffSeconds:rule.Seconds,actionCostReduction:Math.Min(90,power));
            var status=effect switch {
                AbilityEffect.Poison=>TemporaryStatusEffect.Poison,AbilityEffect.CurePoison=>TemporaryStatusEffect.CurePoison,AbilityEffect.MovementReduction=>TemporaryStatusEffect.MovementReduction,
                AbilityEffect.Silence=>TemporaryStatusEffect.Silence,AbilityEffect.Sleep=>TemporaryStatusEffect.Sleep,AbilityEffect.Wake=>TemporaryStatusEffect.Wake,
                AbilityEffect.AttackDown=>TemporaryStatusEffect.AttackDown,AbilityEffect.MagicDefenseDown=>TemporaryStatusEffect.MagicDefenseDown,AbilityEffect.MagicDefenseUp=>TemporaryStatusEffect.MagicDefenseUp,
                AbilityEffect.MagicAttackUp=>TemporaryStatusEffect.MagicAttackUp,AbilityEffect.Slow=>TemporaryStatusEffect.Slow,AbilityEffect.Regeneration=>TemporaryStatusEffect.Regeneration,
                AbilityEffect.PoisonWeapon=>TemporaryStatusEffect.PoisonWeapon,AbilityEffect.Accuracy=>TemporaryStatusEffect.Accuracy,AbilityEffect.Berserk=>TemporaryStatusEffect.Berserk,
                AbilityEffect.SleepWard=>TemporaryStatusEffect.SleepWard,_=>throw new InvalidOperationException("Unclassified ability effect.")};
            if(status==TemporaryStatusEffect.MovementReduction || status==TemporaryStatusEffect.Slow)power=Math.Min(90,power);
            return new CombatResult(actor.Id,target.Id,target.Hp,mana,0,false,statusEffect:status,statusPower:power,statusSeconds:rule.Seconds);
        }
    }
}
