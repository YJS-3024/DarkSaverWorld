using System;
namespace DarkSaver.Original.UI
{
    // Late Saver client defaults/profile keys; these are not game economy or account data.
    public sealed class ClientWhisperNotice
    {public string id,character,session;public byte[] packet;}
    public sealed class OriginalClientSettings
    {
        public bool Music=true,Sound=true,GlobalChat=true,Whisper=true,ViewMenu=true;
        public int Font;
        public bool[] PanelStates()=>new[]{false,true,Sound,GlobalChat,Whisper,ViewMenu,true,false};
        public bool Toggle(int control,bool value)
        {
            switch(control)
            {
                case 1:Music=value;return true;
                case 2:Sound=value;return true;
                case 3:GlobalChat=value;return true;
                case 4:Whisper=value;return true;
                case 5:ViewMenu=value;return true;
                // The original callback restores control6 to state2; it does not change the screen mode.
                case 6:return true;
                default:return false;
            }
        }
        public bool CycleFont(Func<int,bool> load)
        {int next=(Font+1)&1;if(load==null||!load(next))return false;Font=next;return true;}
        public static byte[] WhisperPacket(bool allowed)=>new[]{(byte)0x7d,(byte)(allowed?1:2),(byte)0};
        public static bool TryReadWhisper(byte[] packet,out bool allowed)
        {allowed=false;if(packet?.Length!=3||packet[0]!=0x7d)return false;allowed=(packet[1]|packet[2]<<8)==1;return true;}
        public bool AcceptsMessage(int nativeKind)=>nativeKind!=3||GlobalChat;
    }
}
