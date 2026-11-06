using System;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed class NativeBattleNotice
    {
        // Local delivery context. These fields are not appended to the original packet bytes.
        public string id,character,session;
        public long expectedRevision,unitLife,targetLife;
        public long? expectedGrowthRevision,expectedTeamRevision,expectedResultKillRevision;
        public bool approved;
        public byte[] packet;
        public int? localTeam,sourceTeam;
        public BattleLifeRecord[] targetLives;
        public NativeBattleNotice Copy(){var r=(NativeBattleNotice)MemberwiseClone();r.packet=packet==null?null:(byte[])packet.Clone();r.targetLives=targetLives==null?null:System.Array.ConvertAll(targetLives,l=>l?.Copy());return r;}
    }
    // Captured before public callbacks; mutable reply views cannot retarget a leave notification.
    internal readonly struct NativeScenarioReturn
    {
        private readonly string character,commanderSession,session,result;
        private readonly int map;
        private NativeScenarioReturn(WorldView view)
        {character=view.characterId;commanderSession=view.commanderSession;session=view.campaignSession;map=view.record.map;result=view.record.lastScenarioResult.id;}
        public static NativeScenarioReturn? Capture(WorldView view)=>view?.record!=null&&view.record.scenarioVisit==null&&view.record.lastScenarioResult?.id!=null?(NativeScenarioReturn?)new NativeScenarioReturn(view):null;
        public bool Matches(WorldView view)=>view!=null&&view.characterId==character&&view.commanderSession==commanderSession&&view.campaignSession==session
            &&view.record.map==map&&view.record.scenarioVisit==null&&view.record.lastScenarioResult?.id==result;
    }
    // Immutable local delivery context; never appended to a native packet.
    public readonly struct NativeBattleFocus
    {
        public readonly string character,commanderSession,session;
        public readonly int map,unit,x,y;
        public readonly long revision,life;
        private NativeBattleFocus(WorldView view,TrainingUnit actor)
        {character=view.characterId;commanderSession=view.commanderSession;session=view.campaignSession;map=view.record.map;revision=view.battle.revision;unit=actor.id;life=actor.life;var tile=Position(view,actor);x=tile.X;y=tile.Y;}
        private static TutorialTile Position(WorldView view,TrainingUnit actor)
        {
            var motion=view.battle.commanderMotion;
            if(actor.id==0&&motion?.unit==0&&motion.life==actor.life)return new TutorialTile(motion.tileX,motion.tileY);
            foreach(var member in view.battle.motions??Array.Empty<BattleUnitMotionView>())
                if(member!=null&&member.unit==actor.id&&member.life==actor.life)return new TutorialTile(member.tileX,member.tileY);
            return actor.Tile;
        }
        public static NativeBattleFocus? Capture(WorldView view,int unit)
        {var actor=view?.battle?.Unit(unit);return actor==null||view.record==null?(NativeBattleFocus?)null:new NativeBattleFocus(view,actor);}
        public bool Matches(WorldView view)
        {
            var actor=view?.battle?.Unit(unit);
            return actor!=null&&view.record!=null&&character==view.characterId&&commanderSession==view.commanderSession&&session==view.campaignSession
                &&map==view.record.map&&revision==view.battle.revision&&life==actor.life&&Position(view,actor)==new TutorialTile(x,y);
        }
    }
    public static class OriginalBattleExperience
    {
        public static bool TryDecode(byte[] bytes,out int unit,out uint experience)
        {
            unit=-1;experience=0;
            if(bytes==null||bytes.Length!=8||bytes[0]!=0x5c||bytes[1]!=0x53)return false;
            int id=(short)(bytes[2]|bytes[3]<<8);if(id<0||id>=3000)return false;
            unit=id;experience=(uint)(bytes[4]|bytes[5]<<8|bytes[6]<<16|bytes[7]<<24);return true;
        }
        public static byte[] Packet(int unit,uint experience)
            =>unit<0||unit>=3000?null:new[]{(byte)0x5c,(byte)0x53,(byte)unit,(byte)(unit>>8),(byte)experience,(byte)(experience>>8),(byte)(experience>>16),(byte)(experience>>24)};
    }
    // GameCtl5C13 -> DCB0 -> UserUnit.MagicFlag: only runtime+104 status bits.
    public static class OriginalBattleStatus
    {
        public static bool TryDecode(byte[] bytes,out int unit,out uint flags)
        {
            unit=-1;flags=0;
            if(bytes==null||bytes.Length!=8||bytes[0]!=0x5c||bytes[1]!=0x13)return false;
            int id=(short)(bytes[2]|bytes[3]<<8);if(id<0||id>=3000)return false;
            unit=id;flags=(uint)(bytes[4]|bytes[5]<<8|bytes[6]<<16|bytes[7]<<24);return true;
        }
        public static byte[] Packet(int unit,uint flags)
        {var bytes=OriginalBattleExperience.Packet(unit,flags);if(bytes!=null)bytes[1]=0x13;return bytes;}
    }
    public sealed class NativeBattleRequest
    {
        public TrainingAction action;
        public int source,target=-1,sourceTerrain=-1,targetTerrain=-1;
        public int x,y;
        public int magic,mode,item=-1,itemToken=-1;public uint signature;public int[] targets;
        public TutorialTile[] movePoints;
    }
    public static class OriginalBattleRequests
    {
        private static bool Unit(int id)=>id>=0&&id<3000;
        public static byte[] Packet(TrainingActionRequest request)
        {
            if(request==null||!Unit(request.source))return null;
            if(request.action==TrainingAction.CastMagic||request.action==TrainingAction.UseSkill)return request.NativeMagicPacket(1);
            if(request.action==TrainingAction.Move||request.action==TrainingAction.Regroup||request.action==TrainingAction.SoldierMove)return OriginalNativeMoveHints.Packet(request.source,request.nativeMovePoints);
            if(request.action==TrainingAction.Rest)return new[]{(byte)0x5c,(byte)0x0b,(byte)request.source,(byte)(request.source>>8)};
            if(request.action!=TrainingAction.Attack||!Unit(request.target)||request.source==request.target
                ||request.sourceTerrain<0||request.sourceTerrain>65535||request.targetTerrain<0||request.targetTerrain>65535)return null;
            return new[]{(byte)0x5c,(byte)0x82,(byte)request.source,(byte)(request.source>>8),(byte)request.target,(byte)(request.target>>8),
                (byte)request.sourceTerrain,(byte)(request.sourceTerrain>>8),(byte)request.targetTerrain,(byte)(request.targetTerrain>>8)};
        }
        public static bool TryDecode(byte[] bytes,out NativeBattleRequest request)
        {
            request=null;if(bytes==null||bytes.Length<2||bytes[0]!=0x5c)return false;
            if(OriginalNativeMagic.TryRequest(bytes,out var magic))
            {request=new NativeBattleRequest{action=magic.item>=0?TrainingAction.UseItem:magic.magic<10000?TrainingAction.CastMagic:TrainingAction.UseSkill,source=magic.source,x=magic.x,y=magic.y,magic=magic.magic,mode=magic.mode,item=magic.item,itemToken=magic.itemToken,signature=magic.signature,targets=magic.targets};return true;}
            if(OriginalNativeMoveHints.TryDecode(bytes,out int mover,out var points))
            {request=new NativeBattleRequest{action=TrainingAction.Move,source=mover,x=points[0].X,y=points[0].Y,movePoints=points};return true;}
            if(bytes[1]==0x0b&&bytes.Length==4)
            {int unit=bytes[2]|bytes[3]<<8;if(!Unit(unit))return false;request=new NativeBattleRequest{action=TrainingAction.Rest,source=unit};return true;}
            if(bytes[1]!=0x82||bytes.Length!=10)return false;
            int source=bytes[2]|bytes[3]<<8,target=bytes[4]|bytes[5]<<8;
            if(!Unit(source)||!Unit(target)||source==target)return false;
            request=new NativeBattleRequest{action=TrainingAction.Attack,source=source,target=target,sourceTerrain=bytes[6]|bytes[7]<<8,targetTerrain=bytes[8]|bytes[9]<<8};return true;
        }
    }
}
