using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeAiRuleDifferential
{
    internal sealed class Case {public string key,text,kind;public int[] seed;}
    public static void Main(string[] args)
    {
        var j=new JsonSerializerOptions{IncludeFields=true};var cases=JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]),j);
        var result=cases.Select(c=>
        {
            int[] output=null;string error="";bool ok=true;
            if(c.kind=="move"||c.kind=="actions")
            {output=(int[])c.seed.Clone();if(c.kind=="move")NativeAiRuleTable.ParseMovement(c.text,output);else NativeAiRuleTable.ParseActions(c.text,output);}
            else {ok=NativeAiRuleTable.TryLoad(c.text,c.seed==null?null:new NativeAiRuleTable(c.seed),out var table,out error);output=table?.Snapshot();}
            return new{c.key,ok,error,output};
        }).ToArray();File.WriteAllText(args[1],JsonSerializer.Serialize(result,j));
    }
}
