using System;
namespace DarkSaver.Original.UI
{
    public sealed class WorldChatTermsNotice
    {public string id,character,session;public bool approved;public string[] terms;}
    // Saver425680: ordered, case-sensitive CString replacements; command/recipient exceptions precede filtering.
    public sealed class NativeWorldChatFilter
    {
        private string[] terms=Array.Empty<string>();private int[] lengths=Array.Empty<int>();
        public bool SetTerms(string[] values,NativeCodePage949 codec)
        {
            if(values==null||codec==null)return false;var sizes=new int[values.Length];
            for(int i=0;i<values.Length;i++){if(!codec.TryEncode(values[i],out var bytes))return false;sizes[i]=bytes.Length;}
            terms=(string[])values.Clone();lengths=sizes;return true;
        }
        public string Apply(string text,bool commandAware=true)
        {
            if(text==null)return null;int prefix=0;
            if(commandAware&&text.StartsWith("/",StringComparison.Ordinal))
            {
                int first=text.IndexOf(' ');if(first<0)return text;int second=text.IndexOf(' ',first+1);if(second<0)return text;
                string command=text.Substring(1,first-1);
                if(!string.Equals(command,"to",StringComparison.OrdinalIgnoreCase)&&command!="새"&&command!="메모")return text;prefix=second;
            }
            string body=text.Substring(prefix);
            for(int i=0;i<terms.Length;i++)if(terms[i].Length>0)body=body.Replace(terms[i],new string('*',lengths[i]));
            return text.Substring(0,prefix)+body;
        }
    }
}
