using System;
using System.Collections.Generic;
using System.Linq;
namespace DarkSaver.Original.UI
{
    public sealed class WorldMessageNotice
    {
        public string id,character,session,text;public int kind;public bool approved;
        public WorldMessageNotice Copy()=>(WorldMessageNotice)MemberwiseClone();
    }
    public readonly struct NativeChatLine
    {
        public readonly string Text;public readonly int Kind;
        public NativeChatLine(string text,int kind){Text=text;Kind=kind;}
    }
    // Saver421CD0/436B00/43FDF0: fixed-font lines, not a Unicode word-wrapped chat widget.
    public sealed class NativeWorldChatLog
    {
        public const int Capacity=300,NormalWidth=545,ExpandedWidth=545,LineHeight=14;
        private readonly List<NativeChatLine> lines=new List<NativeChatLine>();
        private readonly int width,initialVisible;
        public NativeWorldChatLog(int width=NormalWidth,int visible=5)
        {if(width<1||visible<1)throw new ArgumentOutOfRangeException();this.width=width;initialVisible=visible;VisibleLines=visible;}
        public int First {get;private set;}
        public int VisibleLines {get;private set;}=5;
        public int Count=>lines.Count;
        public NativeChatLine[] Lines=>lines.ToArray();
        public void Clear(){lines.Clear();First=0;VisibleLines=initialVisible;}
        public void SetExpanded(bool expanded)
        {int count=expanded?23:5;if(VisibleLines==count)return;VisibleLines=count;First=Math.Max(0,lines.Count-VisibleLines);}
        private void Clamp(){First=Math.Max(0,Math.Min(First,Math.Max(0,lines.Count-VisibleLines)));}
        public void Scroll(int direction){if(direction<0)First=Math.Max(0,First-1);else if(direction>0)First=Math.Min(Math.Max(0,lines.Count-VisibleLines),First+1);}
        public NativeChatLine[] Visible()=>lines.Skip(First).Take(VisibleLines).ToArray();
        public static bool IsMainKind(int kind)=>kind>=1&&kind<=5;
        // COLORREF literals passed to DDColorMatch in421CD0; return portable RGB, not device palette indices.
        public static int Rgb(int kind)=>kind==1?0xffffff:kind==2?0xffff40:kind==3?0x00ff00:kind==4?0x00ffff:kind==5?0xc0c0c0:0;
        public bool Append(string text,int kind,NativeCodePage949 codec)
        {
            if(!IsMainKind(kind)||!TryLines(text,codec,width,out var added))return false;
            foreach(string line in added)lines.Add(new NativeChatLine(line,kind));
            int trimmed=Math.Max(0,lines.Count-Capacity);if(trimmed>0)lines.RemoveRange(0,trimmed);
            // 453EE0 calls437BA0 once for every inserted row, even while the user is scrolled up.
            for(int i=0;i<Math.Max(0,added.Length-trimmed);i++)Scroll(1);
            Clamp();return true;
        }
        public static bool TryLines(string text,NativeCodePage949 codec,int width,out string[] result)
        {
            result=null;if(width<1||codec==null||!codec.TryEncode(text,out byte[] bytes))return false;
            var rows=new List<string>();int offset=0;
            while(Width(bytes,offset)>width)
            {
                int split=Split(bytes,offset,width);
                // Native43FDF0 returns two empty strings if even the first character reaches the limit.
                if(split==offset){rows.Add("");offset=bytes.Length;break;}
                if(!codec.TryCString(bytes,offset,split-offset,out string row))return false;rows.Add(row);offset=split;
            }
            if(!codec.TryCString(bytes,offset,bytes.Length-offset,out string last))return false;rows.Add(last);result=rows.ToArray();return true;
        }
        private static int Width(byte[] bytes,int start)
        {int width=0;for(int i=start;i<bytes.Length;){int advance=Advance(bytes,ref i);width=advance<0?0:width+advance;}return width;}
        private static int Split(byte[] bytes,int start,int maximum)
        {
            int width=0;for(int i=start;i<bytes.Length;){int before=i,advance=Advance(bytes,ref i);width=advance<0?0:width+advance;if(width>=maximum)return before;}
            return bytes.Length;
        }
        private static int Advance(byte[] bytes,ref int at)
        {
            int first=bytes[at++];if(first==10||first==13)return -1;if(first==32)return 5;
            if(first<128)return 7;
            int second=bytes[at++];return (first==0xa4||first>=0xb0&&first<0xc9)&&second>0x9f?14:7;
        }
    }
}
