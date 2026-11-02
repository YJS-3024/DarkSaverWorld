using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal static class NativeCommanderChecks
{
 static int assertions;static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true};
 static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static string Bytes(byte[] bytes)=>bytes==null?null:Convert.ToHexString(bytes).ToLowerInvariant();
 static void Main(string[] args)
 {
  using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));var root=doc.RootElement;
  foreach(var row in root.GetProperty("toggles").EnumerateArray())Check(Bytes(OriginalCommanderPackets.Toggle(row.GetProperty("record").GetInt32(),row.GetProperty("value").GetInt32()))==row.GetProperty("packet").GetString(),"original toggle bytes");
  foreach(var row in root.GetProperty("swaps").EnumerateArray())
  {
   int source=row.GetProperty("source").GetInt32(),target=row.GetProperty("target").GetInt32();var rows=JsonSerializer.Deserialize<OwnedCommander[]>(row.GetProperty("rows").GetRawText(),Json);
   var expected=JsonSerializer.Deserialize<OwnedCommander[]>(row.GetProperty("expected").GetRawText(),Json);var request=new CommanderRequest{operation=CommanderOperation.Swap,source=source,target=target,state=new CommanderRoster{rows=rows}};
   Check(Bytes(request.NativePacket)==(row.GetProperty("packet").ValueKind==JsonValueKind.Null?null:row.GetProperty("packet").GetString()),"original drag bytes");
   var actual=OriginalCommanderPackets.Edit(request);if(source==target){Check(actual==null,"self drop emits no exchange");continue;}
   for(int i=0;i<rows.Length;i++)Check(JsonSerializer.Serialize(actual.rows[i],Json)==JsonSerializer.Serialize(expected[i],Json),"original record projection after full exchange");
  }
  Console.WriteLine(JsonSerializer.Serialize(new{assertions,mismatches=0}));
 }
}
