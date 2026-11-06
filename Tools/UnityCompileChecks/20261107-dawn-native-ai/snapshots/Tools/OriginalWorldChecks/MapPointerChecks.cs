using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using DarkSaver.Original.World;
namespace UnityEngine
{
 public class MonoBehaviour:Component{public RectTransform transform=>gameObject.GetComponent<RectTransform>();}
 public static class Mathf{public static int FloorToInt(float value)=>(int)Math.Floor(value);}
 public static class Time{public static double realtimeSinceStartupAsDouble;}
 public static class Input{public static bool left=true;public static bool GetMouseButton(int button)=>left&&button==0;}
 public static class RectTransformUtility
 {
  public static bool convert=true,inside=true;public static float cameraOffset;
  public static bool ScreenPointToLocalPointInRectangle(RectTransform rect,Vector2 position,object camera,out Vector2 point){point=new Vector2(position.x+cameraOffset,position.y);return convert;}
  public static bool RectangleContainsScreenPoint(RectTransform rect,Vector2 position,object camera)=>inside;
 }
}
namespace UnityEngine.EventSystems
{
 public interface IPointerClickHandler{} public interface IPointerDownHandler{} public interface IPointerUpHandler{} public interface IDragHandler{} public interface IInitializePotentialDragHandler{}
 public struct RaycastResult{public GameObject gameObject;}
 public class EventSystem:UnityEngine.Object
 {
  public static EventSystem current;public GameObject hit;
  public void RaycastAll(PointerEventData data,List<RaycastResult> results){if(hit!=null)results.Add(new RaycastResult{gameObject=hit});}
 }
 public class PointerEventData
 {public enum InputButton{Left,Right,Middle}public InputButton button;public Vector2 position;public object pressEventCamera;public bool useDragThreshold=true;public GameObject pointerDrag;}
}
internal static class MapPointerChecks
{
 static int checks;static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static PointerEventData At(float x,float y)=>new PointerEventData{position=new Vector2(x,y)};
 static int now;
 static void Clock(int milliseconds){now=milliseconds;Time.realtimeSinceStartupAsDouble=(milliseconds+0.125)/1000.0;}
 static void Tick(){Clock(now+31);}
 static void Main(string[] args)
 {
  if(args.Length!=0){ClockDifferential(args[0]);return;}
  Clock(0);
  var go=new GameObject("Map",typeof(RectTransform),typeof(NativeMapPointer));var p=go.GetComponent<NativeMapPointer>();
  var events=new EventSystem{hit=go};EventSystem.current=events;var sent=new List<string>();p.Configure((x,y)=>sent.Add(x+","+y));
  var e=At(64,-96);e.pointerDrag=go;p.OnInitializePotentialDrag(e);Check(e.pointerDrag==null&&e.useDragThreshold,"battle grid does not capture drag and retains click release");
  p.OnPointerDown(e);Check(sent.Count==0,"unconfigured battle mode still waits for click");p.OnPointerUp(e);p.OnPointerClick(e);Check(sent.Count==1&&sent[0]=="2,3","legacy click exactly once");
  bool allowed=true;p.ConfigureContinuous(()=>allowed);p.OnInitializePotentialDrag(e);Check(!e.useDragThreshold,"walking does not wait for pixel drag threshold");sent.Clear();p.OnPointerDown(e);Check(sent.Count==1,"held walking begins on left down");
  Tick();p.OnDrag(At(65,-97));Check(sent.Count==1,"same tile does not duplicate request");Tick();p.OnDrag(At(96,-96));Check(sent.Count==2&&sent[1]=="3,3","new tile retargets while held");
  p.OnPointerUp(e);p.OnPointerClick(e);Check(sent.Count==2,"release does not send a second request");Tick();p.OnDrag(At(128,-96));Check(sent.Count==2,"drag without hold ignored");
  p.OnPointerDown(e);int count=sent.Count;events.hit=new GameObject("Personal window");Tick();p.OnDrag(At(160,-96));Check(sent.Count==count,"topmost personal UI blocks world movement");events.hit=go;Tick();p.OnDrag(At(160,-96));Check(sent.Count==count+1,"returning over map retargets");
  allowed=false;count=sent.Count;Tick();p.OnDrag(At(192,-96));Check(sent.Count==count,"facility/modal predicate blocks held movement");allowed=true;
  RectTransformUtility.inside=false;Tick();p.OnDrag(At(224,-96));Check(sent.Count==count,"outside map does not retarget");RectTransformUtility.inside=true;
  EventSystem.current=null;Tick();p.OnDrag(At(224,-96));Check(sent.Count==count,"missing EventSystem is safe");EventSystem.current=events;
  typeof(NativeMapPointer).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p,null);Tick();p.OnDrag(At(224,-96));Check(sent.Count==count,"disable clears captured hold");
  e.button=PointerEventData.InputButton.Right;p.OnPointerDown(e);p.OnPointerClick(e);Check(sent.Count==count,"right button never walks");
  e=At(32,-32);RectTransformUtility.convert=false;p.OnPointerDown(e);Check(sent.Count==count,"failed screen projection does not send");RectTransformUtility.convert=true;
  p.OnPointerUp(e);e=At(64,-96);p.OnPointerDown(e);count=sent.Count;
  var update=typeof(NativeMapPointer).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  RectTransformUtility.cameraOffset=32;Tick();update.Invoke(p,null);Check(sent.Count==count+1&&sent[sent.Count-1]=="3,3","stationary held cursor retargets when camera scrolls");
  Tick();update.Invoke(p,null);Check(sent.Count==count+1,"stationary same tile stays deduplicated");
  Input.left=false;RectTransformUtility.cameraOffset=64;update.Invoke(p,null);Check(sent.Count==count+1,"physical release cancels hold even without pointer-up dispatch");Input.left=true;RectTransformUtility.cameraOffset=0;
  // Exact input clock boundary and no duplicate Update/drag within a poll.
  sent.Clear();p.ConfigureContinuous(()=>true);Clock(1000);p.OnPointerDown(At(32,-32));
  Clock(1029);p.OnDrag(At(64,-32));Check(sent.Count==1,"29ms does not repeat");
  Clock(1030);p.OnDrag(At(64,-32));Check(sent.Count==1,"30ms does not repeat");
  Clock(1031);p.OnDrag(At(64,-32));Check(sent.Count==2,"31ms repeats once");
  p.OnDrag(At(96,-32));update.Invoke(p,null);Check(sent.Count==2,"same poll drag and Update cannot repeat twice");
  Clock(1061);update.Invoke(p,null);Check(sent.Count==2,"strict threshold is measured from last poll");
  Clock(1062);update.Invoke(p,null);Check(sent.Count==3&&sent[2]=="3,1","pending cursor survives until next poll");
  Clock(100000);p.OnDrag(At(128,-32));Check(sent.Count==4,"delayed frame sends at most once");
  p.OnPointerUp(e);Clock(100010);p.OnPointerDown(At(160,-32));Check(sent.Count==5,"new click is immediate after prior long hold");
  Clock(100040);p.OnDrag(At(192,-32));Check(sent.Count==5,"new press starts a fresh repeat clock");
  Clock(100041);p.OnDrag(At(192,-32));Check(sent.Count==6,"new press repeats after31ms");
  Input.left=false;update.Invoke(p,null);Input.left=true;Clock(100100);p.OnDrag(At(224,-32));Check(sent.Count==6,"physical release disarms timer");
  var clock=new NativePointerRepeatClock();Check(!clock.TryRepeat(999),"unstarted clock cannot repeat");
  clock.Begin(int.MaxValue-10);Check(!clock.TryRepeat(unchecked(int.MinValue+19)),"signed rollover30ms rejected");
  Check(clock.TryRepeat(unchecked(int.MinValue+20)),"signed rollover31ms accepted");clock.Reset();Check(!clock.TryRepeat(100),"reset clock cannot repeat");
  Console.WriteLine(JsonSerializer.Serialize(new{checks,unityPlay=false,scope="Actual production NativeMapPointer callbacks with managed input/projection/raycast/component boundaries; no Unity EventSystem dispatch or physical input."}));
 }
 static void ClockDifferential(string path)
 {
  using(var input=JsonDocument.Parse(System.IO.File.ReadAllText(path)))
  {
   var cases=new List<object>();
   foreach(var item in input.RootElement.EnumerateArray())
   {
    var clock=new NativePointerRepeatClock();clock.Begin(item.GetProperty("start").GetInt32());var rows=new List<object>();
    foreach(var t in item.GetProperty("times").EnumerateArray())
    {bool repeated=clock.TryRepeat(t.GetInt32());rows.Add(new{repeated,last=clock.LastTick,counter=clock.Repeats});}
    cases.Add(rows);
   }
   Console.WriteLine(JsonSerializer.Serialize(cases));
  }
 }

}
