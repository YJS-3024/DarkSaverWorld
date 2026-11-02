using System;
using System.Collections.Generic;
using System.Globalization;

namespace DarkSaver.Original.World
{
    public enum BattleFeedbackSurface { Board, Contact, Effects, Marker, Body }
    public sealed class BattleFeedbackFrame
    {
        public BattleFeedbackSurface surface;
        public double x,y;
        public int atlasX,atlasY,width,height;
    }
    public static class BattleFeedbackFrames
    {
        private static BattleFeedbackFrame Frame(BattleFeedbackSurface surface,double x,double y,int sx,int sy,int w,int h)
            =>new BattleFeedbackFrame{surface=surface,x=x,y=y,atlasX=sx,atlasY=sy,width=w,height=h};
        private static void Text(List<BattleFeedbackFrame> frames,string text,int row,double x,double y)
        {
            foreach(char c in text)
            {
                int cell=OriginalBattleFeedback.DigitCell(c);if(cell<0)continue;
                frames.Add(Frame(BattleFeedbackSurface.Board,x,y,227+cell*8,418+row*18,8,14));x+=8;
            }
        }
        public static BattleFeedbackFrame[] Numbers(BattleNumberEffect effect,double x,double y)
        {
            var frames=new List<BattleFeedbackFrame>();if(effect==null||effect.Expired)return frames.ToArray();
            if(effect.HealthVisible)Text(frames,effect.DisplayHealth.ToString(CultureInfo.InvariantCulture),effect.HealthRow,x,y);
            if(effect.DeltaVisible)
            {
                x+=effect.OffsetX;y+=effect.OffsetY;
                if(effect.kind==BattleFeedbackKind.Miss)frames.Add(Frame(BattleFeedbackSurface.Board,x,y,1,447,36,11));
                else Text(frames,effect.DeltaText,effect.DeltaRow,x,y);
            }
            return frames.ToArray();
        }
        public static BattleFeedbackFrame[] Attack(BattleAttackEffect effect,double x,double y,double markerOffsetX=0,double markerOffsetY=0)
        {
            if(effect==null||effect.Expired)return Array.Empty<BattleFeedbackFrame>();
            var frames=new List<BattleFeedbackFrame>{Frame(BattleFeedbackSurface.Marker,x+markerOffsetX,y+markerOffsetY,255+effect.Counter%2*16,0,15,15)};
            if(effect.ContactVisible)frames.Add(Frame(BattleFeedbackSurface.Contact,x+effect.offsetX,y+effect.offsetY,effect.Frame*32,32,32,32));
            if(effect.ExtraVisible)frames.Add(Frame(BattleFeedbackSurface.Effects,x,y,250+effect.Frame*32,240,32,32));
            return frames.ToArray();
        }
    }
}
