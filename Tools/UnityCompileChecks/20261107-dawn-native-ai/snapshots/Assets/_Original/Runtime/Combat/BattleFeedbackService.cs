using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ReceiveBattleFeedback(BattleFeedbackNotice notice)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            var combat=world.Combat;if(combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(notice==null||!notice.approved||!OriginalBattleFeedback.TryDecode(notice.packet,out var packet))return WorldResult(WorldFailure.InvalidInput);
            if(notice.character!=worldCharacter||notice.session!=world.CampaignSession||notice.expectedRevision!=combat.Revision
                ||!Guid.TryParseExact(notice.id,"N",out _)||combat.Notices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            var actor=combat.Units.FirstOrDefault(u=>u.id==packet.unit);
            if(actor==null||actor.life!=notice.unitLife)return WorldResult(WorldFailure.StaleRequest);
            var units=combat.Units.Select(u=>u.Copy()).ToArray();var updated=units.First(u=>u.id==actor.id);
            if(packet.kind==BattleFeedbackKind.Health)
            {if(packet.value<0||packet.value>actor.maxHp)return WorldResult(WorldFailure.InvalidInput);updated.hp=packet.value;}
            else if(packet.kind==BattleFeedbackKind.Mana)
            {if(packet.value<0||packet.value>actor.maxMp)return WorldResult(WorldFailure.InvalidInput);updated.mp=packet.value;}
            else if(packet.kind==BattleFeedbackKind.Attack)
            {
                var target=units.FirstOrDefault(u=>u.id==packet.target);
                if(target==null||target.life!=notice.targetLife)return WorldResult(WorldFailure.StaleRequest);
                var pair=BattleAttackEffect.Create(actor,target,packet.value);updated.direction=pair[0].direction;target.direction=pair[1].direction;
            }
            var e=new BattleFeedbackEvent{kind=packet.kind,unit=actor.id,life=actor.life,target=packet.target,targetLife=packet.kind==BattleFeedbackKind.Attack?notice.targetLife:0,value=packet.value};
            return ReceiveBattleUnits(new TrainingUnitsNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,approved=true,units=units,feedbackEvents=new[]{e}});
        }
        public WorldReply ReceiveTrainingFeedback(BattleFeedbackNotice notice)
        {var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveBattleFeedback(notice):WorldResult(ready);}
        private static WorldFailure ValidateFeedback(TrainingCombatSession combat,TrainingUnitsNotice notice)
        {
            if(notice.feedbackEvents==null)return WorldFailure.None;
            if(notice.feedbackEvents.Length>3000)return WorldFailure.InvalidInput;
            // HP/MP packets have order. An unordered snapshot carries at most one numeric event
            // per unit; callers that know packet order can submit separate notices.
            var numeric=notice.feedbackEvents.Where(e=>e!=null&&e.kind!=BattleFeedbackKind.Attack).ToArray();
            if(numeric.Select(e=>e.unit).Distinct().Count()!=numeric.Length)return WorldFailure.InvalidInput;
            var attacks=notice.feedbackEvents.Where(e=>e?.kind==BattleFeedbackKind.Attack).ToArray();
            if(attacks.SelectMany(e=>new[]{e.unit,e.target}).GroupBy(id=>id).Any(g=>g.Count()>1))return WorldFailure.InvalidInput;
            return notice.feedbackEvents.All(e=>OriginalBattleFeedback.Valid(e,combat.Units,notice.units))?WorldFailure.None:WorldFailure.InvalidInput;
        }
        private static void ApplyFeedback(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            foreach(var id in combat.AttackFlags.Keys.ToArray())
                if(!notice.units.Any(u=>u.id==id&&u.life==combat.AttackFlags[id].life))combat.AttackFlags.Remove(id);
            foreach(var id in combat.NumberEffects.Keys.ToArray())
                if(!notice.units.Any(u=>u.id==id&&u.life==combat.NumberEffects[id].life))combat.NumberEffects.Remove(id);
            foreach(var id in combat.AttackEffects.Keys.ToArray())
                if(!notice.units.Any(u=>u.id==id&&u.life==combat.AttackEffects[id].life))combat.AttackEffects.Remove(id);
            foreach(var e in notice.feedbackEvents??Array.Empty<BattleFeedbackEvent>())
            {
                var before=combat.Units.First(u=>u.id==e.unit);var after=notice.units.First(u=>u.id==e.unit);
                if(e.kind==BattleFeedbackKind.Attack)
                {
                    ApplyAttackFeedback(combat,before,combat.Units.First(u=>u.id==e.target),e.value);
                }
                else combat.NumberEffects[e.unit]=new BattleNumberEffect{unit=e.unit,life=e.life,kind=e.kind,hp=after.hp,maxHp=after.maxHp,delta=e.kind==BattleFeedbackKind.Health?after.hp-before.hp:e.kind==BattleFeedbackKind.Mana?after.mp-before.mp:0};
            }
            if(notice.nativeResponse==null&&!string.IsNullOrEmpty(notice.request)&&request?.action==TrainingAction.Attack
                &&notice.feedbackEvents?.Any(e=>e.kind==BattleFeedbackKind.Attack&&e.unit==request.source&&e.target==request.target)!=true)
            {
                // Accepted local basic attack corresponds to AttackChar. Unknown third WORD is0;
                // do not manufacture its extra graphic, damage or a MISS from unchanged HP.
                ApplyAttackFeedback(combat,combat.Units.First(u=>u.id==request.source),combat.Units.First(u=>u.id==request.target),0);
            }
        }
        private static void ApplyAttackFeedback(TrainingCombatSession combat,TrainingUnit source,TrainingUnit target,int flag)
        {
            // AttackChar writes source294 only; the target's previous294 survives until reset.
            int targetFlag=combat.AttackFlags.TryGetValue(target.id,out var prior)&&prior.life==target.life?prior.flag:0;
            foreach(var effect in BattleAttackEffect.Create(source,target,flag,targetFlag))combat.AttackEffects[effect.unit]=effect;
            combat.AttackFlags[source.id]=(source.life,flag);
        }
    }
}
