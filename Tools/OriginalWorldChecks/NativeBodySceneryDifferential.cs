using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeBodySceneryDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public bool moving;public int size,direction,nativeDirection,phase,appearance,attribute,kind;public double clock;public TrainingUnit[] units;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);
        File.WriteAllText(args[1],JsonSerializer.Serialize(cases.Select(c=>
        {
            var actor=c.units[0];double x=actor.x*32,y=actor.y*32;
            if(c.moving){int[] dx={0,-1,1,0,0,-1,-1,1,1},dy={0,0,0,-1,1,-1,1,-1,1};int step=c.nativeDirection>=5?5:8;x+=dx[c.nativeDirection]*c.phase*step;y+=dy[c.nativeDirection]*c.phase*step;}
            var result=new List<BattleFeedbackFrame>();bool wet=NativeBodyFrames.Wet(c.attribute,c.kind,c.appearance);
            var body=NativeBodyFrames.Body(c.size*3,c.size*4,c.direction,c.clock,c.moving,x,y,c.appearance,c.attribute,c.kind);
            var shadow=NativeBodyFrames.Shadow(c.size,c.appearance,c.moving,wet,c.clock,x,y);
            if(body!=null){if(shadow!=null)result.Add(shadow);result.Add(body);if(wet)result.Add(NativeBodyFrames.Water(c.size,c.clock,x,y,c.moving));var ready=NativeBodyFrames.Readiness(actor.nativeState,c.moving,c.clock,x,y);if(ready!=null)result.Add(ready);}
            return result.Select(f=>new{surface=(int)f.surface,x=(int)f.x,y=(int)f.y,f.atlasX,f.atlasY,f.width,f.height}).ToArray();
        }).ToArray()));
    }
}
