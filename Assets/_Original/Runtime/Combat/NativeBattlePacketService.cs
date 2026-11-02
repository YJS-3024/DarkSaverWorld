using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ReceiveBattlePacket(NativeBattleNotice notice)
        {
            if(notice?.packet==null||notice.packet.Length<2||notice.packet[0]!=0x5c)return WorldResult(WorldFailure.InvalidInput);
            byte type=notice.packet[1];
            if(type==1||type==14)return ReceiveBattleMovement(notice);
            if(type==0x89)return ReceiveBattleMagicPacket(notice);
            if(type==4||type==0x52)return ReceiveBattleTicks(new BattleTickNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,unitLife=notice.unitLife,approved=notice.approved,packet=notice.packet});
            if(type==5||type==6||type==0x0f||type==0x82)return ReceiveBattleFeedback(new BattleFeedbackNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,unitLife=notice.unitLife,targetLife=notice.targetLife,approved=notice.approved,packet=notice.packet});
            if(type==3||type==7)return ReceiveBattleLifecycle(new BattleLifecycleNotice{id=notice.id,character=notice.character,session=notice.session,expectedRevision=notice.expectedRevision,unitLife=notice.unitLife,approved=notice.approved,packet=notice.packet});
            return WorldResult(WorldFailure.RulesUnavailable);
        }
        public WorldReply ReceiveTrainingPacket(NativeBattleNotice notice)
        {var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveBattlePacket(notice):WorldResult(ready);}
    }
}
