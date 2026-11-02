using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeBodyOrderDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case
    {
        public string mode;
        public int width,height,left,top,columns,rows,size,direction,nativeDirection,phase,appearance,attribute,kind,part;
        public bool moving;
        public double clock;
        public int[] attributes;
        public TrainingUnit[] units;
    }
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);
        File.WriteAllText(args[1],JsonSerializer.Serialize(cases.Select(c=>
        {
            if(c.mode=="plan")return (object)NativeBodyOrder.Plan(c.width,c.height,(x,y)=>x>=0&&y>=0&&x<c.width&&y<c.height?c.attributes[y*c.width+x]:-1,
                c.units.Where(u=>u.nativeActive!=false&&u.nativeEnabled!=false).Select(u=>new NativeBodyOccupant{id=u.id,x=u.x,y=u.y,appearance=u.job,movementType=u.movementType}),c.left,c.top,c.columns,c.rows);
            var actor=c.units[0];double x=actor.x*32,y=actor.y*32;
            if(c.moving){int[] dx={0,-1,1,0,0,-1,-1,1,1},dy={0,0,0,-1,1,-1,1,-1,1};int step=c.nativeDirection>=5?5:8;x+=dx[c.nativeDirection]*c.phase*step;y+=dy[c.nativeDirection]*c.phase*step;}
            return NativeBodyFrames.Compose(c.size*3,c.size*4,c.direction,c.clock,c.moving,x,y,c.appearance,c.attribute,c.kind,actor.nativeState,c.part)
                .Select(f=>new{surface=(int)f.surface,x=(int)f.x,y=(int)f.y,f.atlasX,f.atlasY,f.width,f.height}).ToArray();
        }).ToArray(),Options));
    }
}
