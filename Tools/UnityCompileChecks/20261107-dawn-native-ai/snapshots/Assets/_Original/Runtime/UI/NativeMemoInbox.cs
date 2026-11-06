using System;
using System.Collections.Generic;
using System.Linq;
namespace DarkSaver.Original.UI
{
    public sealed class NativeMemo
    {
        public byte id;public string sender,date,body;
        public NativeMemo Copy()=>(NativeMemo)MemberwiseClone();
    }
    public sealed class NativeMemoView
    {public string character,session;public long revision;public int page;public NativeMemo[] rows;}
    public sealed class NativeMemoNotice
    {
        public string id,character,session;public bool approved;
        // The original7E transfer's already validated/decompressed payload, not its compressed wire envelope.
        public byte[] payload;
    }
    public static class NativeMemoPackets
    {
        public static byte[] ViewRequest()=>new byte[]{0x7a,5,9,0x2f,0xb8,0xde,0xb8,0xf0,0xba,0xb8,0xb1,0xe2};
        public static byte[] Delete(byte id)=>new[]{(byte)0x80,id};
        public static bool TryBatch(byte[] payload,NativeCodePage949 codec,out NativeMemo[] rows)
        {
            rows=null;if(payload==null||payload.Length==0||codec==null)return false;int cursor=1;var found=new List<NativeMemo>();
            for(int i=0;i<payload[0];i++)
            {
                if(cursor>payload.Length-13)return false;byte id=payload[cursor++];
                if(!codec.TryCString(payload,cursor,12,out string sender))return false;cursor+=12;
                if(!ReadText(payload,codec,ref cursor,31,out string date)||!ReadText(payload,codec,ref cursor,99,out string body))return false;
                found.Add(new NativeMemo{id=id,sender=sender,date=date,body=body});
            }
            // Trailing data remains an outer transfer concern; native43C690 consumes exactly the declared records.
            rows=found.ToArray();return true;
        }
        private static bool ReadText(byte[] bytes,NativeCodePage949 codec,ref int cursor,int maximum,out string text)
        {
            text=null;if(cursor>bytes.Length-2)return false;int count=bytes[cursor]|bytes[cursor+1]<<8;cursor+=2;
            if(count>maximum||cursor>bytes.Length-count||!codec.TryCString(bytes,cursor,count,out text))return false;cursor+=count;return true;
        }
    }
    public sealed class NativeMemoInbox
    {
        private readonly List<NativeMemo> rows=new List<NativeMemo>();
        public long Revision {get;private set;}
        public bool NeedsQuery {get;private set;}=true;
        public int Page {get;private set;}
        public NativeMemo[] Rows=>rows.Select(r=>r.Copy()).ToArray();
        public int Count=>rows.Count;
        public bool Query(){if(!NeedsQuery)return false;NeedsQuery=false;return true;}
        public void InvalidateQuery(){NeedsQuery=true;}
        public bool SetPage(int page)
        {if(page<0||page>1||page==1&&rows.Count<=10)return false;Page=page;return true;}
        public bool Append(byte[] payload,NativeCodePage949 codec)
        {
            if(Revision==long.MaxValue||!NativeMemoPackets.TryBatch(payload,codec,out var values))return false;
            rows.AddRange(values);Revision++;return true;
        }
        public bool Delete(int row,long revision,byte id,out byte[] packet)
        {
            packet=null;int index=Page*10+row;
            if(row<0||row>=10||revision!=Revision||Revision==long.MaxValue||index>=rows.Count||rows[index].id!=id)return false;
            packet=NativeMemoPackets.Delete(id);rows.RemoveAt(index);Revision++;return true;
        }
    }
}
