using System;
namespace DarkSaver.Original.UI
{
    // Native window44/scene28. This generated image container is an asset format,
    // not an original network packet. No action buttons or timeout are invented.
    public static class NativeWorldMessageLayout
    {
        public const int Window=44,Width=562,Height=84,LineX=288,LineY=35,TextX=20,TextY=10,TextWidth=522,TextHeight=64;
        public static bool TryImage(byte[] data,out byte[] rgba)
        {
            rgba=null;if(data==null||data.Length!=8+Width*Height*4||data[0]!='D'||data[1]!='S'||data[2]!='N'||data[3]!='1'
                ||(data[4]|data[5]<<8)!=Width||(data[6]|data[7]<<8)!=Height)return false;
            rgba=new byte[data.Length-8];Buffer.BlockCopy(data,8,rgba,0,rgba.Length);return true;
        }
        public static byte[] TexturePixels(byte[] topDown)
        {
            if(topDown==null||topDown.Length!=Width*Height*4)return null;
            var bottomUp=new byte[topDown.Length];int stride=Width*4;
            for(int y=0;y<Height;y++)Buffer.BlockCopy(topDown,y*stride,bottomUp,(Height-1-y)*stride,stride);return bottomUp;
        }
    }
    public sealed class NativeWorldMessageState
    {
        public bool Visible{get;private set;}public bool Paragraph{get;private set;}public string Text{get;private set;}="";
        public int DismissedFrame{get;private set;}=int.MinValue;
        public void Show(string text,bool paragraph){Text=text??"";Paragraph=paragraph;Visible=true;}
        public bool ConsumeLeftDown(int frame)
        {if(!Visible)return false;Visible=false;DismissedFrame=frame;return true;}
        public bool ConsumedIn(int frame)=>DismissedFrame==frame;
        public void Clear(){Visible=false;Text="";DismissedFrame=int.MinValue;}
    }
}
