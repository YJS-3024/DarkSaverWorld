using System;
using System.Collections.Generic;

namespace DarkSaver.Original.World
{
    // Verified general-magic render families. Costs, damage and status policy live in authority notices.
    public static class GeneralMagicFrames
    {
        public static double Duration(BattleMagicEffect e)
        {
            int type=e.pattern.type,n=e.pattern.impact?.count??0,ticks;
            if(e.role==BattleMagicEffectRole.ChargeOnly&&BattleSkillFrames.HasCaster(type,e.radius))return BattleSkillFrames.Duration(e);
            if(e.role==BattleMagicEffectRole.ChargeOnly)return (type==18?24:11)*40;
            switch(type)
            {
                case 0:return 0;
                case 4:case 12:return double.PositiveInfinity;
                case 16:return 0;
                case 24:return(6+n)*40;
                case 5:case 7:case 9:ticks=(n<3?8:n)+1;break;
                case 8:ticks=6;break;
                case 21:ticks=11;break;
                case 22:ticks=(n*2<3?8:n*2)+1;break;
                case 6:case 14:case 15:case 18:case 19:case 20:ticks=26;break;
                case 10:case 11:ticks=10+n;break;
                case 13:ticks=12;break;
                default:return 0;
            }
            return ticks*40;
        }
        public static BattleMagicFrame Draw(MagicEffectStrip s,int frame,double x,double y,int part=0,int width=0,int height=0)
            =>new BattleMagicFrame{part=part,surface=s.surface,sourceWidth=s.width,sourceHeight=s.height,width=width>0?width:s.width,height=height>0?height:s.height,
                x=x,y=y,atlasX=s.x+frame*s.stepX,atlasY=s.y+frame*s.stepY};
        public static BattleMagicFrame[] Frames(BattleMagicEffect e,int randomCounter)
        {
            int type=e.pattern.type,c=e.Counter-1;var s=e.pattern.impact;
            if(e.Expired||type==0)return Array.Empty<BattleMagicFrame>();
            if(e.role==BattleMagicEffectRole.ChargeOnly)
            {
                if(BattleSkillFrames.HasCaster(type,e.radius))return BattleSkillFrames.Caster(e);
                if(type==18)return LevelCircle(e,c);
                return c>=0&&c<10?new[]{BattleThunderPlayback.Charge(e.pattern.charge,c,e.sourceX,e.sourceY)}:Array.Empty<BattleMagicFrame>();
            }
            if(type==16)return Array.Empty<BattleMagicFrame>();
            if(type==24)return BattleSkillFrames.Linear(e);
            if(type==10||type==11)return Projectile(e);
            if(c<0)return Array.Empty<BattleMagicFrame>();
            int frame=c%s.count,w=s.width,h=s.height;double x=e.targetX*32-(s.width-32)/2,y=e.targetY*32-(s.height-32)/2;
            switch(type)
            {
                case 4:case 12:
                    if(randomCounter>=(s.count+1<3?8:s.count+1)*4)return Array.Empty<BattleMagicFrame>();frame=randomCounter/2%s.count;break;
                case 6:case 14:case 15:case 18:case 19:case 20:if(c<11||c>=25)return Array.Empty<BattleMagicFrame>();break;
                case 8:
                    w=h=(7-c)*16;x+=c*8-40;y+=c*8-40;break;
                case 21:
                    if(c<5){w=h=112;x-=40;y-=40;}
                    else{frame=4%s.count;w=h=(12-c)*16;x+=c*8-80;y+=c*8-80;}break;
                case 13:if(c>=10)return Array.Empty<BattleMagicFrame>();break;
            }
            return new[]{Draw(s,frame,x,y,0,w,h)};
        }
        public static (double x,double y) Curve(BattleMagicEffect e,int counter)
        {
            double t=Math.Min(1,Math.Max(0,counter/10.0)),a=1-t;
            double X(double source,double target,double lift)=>source*a*a*a+((source+target)*.5+lift)*(3*a*a*t+3*a*t*t)+target*t*t*t;
            return(X(e.sourceX*32,e.targetX*32,0),X(e.sourceY*32,e.targetY*32,-50));
        }
        public static int Direction(double dx,double dy)
        {int x=Math.Sign(dx),y=Math.Sign(dy);return x==0?y<0?0:y>0?4:-1:x>0?y<0?1:y>0?3:2:y<0?7:y>0?5:6;}
        private static BattleMagicFrame[] Projectile(BattleMagicEffect e)
        {
            int c=e.Counter;var s=c<10?e.pattern.travel:e.pattern.impact;
            if(c==0&&e.pattern.type==11||c==10&&e.pattern.type==10)return Array.Empty<BattleMagicFrame>();
            int frame;
            if(c<10)
            {
                var p=Curve(e,c);
                if(e.pattern.type==11){var prior=Curve(e,c-1);frame=Direction(p.x-prior.x,p.y-prior.y);if(frame<0)return Array.Empty<BattleMagicFrame>();}
                else{uint v=2166136261;foreach(char ch in e.id??"")v=unchecked((v^ch)*16777619);for(int i=0;i<=c;i++)v=unchecked(v*214013+2531011);frame=(int)((v>>16&32767)%(uint)s.count);}
                return new[]{Draw(s,frame,p.x,p.y)};
            }
            frame=c-10;return new[]{Draw(s,frame,e.targetX*32-(s.width-32)/2,e.targetY*32-(s.height-32)/2)};
        }
        // Rects and destination origins are from userunit 10008B70, checked independently of pattern45's fx2 strip.
        private static readonly int[][] Circle={
            new[]{0,560,72,23,367,231},new[]{72,560,26,51,431,253},new[]{98,560,83,34,367,304},new[]{181,560,86,34,281,304},
            new[]{267,560,26,51,275,253},new[]{293,560,73,22,294,231},new[]{360,560,144,5,296,253},new[]{366,565,75,81,362,254},
            new[]{441,565,71,83,298,251},new[]{366,648,159,6,286,303},new[]{513,565,84,72,284,233},new[]{366,654,82,73,361,232}};
        private static BattleMagicFrame[] LevelCircle(BattleMagicEffect e,int c)
        {
            var frames=new List<BattleMagicFrame>();if(c<0)return frames.ToArray();
            if(c>12&&c<=22&&(c&1)==0)
            {
                frames.Add(new BattleMagicFrame{part=100,surface=2,width=640,height=480});
                frames.Add(new BattleMagicFrame{part=101,surface=3,unit=e.sourceUnit,x=e.sourceX*32,y=e.sourceY*32,width=32,height=32});return frames.ToArray();
            }
            int count=c<=0||c>=22?0:Math.Min(c,12);
            for(int i=0;i<count;i++){var r=Circle[i];frames.Add(new BattleMagicFrame{part=10+i,surface=1,x=e.sourceX*32-349+r[4],y=e.sourceY*32-259+r[5],width=r[2],height=r[3],atlasX=r[0],atlasY=r[1]});}
            if(count>0)frames.Add(new BattleMagicFrame{part=101,surface=3,unit=e.sourceUnit,x=e.sourceX*32,y=e.sourceY*32,width=32,height=32});
            return frames.ToArray();
        }
        public static BattleMagicFrame[] Coordinate(BattleMagicEffect e)
        {
            int c=e.PostCounter;if(e.Expired||e.Counter<1||c<0||c>=10)return Array.Empty<BattleMagicFrame>();var s=e.pattern.travel;
            if(e.pattern.type==13)return new[]{Draw(s,c%s.count,e.areaX*32-(s.width-32)/2,e.areaY*32-(s.height-32)/2,3)};
            if(e.pattern.type!=4||c==0)return Array.Empty<BattleMagicFrame>();
            uint v=2166136261;foreach(char ch in e.id??"")v=unchecked((v^ch)*16777619);v^=(uint)e.Counter;
            int Next(int n){v=unchecked(v*214013+2531011);return(int)((v>>16&32767)%(uint)n);}
            int b=11-c,count=b%7+1;var result=new BattleMagicFrame[count];
            for(int i=0;i<count;i++)
            {
                Next(32768);Next(32768);double angle=Next(360)*3.1415926535/180;int rx=Next(10),ry=Next(25);
                int dx=8*(b-4+i)+(int)(rx*Math.Cos(angle)+.5),dy=-15*b-15*i+32+(int)(ry*Math.Sin(angle)+3*i+.5);
                result[i]=Draw(s,0,e.areaX*32+dx,e.areaY*32+dy,3+i);
            }
            return result;
        }
    }
}
