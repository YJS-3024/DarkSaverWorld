using System;
using System.Collections.Generic;

namespace DarkSaver.Original.World
{
    public struct BattleMagicFrame
    {
        public int part,width,height,atlasX,atlasY,sourceWidth,sourceHeight,surface,unit;
        public double x,y;
    }
    // Native fxptn2 target + fxptn2 caster charge. This RNG is presentation-only;
    // its per-notice seed does not reproduce the original shared rand() sequence.
    internal sealed class BattleThunderPlayback
    {
        private uint random;
        private int end=20;private bool pause;
        public int Steps { get; private set; }
        public int StoredCounter { get; private set; }
        public int DrawCounter { get; private set; }
        public bool Finished { get; private set; }
        public bool Visible=>Steps>0&&!Finished;
        public BattleThunderPlayback(string id,int end=20,bool pause=true)
        {this.end=end;this.pause=pause;random=2166136261;foreach(char c in id??"")random=unchecked((random^c)*16777619);}
        public BattleThunderPlayback Copy()=>(BattleThunderPlayback)MemberwiseClone();
        private int Next(int modulus)
        {random=unchecked(random*214013+2531011);return (int)((random>>16&32767)%(uint)modulus);}
        public void AdvanceTo(int step)
        {
            while(Steps<step&&!Finished)
            {
                Steps++;
                // The renderer loads EBP before initializing/updating the stored counter.
                // Use that previous value for both frame strips, including the first zero frame.
                DrawCounter=StoredCounter;
                if(DrawCounter>=end){Finished=true;break;}
                if(StoredCounter==0)StoredCounter=Next(5);
                if(!pause||Next(3)!=0)StoredCounter++;
            }
        }
        public BattleMagicFrame[] Frames(MagicProjectilePattern pattern,int sourceX,int sourceY,int targetX,int targetY,bool includeCharge=true)
        {
            if(!Visible)return Array.Empty<BattleMagicFrame>();
            var frames=new List<BattleMagicFrame>(3);
            if(includeCharge&&Steps<=10)frames.Add(Charge(pattern.charge,Steps-1,sourceX,sourceY));
            var bolt=pattern.travel;var impact=pattern.impact;
            frames.Add(Frame(0,bolt,(DrawCounter/2)%bolt.count,targetX*32-(bolt.width-32)/2,(targetY+1)*32-bolt.height));
            frames.Add(Frame(1,impact,DrawCounter%impact.count,targetX*32-(impact.width-32)/2,targetY*32-(impact.height-32)/2));
            return frames.ToArray();
        }
        public static BattleMagicFrame Charge(MagicEffectStrip strip,int counter,int sourceX,int sourceY)
            =>Frame(2,strip,counter%strip.count,sourceX*32-(strip.width-32)/2,sourceY*32-(strip.height-32)/2);
        private static BattleMagicFrame Frame(int part,MagicEffectStrip strip,int frame,double x,double y)=>new BattleMagicFrame
        {part=part,x=x,y=y,width=strip.width,height=strip.height,atlasX=strip.x+frame*strip.stepX,atlasY=strip.y+frame*strip.stepY};
    }
}
