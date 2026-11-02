using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using DarkSaver.Original.Maps;
internal static class MapAnimationChecks
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Input{public Case[] cases;public Map[] maps;}
    public sealed class Case{public string mode;public int selector,random,phase;public byte[] frames;public double[] times;public int[] stages;}
    public sealed class Map{public string key,folder;public int animated,staticVariants;public Sample[] samples;}
    public sealed class Sample{public int cell;public byte phase;public bool animation;public string hash;}
    public static void Main(string[] args)
    {
        var input=JsonSerializer.Deserialize<Input>(File.ReadAllText(args[0]),Options);
        var results=input.cases.Select(c=>
        {
            if(c.mode=="select")return (object)c.frames[NativeMapAnimation.Index((byte)c.selector,(byte)c.random,(byte)c.phase,c.frames.Length)];
            var clock=new NativeMapAnimationClock();byte phase=(byte)c.phase;
            return c.times.Select((time,i)=>{if(c.stages[i]==0&&clock.Advance(time))phase=unchecked((byte)(phase+1));return (int)phase;}).ToArray();
        }).ToArray();
        int assertions=0,animated=0,staticVariants=0;
        foreach(var row in input.maps)
        {
            var bytes=File.ReadAllBytes(Path.Combine(row.folder,row.key+".mrc.bytes"));
            var pixels=NativeMapDecoder.RenderAnimatedLayers(bytes,id=>File.ReadAllBytes(Path.Combine(row.folder,"mdsr"+id+".grs.bytes")),123);
            if(pixels.animations.Length!=row.animated||pixels.animationPhase!=0)throw new Exception("Animation inventory "+row.key);
            assertions++;animated+=row.animated;staticVariants+=row.staticVariants;
            var cells=pixels.animations.ToDictionary(t=>t.y*(pixels.width/32)+t.x);
            foreach(var s in row.samples)
            {
                var cell=new byte[32*32*4];int tx=s.cell%(pixels.width/32)*32,ty=s.cell/(pixels.width/32)*32;
                if(s.animation)cell=cells[s.cell].Frame(s.phase).rgba;
                else for(int y=0;y<32;y++)Buffer.BlockCopy(pixels.rgba,((ty+y)*pixels.width+tx)*4,cell,y*32*4,32*4);
                if(Convert.ToHexString(SHA256.HashData(cell)).ToLowerInvariant()!=s.hash)throw new Exception("Native initial frame pixels "+row.key+"/"+s.cell);
                assertions++;
            }
            foreach(var tile in pixels.animations)
            {
                foreach(byte phase in new byte[]{0,1,2,3,254,255})
                {var frame=tile.Frame(phase);if(frame.width!=32||frame.height!=32||frame.rgba.Length!=4096)throw new Exception("Invalid animated tile");}
            }
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(new{results,assertions,maps=input.maps.Length,animated,staticVariants},Options));
    }
}
