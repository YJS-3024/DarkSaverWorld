using System;
using System.Collections.Generic;
using System.Text;
namespace DarkSaver.Original.UI
{
    [Serializable] public sealed class NativeCodePage949
    {
        public int[] unicode,encoded;
        private Dictionary<int,int> encode,decode;
        private bool Ready()
        {
            if(encode!=null)return true;
            if(unicode==null||encoded?.Length!=unicode.Length)return false;
            var a=new Dictionary<int,int>();var b=new Dictionary<int,int>();
            for(int i=0;i<unicode.Length;i++)
            {if(unicode[i]<0||unicode[i]>65535||encoded[i]<0||encoded[i]>65535||a.ContainsKey(unicode[i])||b.ContainsKey(encoded[i]))return false;a[unicode[i]]=encoded[i];b[encoded[i]]=unicode[i];}
            encode=a;decode=b;return true;
        }
        public bool TryEncode(string text,out byte[] bytes)
        {
            bytes=null;if(text==null||!Ready())return false;var result=new List<byte>();
            foreach(char c in text)
            {if(c=='\0'||!encode.TryGetValue(c,out int value))return false;if(value>255)result.Add((byte)(value>>8));result.Add((byte)value);}
            bytes=result.ToArray();return true;
        }
        public bool TryCString(byte[] bytes,int offset,int count,out string text)
        {
            text=null;if(bytes==null||offset<0||count<0||offset>bytes.Length-count||!Ready())return false;var result=new StringBuilder();int end=offset+count;
            for(int at=offset;at<end;at++)
            {
                int key=bytes[at];if(key==0)break;
                if(key>=128){if(at+1>=end)return false;key=(key<<8)|bytes[++at];}
                if(!decode.TryGetValue(key,out int cp))return false;result.Append((char)cp);
            }
            text=result.ToString();return true;
        }
    }
}
