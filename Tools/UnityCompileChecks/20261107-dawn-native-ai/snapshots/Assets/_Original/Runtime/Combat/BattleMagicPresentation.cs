using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkSaver.Original.World
{
    public static class BattleMagicPresentation
    {
        // Native display pass clears this table every call. It uses a modulo15 cursor
        // for both insertion and the inclusive render prefix, including its wrap behavior.
        public static BattleMagicEffect[] TornadoOwners(BattleMagicEffect[] effects)=>CoordinateOwners(effects).Where(e=>e.IsTornado).ToArray();
        public static BattleMagicEffect[] CoordinateOwners(BattleMagicEffect[] effects)
        {
            var slots=new BattleMagicEffect[15];int cursor=0;
            foreach(var effect in effects.Where(e=>new[]{3,4,13}.Contains(e.pattern?.type??-1)&&e.role!=BattleMagicEffectRole.ChargeOnly&&e.Counter>=1&&!e.Expired).OrderBy(e=>e.targetUnit))
            {
                if(slots.Take(cursor+1).Any(e=>e!=null&&e.pattern.type==effect.pattern.type&&e.areaX==effect.areaX&&e.areaY==effect.areaY))continue;
                slots[cursor]=effect;cursor=(cursor+1)%slots.Length;
            }
            return slots.Take(cursor+1).Where(e=>e!=null).ToArray();
        }
        public static (string key,BattleMagicFrame frame)[] Frames(BattleMagicEffect[] effects)
        {
            var frames=new List<(string key,BattleMagicFrame frame)>();
            var flash=effects.Where(e=>e.pattern?.type==18&&e.role==BattleMagicEffectRole.ChargeOnly&&e.Frames.Any(f=>f.surface==2)).OrderBy(e=>e.sourceUnit).FirstOrDefault();
            if(flash!=null)return flash.Frames.Select(f=>(flash.id+"/"+f.part,f)).ToArray();
            foreach(var effect in effects)foreach(var frame in effect.Frames)frames.Add((effect.id+"/"+frame.part,frame));
            foreach(var owner in CoordinateOwners(effects))
            {
                int i=0;foreach(var frame in owner.IsTornado?TornadoFrames(owner):GeneralMagicFrames.Coordinate(owner))frames.Add(("coordinate/"+owner.id+"/"+i++,frame));
            }
            return frames.ToArray();
        }
        public static BattleMagicFrame[] TornadoFrames(BattleMagicEffect owner)
        {
            if(!owner.IsTornado||owner.Counter<1||owner.Counter>11)return Array.Empty<BattleMagicFrame>();
            // Target old counter10 clears +1C4 to0 after registration. The coordinate
            // pass then sees0 one final time, before the next pass has no registered target.
            int counter=owner.Counter==11?0:owner.Counter;
            if(counter>=10)return Array.Empty<BattleMagicFrame>();
            uint random=2166136261;foreach(char c in owner.id??"")random=unchecked((random^c)*16777619);
            int Next(int modulus){random=unchecked(random*214013+2531011);return (int)((random>>16&32767)%(uint)modulus);}
            var frames=new BattleMagicFrame[3];var strip=owner.pattern.travel;
            // Replay only local visual draws, keeping elapsed-time partitioning stable.
            // This per-notice seed is not the original shared global rand() sequence.
            for(int step=1;step<=owner.Counter;step++)
            {
                if(step==10)continue;
                for(int i=0;i<3;i++)
                {
                    int radius=Next(50)+20,angle=Next(360);double radians=angle*3.1415926535/180;
                    int dx=(int)(radius*Math.Cos(radians)+.5),dy=(int)(radius*Math.Sin(radians)+.5);
                    if(step==owner.Counter)frames[i]=new BattleMagicFrame{part=3+i,width=strip.width,height=strip.height,
                        x=owner.areaX*32-(strip.width-32)/2+dx,y=(owner.areaY+1)*32-strip.height+dy,
                        atlasX=strip.x+(counter%strip.count)*strip.stepX,atlasY=strip.y+(counter%strip.count)*strip.stepY};
                }
            }
            return frames;
        }
    }
}
