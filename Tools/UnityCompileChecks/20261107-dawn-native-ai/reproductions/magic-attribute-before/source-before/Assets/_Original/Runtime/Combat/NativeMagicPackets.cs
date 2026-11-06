using System;
using System.Linq;

namespace DarkSaver.Original.World
{
    public sealed class NativeMagicPacket
    {
        public int magic,source,x,y,mode;
        public int item=-1,itemToken=-1;
        public uint signature;
        public int[] targets;
    }
    public static class OriginalNativeMagic
    {
        private static int Word(byte[] b,int at)=>(short)(b[at]|b[at+1]<<8);
        private static bool WordValue(int value)=>value>=short.MinValue&&value<=short.MaxValue;
        // GameCtl EB10 -> DF10: both commands carry one signed unit WORD and
        // request magic4001 in mode1. They do not themselves set XP or a level.
        public static bool TryLevelEffectNotice(byte[] bytes,out int unit)
        {
            unit=-1;if(bytes==null||bytes.Length!=4||bytes[0]!=0x5c||(bytes[1]!=0x64&&bytes[1]!=0x65))return false;
            unit=Word(bytes,2);return true;
        }
        public static NativeMagicPacket LevelEffectRequest(int source,MagicMenuEntry metadata,TrainingUnit[] units,int[] permitted)
        {
            if(source<0||metadata?.id!=4001||metadata.distance!=0||metadata.range!=8||metadata.unit!=3||units==null||permitted==null)return null;
            var actor=units.FirstOrDefault(u=>u?.id==source);if(actor==null)return null;
            // Eligibility is supplied by the authority. ID ranges/palette colors
            // cannot replace the original alliance/team relation calculation.
            var targets=units.Where(u=>u!=null&&u.nativeActive==true&&u.nativeEnabled==true&&permitted.Contains(u.id)
                &&BattleMagicArea.Weight(actor.x,actor.y,u.x,u.y,metadata.range)>0).OrderBy(u=>u.x).ThenBy(u=>u.y).Select(u=>u.id).ToArray();
            if(targets.Length==0||targets.Length>255)return null;
            return new NativeMagicPacket{magic=4001,source=source,x=actor.x,y=actor.y,mode=1,signature=Signature(metadata),targets=targets};
        }
        public static uint Signature(MagicMenuEntry row)=>unchecked((uint)(row.id*(row.pattern+row.unit+row.team+row.range+row.distance)));
        public static bool? Dispatches(NativeMagicPacket packet,int? localTeam,int? sourceTeam,int opcode=0x89)
        {
            if(packet==null||packet.targets==null||(opcode!=0x89&&opcode!=0x67))return null;
            if(packet.targets.Length==0||packet.magic<0)return false;
            // GameCtl DB20 (67) has no DA10 (89) same-team echo check.
            if(opcode==0x67||packet.source<0)return true;
            return localTeam.HasValue&&sourceTeam.HasValue?(bool?)(localTeam.Value!=sourceTeam.Value):null;
        }
        public static byte[] Request(NativeMagicPacket packet)
        {
            if(packet==null||packet.item<0&&(packet.mode<1||packet.mode>2)||packet.magic<0||!WordValue(packet.magic)||!WordValue(packet.source)||!WordValue(packet.x)||!WordValue(packet.y)
                ||packet.targets==null||packet.targets.Length>255||packet.targets.Any(t=>!WordValue(t)))return null;
            if(packet.item>=0)
            {
                if(packet.item<1||packet.item>65535||packet.itemToken<0||packet.itemToken>65535)return null;
                var item=new byte[19+packet.targets.Length*2];item[0]=0x5c;item[1]=0x91;Put(item,2,packet.item);Put(item,4,packet.itemToken);Put(item,6,packet.magic);Put(item,8,packet.source);Put(item,10,packet.x);Put(item,12,packet.y);
                for(int i=0;i<4;i++)item[14+i]=(byte)(packet.signature>>(8*i));item[18]=(byte)packet.targets.Length;
                for(int i=0;i<packet.targets.Length;i++)Put(item,19+2*i,packet.targets[i]);return item;
            }
            var bytes=new byte[16+packet.targets.Length*2];bytes[0]=0x5c;bytes[1]=(byte)(packet.magic<10000?0x89:0x92);bytes[2]=(byte)packet.mode;
            Put(bytes,3,packet.magic);Put(bytes,5,packet.source);Put(bytes,7,packet.x);Put(bytes,9,packet.y);
            for(int i=0;i<4;i++)bytes[11+i]=(byte)(packet.signature>>(8*i));bytes[15]=(byte)packet.targets.Length;
            for(int i=0;i<packet.targets.Length;i++)Put(bytes,16+2*i,packet.targets[i]);return bytes;
        }
        public static bool TryRequest(byte[] bytes,out NativeMagicPacket packet)
        {
            packet=null;if(bytes==null||bytes.Length<16||bytes[0]!=0x5c)return false;
            if(bytes[1]==0x91)
            {
                if(bytes.Length<19||bytes.Length!=19+2*bytes[18]||Word(bytes,6)<0)return false;
                packet=new NativeMagicPacket{item=bytes[2]|bytes[3]<<8,itemToken=bytes[4]|bytes[5]<<8,magic=Word(bytes,6),source=Word(bytes,8),x=Word(bytes,10),y=Word(bytes,12),signature=(uint)(bytes[14]|bytes[15]<<8|bytes[16]<<16|bytes[17]<<24),targets=Enumerable.Range(0,bytes[18]).Select(i=>Word(bytes,19+2*i)).ToArray()};return packet.item>0;
            }
            if(bytes[1]!=0x89&&bytes[1]!=0x92||bytes[2]<1||bytes[2]>2||bytes.Length!=16+2*bytes[15])return false;
            int magic=Word(bytes,3);if(magic<0||bytes[1]!=(magic<10000?0x89:0x92))return false;
            packet=new NativeMagicPacket{magic=magic,source=Word(bytes,5),x=Word(bytes,7),y=Word(bytes,9),mode=bytes[2],signature=(uint)(bytes[11]|bytes[12]<<8|bytes[13]<<16|bytes[14]<<24),targets=Enumerable.Range(0,bytes[15]).Select(i=>Word(bytes,16+2*i)).ToArray()};return true;
        }
        public static bool TryNotice(byte[] bytes,out NativeMagicPacket packet)
        {
            packet=null;if(bytes==null||bytes.Length<11||bytes[0]!=0x5c||(bytes[1]!=0x89&&bytes[1]!=0x67)||bytes.Length!=11+2*bytes[10])return false;
            packet=new NativeMagicPacket{magic=Word(bytes,2),source=Word(bytes,4),x=Word(bytes,6),y=Word(bytes,8),targets=Enumerable.Range(0,bytes[10]).Select(i=>Word(bytes,11+2*i)).ToArray()};return true;
        }
        public static byte[] Notice(NativeMagicPacket packet,int opcode=0x89)
        {
            if((opcode!=0x89&&opcode!=0x67)||packet==null||!WordValue(packet.magic)||!WordValue(packet.source)||!WordValue(packet.x)||!WordValue(packet.y)||packet.targets==null||packet.targets.Length>255||packet.targets.Any(t=>!WordValue(t)))return null;
            var bytes=new byte[11+2*packet.targets.Length];bytes[0]=0x5c;bytes[1]=(byte)opcode;Put(bytes,2,packet.magic);Put(bytes,4,packet.source);Put(bytes,6,packet.x);Put(bytes,8,packet.y);bytes[10]=(byte)packet.targets.Length;
            for(int i=0;i<packet.targets.Length;i++)Put(bytes,11+2*i,packet.targets[i]);return bytes;
        }
        private static void Put(byte[] b,int at,int value){b[at]=(byte)value;b[at+1]=(byte)(value>>8);}
    }
}
