using System;
using System.Collections.Generic;

namespace DarkSaver.Network
{
    // Confirmed Saver.exe 0x439320/0x439F30 framing. Strict complete-frame decoder,
    // not a claim to reproduce the old socket receiver's malformed-stream recovery.
    public static class OriginalLocalPacketCodec
    {
        public const int MaximumPayload=2048;
        public static byte[] EncodeFrame(ReadOnlySpan<byte> payload)
        {
            if(payload.Length>MaximumPayload)throw new ArgumentOutOfRangeException(nameof(payload));
            var bytes=new List<byte>(payload.Length+8){0xaa,0x55,1};
            void Escape(byte b){if(b==9){bytes.Add(0x47);bytes.Add(0x50);}else if(b==0x47){bytes.Add(0x47);bytes.Add(0x47);}else bytes.Add(b);}
            byte lo=(byte)payload.Length,hi=(byte)(payload.Length>>8);int sum=1+lo+hi;
            Escape(lo);Escape(hi);foreach(byte b in payload){Escape(b);sum+=b;}Escape(unchecked((byte)sum));
            bytes.Add(0x55);bytes.Add(0xaa);return bytes.ToArray();
        }
        public static bool TryDecodeFrame(ReadOnlySpan<byte> frame,out byte[] payload)
        {
            payload=null;if(frame.Length<8 || frame[0]!=0xaa || frame[1]!=0x55 || frame[2]!=1 || frame[frame.Length-2]!=0x55 || frame[frame.Length-1]!=0xaa)return false;
            var decoded=new List<byte>();
            for(int i=3;i<frame.Length-2;i++)
            {
                byte b=frame[i];
                if(b==0x47){if(++i>=frame.Length-2)return false;byte next=frame[i];if(next==0x50)b=9;else if(next==0x47)b=0x47;else return false;}
                decoded.Add(b);if(decoded.Count>MaximumPayload+3)return false;
            }
            if(decoded.Count<3)return false;int length=decoded[0]|decoded[1]<<8;
            if(length>MaximumPayload || decoded.Count!=length+3)return false;
            int sum=1;for(int i=0;i<decoded.Count-1;i++)sum+=decoded[i];if(unchecked((byte)sum)!=decoded[decoded.Count-1])return false;
            payload=decoded.GetRange(2,length).ToArray();return true;
        }
        private static void Word(byte[] b,int offset,short value){b[offset]=unchecked((byte)value);b[offset+1]=unchecked((byte)(value>>8));}
        private static short ReadWord(ReadOnlySpan<byte> b,int offset)=>unchecked((short)(b[offset]|b[offset+1]<<8));
        public static bool TryEncodePayload(LocalPacket packet,out byte[] payload)
        {
            payload=null;
            if(packet is Request_WorldWhisperSetting whisper){payload=new byte[]{0x7d,(byte)whisper.OriginalValue,0};return true;}
            if(packet is Request_HireMercenary hire){payload=new byte[7];payload[0]=0x4d;Word(payload,1,(short)hire.OwnerId);Word(payload,3,(short)hire.JobId);Word(payload,5,(short)hire.Maximum);return true;}
            if(packet is Request_DismissMercenary dismiss){payload=new byte[5];payload[0]=0x4e;Word(payload,1,(short)dismiss.OwnerId);Word(payload,3,(short)dismiss.JobId);return true;}
            if(packet is Request_WorldRosterFlag flag){payload=new byte[5];payload[0]=0x42;Word(payload,1,unchecked((short)flag.RecordId));Word(payload,3,(short)flag.Flag);return true;}
            if(packet is Request_WorldRosterOrder order){payload=new byte[5];payload[0]=0x3b;Word(payload,1,(short)order.Destination);Word(payload,3,(short)order.Source);return true;}
            if(packet is CharacterMagicListRequest list){payload=new byte[5];return list.TryWriteOriginalPayload(payload,out _);}
            if(packet is CharacterAdjustmentQueryRequest query){payload=new byte[3];return query.TryWriteOriginalPayload(payload,out _);}
            if(packet is CharacterAdjustmentCommandRequest command){payload=new byte[3];return command.TryWriteOriginalPayload(payload,out _);}
            if(packet is CharacterStatAdjustmentRequest stat){payload=new byte[7];return stat.TryWriteOriginalPayload(payload,out _);}
            if(packet is CharacterMagicSelectionRequest selection){payload=new byte[7];return selection.TryWriteOriginalPayload(payload,out _);}
            if(packet is CharacterAdjustmentClassRequest cls){payload=new byte[5];return cls.TryWriteOriginalPayload(payload,out _);}
            if(packet is Request_WorldText text)return OriginalWorldTextCodec.TryEncode(text,out payload);
            if(packet is EquipmentRequest equipment){payload=new byte[EquipmentRequest.OriginalPayloadSize];if(equipment.TryWriteOriginalPayload(payload,out _))return true;payload=null;return false;}
            if(packet is AttackRequest attack)
            {payload=new byte[10];payload[0]=0x5c;payload[1]=0x82;Word(payload,2,attack.TargetUnitId);Word(payload,4,attack.AttackerUnitId);Word(payload,6,attack.TargetTileAttribute);Word(payload,8,attack.AttackerTileAttribute);return true;}
            if(packet is MagicRequest magic && magic.OriginalOpcode.HasValue && magic.Mode.HasValue)
            {
                payload=new byte[16+magic.TargetCount*2];payload[0]=0x5c;payload[1]=magic.OriginalOpcode.Value;payload[2]=magic.Mode.Value;
                Word(payload,3,magic.MagicId);Word(payload,5,magic.CasterUnitId);Word(payload,7,magic.X);Word(payload,9,magic.Y);
                for(int i=0;i<4;i++)payload[11+i]=unchecked((byte)(magic.ComputedCost>>(i*8)));
                payload[15]=magic.TargetCount;for(int i=0;i<magic.TargetCount;i++)Word(payload,16+i*2,magic.TargetUnitIds[i]);return true;
            }
            if(packet is AttackEffectNotification effect)
            {payload=new byte[8];payload[0]=0x5c;payload[1]=0x82;Word(payload,2,effect.TargetUnitId);Word(payload,4,effect.AttackerUnitId);Word(payload,6,effect.Effect);return true;}
            if(packet is HpSetNotification hp)
            {payload=new byte[6];payload[0]=0x5c;payload[1]=5;Word(payload,2,hp.UnitId);Word(payload,4,hp.AbsoluteHp);return true;}
            if(packet is MpSetNotification mp)
            {payload=new byte[6];payload[0]=0x5c;payload[1]=6;Word(payload,2,mp.UnitId);Word(payload,4,mp.AbsoluteMp);return true;}
            return false; // Unknown payloads remain explicit local typed messages.
        }
        public static bool TryDecodePayload(LocalPacketDirection direction,ReadOnlySpan<byte> payload,out LocalPacket packet,short? localActingUnitId=null)
        {
            packet=null;
            if(direction==LocalPacketDirection.ClientToServer)
            {
                if(payload.Length==3 && payload[0]==0x7d && (ReadWord(payload,1)==1 || ReadWord(payload,1)==2)){packet=new Request_WorldWhisperSetting(ReadWord(payload,1)==1);return true;}
                if(payload.Length==7 && payload[0]==0x4d){packet=new Request_HireMercenary((ushort)ReadWord(payload,1),(ushort)ReadWord(payload,3),(ushort)ReadWord(payload,5));return true;}
                if(payload.Length==5 && payload[0]==0x4e){packet=new Request_DismissMercenary((ushort)ReadWord(payload,1),(ushort)ReadWord(payload,3));return true;}
                if(payload.Length==5 && payload[0]==0x42 && (ReadWord(payload,3)==1 || ReadWord(payload,3)==2)){packet=new Request_WorldRosterFlag(unchecked((ushort)ReadWord(payload,1)),(ushort)ReadWord(payload,3));return true;}
                if(payload.Length==5 && payload[0]==0x3b && unchecked((ushort)ReadWord(payload,1))<10 && unchecked((ushort)ReadWord(payload,3))<10){packet=new Request_WorldRosterOrder((ushort)ReadWord(payload,1),(ushort)ReadWord(payload,3));return true;}
                if(payload.Length==5 && (payload[0]==0x83 || payload[0]==0x85)){packet=new CharacterMagicListRequest((CharacterMagicListCommand)payload[0],unchecked((ushort)ReadWord(payload,1)),unchecked((ushort)ReadWord(payload,3)));return true;}
                if(payload.Length==3 && payload[0]==0x86){packet=new CharacterAdjustmentQueryRequest(unchecked((ushort)ReadWord(payload,1)));return true;}
                if(payload.Length==3 && (payload[0]==0x88 || payload[0]==0x89 || payload[0]==0x8a)){packet=new CharacterAdjustmentCommandRequest((CharacterAdjustmentCommand)payload[0],unchecked((ushort)ReadWord(payload,1)));return true;}
                if(payload.Length==7 && payload[0]==0x82 && unchecked((ushort)ReadWord(payload,3))<6 && (ReadWord(payload,5)==1 || ReadWord(payload,5)==2)){packet=new CharacterStatAdjustmentRequest(unchecked((ushort)ReadWord(payload,1)),ReadWord(payload,3),(CharacterStatAdjustmentDirection)ReadWord(payload,5));return true;}
                if(payload.Length==7 && payload[0]==0x84){packet=new CharacterMagicSelectionRequest(unchecked((ushort)ReadWord(payload,1)),unchecked((ushort)ReadWord(payload,3)),unchecked((ushort)ReadWord(payload,5)));return true;}
                if(payload.Length==5 && payload[0]==0x81){packet=new CharacterAdjustmentClassRequest(unchecked((ushort)ReadWord(payload,1)),unchecked((ushort)ReadWord(payload,3)));return true;}
                if(OriginalWorldTextCodec.TryDecode(payload,out var text)){packet=text;return true;}
                if(payload.Length==11 && payload[0]==0x40 && unchecked((ushort)ReadWord(payload,3))<=6)
                {packet=new EquipmentRequest(unchecked((ushort)ReadWord(payload,1)),unchecked((ushort)ReadWord(payload,3)),unchecked((ushort)ReadWord(payload,5)),unchecked((ushort)ReadWord(payload,7)),unchecked((ushort)ReadWord(payload,9)));return true;}
            }
            if(payload.Length<2 || payload[0]!=0x5c)return false;
            byte op=payload[1];
            if(direction==LocalPacketDirection.ClientToServer)
            {
                if(op==0x82 && payload.Length==10){packet=new AttackRequest(ReadWord(payload,2),ReadWord(payload,4),ReadWord(payload,6),ReadWord(payload,8));return true;}
                if((op==0x89 || op==0x92) && payload.Length>=16 && payload.Length==16+payload[15]*2)
                {
                    var targets=new short[payload[15]];for(int i=0;i<targets.Length;i++)targets[i]=ReadWord(payload,16+i*2);
                    int cost=payload[11]|payload[12]<<8|payload[13]<<16|payload[14]<<24;
                    packet=new MagicRequest(ReadWord(payload,3),ReadWord(payload,5),ReadWord(payload,7),ReadWord(payload,9),cost,targets,op,payload[2],localActingUnitId);return true;
                }
            }
            else if(direction==LocalPacketDirection.ServerToClient)
            {
                if(op==0x82 && payload.Length==8){packet=new AttackEffectNotification(ReadWord(payload,2),ReadWord(payload,4),ReadWord(payload,6));return true;}
                if(op==5 && payload.Length==6){packet=new HpSetNotification(ReadWord(payload,2),ReadWord(payload,4));return true;}
                if(op==6 && payload.Length==6){packet=new MpSetNotification(ReadWord(payload,2),ReadWord(payload,4));return true;}
            }
            return false;
        }
    }
}
