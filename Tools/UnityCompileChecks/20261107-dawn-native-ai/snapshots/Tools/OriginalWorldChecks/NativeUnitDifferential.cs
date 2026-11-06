using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeUnitDifferential
{
    public sealed class Case {public int[] packet;}
    public static void Main(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),options);var results=new List<object>();
        foreach(var c in cases)
        {
            var bytes=c.packet.Select(i=>(byte)i).ToArray();
            if(!OriginalNativeUnits.TryDecode(bytes,out var packet))throw new Exception("Fixture unit decode failed");
            var state=packet.State();
            results.Add(new{fields=state.fields.Select(i=>unchecked((uint)i)).ToArray(),magic=state.Slots(false),skills=state.Slots(true),
                className=Convert.ToHexString(state.className),name=Convert.ToHexString(state.name),roundtrip=OriginalNativeUnits.Packet(packet).SequenceEqual(bytes)});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(results));
        if(args.Length>2)
        {
            using(var stream=File.Create(args[2]))using(var writer=new BinaryWriter(stream))
                for(uint i=0;i<65536;i++)writer.Write(OriginalNativeUnits.StatusFlags(i));
        }
    }
}
