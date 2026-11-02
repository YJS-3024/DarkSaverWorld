using System;
using System.IO;
using System.Text.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.Tutorial;
internal static class WindowPlacementChecks
{
 static void Main(string[] args)
 {
  int count=0;using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));foreach(var row in doc.RootElement.EnumerateArray())
  {
   var r=row.GetProperty("region");var m=row.GetProperty("minRegion");var spec=new WaitingWindowDefinition{id=row.GetProperty("id").GetInt32(),width=row.GetProperty("width").GetInt32(),height=row.GetProperty("height").GetInt32(),dragX=r[0].GetInt32(),dragY=r[1].GetInt32(),dragWidth=r[2].GetInt32()-r[0].GetInt32(),dragHeight=r[3].GetInt32()-r[1].GetInt32(),minWidth=row.GetProperty("minWidth").GetInt32(),minHeight=row.GetProperty("minHeight").GetInt32(),minDragX=m[0].GetInt32(),minDragY=m[1].GetInt32(),minDragWidth=m[2].GetInt32()-m[0].GetInt32(),minDragHeight=m[3].GetInt32()-m[1].GetInt32()};
   string profile=row.GetProperty("x").GetInt32()+" "+row.GetProperty("y").GetInt32()+" "+row.GetProperty("minimized").GetInt32();var actual=NativeWindowPlacement.Restore(spec,profile,row.GetProperty("chatSize").GetInt32());var expected=row.GetProperty("expected");
   Check(actual.Position.X==expected.GetProperty("x").GetInt32()&&actual.Position.Y==expected.GetProperty("y").GetInt32(),"native profile position");
   Check(actual.Minimized==expected.GetProperty("minimized").GetBoolean(),"native minimized token");Check(actual.Expanded==expected.GetProperty("expanded").GetBoolean(),"native chat-size restoration");
   Check(NativeWindowPlacement.Save(spec,actual.Position.X,actual.Position.Y,actual.Minimized,actual.Expanded)==expected.GetProperty("saved").GetString(),"native saved position and minimized flag");
   if(spec.id==19)Check((actual.Expanded&&!actual.Minimized?2:1)==expected.GetProperty("savedChat").GetInt32(),"minimized chat saves normal size");
   void Check(bool ok,string label){count++;if(!ok)throw new Exception(label+" "+row.GetRawText());}
  }
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=count,mismatches=0}));
 }
}
