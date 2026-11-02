using System;
using System.Globalization;
using DarkSaver.Original.Tutorial;
namespace DarkSaver.Original.UI
{
    // Saver4347E0 restoration tests the title rectangle first, then4346D0 may force centering.
    public readonly struct NativeWindowPosition
    {public readonly int X,Y;public NativeWindowPosition(int x,int y){X=x;Y=y;}}
    public readonly struct NativeWindowState
    {
        public readonly NativeWindowPosition Position;public readonly bool Minimized,Expanded;
        public NativeWindowState(NativeWindowPosition position,bool minimized,bool expanded){Position=position;Minimized=minimized;Expanded=expanded;}
    }
    public sealed class NativeWindowClickClock
    {
        private int last;
        // Native433D00 uses a signed32-bit clock difference and strict <250,
        // updating the window's timestamp on ordinary non-button releases too.
        public bool Release(int milliseconds){int elapsed=unchecked(milliseconds-last);last=milliseconds;return elapsed<250;}
    }
    public static class NativeWindowPlacement
    {
        public static NativeWindowState Restore(WaitingWindowDefinition spec,string profile,int chatSize)
        {
            var fields=(profile??"").Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);int x=0,y=0,mini=0;
            if(fields.Length>0)int.TryParse(fields[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out x);
            if(fields.Length>1)int.TryParse(fields[1],NumberStyles.Integer,CultureInfo.InvariantCulture,out y);
            if(fields.Length>2)int.TryParse(fields[2],NumberStyles.Integer,CultureInfo.InvariantCulture,out mini);
            bool minimized=mini!=0&&spec.minWidth>0&&spec.minHeight>0,expanded=spec.id==19&&chatSize==2&&!minimized;
            var position=Geometry(spec,x,y,minimized,expanded,spec.id==33);return new NativeWindowState(position,minimized,expanded);
        }
        public static string Save(WaitingWindowDefinition spec,int x,int y,bool minimized,bool expanded)
        {
            minimized=minimized&&spec.minWidth>0&&spec.minHeight>0;var at=Geometry(spec,x,y,minimized,expanded,false);
            return at.X.ToString(CultureInfo.InvariantCulture)+" "+at.Y.ToString(CultureInfo.InvariantCulture)+" "+(minimized?"1":"0");
        }
        private static NativeWindowPosition Geometry(WaitingWindowDefinition spec,int x,int y,bool minimized,bool expanded,bool center)
        {
            if(minimized)return Resolve(x,y,spec.minWidth,spec.minHeight,spec.minDragX,spec.minDragY,spec.minDragX+spec.minDragWidth,spec.minDragY+spec.minDragHeight,center);
            return Resolve(x,y,spec.width,spec.id==19&&expanded?401:spec.height,spec.dragX,spec.dragY,spec.dragX+spec.dragWidth,spec.dragY+spec.dragHeight,center);
        }
        public static NativeWindowPosition Resolve(int x,int y,int width,int height,int left,int top,int right,int bottom,bool center)
        {
            long l=x,t=y,r=(long)x+width,b=(long)y+height;
            if(right>left&&bottom>top){l=(long)x+left;t=(long)y+top;r=(long)x+right;b=(long)y+bottom;}
            bool visible=l<640&&r>0&&t<480&&b>0;
            if(!visible){x=x<0?0:640-width;y=y<0?0:480-height;}
            if(center){x=(640-width)/2;y=(480-height)/2;}
            return new NativeWindowPosition(x,y);
        }
    }
}
