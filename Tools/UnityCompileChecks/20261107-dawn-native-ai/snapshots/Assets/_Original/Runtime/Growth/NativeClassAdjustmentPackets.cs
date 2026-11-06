namespace DarkSaver.Original.World
{
    // Late Saver461DD0/462000/462900/462980/462A00/462B10/4623A0.
    // The record is the received NativeCommander+4 WORD, never a job or Unity roster index.
    public static class NativeClassAdjustmentPackets
    {
        private static bool Record(int? record)=>record.HasValue&&record>=0&&record<65535;
        private static byte[] Packet(int opcode,params int[] words)
        {var data=new byte[1+2*words.Length];data[0]=(byte)opcode;for(int i=0;i<words.Length;i++){data[1+i*2]=(byte)words[i];data[2+i*2]=(byte)(words[i]>>8);}return data;}
        private static int SignedWord(byte[] packet,int offset)=>(short)(packet[offset]|packet[offset+1]<<8);
        public static bool TryReadCosts(byte[] packet,out int[] costs)
        {costs=null;if(packet?.Length!=13||packet[0]!=0x8b)return false;costs=new int[6];for(int i=0;i<6;i++)costs[i]=SignedWord(packet,1+i*2);return true;}
        public static bool TryReadOptions(byte[] packet,out NativeClassOptionsPayload options)
        {
            options=null;if(packet==null||packet.Length<9||packet[0]!=0x87)return false;int count=SignedWord(packet,7);
            if(count<0||count>10||packet.Length!=9+count*2)return false;
            var result=new NativeClassOptionsPayload{opaqueHeader=(ushort)(packet[1]|packet[2]<<8),right=SignedWord(packet,3),left=SignedWord(packet,5),choices=new int[count]};
            for(int i=0;i<count;i++)result.choices[i]=SignedWord(packet,9+i*2);options=result;return true;
        }
        public static byte[] Costs(int? record)=>Record(record)?Packet(0x8a,record.Value):null;
        public static byte[] Options(int? record,int? classPoints)=>Record(record)&&classPoints.HasValue&&classPoints!=0?Packet(0x86,record.Value):null;
        public static byte[] Close(int? record,bool confirm)=>Record(record)?Packet(confirm?0x88:0x89,record.Value):null;
        public static byte[] Change(int? record,int job)=>Record(record)&&job>=0&&job<65535?Packet(0x81,record.Value,job):null;
        public static byte[] Stat(int? record,int stat,int direction)=>Record(record)&&stat>=0&&stat<6&&(direction==1||direction==2)?Packet(0x82,record.Value,stat,direction):null;
        // Native462000 writes mode first:1 remove,2 add; the second WORD is the record.
        public static byte[] DragMagic(int? record,int magic,bool fromAvailable)=>Record(record)&&magic>0&&magic<65535?Packet(fromAvailable?0x83:0x85,record.Value,magic):null;
        public static byte[] Magic(int? record,int magic,bool add)=>Record(record)&&magic>0&&magic<65535?Packet(0x84,add?2:1,record.Value,magic):null;
    }
    public sealed class NativeClassOptionsPayload
    {
        //4624D0 reads and ignores the first WORD. Its meaning is deliberately not assigned.
        public ushort opaqueHeader;public int right,left;public int[] choices;
        public int DefaultSlot=>right!=-1?1:choices?.Length>0?2:left!=-1?0:-1;
        public NativeClassOptionsPayload Copy(){var r=(NativeClassOptionsPayload)MemberwiseClone();r.choices=choices==null?null:(int[])choices.Clone();return r;}
    }
    public sealed class NativeClassClientView
    {
        public string token;public long costRevision,optionsRevision,magicRevision;public NativeClassMagicState magic;public int[] costs;public NativeClassOptionsPayload options;
        public NativeClassClientView Copy()=>new NativeClassClientView{token=token,costRevision=costRevision,optionsRevision=optionsRevision,magicRevision=magicRevision,magic=magic?.Copy(),costs=costs==null?null:(int[])costs.Clone(),options=options?.Copy()};
    }
}
