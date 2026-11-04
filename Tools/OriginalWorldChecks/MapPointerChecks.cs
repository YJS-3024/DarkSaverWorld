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
 static void Main()
 {
  var go=new GameObject("Map",typeof(RectTransform),typeof(NativeMapPointer));var p=go.GetComponent<NativeMapPointer>();
  var events=new EventSystem{hit=go};EventSystem.current=events;var sent=new List<string>();p.Configure((x,y)=>sent.Add(x+","+y));
  var e=At(64,-96);e.pointerDrag=go;p.OnInitializePotentialDrag(e);Check(e.pointerDrag==null&&e.useDragThreshold,"battle grid does not capture drag and retains click release");
  p.OnPointerDown(e);Check(sent.Count==0,"unconfigured battle mode still waits for click");p.OnPointerUp(e);p.OnPointerClick(e);Check(sent.Count==1&&sent[0]=="2,3","legacy click exactly once");
  bool allowed=true;p.ConfigureContinuous(()=>allowed);p.OnInitializePotentialDrag(e);Check(!e.useDragThreshold,"walking does not wait for pixel drag threshold");sent.Clear();p.OnPointerDown(e);Check(sent.Count==1,"held walking begins on left down");
  p.OnDrag(At(65,-97));Check(sent.Count==1,"same tile does not duplicate request");p.OnDrag(At(96,-96));Check(sent.Count==2&&sent[1]=="3,3","new tile retargets while held");
  p.OnPointerUp(e);p.OnPointerClick(e);Check(sent.Count==2,"release does not send a second request");p.OnDrag(At(128,-96));Check(sent.Count==2,"drag without hold ignored");
  p.OnPointerDown(e);int count=sent.Count;events.hit=new GameObject("Personal window");p.OnDrag(At(160,-96));Check(sent.Count==count,"topmost personal UI blocks world movement");events.hit=go;p.OnDrag(At(160,-96));Check(sent.Count==count+1,"returning over map retargets");
  allowed=false;count=sent.Count;p.OnDrag(At(192,-96));Check(sent.Count==count,"facility/modal predicate blocks held movement");allowed=true;
  RectTransformUtility.inside=false;p.OnDrag(At(224,-96));Check(sent.Count==count,"outside map does not retarget");RectTransformUtility.inside=true;
  EventSystem.current=null;p.OnDrag(At(224,-96));Check(sent.Count==count,"missing EventSystem is safe");EventSystem.current=events;
  typeof(NativeMapPointer).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p,null);p.OnDrag(At(224,-96));Check(sent.Count==count,"disable clears captured hold");
  e.button=PointerEventData.InputButton.Right;p.OnPointerDown(e);p.OnPointerClick(e);Check(sent.Count==count,"right button never walks");
  e=At(32,-32);RectTransformUtility.convert=false;p.OnPointerDown(e);Check(sent.Count==count,"failed screen projection does not send");RectTransformUtility.convert=true;
  p.OnPointerUp(e);e=At(64,-96);p.OnPointerDown(e);count=sent.Count;
  var update=typeof(NativeMapPointer).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  RectTransformUtility.cameraOffset=32;update.Invoke(p,null);Check(sent.Count==count+1&&sent[sent.Count-1]=="3,3","stationary held cursor retargets when camera scrolls");
  update.Invoke(p,null);Check(sent.Count==count+1,"stationary same tile stays deduplicated");
  Input.left=false;RectTransformUtility.cameraOffset=64;update.Invoke(p,null);Check(sent.Count==count+1,"physical release cancels hold even without pointer-up dispatch");Input.left=true;RectTransformUtility.cameraOffset=0;
  Console.WriteLine(JsonSerializer.Serialize(new{checks,unityPlay=false,scope="Actual production NativeMapPointer callbacks with managed input/projection/raycast/component boundaries; no Unity EventSystem dispatch or physical input."}));
 }
}
