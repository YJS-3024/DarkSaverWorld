using System;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed class NativeBattleNotice
    {
        // Local delivery context. These fields are not appended to the original packet bytes.
        public string id,character,session;
        public long expectedRevision,unitLife,targetLife;
        public bool approved;
        public byte[] packet;
        public int? localTeam,sourceTeam;
        public BattleLifeRecord[] targetLives;
        public NativeBattleNotice Copy(){var r=(NativeBattleNotice)MemberwiseClone();r.packet=packet==null?null:(byte[])packet.Clone();r.targetLives=targetLives==null?null:System.Array.ConvertAll(targetLives,l=>l?.Copy());return r;}
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
