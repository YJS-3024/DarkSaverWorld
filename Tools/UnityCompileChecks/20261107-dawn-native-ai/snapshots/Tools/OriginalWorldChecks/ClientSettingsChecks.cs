using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.UI;
internal static class ClientSettingsChecks
{
 static int assertions;static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
 static OriginalClientSettings Read(JsonElement e)=>new OriginalClientSettings{Music=e.GetProperty("music").GetBoolean(),Sound=e.GetProperty("sound").GetBoolean(),GlobalChat=e.GetProperty("globalChat").GetBoolean(),Whisper=e.GetProperty("whisper").GetBoolean(),ViewMenu=e.GetProperty("viewMenu").GetBoolean()};
 static void Main(string[] args)
 {
  using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));foreach(var row in doc.RootElement.EnumerateArray())
  {
   var settings=Read(row.GetProperty("initial"));var panel=row.GetProperty("panel").EnumerateArray().Select(v=>v.GetBoolean()).ToArray();
   // Native constructor forces control6 checked after45A130; initialization helper itself does not.
   panel[6]=true;Check(settings.PanelStates().SequenceEqual(panel),"native checkbox initialization including forced music-on display");
   int control=row.GetProperty("control").GetInt32();bool value=row.GetProperty("value").GetBoolean();Check(settings.Toggle(control,value),"native toggle accepted");var expected=Read(row.GetProperty("expected").GetProperty("state"));
   Check(settings.Music==expected.Music&&settings.Sound==expected.Sound&&settings.GlobalChat==expected.GlobalChat&&settings.Whisper==expected.Whisper&&settings.ViewMenu==expected.ViewMenu,"native callback state");
   if(control==4)Check(Convert.ToHexString(OriginalClientSettings.WhisperPacket(settings.Whisper)).ToLowerInvariant()==row.GetProperty("expected").GetProperty("packet").GetString(),"native7D whisper payload uses1/2");
  }
  var state=new OriginalClientSettings();Check(!state.Toggle(0,false)&&!state.Toggle(7,false),"non-toggle controls do not mutate options");
  Check(!state.CycleFont(_=>false)&&state.Font==0,"failed source font load preserves selected font");Check(state.CycleFont(i=>i==1)&&state.Font==1&&state.CycleFont(i=>i==0)&&state.Font==0,"two source fonts cycle after successful load only");
  foreach(int value in new[]{0,1,2,255,256,65535})Check(OriginalClientSettings.TryReadWhisper(new[]{(byte)0x7d,(byte)value,(byte)(value>>8)},out bool allowed)&&allowed==(value==1),"source7D notification interprets WORD==1");
  Check(!OriginalClientSettings.TryReadWhisper(new byte[]{0x7d,1},out _)&&!OriginalClientSettings.TryReadWhisper(new byte[]{0x7c,1,0},out _)&&!OriginalClientSettings.TryReadWhisper(new byte[]{0x7d,1,0,0},out _),"truncated, wrong opcode and trailing data are rejected");
  state.GlobalChat=false;Check(!state.AcceptsMessage(3)&&state.AcceptsMessage(0)&&state.AcceptsMessage(4),"native global chat filter affects category3 only");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions,mismatches=0}));
 }
}
