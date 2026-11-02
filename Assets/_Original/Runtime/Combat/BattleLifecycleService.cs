using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ReceiveBattleLifecycle(BattleLifecycleNotice notice)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            var combat=world.Combat;if(combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(notice==null||!notice.approved||!OriginalBattleLifecycle.TryDecode(notice.packet,out var packet))return WorldResult(WorldFailure.InvalidInput);
            if(notice.character!=worldCharacter||notice.session!=world.CampaignSession||notice.expectedRevision!=combat.Revision
                ||!Guid.TryParseExact(notice.id,"N",out _)||combat.Notices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            var actor=combat.Units.FirstOrDefault(u=>u.id==packet.unit);
            if(actor==null||actor.life!=notice.unitLife)return WorldResult(WorldFailure.StaleRequest);
            if(!OriginalBattleLifecycle.TryApply(actor,packet.kind,out var updated))return WorldResult(WorldFailure.RulesUnavailable);
            return ReceiveBattleUnits(new TrainingUnitsNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,approved=true,
                units=combat.Units.Select(u=>u.id==actor.id?updated:u.Copy()).ToArray(),lifecycleEvents=new[]{new BattleLifecycleEvent{kind=packet.kind,unit=actor.id,life=actor.life}}});
        }
        public WorldReply ReceiveTrainingLifecycle(BattleLifecycleNotice notice)
        {var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveBattleLifecycle(notice):WorldResult(ready);}
        private static WorldFailure ValidateLifecycle(TrainingCombatSession combat,TrainingUnitsNotice notice)
        {
            if(notice.lifecycleEvents==null)return WorldFailure.None;
            if(notice.lifecycleEvents.Length>1000||notice.lifecycleEvents.Any(e=>e==null)||notice.lifecycleEvents.Select(e=>e.unit).Distinct().Count()!=notice.lifecycleEvents.Length)return WorldFailure.InvalidInput;
            foreach(var e in notice.lifecycleEvents)
            {
                var before=combat.Units.FirstOrDefault(u=>u.id==e.unit&&u.life==e.life);var after=notice.units.FirstOrDefault(u=>u.id==e.unit&&u.life==e.life);
                if(before==null||after==null||!Enum.IsDefined(typeof(BattleLifecycleKind),e.kind)||!before.nativeState.HasValue||!after.nativeState.HasValue)return WorldFailure.InvalidInput;
                if(e.kind==BattleLifecycleKind.Death&&(after.nativeState.Value&0x80)==0)return WorldFailure.InvalidInput;
                if(e.kind==BattleLifecycleKind.Remove&&(before.nativeState.Value&0x80)==0&&after.nativeActive!=false)return WorldFailure.InvalidInput;
            }
            return WorldFailure.None;
        }
        private static void ApplyLifecycle(TrainingCombatSession combat,TrainingUnitsNotice notice)
        {
            foreach(var id in combat.Deaths.Keys.ToArray())
            {
                var actor=notice.units.FirstOrDefault(u=>u.id==id&&u.life==combat.Deaths[id].life);
                if(actor==null||!actor.nativeState.HasValue||(actor.nativeState.Value&0x80)==0)combat.Deaths.Remove(id);
                else combat.Deaths[id].held=!combat.Deaths[id].Expired&&combat.NumberEffects.TryGetValue(id,out var numeric)&&numeric.life==actor.life&&!numeric.Expired;
            }
            foreach(var e in notice.lifecycleEvents??Array.Empty<BattleLifecycleEvent>())
            {
                if(e.kind!=BattleLifecycleKind.Death||combat.Deaths.ContainsKey(e.unit))continue;
                var actor=notice.units.First(u=>u.id==e.unit);
                combat.Deaths[e.unit]=new BattleDeathEffect{unit=e.unit,life=e.life,job=OriginalBattleLifecycle.AppearanceKey(actor),held=combat.NumberEffects.TryGetValue(e.unit,out var number)&&number.life==e.life&&!number.Expired};
            }
        }
    }
}
