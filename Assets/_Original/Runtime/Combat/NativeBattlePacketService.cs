using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ReceiveBattlePacket(NativeBattleNotice notice)
            =>ReceiveBattlePacket(notice,out _);
        public WorldReply ReceiveBattlePacket(NativeBattleNotice notice,out byte[] followup)
        {
            followup=null;
            if(notice?.packet==null||notice.packet.Length<2||notice.packet[0]!=0x5c)return WorldResult(WorldFailure.InvalidInput);
            byte type=notice.packet[1];
            if(type==0x66)return ReceiveScenarioScriptSuffix(notice);
            if(type==0x20)return ReceiveBattleVictoryAnnouncement(notice);
            if(type==0x0d)return ReceiveNativeDirectedEvent(notice);
            if(type==0x15)return ReceiveNativeImmediateEvents(notice);
            if(type==0x31)return ReceiveNativeResultKills(notice);
            if(type==0x54)return ReceiveNativeScenarioResult(notice);
            if(type==0x61)return ReceiveNativeScenarioEnd(notice);
            if(type==0x60)return ReceiveBattleTeamNames(notice);
            if(type==0x50||type==0x51)return ReceiveBattleUnitPackets(new NativeBattleUnitsNotice{id=notice.id,character=notice.character,session=notice.session,approved=notice.approved,
                expectedRevision=notice.expectedRevision,expectedGrowthRevision=notice.expectedGrowthRevision,packets=new[]{new NativeUnitDelivery{packet=notice.packet,life=notice.unitLife}}});
            if(type==0x53)return ReceiveBattleExperiencePacket(notice);
            if(type==0x13)return ReceiveBattleStatusPacket(notice);
            if(type==0x68)return ReceiveBattleMagicRedirect(notice,out followup);
            if(type==0x64||type==0x65)return ReceiveBattleLevelEffectPacket(notice,out followup);
            if(type==1||type==14)return ReceiveBattleMovement(notice);
            if(type==0x89||type==0x67)return ReceiveBattleMagicPacket(notice);
            if(type==4||type==0x52)return ReceiveBattleTicks(new BattleTickNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,unitLife=notice.unitLife,approved=notice.approved,packet=notice.packet});
            if(type==5||type==6||type==0x0f||type==0x82)return ReceiveBattleFeedback(new BattleFeedbackNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,unitLife=notice.unitLife,targetLife=notice.targetLife,approved=notice.approved,packet=notice.packet});
            if(type==3||type==7)return ReceiveBattleLifecycle(new BattleLifecycleNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,unitLife=notice.unitLife,approved=notice.approved,packet=notice.packet});
            return WorldResult(WorldFailure.RulesUnavailable);
        }
        public WorldReply ReceiveTrainingPacket(NativeBattleNotice notice)
            =>ReceiveTrainingPacket(notice,out _);
        public WorldReply ReceiveTrainingPacket(NativeBattleNotice notice,out byte[] followup)
        {followup=null;var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveBattlePacket(notice,out followup):WorldResult(ready);}
        private WorldReply ReceiveBattleStatusPacket(NativeBattleNotice notice)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            if(notice?.approved!=true||!OriginalBattleStatus.TryDecode(notice.packet,out int unit,out uint flags))return WorldResult(WorldFailure.InvalidInput);
            var combat=world.Combat;var target=combat?.Units.FirstOrDefault(u=>u.id==unit);
            if(target==null||target.life!=notice.unitLife)return WorldResult(WorldFailure.StaleRequest);
            if(!target.nativeActive.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
            if(target.nativeActive==false)
            {
                if(notice.character!=worldCharacter||notice.session!=world.CampaignSession||!System.Guid.TryParseExact(notice.id,"N",out _)
                    ||notice.expectedRevision!=combat.Revision||combat.Notices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
                // C950 does nothing for inactive records. Do not turn that no-op
                // into a snapshot save, a revision change or an unrelated action cancellation.
                combat.Notices.Add(notice.id);return WorldResult(WorldFailure.None);
            }
            var units=combat.Units.Select(u=>u.Copy()).ToArray();
            // C950 ignores inactive units, replaces the whole+104 DWORD for active
            // units, and ignores the upper16 input bits. No duration or damage rule.
            if(target.nativeActive==true)units.First(u=>u.id==unit).nativeStatusFlags=OriginalNativeUnits.StatusFlags(flags);
            return ReceiveBattleUnits(new TrainingUnitsNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,approved=true,units=units});
        }
        private WorldReply ReceiveBattleVictoryAnnouncement(NativeBattleNotice notice)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            var input=notice?.Copy();
            if(input?.approved!=true||!OriginalVictoryAnnouncement.TryDecode(input.packet,out _,out var name))return WorldResult(WorldFailure.InvalidInput);
            if(input.character!=worldCharacter||input.session!=world.CampaignSession||!System.Guid.TryParseExact(input.id,"N",out _)
                ||input.expectedRevision!=(world.Combat?.Revision??0)||world.CampaignNotices.Contains(input.id))return WorldResult(WorldFailure.StaleRequest);
            // End-stage takeover shares more host state; preserve its existing
            // boundary rather than changing an accepted61 decision.
            if(world.Record.scenarioVisit?.nativeEnd?.Started==true||world.Presentation?.Snapshot() is Campaign.CampaignPresentationView p&&!p.completed&&!p.nativeEvent)
                return WorldResult(WorldFailure.Busy);
            if(input.localTeam.HasValue&&(!input.expectedTeamRevision.HasValue||input.expectedTeamRevision!=world.CurrentNativeTeamRevision))return WorldResult(WorldFailure.StaleRequest);
            if(!world.TryNativeWinner(input,out var own))return WorldResult(WorldFailure.RulesUnavailable);
            if(!OriginalVictoryAnnouncement.SameName(name,own))
            {
                var bytes=OriginalVictoryAnnouncement.CString(name);
                string text=bytes.Length==0?"":world.Definition.nativeText?.Invoke(bytes);
                if(text==null)return WorldResult(WorldFailure.RulesUnavailable);
                world.NativeDialogue.Show(text+"님이 승리하셨습니다.",0);
            }
            world.CampaignNotices.Add(input.id);return WorldResult(WorldFailure.None);
        }
        private WorldReply ReceiveBattleExperiencePacket(NativeBattleNotice notice)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            if(notice?.approved!=true||!OriginalBattleExperience.TryDecode(notice.packet,out int unit,out uint value))return WorldResult(WorldFailure.InvalidInput);
            var combat=world.Combat;var target=combat?.Units.FirstOrDefault(u=>u.id==unit);
            if(target==null||target.life!=notice.unitLife)return WorldResult(WorldFailure.StaleRequest);
            if(unit==0&&(world.Record.growth==null||!notice.expectedGrowthRevision.HasValue))return WorldResult(WorldFailure.RulesUnavailable);
            var units=combat.Units.Select(u=>u.Copy()).ToArray();var received=units.First(u=>u.id==unit);received.nativeExperience=value;received.nativeInfo?.ExperienceSet(value);
            return ReceiveBattleUnits(new TrainingUnitsNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,
                approved=true,units=units,experience=unit==0?(ulong?)value:null,expectedGrowthRevision=notice.expectedGrowthRevision??0});
        }
    }
}
