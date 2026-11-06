using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.UI;
using DarkSaver.Original.Tutorial;
namespace UnityEngine{public class MonoBehaviour{}}
namespace UnityEngine.EventSystems
{
 public sealed class PointerEventData
 {public enum InputButton{Left,Right,Middle}public InputButton button;public bool used;public void Use(){used=true;}}
 public interface IPointerDownHandler{void OnPointerDown(PointerEventData e);}
}
static class NativeWorldMessageChecks
{
 static int checks;static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static void Main(string[] args)
 {
  byte[] source=File.ReadAllBytes(Path.Combine(args[0],"Assets/_Original/Resources/OriginalWorldNotice.bytes"));Check(NativeWorldMessageLayout.TryImage(source,out var pixels)&&pixels.Length==562*84*4,"actual decoded original asset");
  var texture=NativeWorldMessageLayout.TexturePixels(pixels);for(int y=0;y<84;y++)Check(pixels.Skip(y*562*4).Take(562*4).SequenceEqual(texture.Skip((83-y)*562*4).Take(562*4)),"Unity bottom-up row conversion preserves source pixels: "+y);
  Check(NativeWorldMessageLayout.TexturePixels(texture).SequenceEqual(pixels),"row reversal roundtrip");texture[0]^=1;Check(!texture.SequenceEqual(NativeWorldMessageLayout.TexturePixels(pixels)),"texture rows detached");
  var copy=(byte[])source.Clone();copy[8]^=1;Check(NativeWorldMessageLayout.TryImage(copy,out var independent)&&independent[0]!=pixels[0],"decoded image detached from prior data");
  foreach(var bad in new byte[][]{null,Array.Empty<byte>(),source.Take(source.Length-1).ToArray(),source.Concat(new byte[]{0}).ToArray(),source.Skip(1).ToArray()})Check(!NativeWorldMessageLayout.TryImage(bad,out _),"malformed resource rejected");
  copy=(byte[])source.Clone();copy[0]=0;Check(!NativeWorldMessageLayout.TryImage(copy,out _),"wrong asset magic");copy=(byte[])source.Clone();copy[4]=0;Check(!NativeWorldMessageLayout.TryImage(copy,out _),"wrong dimensions");
  Check(NativeWorldMessageLayout.TexturePixels(null)==null&&NativeWorldMessageLayout.TexturePixels(new byte[3])==null,"invalid row input rejected");
  using(var doc=JsonDocument.Parse(File.ReadAllText(args[1])))
  {
   foreach(var row in doc.RootElement.GetProperty("draws").EnumerateArray())
   {
    int x=row.GetProperty("origin")[0].GetInt32(),y=row.GetProperty("origin")[1].GetInt32();bool paragraph=row.GetProperty("paragraph").GetBoolean();var drawn=row.GetProperty("drawn");
    Check(drawn.GetProperty("color").GetUInt32()==(row.GetProperty("depth").GetInt32()==8?255u:65535u),"original white text in both depths");
    if(paragraph)Check(drawn.GetProperty("rect").EnumerateArray().Select(v=>v.GetInt32()).SequenceEqual(new[]{x+NativeWorldMessageLayout.TextX,y+NativeWorldMessageLayout.TextY,x+NativeWorldMessageLayout.TextX+NativeWorldMessageLayout.TextWidth,y+NativeWorldMessageLayout.TextY+NativeWorldMessageLayout.TextHeight}),"native paragraph rectangle");
    else Check(drawn.GetProperty("x").GetInt32()==x+NativeWorldMessageLayout.LineX&&drawn.GetProperty("y").GetInt32()==y+NativeWorldMessageLayout.LineY&&drawn.GetProperty("alignment").GetInt32()==6,"native single-line anchor and literal center6");
   }
   foreach(var row in doc.RootElement.GetProperty("placements").EnumerateArray())
   {
    var spec=new WaitingWindowDefinition{id=44,width=562,height=84};int x=row.GetProperty("inputX").GetInt32(),y=row.GetProperty("inputY").GetInt32();var position=NativeWindowPlacement.Restore(spec,x+" "+y+" 0",1).Position;
    Check(position.X==row.GetProperty("x").GetInt32()&&position.Y==row.GetProperty("y").GetInt32(),"native profile position is restored and bounded, no forced centering");
   }
  }
  var state=new NativeWorldMessageState();Check(!state.Visible&&!state.ConsumeLeftDown(0),"no message means no consumed click");state.Show("원본 안내",false);Check(state.Visible&&state.Text=="원본 안내"&&!state.Paragraph,"single message");
  var input=new NativeWorldMessageInput();int callbacks=0;input.Configure(()=>{if(state.ConsumeLeftDown(5))callbacks++;});
  foreach(var button in new[]{UnityEngine.EventSystems.PointerEventData.InputButton.Right,UnityEngine.EventSystems.PointerEventData.InputButton.Middle})
  {var e=new UnityEngine.EventSystems.PointerEventData{button=button};input.OnPointerDown(e);Check(!e.used&&state.Visible&&callbacks==0,"only verified left press invokes dismissal");}
  var left=new UnityEngine.EventSystems.PointerEventData{button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};input.OnPointerDown(left);Check(left.used&&!state.Visible&&callbacks==1&&state.ConsumedIn(5)&&!state.ConsumedIn(6),"first left press owns event and blocks same-frame shortcut processing");
  Check(!state.ConsumeLeftDown(5),"closed message consumes no second logical event");state.Show("새 메시지",true);Check(state.Visible&&state.Paragraph&&state.Text=="새 메시지","replacement does not retain old text");state.Clear();Check(!state.Visible&&!state.ConsumedIn(5)&&state.Text=="","session reset clears message");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Actual production source-RGBA parser/orientation, scene28 text geometry and generic native position comparison, message/first-left-press state. MonoBehaviour/PointerEventData lifecycle boundary stubbed; no full Unity EventSystem release/raycast order, GPU clipping, physical input or non-left dispatch equivalence claim."}));
 }
}
