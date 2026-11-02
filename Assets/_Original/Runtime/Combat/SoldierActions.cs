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
        public static int Mode(int? order)=>order==2?1:order==4?2:order==8?3:0;
        public static int RestPercent(int mode)=>mode==1?60:mode==2?40:mode==3?90:0;
        public static BattleLifeRecord[] Candidates(WorldMapDefinition map,OriginalCombatCatalog catalog,TrainingUnit[] units,TrainingUnit source)
        {
            if(source==null||source.attackTargets==null)return null;
            var weights=OriginalBasicAttackArea.Weights(map.map,catalog,source);if(weights==null)return null;
            return units.Where(u=>u.id>=10&&source.attackTargets.Contains(u.id)&&OriginalCommandRange.Living(u)&&map.map.Contains(u.Tile)&&weights[u.y*map.map.width+u.x]>0)
                .OrderBy(u=>u.x).ThenBy(u=>u.y).ThenBy(u=>u.id).Select(u=>new BattleLifeRecord{id=u.id,life=u.life}).ToArray();
        }
        // FUN_10003DD0:40/60/90 percent rest thresholds and the client-local forced-rest sentinel99.
        public static SoldierActionPlan Select(WorldMapDefinition map,OriginalCombatCatalog catalog,TrainingUnit[] units,int member,int hint,Func<int,int> choose)
        {
            var result=new SoldierActionPlan{member=member,hintBefore=hint,hintAfter=hint};
            if(!OriginalMemberCommands.Controlled(units,member))return result;
            var source=units.First(u=>u.id==member);var leader=units.First(u=>u.id==0);result.nativeMode=Mode(leader.troopOrder);
            if(result.nativeMode==0)return result;
            bool rest=hint==99||source.hp<(long)source.maxHp*RestPercent(result.nativeMode)/100;
            if(hint==99)result.hintAfter=-1;
            if(rest){if(source.hp<source.maxHp){if(source.canRest)result.action=TrainingAction.Rest;else result.unavailable=true;}return result;}
            var candidates=Candidates(map,catalog,units,source);
            // Unknown alliance/target permissions are not an empty enemy list or an excuse to heal.
            if(candidates==null||choose==null||!source.canAttack){result.unavailable=true;return result;}
            if(candidates.Length==0)return result;
            int index=choose(candidates.Length);if(index<0||index>=candidates.Length){result.unavailable=true;return result;}
            result.action=TrainingAction.Attack;result.target=candidates[index].id;result.candidates=candidates;return result;
        }
        public static SoldierActionPlan Plan(WorldMapDefinition map,OriginalCombatCatalog catalog,TrainingUnit[] units,int member,int hint,Func<int,int> choose,double clockMs=0)
        {
            var none=new SoldierActionPlan{member=member,hintBefore=hint,hintAfter=hint};
            if(!OriginalMemberCommands.Controlled(units,member)){none.unavailable=true;return none;}
            var source=units.First(u=>u.id==member);var leader=units.First(u=>u.id==0);
            if(!source.nativeState.HasValue||Mode(leader.troopOrder)==0||double.IsNaN(clockMs)||double.IsInfinity(clockMs)||clockMs<0){none.unavailable=true;return none;}
            uint state=source.nativeState.Value;none.stateAfter=state;
            if((state&1)!=0)return none;
            Func<int,uint,SoldierActionPlan> action=(currentHint,flags)=>
            {
                var selected=Select(map,catalog,units,member,currentHint,choose);
                selected.hintBefore=hint;selected.stateAfter=selected.action.HasValue?flags&~0x10u:flags;return selected;
            };
            if(hint==99&&(state&0x10)!=0)
            {
                var forced=action(hint,state);if(!forced.unavailable)forced.stateAfter&=~0x30u;return forced;
            }
            if((state&0x20)!=0)
            {
                var range=OriginalCommandRange.Evaluate(units).FirstOrDefault(r=>r.member==member);
                if(range==null||!range.inside.HasValue){none.unavailable=true;return none;}
                if(range.inside==true)
                {
                    state&=~0x20u;
                    if((state&0x10)!=0)
                    {
                        var probe=Select(map,catalog,units,member,hint,choose);
                        if(probe.unavailable){none.unavailable=true;return none;}hint=probe.hintAfter;
                        if(!probe.action.HasValue)state&=~0x10u;
                        else if(probe.action!=TrainingAction.Rest||leader.troopOrder!=8||clockMs<3000)return action(hint,state);
                    }
                }
                var movement=OriginalSoldierMovement.Find(map,catalog,units,member,hint,choose);
                if(movement.unavailable){none.unavailable=true;return none;}hint=movement.hintAfter;
                if(movement.plan!=null)return new SoldierActionPlan{member=member,target=0,action=TrainingAction.SoldierMove,movement=movement.plan,
                    hintBefore=none.hintBefore,hintAfter=hint,nativeMode=Mode(leader.troopOrder),stateAfter=state&~0x30u};
                if((state&0x10)!=0)return action(hint,state);
                none.hintAfter=hint;none.stateAfter=state;return none;
            }
            if((state&0x10)!=0)return action(hint,state);
            // Native ExecuteSoldierAI ends this tick after preparing30. The next tick decides.
            if(source.ap==source.maxAp)none.stateAfter=0x30;
            return none;
        }
    }
}
