using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkSaver.Original.World
{
    public sealed class NativeBodyOccupant
    {
        public int id,x,y,appearance;
        public int? movementType;
    }
    public sealed class NativeBodyDraw
    {public int id,part,stage;}
    // UserUnit UpdateCharacter(stage1) and FUN10010C00(stage2).
    // Raw MRC attribute bit128 is a redraw flag, independent of navigation terrain.
    public static class NativeBodyOrder
    {
        public static bool Foreground(int attribute)=>attribute%256/128==1;
        public static NativeBodyDraw[] Plan(int width,int height,Func<int,int,int> attribute,IEnumerable<NativeBodyOccupant> bodies,int left=0,int top=0,int columns=int.MaxValue,int rows=int.MaxValue,bool includePostPasses=false)
        {
            var occupied=new Dictionary<long,NativeBodyOccupant>();
            long Key(int x,int y)=>(long)y*width+x;
            foreach(var u in bodies.OrderBy(u=>u.id))if(u.x>=0&&u.y>=0&&u.x<width&&u.y<height)occupied[Key(u.x,u.y)]=u;
            NativeBodyOccupant At(int x,int y)=>x>=0&&y>=0&&x<width&&y<height&&occupied.TryGetValue(Key(x,y),out var u)?u:null;
            bool Large(NativeBodyOccupant u)=>u!=null&&u.appearance/100==7;
            var cells=occupied.Values.Where(u=>u.x>=left&&u.y>=top&&(long)u.x<((long)left+columns)&&(long)u.y<((long)top+rows)).OrderBy(u=>u.y).ThenBy(u=>u.x).ToArray();
            var result=new List<NativeBodyDraw>();
            void Draw(NativeBodyOccupant u,int part,int stage){if(u!=null)result.Add(new NativeBodyDraw{id=u.id,part=part,stage=stage});}
            foreach(var u in cells)if(!Foreground(attribute(u.x,u.y))||Large(u))Draw(u,0,1);
            foreach(var u in cells)
            {
                if(Foreground(attribute(u.x,u.y)))
                {
                    var below=At(u.x,u.y+1);if(Large(below)&&below.movementType!=1)Draw(below,1,2);
                    var right=At(u.x+1,u.y);if(Large(right)&&right.movementType!=1)Draw(right,2,2);
                    if(u.movementType!=1)
                    {
                        if(!Large(u))Draw(u,0,2);
                        else
                        {
                            for(int p=1;p<=3;p++)Draw(u,p,2);
                            if(Foreground(attribute(u.x+1,u.y)))Draw(u,4,2);
                            if(Foreground(attribute(u.x,u.y+1)))Draw(u,5,2);
                        }
                    }
                }
                if(u.movementType==1)Draw(u,0,2);
            }
            // UpdateCharacter(stage2) invokes magic after the body redraws,
            // then scans the same occupied cells once more for numeric display.
            if(includePostPasses)
            {
                result.Add(new NativeBodyDraw{id=-1,part=0,stage=3}); // Magic pass boundary.
                foreach(var u in cells)Draw(u,0,4);
            }
            return result.ToArray();
        }
    }
}
