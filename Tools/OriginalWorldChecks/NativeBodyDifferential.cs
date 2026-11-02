using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeBodyDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public string name;public bool moving;public int unit=0,size=32,direction,nativeDirection=4,phase;public double clock;public TrainingUnit[] units;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);var output=new List<object>();
        foreach(var c in cases)
        {
            var u=c.units.Single(x=>x.id==c.unit);double x=u.x*32,y=u.y*32;
            if(c.moving)
            {
                int[] dx={0,-1,1,0,0,-1,-1,1,1},dy={0,0,0,-1,1,-1,1,-1,1};int step=c.nativeDirection>=5?5:8;
                x+=dx[c.nativeDirection]*c.phase*step;y+=dy[c.nativeDirection]*c.phase*step;
            }
            var f=NativeBodyFrames.Body(c.size*3,c.size*4,c.direction,c.clock,c.moving,x,y);
            output.Add(new[]{new{surface=(int)f.surface,x=(int)f.x,y=(int)f.y,f.atlasX,f.atlasY,f.width,f.height}});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output));
    }
}
