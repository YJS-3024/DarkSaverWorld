using System;

namespace DarkSaver.Original.World
{
    public static class NativeBodyFrames
    {
        // FUN1000AC60(clock / period):0,1,2,1. Static body333, walking body125.
        // The caller supplies its presentation clock; original process-clock phase is not measured.
        public const int IdleFrameMilliseconds=333,MovingFrameMilliseconds=125;
        public static int Frame(double clock,bool moving,int appearance=-1)
        {
            if(double.IsNaN(clock)||double.IsInfinity(clock)||clock<0)return 0;
            if(moving&&appearance>=980&&appearance<=998)return (int)(Math.Floor(clock/MovingFrameMilliseconds)%3);
            int phase=(int)(Math.Floor(clock/(moving?MovingFrameMilliseconds:IdleFrameMilliseconds))%4);
            return phase==3?1:phase;
        }
        public static bool Wet(int attribute,int? movementType,int appearance)=>(attribute%128==6||attribute%128==20)&&movementType.HasValue&&movementType!=1&&appearance!=899;
        public static BattleFeedbackFrame Water(int size,double clock,double x,double y,bool moving=false,int relativeRow=-1)
            =>new BattleFeedbackFrame{surface=BattleFeedbackSurface.Marker,x=x,y=y+(size==64?moving?3:5:0),atlasX=(size==64&&!moving?Frame(clock,false):(int)(Math.Floor(Math.Max(0,clock)/250)%2))*32,atlasY=32,width=32,height=size==64&&relativeRow==9?26:32};
        public static BattleFeedbackFrame Shadow(int size,int appearance,bool moving,bool wet,double clock,double x,double y)
            =>appearance!=999&&appearance!=981&&!wet&&(moving?appearance!=790&&appearance!=791:size==32)&&(int)(Math.Floor(Math.Max(0,clock)/100)%2)!=0
                ?new BattleFeedbackFrame{surface=BattleFeedbackSurface.Marker,x=x,y=y,atlasX=160,atlasY=0,width=30,height=32}:null;
        public static BattleFeedbackFrame Readiness(uint? state,bool moving,double clock,double x,double y)
            =>!moving&&state.HasValue&&(state.Value&0x30)!=0?new BattleFeedbackFrame{surface=BattleFeedbackSurface.Marker,x=x+15,y=y+15,atlasX=255+Frame(clock,false)*16,atlasY=(state.Value&0x20)!=0?16:0,width=15,height=16}:null;
        public static BattleFeedbackFrame Body(int textureWidth,int textureHeight,int direction,double clock,bool moving,double x,double y,int appearance=-1,int attribute=-1,int? movementType=null,int part=0,int relativeRow=-1)
        {
            if(appearance==999||direction<0||direction>3||textureWidth!=96&&textureWidth!=192)return null;
            int size=textureWidth/3,sy=direction*size;
            if(sy+size>textureHeight)return null;
            var f=new BattleFeedbackFrame{surface=BattleFeedbackSurface.Body,x=x+(32-size)/2,y=y+(32-size)/2,atlasX=Frame(clock,moving,appearance)*size,atlasY=sy,width=size,height=Wet(attribute,movementType,appearance)?size==64?48:25:size==64&&moving&&relativeRow==9?58:size};
            if(size==64)
            {
                int dx=0,dy=0,right=0,bottom=0;
                switch(part)
                {
                    case 1:dx=16;right=16;bottom=48;break;
                    case 2:dy=16;right=48;bottom=16;break;
                    case 3:dx=16;dy=16;right=16;bottom=16;break;
                    case 4:dx=48;dy=16;bottom=16;break;
                    case 5:dx=16;dy=48;right=16;break;
                }
                f.x+=dx;f.y+=dy;f.atlasX+=dx;f.atlasY+=dy;f.width-=dx+right;f.height-=dy+bottom;
            }
            return f;
        }
        // B5D0/BD00 draw the shadow before cropping. Moving large ripples inherit
        // the selected part's destination offset; static ripples/readiness do not.
        public static BattleFeedbackFrame[] Compose(int textureWidth,int textureHeight,int direction,double clock,bool moving,double x,double y,int appearance,int attribute,int? movementType,uint? state,int part=0,int relativeRow=-1)
        {
            var body=Body(textureWidth,textureHeight,direction,clock,moving,x,y,appearance,attribute,movementType,part,relativeRow);
            if(body==null)return Array.Empty<BattleFeedbackFrame>();
            var frames=new System.Collections.Generic.List<BattleFeedbackFrame>();int size=textureWidth/3;
            bool wet=Wet(attribute,movementType,appearance);var shadow=Shadow(size,appearance,moving,wet,clock,x,y);
            if(shadow!=null)frames.Add(shadow);frames.Add(body);
            if(wet)
            {
                double dx=moving&&size==64?body.x-(x-16):0,dy=moving&&size==64?body.y-(y-16):0;
                frames.Add(Water(size,clock,x+dx,y+dy,moving,relativeRow));
            }
            var ready=Readiness(state,moving,clock,x,y);if(ready!=null)frames.Add(ready);
            return frames.ToArray();
        }
        public static bool Fits(BattleFeedbackFrame frame,int textureWidth,int textureHeight)
            =>frame!=null&&frame.atlasX>=0&&frame.atlasY>=0&&frame.width>0&&frame.height>0
                &&(long)frame.atlasX+frame.width<=textureWidth&&(long)frame.atlasY+frame.height<=textureHeight;
    }
}
