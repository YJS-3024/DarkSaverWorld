using System;
using System.Linq;

namespace DarkSaver.Original.World
{
    public sealed class SoldierActionPlan
    {
        public TrainingAction? action;
        public int member,target=-1,hintBefore,hintAfter,nativeMode;
        public uint stateAfter;
        public bool unavailable;
        public BattleRegroupPlan movement;
        public BattleLifeRecord[] candidates=Array.Empty<BattleLifeRecord>();
    }
    public static class OriginalSoldierActions
    {
        // Automatic native execution uses active/+4, not HP, ghost state or
        // the command/action buttons. Keep the typed/manual path separate.
        public static bool NativeContext(TrainingUnit[] units,int member)
        {
            var actor=units?.FirstOrDefault(u=>u.id==member);var leader=units?.FirstOrDefault(u=>u.id==0);
            return member>0&&member<10&&actor?.nativeActive.HasValue==true&&actor.nativeEnabled.HasValue&&actor.nativeState.HasValue
                &&leader?.nativeActive.HasValue==true&&leader.nativeEnabled.HasValue&&leader.troopOrder.HasValue;
        }
        public static bool Active(TrainingUnit unit)=>unit!=null&&unit.nativeActive==true&&unit.nativeEnabled==true;
        public static bool Controlled(TrainingUnit[] units,int member)=>NativeContext(units,member)
            ?Active(units.First(u=>u.id==member)):OriginalMemberCommands.Controlled(units,member);
        public static int ActionMode(TrainingUnit[] units,int member)
        {
            var leader=units?.FirstOrDefault(u=>u.id==0);
            return NativeContext(units,member)&&!Active(leader)?2:Mode(leader?.troopOrder);
        }
        public static int Mode(int? order)=>order==2?1:order==4?2:order==8?3:0;
        public static int RestPercent(int mode)=>mode==1?60:mode==2?40:mode==3?90:0;
        public static bool Targetable(TrainingUnit unit)
        {
            if(unit==null)return false;
            //03860/03A40/03C00 require active/+4 and clear80. HP0 by
            // itself is not a corpse notice:05 and07 arrive independently.
            if(unit.nativeActive.HasValue&&unit.nativeEnabled.HasValue&&unit.nativeState.HasValue)
                return unit.nativeActive==true&&unit.nativeEnabled==true&&(unit.nativeState.Value&0x80)==0;
            return OriginalCommandRange.Living(unit); // Existing typed/legacy context.
        }
        public static bool? Enemy(TrainingUnit source,TrainingUnit target)
        {
            if(source==null||target==null)return false;
            if(source.nativeInfo?.Valid==true&&target.nativeInfo?.Valid==true)
            {
                int? Team(int id)=>id==source.id?(int?)source.nativeInfo.fields[24]:id==target.id?(int?)target.nativeInfo.fields[24]:null;
                int? ally=OriginalNativeRelations.Ally3(target.id,source.id,Team);
                if(ally.HasValue)return ally==0;
            }
            // An explicit local authority may supply eligibility when received
            // player alliance context is incomplete. Never infer it from IDs/jobs.
            return source.attackTargets==null?(bool?)null:source.attackTargets.Contains(target.id);
        }
        public static BattleLifeRecord[] Candidates(WorldMapDefinition map,OriginalCombatCatalog catalog,TrainingUnit[] units,TrainingUnit source,bool ownFollower=true)
        {
            if(source==null||source.nativeInfo?.Valid!=true&&source.attackTargets==null)return null;
            var weights=OriginalBasicAttackArea.Weights(map.map,catalog,source);if(weights==null)return null;
            var result=new System.Collections.Generic.List<BattleLifeRecord>();
            foreach(var target in units.Where(u=>(!ownFollower||u.id>=10)&&Targetable(u)&&map.map.Contains(u.Tile)&&weights[u.y*map.map.width+u.x]>0)
                .OrderBy(u=>u.x).ThenBy(u=>u.y).ThenBy(u=>u.id))
            {
                bool? enemy=Enemy(source,target);if(!enemy.HasValue)return null;
                if(enemy==true)result.Add(new BattleLifeRecord{id=target.id,life=target.life});
            }
            return result.ToArray();
        }
        // FUN_10003DD0:40/60/90 percent rest thresholds and the client-local forced-rest sentinel99.
        public static SoldierActionPlan Select(WorldMapDefinition map,OriginalCombatCatalog catalog,TrainingUnit[] units,int member,int hint,Func<int,int> choose)
        {
            var result=new SoldierActionPlan{member=member,hintBefore=hint,hintAfter=hint};
            if(!Controlled(units,member))return result;
            var source=units.First(u=>u.id==member);var leader=units.First(u=>u.id==0);result.nativeMode=ActionMode(units,member);bool native=NativeContext(units,member);
            if(result.nativeMode==0)return result;
            bool rest=hint==99||source.hp<(long)source.maxHp*RestPercent(result.nativeMode)/100;
            if(hint==99)result.hintAfter=-1;
            if(rest){if(source.hp<source.maxHp){if(native||source.canRest)result.action=TrainingAction.Rest;else result.unavailable=true;}return result;}
            var candidates=Candidates(map,catalog,units,source,!native);
            // Unknown alliance/target permissions are not an empty enemy list or an excuse to heal.
            if(candidates==null||choose==null||!native&&!source.canAttack){result.unavailable=true;return result;}
            if(candidates.Length==0)return result;
            int index=choose(candidates.Length);if(index<0||index>=candidates.Length){result.unavailable=true;return result;}
            result.action=TrainingAction.Attack;result.target=candidates[index].id;result.candidates=candidates;return result;
        }
        public static SoldierActionPlan Plan(WorldMapDefinition map,OriginalCombatCatalog catalog,TrainingUnit[] units,int member,int hint,Func<int,int> choose,double clockMs=0)
        {
            var none=new SoldierActionPlan{member=member,hintBefore=hint,hintAfter=hint};
            var source=units?.FirstOrDefault(u=>u.id==member);var leader=units?.FirstOrDefault(u=>u.id==0);
            none.stateAfter=source?.nativeState??0;
            bool native=NativeContext(units,member);
            if(!Controlled(units,member)){none.unavailable=!native;return none;}
            if(!source.nativeState.HasValue||ActionMode(units,member)==0||double.IsNaN(clockMs)||double.IsInfinity(clockMs)||clockMs<0){none.unavailable=true;return none;}
            //041F0 clears298 before035E0, so that entry compares its signed
            //32-bit CRT clock with3000, not time since a previous movement.
            int nativeClock=unchecked((int)(long)clockMs);
            uint state=source.nativeState.Value;none.stateAfter=state;
            Func<int,uint,SoldierActionPlan> action=(currentHint,flags)=>
            {
                //03F70 suppresses action while moving;041F0 still evaluates
                //20 movement and full-AP preparation outside this guard.
                if((flags&1)!=0)return new SoldierActionPlan{member=member,hintBefore=hint,hintAfter=currentHint,stateAfter=flags,nativeMode=ActionMode(units,member)};
                var selected=Select(map,catalog,units,member,currentHint,choose);
                selected.hintBefore=hint;selected.stateAfter=selected.action.HasValue?flags&~0x10u:flags;return selected;
            };
            if(hint==99&&(state&0x10)!=0)
            {
                var forced=action(hint,state);if(!forced.unavailable)forced.stateAfter&=~0x30u;return forced;
            }
            if((state&0x20)!=0)
            {
                bool? inside;
                if(native)inside=!Active(leader)?false:leader.commandRange.HasValue
                    ?(bool?)(OriginalCommandRange.Distance(source.x,source.y,leader.x,leader.y)<=leader.commandRange.Value):null;
                else inside=OriginalCommandRange.Evaluate(units).FirstOrDefault(r=>r.member==member)?.inside;
                if(!inside.HasValue){none.unavailable=true;return none;}
                if(inside==true)
                {
                    state&=~0x20u;
                    if((state&0x10)!=0&&(state&1)==0)
                    {
                        var probe=Select(map,catalog,units,member,hint,choose);
                        if(probe.unavailable){none.unavailable=true;return none;}hint=probe.hintAfter;
                        if(!probe.action.HasValue)state&=~0x10u;
                        else if(probe.action!=TrainingAction.Rest||leader.troopOrder!=8||nativeClock<3000)return action(hint,state);
                    }
                    else state&=~0x10u;
                }
                var movement=OriginalSoldierMovement.Find(map,catalog,units,member,hint,choose);
                if(movement.unavailable){none.unavailable=true;return none;}hint=movement.hintAfter;
                if(movement.plan!=null)return new SoldierActionPlan{member=member,target=0,action=TrainingAction.SoldierMove,movement=movement.plan,
                    hintBefore=none.hintBefore,hintAfter=hint,nativeMode=ActionMode(units,member),stateAfter=state&~0x30u};
                if((state&0x10)!=0)return action(hint,state);
                none.hintAfter=hint;none.stateAfter=state;return none;
            }
            if((state&0x10)!=0&&(state&1)==0)return action(hint,state);
            // Native ExecuteSoldierAI ends this tick after preparing30. The next tick decides.
            if(source.ap==source.maxAp)none.stateAfter=0x30;
            return none;
        }
    }
}
