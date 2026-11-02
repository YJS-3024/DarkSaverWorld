using System;
using System.IO;
using System.Text.Json;
using DarkSaver.Original.UI;
internal static class NativeFontChecks
{
 static int assertions;static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Main(string[] args)
 {
  var options=new JsonSerializerOptions{IncludeFields=true};var map=JsonSerializer.Deserialize<NativeTextMapping>(File.ReadAllText(args[0]),options);
  using var doc=JsonDocument.Parse(File.ReadAllText(args[1]));foreach(var row in doc.RootElement.GetProperty("cases").EnumerateArray())
  {
   string text=row.GetProperty("text").GetString();Check(NativeTextMetrics.Width(text,map)==row.GetProperty("width").GetInt32(),"late native measured width: "+text);
   if(text.Length==1&&text[0]!='\t')Check(map.Glyph(text[0])==row.GetProperty("glyph").GetInt32(),"native glyph index");
  }
  var raw=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(args[0]),"../../SourceData/Native/dark14.mxf.bytes"));
  foreach(var row in doc.RootElement.GetProperty("paragraphs").EnumerateArray())
  {
   string text=row.GetProperty("text").GetString();var output=new byte[640*80];
   foreach(var cell in NativeTextMetrics.Layout(text,map,row.GetProperty("width").GetInt32(),row.GetProperty("height").GetInt32(),true,0,0,true,true))
    for(int y=0;y<14;y++)for(int x=0;x<14;x++)
    {int atX=2+cell.X,atY=2+cell.Y;if(atX<0||atY<0||atX+14>640||atY+14>80)continue;if((raw[cell.Glyph*32+y*2+x/8]&(128>>(x%8)))==0)output[(atY+y)*640+atX+x]=255;}
   Check(Convert.ToHexString(output).ToLowerInvariant()==row.GetProperty("pixels").GetString(),"native paragraph pixels/advance/wrap: "+text);
  }
  foreach(var row in doc.RootElement.GetProperty("clips").EnumerateArray())Check(NativeTextMetrics.GlyphVisible(row.GetProperty("x").GetInt32(),row.GetProperty("y").GetInt32(),row.GetProperty("width").GetInt32(),row.GetProperty("height").GetInt32())==row.GetProperty("visible").GetBoolean(),"original surface clips a whole glyph at every edge");
  foreach(var row in doc.RootElement.GetProperty("anchors").EnumerateArray())Check(NativeTextMetrics.LineOriginX(row.GetProperty("anchor").GetInt32(),row.GetProperty("width").GetInt32(),row.GetProperty("alignment").GetInt32())==row.GetProperty("origin").GetInt32(),"actual native single-line alignment raster origin");
  Check(map.Glyph('힣')==31&&map.Glyph('漢')==31,"native unsupported CP949 chars use question mark, not OS fallback");
  var cells=NativeTextMetrics.Layout("가A",map,14,28,true,0,0,true);Check(cells.Count==2&&cells[0].X==0&&cells[0].Y==0&&cells[1].Y==14,"bounded uGUI adapter wraps between glyphs");
  cells=NativeTextMetrics.Layout("가",map,30,30,false,1,1,true);Check(cells[0].X==8&&cells[0].Y==8,"bounded uGUI adapter centers measured glyph");
  Check(NativeTextMetrics.Layout("가",map,30,13,false,0,0,true).Count==0,"bounded uGUI adapter does not draw a partial line");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions,mismatches=0}));
 }
}
