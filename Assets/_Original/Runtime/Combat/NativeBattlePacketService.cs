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
            if(type==0x31)return ReceiveNativeResultKills(notice);
            if(type==0x54)return ReceiveNativeScenarioResult(notice);
            if(type==0x60)return ReceiveBattleTeamNames(notice);
            if(type==0x50||type==0x51)return ReceiveBattleUnitPackets(new NativeBattleUnitsNotice{id=notice.id,character=notice.character,session=notice.session,approved=notice.approved,
                expectedRevision=notice.expectedRevision,expectedGrowthRevision=notice.expectedGrowthRevision,packets=new[]{new NativeUnitDelivery{packet=notice.packet,life=notice.unitLife}}});
            if(type==0x53)return ReceiveBattleExperiencePacket(notice);
            if(type==0x64||type==0x65)return ReceiveBattleLevelEffectPacket(notice,out followup);
            if(type==1||type==14)return ReceiveBattleMovement(notice);
            if(type==0x89)return ReceiveBattleMagicPacket(notice);
            if(type==4||type==0x52)return ReceiveBattleTicks(new BattleTickNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,unitLife=notice.unitLife,approved=notice.approved,packet=notice.packet});
            if(type==5||type==6||type==0x0f||type==0x82)return ReceiveBattleFeedback(new BattleFeedbackNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,unitLife=notice.unitLife,targetLife=notice.targetLife,approved=notice.approved,packet=notice.packet});
            if(type==3||type==7)return ReceiveBattleLifecycle(new BattleLifecycleNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,unitLife=notice.unitLife,approved=notice.approved,packet=notice.packet});
            return WorldResult(WorldFailure.RulesUnavailable);
        }
        public WorldReply ReceiveTrainingPacket(NativeBattleNotice notice)
            =>ReceiveTrainingPacket(notice,out _);
        public WorldReply ReceiveTrainingPacket(NativeBattleNotice notice,out byte[] followup)
        {followup=null;var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveBattlePacket(notice,out followup):WorldResult(ready);}
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
