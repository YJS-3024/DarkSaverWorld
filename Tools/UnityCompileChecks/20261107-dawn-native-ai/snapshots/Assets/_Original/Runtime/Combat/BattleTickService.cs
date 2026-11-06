using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ReceiveBattleTicks(BattleTickNotice notice)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            var combat=world.Combat;if(combat==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(notice==null||!notice.approved||!OriginalBattleTicks.TryDecode(notice.packet,out var packet))return WorldResult(WorldFailure.InvalidInput);
            if(notice.character!=worldCharacter||notice.session!=world.CampaignSession||notice.expectedRevision!=combat.Revision
                ||!Guid.TryParseExact(notice.id,"N",out _)||combat.Notices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            TrainingUnit[] units;
            if(packet.pulse)
            {if(!OriginalBattleTicks.TryPulse(combat.Units,out units))return WorldResult(WorldFailure.RulesUnavailable);}
            else
            {
                var before=combat.Units.FirstOrDefault(u=>u.id==packet.unit);
                if(before==null||before.life!=notice.unitLife)return WorldResult(WorldFailure.StaleRequest);
                if(!OriginalBattleTicks.TryMenu(before,packet.ap,packet.menu,out var updated))return WorldResult(WorldFailure.RulesUnavailable);
                units=combat.Units.Select(u=>u.id==before.id?updated:u.Copy()).ToArray();
            }
            // Same atomic snapshot ingress/lifetime/revision contract as other local server notices.
            // No timer, cost, recharge rate or maximum is fabricated from these received messages.
            return ReceiveBattleUnits(new TrainingUnitsNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,approved=true,units=units});
        }
        public WorldReply ReceiveTrainingTicks(BattleTickNotice notice)
        {var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveBattleTicks(notice):WorldResult(ready);}
    }
}
