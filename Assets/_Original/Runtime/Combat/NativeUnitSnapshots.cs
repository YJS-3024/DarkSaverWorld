using System;
using System.Linq;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    // Client unitinfo values, not a character-growth profile or a native pointer.
    [Serializable,DataContract] public sealed class NativeUnitState
    {
        [DataMember] public int[] fields;
        [DataMember] public byte[] name,className;
        public NativeUnitState Copy()=>new NativeUnitState{fields=fields?.ToArray(),name=name?.ToArray(),className=className?.ToArray()};
        public bool Valid=>fields?.Length==44&&fields[41]==0&&fields[42]==0&&name!=null&&name.Length<=255&&className!=null&&className.Length<=255;
        public int Level=>fields[3];public uint Experience=>unchecked((uint)fields[4]);public uint NextExperience=>unchecked((uint)fields[5]);
        public int Hp=>fields[8];public int MaxHp=>fields[7];public int Mp=>fields[10];public int MaxMp=>fields[9];
        public int Attack=>fields[11];public int Defense=>fields[12];
        public int[] Slots(bool skills)=>fields.Skip(skills?33:25).Take(8).TakeWhile(id=>id!=-1).ToArray();
        public void ExperienceSet(uint value)=>fields[4]=unchecked((int)value);
    }
    public sealed class NativeUnitPacket
    {
        public int kind;public short[] words;public uint[] values;public byte[] className,name;
        public int Unit=>words[0];
        public NativeUnitState State()
        {
            var f=new int[44];f[16]=50;
            int[] pairs=kind==0x50?new[]{0,0,1,2,2,3,6,4,7,6,8,7,11,10,12,11,13,12,18,14,19,15,22,8,23,9,24,1,43,13}
                :new[]{0,0,1,2,2,3,3,4,6,5,7,7,8,8,9,9,10,10,11,16,12,17,13,18,14,12,15,13,17,11,18,20,19,21,22,14,23,15,24,1,43,19};
            for(int i=0;i<pairs.Length;i+=2)f[pairs[i]]=words[pairs[i+1]];
            for(int i=0;i<16;i++)f[25+i]=kind==0x50?-1:words[22+i];
            if(kind==0x51){f[4]=unchecked((int)values[1]);f[5]=unchecked((int)values[0]);}
            int flags=kind==0x50?0:2;f[20]=unchecked((int)values[flags]);f[21]=unchecked((int)values[flags+1]);
            return new NativeUnitState{fields=f,className=className.ToArray(),name=name.ToArray()};
        }
    }
    public static class OriginalNativeUnits
    {
        public static bool TryDecode(byte[] packet,out NativeUnitPacket result)
        {
            result=null;if(packet==null||packet.Length<2||packet[0]!=0x5c||(packet[1]!=0x50&&packet[1]!=0x51))return false;
            bool full=packet[1]==0x51;int wc=full?45:16,dc=full?4:2,at=2+wc*2+dc*4;
            if(packet.Length<=at)return false;
            var words=new short[wc];var values=new uint[dc];
            for(int i=0;i<wc;i++)words[i]=(short)(packet[2+2*i]|packet[3+2*i]<<8);
            if(words[0]<0||words[0]>=3000)return false;
            for(int i=0;i<dc;i++){int p=2+wc*2+4*i;values[i]=(uint)(packet[p]|packet[p+1]<<8|packet[p+2]<<16|packet[p+3]<<24);}
            int count=packet[at++];if(count>packet.Length-at)return false;var label=packet.Skip(at).Take(count).ToArray();at+=count;
            byte[] name=Array.Empty<byte>();if(full){if(at>=packet.Length)return false;count=packet[at++];if(count>packet.Length-at)return false;name=packet.Skip(at).Take(count).ToArray();at+=count;}
            if(at!=packet.Length)return false;
            result=new NativeUnitPacket{kind=packet[1],words=words,values=values,className=label,name=name};return true;
        }
        public static byte[] Packet(NativeUnitPacket data)
        {
            if(data==null||(data.kind!=0x50&&data.kind!=0x51)||data.words?.Length!=(data.kind==0x50?16:45)||data.values?.Length!=(data.kind==0x50?2:4)
                ||data.words[0]<0||data.words[0]>=3000||data.className==null||data.className.Length>255||data.name==null||data.name.Length>255||data.kind==0x50&&data.name.Length!=0)return null;
            int at=2+data.words.Length*2+data.values.Length*4;var bytes=new byte[at+1+data.className.Length+(data.kind==0x51?1+data.name.Length:0)];bytes[0]=0x5c;bytes[1]=(byte)data.kind;
            for(int i=0;i<data.words.Length;i++){bytes[2+i*2]=(byte)data.words[i];bytes[3+i*2]=(byte)(data.words[i]>>8);}
            for(int i=0;i<data.values.Length;i++)for(int j=0;j<4;j++)bytes[2+data.words.Length*2+i*4+j]=(byte)(data.values[i]>>(j*8));
            bytes[at++]=(byte)data.className.Length;Array.Copy(data.className,0,bytes,at,data.className.Length);at+=data.className.Length;
            if(data.kind==0x51){bytes[at++]=(byte)data.name.Length;Array.Copy(data.name,0,bytes,at,data.name.Length);}return bytes;
        }
        // UserUnit.MagicFlag C950 permutes the low16 incoming bits into +104.
        public static uint StatusFlags(uint input)
        {
            uint[] map={0x40,0x20,4,0x80,0x10,8,1,0x100,0x200,0x400,0x800,0x1000,0x2000,0x4000,2,0x8000};uint result=0;
            for(int i=0;i<16;i++)if((input&(1u<<i))!=0)result|=map[i];return result;
        }
        public static int MenuMask(uint flags)
        {int result=0;for(int i=0;i<8;i++)if((flags&OriginalBattleTicks.CommandFlags(1<<i))!=0)result|=1<<i;return result;}
    }
    public sealed class NativeUnitDelivery
    {public byte[] packet;public long life;public NativeUnitDelivery Copy()=>new NativeUnitDelivery{packet=packet?.ToArray(),life=life};}
    // Atomic local delivery envelope, not a recovered original TCP batch format.
    public sealed class NativeBattleUnitsNotice
    {
        public string id,character,session;public long expectedRevision;public long? expectedGrowthRevision;public bool approved;
        public NativeUnitDelivery[] packets;
        public NativeBattleUnitsNotice Copy(){var n=(NativeBattleUnitsNotice)MemberwiseClone();n.packets=packets?.Select(p=>p?.Copy()).ToArray();return n;}
    }
}
