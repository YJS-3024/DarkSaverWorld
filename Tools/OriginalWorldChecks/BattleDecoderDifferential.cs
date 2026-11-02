using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class BattleDecoderDifferential
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
    public sealed class Case {public int[] packet;}
    public static void Main(string[] args)
    {
        var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),Options);var output=new List<object>();
        foreach(var c in cases)
        {
            var raw=c.packet.Select(i=>(byte)i).ToArray();string kind;int[] values;
            if(OriginalNativeMovement.TryDecode(raw,out var movement)){kind=movement.failed?"move-fail":"move";values=new[]{movement.unit,movement.x,movement.y};}
            else if(OriginalBattleTicks.TryDecode(raw,out var tick)){kind=tick.pulse?"pulse":"menu";values=tick.pulse?Array.Empty<int>():new[]{tick.unit,tick.ap,tick.menu};}
            else if(OriginalBattleFeedback.TryDecode(raw,out var feedback)){kind=feedback.kind==BattleFeedbackKind.Health?"hp":feedback.kind==BattleFeedbackKind.Mana?"mp":feedback.kind==BattleFeedbackKind.Miss?"miss":"attack";values=kind=="attack"?new[]{feedback.unit,feedback.target,feedback.value}:kind=="miss"?new[]{feedback.unit}:new[]{feedback.unit,feedback.value};}
            else if(OriginalBattleLifecycle.TryDecode(raw,out var life)){kind=life.kind==BattleLifecycleKind.Death?"death":"remove";values=new[]{life.unit};}
            else throw new Exception("Unknown fixture packet");
            output.Add(new{kind,values,wordReads=(raw.Length-2)/2});
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(output));
    }
}
