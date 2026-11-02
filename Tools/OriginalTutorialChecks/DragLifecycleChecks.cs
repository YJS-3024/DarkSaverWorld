using System;
using System.Reflection;
using System.Text.Json;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.EventSystems;
// Projection/component lifetime boundaries only. The production drag component
// is compiled unchanged; this is not Unity EventSystem or rendered Play.
namespace UnityEngine
{
 public class MonoBehaviour{}
 public class RectTransform{}
 public class Camera{}
 public static class Time{public static double unscaledTimeAsDouble;}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public static class RectTransformUtility
 {public static bool Available=true;public static bool ScreenPointToLocalPointInRectangle(RectTransform r,Vector2 point,Camera camera,out Vector2 result){result=point;return Available;}}
}
namespace UnityEngine.EventSystems
{
 public class PointerEventData{public enum InputButton{Left,Right}public InputButton button;public Vector2 position;public Camera pressEventCamera;public bool dragging;}
 public interface IPointerDownHandler{void OnPointerDown(PointerEventData e);}
 public interface IPointerUpHandler{void OnPointerUp(PointerEventData e);}
 public interface IBeginDragHandler{void OnBeginDrag(PointerEventData e);}
 public interface IDragHandler{void OnDrag(PointerEventData e);}
 public interface IEndDragHandler{void OnEndDrag(PointerEventData e);}
}
namespace DarkSaver.Original.Tutorial
{public sealed class WaitingWindowDefinition{public int id,width,height,minWidth,minHeight,minDragX,minDragY,minDragWidth,minDragHeight,dragX,dragY,dragWidth,dragHeight;}}
internal static class DragLifecycleChecks
{
 static int assertions;static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Disable(NativeTutorialDrag drag)=>typeof(NativeTutorialDrag).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(drag,null);
 static void Main()
 {
  var screen=new RectTransform();var point=new PointerEventData{button=PointerEventData.InputButton.Left,position=new Vector2(40,20)};var drag=new NativeTutorialDrag();int updates=0,commits=0,cancels=0;bool allowed=true;Vector2 observed=default;
  drag.Configure(screen,(p,end)=>{observed=p;if(end)commits++;else updates++;},()=>allowed,()=>cancels++);
  drag.OnBeginDrag(point);Check(updates==1&&observed.x==40&&observed.y==-20,"actual logical projection callback");drag.OnDrag(point);Check(updates==2,"moving drag");Disable(drag);Check(cancels==1&&commits==0,"disabled component cancels without an equip/swap end");drag.OnEndDrag(point);drag.OnDrag(point);Disable(drag);Check(updates==2&&commits==0&&cancels==1,"late end/disable cannot submit after cancellation");
  drag.OnBeginDrag(point);drag.OnEndDrag(point);Check(updates==3&&commits==1&&cancels==1,"fresh drag after cancellation can end normally");Disable(drag);Check(cancels==1,"completed drag is not cancelled again");
  allowed=false;drag.OnBeginDrag(point);Disable(drag);Check(updates==3&&commits==1&&cancels==1,"rejected begin has no callbacks");allowed=true;point.button=PointerEventData.InputButton.Right;drag.OnBeginDrag(point);drag.OnEndDrag(point);Check(updates==3&&commits==1,"right mouse never submits");point.button=PointerEventData.InputButton.Left;
  drag.OnBeginDrag(point);RectTransformUtility.Available=false;drag.OnEndDrag(point);Check(commits==1&&cancels==2,"failed end projection cancels the visual without a drop");RectTransformUtility.Available=true;
  drag.OnBeginDrag(point);int newCommits=0;drag.Configure(screen,(p,end)=>{if(end)newCommits++;},onCancel:()=>cancels++);Check(cancels==3,"reconfiguration releases old capture");drag.OnEndDrag(point);Check(newCommits==0,"old end cannot submit through a new binding");drag.OnBeginDrag(point);drag.OnEndDrag(point);Check(newCommits==1,"new binding can perform its own drop");
  drag.Configure(screen,(p,end)=>{if(end){newCommits++;Disable(drag);}},onCancel:()=>cancels++);drag.OnBeginDrag(point);drag.OnEndDrag(point);Check(newCommits==2&&cancels==3,"disabling inside a successful drop does not run cancellation");
  drag.Configure(screen,(p,end)=>{if(end)newCommits++;},onCancel:()=>{cancels++;Disable(drag);});drag.OnBeginDrag(point);Disable(drag);Check(cancels==4&&newCommits==2,"reentrant disable is idempotent");
  int beforeCommit=newCommits,beforeCancel=cancels;RectTransformUtility.Available=false;drag.OnBeginDrag(point);RectTransformUtility.Available=true;drag.OnEndDrag(point);Check(newCommits==beforeCommit&&cancels==beforeCancel+1,"failed begin projection cannot turn a later release into a new transaction");
  drag.OnBeginDrag(point);RectTransformUtility.Available=false;drag.OnDrag(point);RectTransformUtility.Available=true;drag.OnEndDrag(point);Check(newCommits==beforeCommit&&cancels==beforeCancel+2,"lost projection during movement invalidates the captured gesture");
  drag.OnBeginDrag(point);drag.OnBeginDrag(point);Check(cancels==beforeCancel+3,"a replacement begin cancels the prior capture");drag.OnEndDrag(point);Check(newCommits==beforeCommit+1&&cancels==beforeCancel+3,"replacement capture submits once");
  var input=new NativeWindowClickInput();var clock=new NativeWindowClickClock();int releases=0;input.Configure(clock,()=>releases++);Time.unscaledTimeAsDouble=1;input.OnPointerUp(point);Check(releases==0,"unarmed release is ignored");input.OnPointerDown(point);input.OnPointerUp(point);Check(releases==0,"first ordinary release initializes clock");
  Time.unscaledTimeAsDouble=1.1;point.dragging=true;input.OnPointerDown(point);input.OnPointerUp(point);Check(releases==1,"native ordinary dragged release participates in the shared clock");point.dragging=false;
  Time.unscaledTimeAsDouble=1.35;input.OnPointerDown(point);input.OnPointerUp(point);Check(releases==1,"250ms is excluded by native strict comparison");
  input.OnPointerDown(point);input.Configure(clock,()=>releases+=10);input.OnPointerUp(point);Check(releases==1,"reconfigured release cannot reach a new binding");
  input.OnPointerDown(point);typeof(NativeWindowClickInput).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,null);input.OnPointerUp(point);Check(releases==1,"disabled release stays unarmed");point.button=PointerEventData.InputButton.Right;input.OnPointerDown(point);input.OnPointerUp(point);Check(releases==1,"right button cannot update or dispatch the click clock");
  Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlay=false,scope="Production drag lifecycle and shared release-clock input with isolated projection, clock source and Mono lifetime; not Unity EventSystem, GameObject destruction or physical input."}));
 }
}
