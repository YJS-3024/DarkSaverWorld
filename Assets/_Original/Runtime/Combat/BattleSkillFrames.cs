using System;
using System.Collections.Generic;

namespace DarkSaver.Original.World
{
    public static class BattleSkillFrames
    {
        public static bool HasCaster(int type,int area)=>type==6&&area>0||type==12||type==14||type==15||type==16||type==19||type==20;
        public static double Duration(BattleMagicEffect e)
        {
            switch(e.pattern.type)
            {
                case 6:case 15:case 16:case 19:return(e.radius*5+1)*40;
                case 12:return double.PositiveInfinity;
                case 14:return(3*e.radius*e.pattern.travel.count+5)*40;
                case 20:return(e.radius*2+1)*40;
                default:return 0;
            }
        }
        private static BattleMagicFrame Piece(int part,int surface,int x,int y,int sw,int sh,double dx,double dy,int w=0,int h=0)
            =>new BattleMagicFrame{part=part,surface=surface,atlasX=x,atlasY=y,sourceWidth=sw,sourceHeight=sh,width=w>0?w:sw,height=h>0?h:sh,x=dx,y=dy};
        private static readonly int[][] Beam={new[]{281,377,10,22},new[]{291,377,2,22},new[]{293,377,18,22},new[]{281,377,22,10},new[]{281,387,22,2},new[]{281,389,22,10}};
        private static readonly int[][] Trails={new[]{371,311,27,39},new[]{251,315,28,44},new[]{280,314,27,60}};
        private static void AddBeam(List<BattleMagicFrame> output,BattleMagicEffect e,int c,bool doubleArms,int start)
        {
            int dir=e.areaX;if(doubleArms&&dir!=-2&&dir!=-3&&dir!=-4)return;bool vertical=dir==-3||dir==-4;
            int length=34*(c/5+1);
            for(int arm=0;arm<(doubleArms?2:1);arm++)for(int part=0;part<3;part++)
            {
                int index;double ox=0,oy=0;
                switch(dir)
                {
                    case -2:index=part==0?2:part==1?1:0;ox=part==0?-7:part==1?-14-length*.5:-11-length;break;
                    case -3:index=part==0?5:part==1?4:3;oy=part==0?-11:part==1?-14-length*.5:-11-length;break;
                    case -4:index=part==0?3:part==1?4:5;oy=part==0?10:part==1?15+length*.5:20+length;break;
                    default:index=part==0?0:part==1?1:2;ox=part==0?10:part==1?15+length*.5:24+length;break;
                }
                if(doubleArms){if(vertical)ox+=arm==0?-32:32;else oy+=arm==0?32:-32;}
                var r=Beam[index];int w=part==1&&!vertical?length:r[2],h=part==1&&vertical?length:r[3];
                output.Add(Piece(start+arm*3+part,0,r[0]+(doubleArms?70:0),r[1]+(doubleArms?30:0),r[2],r[3],e.sourceX*32+16+ox-w*.5,e.sourceY*32+16+oy-h*.5,w,h));
            }
        }
        public static BattleMagicFrame[] Caster(BattleMagicEffect e)
        {
            var output=new List<BattleMagicFrame>();int c=e.Counter-1,t=e.pattern.type;if(c<0||e.Expired)return output.ToArray();
            if(t==6||t==15||t==19)
            {AddBeam(output,e,c,t==15,0);if(t==19)AddBeam(output,e,c,true,3);}
            else if(t==14)
            {
                var s=e.pattern.travel;int w=2*((c+s.width-32)/2+3*c),h=2*((c+s.height-32)/2+3*c);
                if(w>0&&h>0)output.Add(GeneralMagicFrames.Draw(s,c%s.count,e.sourceX*32+16-w*.5,e.sourceY*32+16-h*.5,0,w,h));
            }
            else if(t==16)
            {
                for(int d=0;d<4;d++)for(int slot=0;slot<4;slot++)
                {
                    int i=Math.Max(0,c-3)+slot;if(d<2?i>c:i>=c)continue;var r=Trails[i%3];
                    output.Add(Piece(d*4+slot,0,r[0],r[1],r[2],r[3],e.sourceX*32+16+(d==0?i*7:d==1?-i*7:0)-(r[2]+1)/2,e.sourceY*32+16+(d==2?-i*7:d==3?i*7:0)-(r[3]+1)/2));
                }
            }
            else if(t==20)
            {
                int head=c+2,life=e.radius*2,vx=(e.areaX-e.sourceX)*32,vy=(e.areaY-e.sourceY)*32;
                if(life<=0)return output.ToArray();
                for(int slot=0;slot<5;slot++)
                {
                    int i=slot==4?head:Math.Max(0,head-3)+slot;if(i>head)continue;int[] r;int surface;
                    if(slot<4){r=Trails[i%3];surface=0;}
                    else{int dir=vx>0?0:vx==0&&vy<0?2:1;r=new[]{618+(dir==1?138:0)+(head%2)*69,dir==2?576:648,69,72};surface=1;}
                    output.Add(Piece(slot,surface,r[0],r[1],r[2],r[3],e.sourceX*32+16+vx/life*i-(r[2]+1)/2,e.sourceY*32+16+vy/life*i-(r[3]+1)/2));
                }
            }
            else if(t==12)
            {
                c=e.NativeRandomCounter;var s=e.pattern.impact;uint random=2166136261;foreach(char ch in e.id??"")random=unchecked((random^ch)*16777619);random^=(uint)e.Counter;
                for(int x=Math.Max(0,e.sourceX-e.radius);x<=Math.Min(e.mapWidth-1,e.sourceX+e.radius);x++)for(int y=Math.Max(0,e.sourceY-e.radius);y<=Math.Min(e.mapHeight-1,e.sourceY+e.radius);y++)
                {
                    int d=Math.Abs(x-e.sourceX)+Math.Abs(y-e.sourceY);if(d>e.radius||(d&1)==((e.radius+1)&1))continue;
                    random=unchecked(random*214013+2531011);int frame=(c/2+(int)(random>>16&32767)%3-1)%s.count;
                    if(frame>=0)output.Add(GeneralMagicFrames.Draw(s,frame,x*32-(s.width-32)/2,y*32-(s.height-32)/2,(x-e.sourceX+e.radius)*(2*e.radius+1)+y-e.sourceY+e.radius));
                }
            }
            return output.ToArray();
        }
        public static int LinearCounter(BattleMagicEffect e)=>e.Counter<=0?-1:e.Counter<=5?(e.Counter-1)*2:e.Counter+4;
        private static int MulDiv(int value,int n)=>checked((int)Math.Round(value*(double)n/10,MidpointRounding.AwayFromZero));
        public static BattleMagicFrame[] Linear(BattleMagicEffect e)
        {
            int c=LinearCounter(e);if(c<=0||e.Expired)return Array.Empty<BattleMagicFrame>();
            var s=c<10?e.pattern.travel:e.pattern.impact;
            if(c>=10)return new[]{GeneralMagicFrames.Draw(s,c-10,e.targetX*32-(s.width-32)/2,e.targetY*32-(s.height-32)/2)};
            int vx=(e.targetX-e.sourceX)*32,vy=(e.targetY-e.sourceY)*32;
            int dx=MulDiv(vx,c),dy=MulDiv(vy,c),direction=GeneralMagicFrames.Direction(dx-MulDiv(vx,c-2),dy-MulDiv(vy,c-2));
            return direction<0?Array.Empty<BattleMagicFrame>():new[]{GeneralMagicFrames.Draw(s,direction,e.sourceX*32+dx,e.sourceY*32+dy)};
        }
    }
}
