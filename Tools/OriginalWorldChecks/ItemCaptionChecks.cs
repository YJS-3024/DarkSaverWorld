using System;
using System.IO;
using System.Text.Json;
using DarkSaver.Original.UI;
internal static class ItemCaptionChecks
{
 static void Main(string[] args)
 {
  int count=0;using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));foreach(var row in doc.RootElement.GetProperty("captions").EnumerateArray())
  {
   string name=row.GetProperty("name").ValueKind==JsonValueKind.Null?null:row.GetProperty("name").GetString();string actual=NativeItemCaption.Format(name,row.GetProperty("uses").GetInt32(),row.GetProperty("mode").GetInt32());count++;if(actual!=row.GetProperty("expected").GetString())throw new Exception("actual46BF20 caption mismatch");
  }
  count++;if(NativeItemCaption.Format("대거",null)!="대거, 사용회수: —")throw new Exception("unreceived use count must remain unknown");
  foreach(var row in doc.RootElement.GetProperty("clicks").EnumerateArray())
  {
   var clock=new NativeWindowClickClock();clock.Release(1000);bool actual=clock.Release(1000+row.GetProperty("delta").GetInt32());var result=row.GetProperty("result");bool native=result.GetProperty("noop").GetInt32()+result.GetProperty("restore").GetInt32()>0;count++;if(actual!=native)throw new Exception("original strict250ms release sequence mismatch");
  }
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=count,mismatches=0,unityPlay=false}));
 }
}
