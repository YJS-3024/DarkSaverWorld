using System;
namespace DarkSaver.Original.UI
{
    public sealed class WorldClientTextRequest
    {
        public string id,character,session,command;public byte[] packet;
        public WorldClientTextRequest Copy()=>new WorldClientTextRequest{id=id,character=character,session=session,command=command,packet=packet?.Clone()as byte[]};
    }
    public static class NativeWorldTextPackets
    {
        // Saver429F70 uses the low byte of CString's byte length, including its256-byte wrap.
        public static byte[] Build(byte[] encoded,int channel)
        {
            if(encoded==null)return null;int count=encoded.Length&255;if(count==0)return null;
            var packet=new byte[count+3];packet[0]=0x7a;packet[1]=unchecked((byte)channel);packet[2]=(byte)count;
            Buffer.BlockCopy(encoded,0,packet,3,count);return packet;
        }
        public static byte[] SpaceMove()=>Build(new byte[]{0x2f,0xb0,0xf8,0xb0,0xa3,0xc0,0xcc,0xb5,0xbf},1);
    }
}
