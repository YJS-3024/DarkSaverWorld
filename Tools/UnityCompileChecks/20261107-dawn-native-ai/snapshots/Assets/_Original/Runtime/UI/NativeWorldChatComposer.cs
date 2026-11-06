using System;
namespace DarkSaver.Original.UI
{
    // Saver470790/470B00. Control0/1 are mutually exclusive; neither checked means ordinary channel1.
    public sealed class NativeWorldChatComposer
    {
        public int Channel {get;private set;}=1;
        public bool Checked(int control)=>control==0?Channel==2:control==1&&Channel==3;
        public bool Toggle(int control)
        {if(control<0||control>1)return false;int selected=control+2;Channel=Channel==selected?1:selected;return true;}
        public void Reset(){Channel=1;}
        public static bool WhisperPrefix(string text,out string recipient)
        {
            recipient=null;if(text==null)return false;int prefix=text.StartsWith("/to ",StringComparison.OrdinalIgnoreCase)?4:text.StartsWith("/새 ",StringComparison.Ordinal)?3:0;
            if(prefix==0)return false;
            string rest=text.Substring(prefix).TrimStart(' ','\t','\r','\n','\f','\v');int space=rest.IndexOf(' ');
            if(rest.Length>0)recipient=space<0?rest:rest.Substring(0,space);return true;
        }
    }
}
