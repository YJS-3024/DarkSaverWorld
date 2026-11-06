using System;
using System.Collections.Generic;
using System.Globalization;
namespace DarkSaver.Original.UI
{
    public sealed class NativeCandidateData
    {
        public byte[] header;public string[] terms,windowClasses;
        public NativeCandidateData Copy()=>new NativeCandidateData{header=(byte[])header.Clone(),terms=(string[])terms.Clone(),windowClasses=(string[])windowClasses.Clone()};
        public int CapturedWindowClassMatchCount(string value)
        {int count=0;if(value!=null&&windowClasses!=null)foreach(string name in windowClasses)if(string.Equals(name,value,StringComparison.Ordinal))count++;return count;}
        public static bool TryCompressed(byte[] compressed,NativeCodePage949 codec,out NativeCandidateData result)
        {result=null;return NativeMemoTransfer.TryDecodeCandidate(compressed,out var bytes)&&TryParse(bytes,codec,out result);}
        //4161E0 uses byte offsets. Preserve CR/LF inside comma-delimited terms;
        //the separate+188 list is not silently applied as another chat filter.
        public static bool TryParse(byte[] bytes,NativeCodePage949 codec,out NativeCandidateData result)
        {
            result=null;if(bytes==null||codec==null||bytes.Length<8)return false;
            int end=Array.IndexOf(bytes,(byte)0);if(end<0)end=bytes.Length;if(end<8)return false;
            int cursor=7;var terms=new List<string>();
            while(true)
            {
                int comma=cursor;while(comma<end&&bytes[comma]!=44)comma++;if(comma==end)return false;
                if(!codec.TryCString(bytes,cursor,comma-cursor,out string term))return false;
                if(term=="99999"){cursor=comma+2;break;}
                terms.Add(term);cursor=comma+1;
            }
            if(!Token(bytes,end,cursor,out int start,out int length)||length>255)return false;
            int number=0;while(number<length&&(number==0&&(bytes[start+number]==43||bytes[start+number]==45)||bytes[start+number]>=48&&bytes[start+number]<=57))number++;
            if(number==0||!int.TryParse(System.Text.Encoding.ASCII.GetString(bytes,start,number),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out int count))return false;
            cursor+=length+2;if(count>end)return false;var windowClasses=new List<string>();
            for(int i=0;i<count;i++)
            {
                if(!Token(bytes,end,cursor,out start,out length)||length>255||!codec.TryCString(bytes,start,length,out string value))return false;
                windowClasses.Add(value);cursor+=length+2;
            }
            var header=new byte[7];Array.Copy(bytes,header,7);result=new NativeCandidateData{header=header,terms=terms.ToArray(),windowClasses=windowClasses.ToArray()};return true;
        }
        private static bool Token(byte[] bytes,int end,int cursor,out int start,out int length)
        {
            start=cursor;length=0;if(cursor<0||cursor>=end)return false;
            while(start<end&&White(bytes[start]))start++;if(start==end)return false;
            int at=start;while(at<end&&!White(bytes[at]))at++;length=at-start;return true;
        }
        private static bool White(byte value)=>value==32||value>=9&&value<=13;
    }
    public sealed class NativeCandidateNotice
    {
        public string id,character,session;public bool approved;
        //Logical11 packet:12 reads the local cache;13 carries its8-byte compressed header+data.
        public byte[] packet;
    }
    public static class NativeCandidatePackets
    {
        public static byte[] Cached()=>new byte[]{0x11,0x12};
        public static byte[] Transfer(byte[] compressed)
        {if(compressed==null)return null;var packet=new byte[compressed.Length+2];packet[0]=0x11;packet[1]=0x13;Array.Copy(compressed,0,packet,2,compressed.Length);return packet;}
    }
}
