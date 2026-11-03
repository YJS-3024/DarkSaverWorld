using System;
using System.Collections.Generic;
using System.IO;
namespace DarkSaver.Original.UI
{
    public sealed class NativeMemoTransferNotice
    {
        public string id,character,session;public bool approved;
        // Complete7E body after opcode demultiplexing: signed lengths, raw CRC32, PKWARE DCL data.
        public byte[] body;
    }
    public static class NativeMemoTransfer
    {
        public const int MaximumCompressed=8192,MaximumDecoded=32767;
        public static bool TryDecode(byte[] body,out byte[] payload)=>TryDecode(body,false,out payload);
        //415BA0 masks candidate-cache WORD lengths to unsigned;427D30 memo sign-extends them.
        internal static bool TryDecodeCandidate(byte[] body,out byte[] payload)=>TryDecode(body,true,out payload);
        private static bool TryDecode(byte[] body,bool unsignedLengths,out byte[] payload)
        {
            payload=null;if(body==null||body.Length<8)return false;
            int compressed=body[0]|body[1]<<8,decoded=body[2]|body[3]<<8;
            if(!unsignedLengths){compressed=(short)compressed;decoded=(short)decoded;}
            if(compressed<=0||compressed>MaximumCompressed||decoded<=0||decoded>(unsignedLengths?ushort.MaxValue:MaximumDecoded)||body.Length!=8+compressed)return false;
            uint expected=(uint)(body[4]|body[5]<<8|body[6]<<16|body[7]<<24);
            try
            {
                var bits=new BitReader(body,8);int literals=bits.Read(8),dictionary=bits.Read(8);
                if(literals>1||dictionary<4||dictionary>6)return false;
                var output=new byte[decoded];int written=0;
                while(true)
                {
                    if(bits.Read(1)==0)
                    {
                        if(written==output.Length)return false;
                        output[written++]=(byte)(literals==0?bits.Read(8):Literal.Read(bits));continue;
                    }
                    int symbol=Length.Read(bits),count=LengthBase[symbol]+bits.Read(LengthExtra[symbol])+2;
                    if(count==519)break;
                    int low=count==2?2:dictionary,distance=(Distance.Read(bits)<<low)+bits.Read(low)+1;
                    if(distance>written||count>output.Length-written)return false;
                    for(int i=0;i<count;i++){output[written]=output[written-distance];written++;}
                }
                if(written!=decoded||Checksum(output)!=expected)return false;payload=output;return true;
            }
            catch(InvalidDataException){return false;}
        }
        // Native477E10 starts atFFFFFFFF and returns its running IEEE CRC without a final XOR.
        public static uint Checksum(byte[] bytes)
        {
            uint value=uint.MaxValue;foreach(byte b in bytes){value^=b;for(int bit=0;bit<8;bit++)value=(value>>1)^((value&1)!=0?0xedb88320u:0u);}return value;
        }
        private sealed class BitReader
        {
            private readonly byte[] data;private int cursor,remaining;private uint buffer;
            public BitReader(byte[] data,int start){this.data=data;cursor=start;}
            public int Read(int count)
            {
                while(remaining<count){if(cursor>=data.Length)throw new InvalidDataException();buffer|=(uint)data[cursor++]<<remaining;remaining+=8;}
                int value=(int)(buffer&((1u<<count)-1));buffer>>=count;remaining-=count;return value;
            }
        }
        private sealed class Huffman
        {
            private readonly Dictionary<int,int> symbols=new Dictionary<int,int>();private readonly int maximum;
            public Huffman(byte[] lengths)
            {
                var counts=new int[17];foreach(int length in lengths){counts[length]++;maximum=Math.Max(maximum,length);}var next=new int[17];int code=0;
                for(int length=1;length<=maximum;length++){code=(code+counts[length-1])<<1;next[length]=code;}
                for(int symbol=0;symbol<lengths.Length;symbol++)
                {
                    int length=lengths[symbol],canonical=next[length]++,wire=0;for(int bit=0;bit<length;bit++)wire|=((~canonical>>(length-1-bit))&1)<<bit;
                    symbols.Add(length<<16|wire,symbol);
                }
            }
            public int Read(BitReader bits)
            {int wire=0;for(int length=1;length<=maximum;length++){wire|=bits.Read(1)<<(length-1);if(symbols.TryGetValue(length<<16|wire,out int symbol))return symbol;}throw new InvalidDataException();}
        }
        private static readonly byte[] LiteralBits={11,12,12,12,12,12,12,12,12,8,7,12,12,7,12,12,12,12,12,12,12,12,12,12,12,12,13,12,12,12,12,12,4,10,8,12,10,12,10,8,7,7,8,9,7,6,7,8,7,6,7,7,7,7,8,7,7,8,8,12,11,7,9,11,12,6,7,6,6,5,7,8,8,6,11,9,6,7,6,6,7,11,6,6,6,7,9,8,9,9,11,8,11,9,12,8,12,5,6,6,6,5,6,6,6,5,11,7,5,6,5,5,6,10,5,5,5,5,8,7,8,8,10,11,11,12,12,12,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,13,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,13,12,13,13,13,12,13,13,13,12,13,13,13,13,12,13,13,13,12,12,12,13,13,13,13,13,13,13,13,13,13,13};
        private static readonly byte[] LengthBits={3,2,3,3,4,4,4,5,5,5,5,6,6,6,7,7};
        private static readonly byte[] DistanceBits={2,4,4,5,5,5,5,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8};
        private static readonly byte[] LengthExtra={0,0,0,0,0,0,0,0,1,2,3,4,5,6,7,8};
        private static readonly ushort[] LengthBase={0,1,2,3,4,5,6,7,8,10,14,22,38,70,134,262};
        private static readonly Huffman Literal=new Huffman(LiteralBits),Length=new Huffman(LengthBits),Distance=new Huffman(DistanceBits);
    }
}
