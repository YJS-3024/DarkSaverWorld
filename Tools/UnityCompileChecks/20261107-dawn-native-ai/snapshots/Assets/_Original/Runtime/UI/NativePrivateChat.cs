using System;
using System.Collections.Generic;
using System.Linq;
namespace DarkSaver.Original.UI
{
    // Decoded CString input to Saver476230, plus explicitly supplied native client
    // gates. This is not a newly inferred network envelope or room-number alias.
    public sealed class WorldPrivateMessageNotice
    {
        public string id,character,session,text;public bool approved;
        public int? nativeSuppression,nativeRoomNumber;
    }
    public sealed class WorldPrivateTextRequest
    {
        public string id,character,session,thread,recipient,text;public byte[] packet;
        public WorldPrivateTextRequest Copy()=>new WorldPrivateTextRequest{id=id,character=character,session=session,thread=thread,recipient=recipient,text=text,packet=packet?.Clone()as byte[]};
    }
    public sealed class NativePrivateChatView
    {
        public string id,sender;public int x,y,first,count;public long revision;
        public NativeChatLine[] lines;
    }
    public sealed class NativePrivateChatInbox
    {
        private sealed class Thread
        {
            public string Id,Sender;public int Y;public long Revision;
            public readonly NativeWorldChatLog Log=new NativeWorldChatLog(177,6);
        }
        private readonly List<Thread> threads=new List<Thread>();
        public long Revision {get;private set;}
        public void Clear(){threads.Clear();Revision++;}
        public NativePrivateChatView[] Views()=>threads.Select(t=>new NativePrivateChatView{id=t.Id,sender=t.Sender,x=442,y=t.Y,first=t.Log.First,count=t.Log.Count,revision=t.Revision,lines=VisibleLines(t.Log.Lines,t.Log.First)}).ToArray();
        public static NativeChatLine[] VisibleLines(NativeChatLine[] rows,int first)
        {
            if(rows==null||first<0||first>=rows.Length)return Array.Empty<NativeChatLine>();var result=new List<NativeChatLine>();char marker='\0';
            // Native422500 scans up to two preceding rows for the current marker,
            // rather than carrying a color from the beginning of the entire log.
            // If none is found, native reads an uninitialized stack byte. The
            // managed display adapter chooses the non-'>' cyan branch explicitly.
            for(int index=Math.Max(0,first-2);index<rows.Length&&index<first+6;index++)
            {
                string text=rows[index].Text;if(text.StartsWith(">",StringComparison.Ordinal))marker='>';else if(text.StartsWith("<",StringComparison.Ordinal))marker='<';
                if(index>=first)result.Add(new NativeChatLine(text,marker=='>'?1:4));
            }
            return result.ToArray();
        }
        public string Recipient(string id)=>threads.FirstOrDefault(t=>t.Id==id)?.Sender;
        public static bool TrySplit(string text,NativeCodePage949 codec,out string sender,out string body)
        {
            sender=body=null;if(codec==null||!codec.TryEncode(text,out var bytes))return false;
            int prefix=Math.Min(12,bytes.Length);
            return codec.TryCString(bytes,0,prefix,out sender)&&codec.TryCString(bytes,prefix,bytes.Length-prefix,out body);
        }
        public bool Receive(string text,int suppression,int room,NativeCodePage949 codec)
        {
            if(suppression!=0)return true;
            if(!TrySplit(text,codec,out string sender,out string body)||!NativeWorldChatLog.TryLines(body,codec,177,out _))return false;
            var thread=threads.FirstOrDefault(t=>t.Sender==sender);
            if(thread==null)
            {
                if(room==2500)return true;
                thread=new Thread{Id=Guid.NewGuid().ToString("N"),Sender=sender,Y=threads.Count*24};threads.Add(thread);
            }
            thread.Log.Append(body,4,codec);
            thread.Revision++;Revision++;return true;
        }
        public bool Close(string id)
        {int at=threads.FindIndex(t=>t.Id==id);if(at<0)return false;threads.RemoveAt(at);Revision++;return true;}
        public bool Scroll(string id,int direction)
        {var thread=threads.FirstOrDefault(t=>t.Id==id);if(thread==null||direction==0)return false;thread.Log.Scroll(direction);thread.Revision++;Revision++;return true;}
        // 4537A0: trailing ASCII spaces only; BYTE recipient length, then both
        // CStrings with no delimiter. strlen stops if that byte is0.
        public static byte[] Reply(byte[] recipient,byte[] text)
        {
            if(recipient==null||text==null||text.Length==0||recipient.Length>12||Array.IndexOf(recipient,(byte)0)>=0||Array.IndexOf(text,(byte)0)>=0)return null;
            int length=recipient.Length;while(length>0&&recipient[length-1]==32)length--;
            if(length==0)return new byte[]{0x7a,9};
            var packet=new byte[3+length+text.Length];packet[0]=0x7a;packet[1]=9;packet[2]=(byte)length;Buffer.BlockCopy(recipient,0,packet,3,length);Buffer.BlockCopy(text,0,packet,3+length,text.Length);return packet;
        }
    }
}
