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
 public class GameObject{public bool activeInHierarchy=true;}
 public class RectTransform{public Vector2 anchoredPosition;public GameObject gameObject=new GameObject();public void SetAsLastSibling(){}}
 public class Camera{}
 public static class Time{public static double unscaledTimeAsDouble;}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Rect{public float xMin,yMin,xMax,yMax;public Rect(float x,float y,float width,float height){xMin=x;yMin=y;xMax=x+width;yMax=y+height;}}
 public static class Mathf{public static int RoundToInt(float value)=>(int)Math.Round(value);}
 public static class RectTransformUtility
 {public static bool Available=true;public static bool ScreenPointToLocalPointInRectangle(RectTransform r,Vector2 point,Camera camera,out Vector2 result){result=point;return Available;}}
}
namespace UnityEngine.EventSystems
{
 public class PointerEventData{public enum InputButton{Left,Right}public InputButton button;public Vector2 position;public Camera pressEventCamera;public bool dragging,eligibleForClick=true,useDragThreshold=true;public int pointerId=-1;}
 public interface IPointerDownHandler{void OnPointerDown(PointerEventData e);}
 public interface IPointerUpHandler{void OnPointerUp(PointerEventData e);}
 public interface IBeginDragHandler{void OnBeginDrag(PointerEventData e);}
 public interface IDragHandler{void OnDrag(PointerEventData e);}
 public interface IEndDragHandler{void OnEndDrag(PointerEventData e);}
}
namespace DarkSaver.Original.Tutorial
{public sealed class WaitingWindowDefinition{public int id,width,height,minWidth,minHeight,minDragX,minDragY,minDragWidth,minDragHeight,dragX,dragY,dragWidth,dragHeight;public bool forceCenter;}}
internal static class DragLifecycleChecks
{
 static int assertions;static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
 static void Disable(NativeTutorialDrag drag)=>typeof(NativeTutorialDrag).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(drag,null);
 static void WindowLifecycle()
 {
  var frame=new RectTransform{anchoredPosition=new Vector2(100,-50)};var screen=new RectTransform();var drag=new NativeWindowDragInput();bool allowed=true;
  var point=new PointerEventData{position=new Vector2(115,-65)};drag.Configure(screen,frame,new Rect(10,10,50,20),()=>allowed);
  drag.OnPointerDown(point);Check(!point.useDragThreshold,"native window starts without uGUI drag threshold");point.position=new Vector2(125,-75);drag.OnBeginDrag(point);Check(frame.anchoredPosition.x==110&&frame.anchoredPosition.y==-60,"first movement retains actual pointer-down offset");
  point.position=new Vector2(-200,80);drag.OnDrag(point);Check(frame.anchoredPosition.x==-215&&frame.anchoredPosition.y==95,"native body is not clamped to the screen");
  point.pointerId=1;point.position=new Vector2(300,-200);drag.OnDrag(point);drag.OnPointerUp(point);Check(frame.anchoredPosition.x==-215,"foreign pointer cannot move or release capture");
  point.pointerId=-1;drag.OnDrag(point);Check(frame.anchoredPosition.x==285&&frame.anchoredPosition.y==-185,"original pointer retains capture");drag.OnPointerUp(point);point.position=new Vector2(400,-300);drag.OnDrag(point);Check(frame.anchoredPosition.x==285,"release cancels movement");
  frame.anchoredPosition=new Vector2(100,-50);point.position=new Vector2(115,-65);drag.OnPointerDown(point);typeof(NativeWindowDragInput).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(drag,null);point.position=new Vector2(200,-100);drag.OnBeginDrag(point);Check(frame.anchoredPosition.x==100,"disabled gesture cannot resume on a late begin");
  point.position=new Vector2(115,-65);drag.OnPointerDown(point);RectTransformUtility.Available=false;point.position=new Vector2(200,-100);drag.OnDrag(point);RectTransformUtility.Available=true;drag.OnDrag(point);Check(frame.anchoredPosition.x==100,"projection failure cancels rather than moving later");
  point.position=new Vector2(115,-65);drag.OnPointerDown(point);allowed=false;point.position=new Vector2(200,-100);drag.OnDrag(point);allowed=true;drag.OnDrag(point);Check(frame.anchoredPosition.x==100,"invalidated window cannot resume an old pointer");
  point.position=new Vector2(160,-65);drag.OnPointerDown(point);point.position=new Vector2(250,-150);drag.OnDrag(point);Check(frame.anchoredPosition.x==100,"title right boundary is excluded like PtInRect");
  point.position=new Vector2(115,-65);point.button=PointerEventData.InputButton.Right;drag.OnPointerDown(point);point.position=new Vector2(200,-100);drag.OnDrag(point);Check(frame.anchoredPosition.x==100,"right pointer never starts a window move");
  point.button=PointerEventData.InputButton.Left;point.position=new Vector2(115,-65);drag.OnPointerDown(point);var replacement=new RectTransform{anchoredPosition=new Vector2(10,-10)};drag.Configure(screen,replacement,new Rect(0,0,100,100));point.position=new Vector2(200,-100);drag.OnDrag(point);Check(replacement.anchoredPosition.x==10,"rebind cancels the previous window capture");
  replacement.gameObject.activeInHierarchy=false;point.position=new Vector2(15,-15);drag.OnPointerDown(point);replacement.gameObject.activeInHierarchy=true;point.position=new Vector2(100,-100);drag.OnDrag(point);Check(replacement.anchoredPosition.x==10,"hidden window cannot capture and resume");
  point.position=new Vector2(15,-15);drag.OnPointerDown(point);point.position=new Vector2(100,-100);drag.OnBeginDrag(point);Check(replacement.anchoredPosition.x==95&&replacement.anchoredPosition.y==-95,"fresh gesture after rebind");drag.OnEndDrag(point);point.position=new Vector2(200,-200);drag.OnDrag(point);Check(replacement.anchoredPosition.x==95,"end closes capture without a late movement");
 }
 static void ClassMagicLifecycle()
 {
  var input=new NativeClassMagicPointer();var frame=new RectTransform();var e=new PointerEventData{position=new Vector2(225,-63)};int moves=0,drops=0,cancels=0;bool allowed=true,current=true;Vector2 observed=default;
  Action bind=()=>input.Configure(frame,()=>allowed,()=>current,(p,end)=>{observed=p;if(end)drops++;else moves++;},()=>cancels++);bind();
  input.OnPointerDown(e);Check(moves==1&&!e.useDragThreshold&&!e.eligibleForClick&&observed.y==63,"class magic ghost begins at pointer-down without click/threshold");
  e.pointerId=4;input.OnDrag(e);input.OnPointerUp(e);Check(moves==1&&drops==0,"class magic capture rejects another pointer");e.pointerId=-1;input.OnPointerUp(e);input.OnEndDrag(e);Check(drops==1&&cancels==0,"class magic release commits exactly once");
  input.OnPointerDown(e);typeof(NativeClassMagicPointer).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,null);input.OnPointerUp(e);Check(drops==1&&cancels==1,"disabled magic input cancels without request");
  input.OnPointerDown(e);current=false;input.OnDrag(e);current=true;input.OnPointerUp(e);Check(drops==1&&cancels==2,"changed state cannot resume an old magic gesture");
  input.OnPointerDown(e);bind();input.OnPointerUp(e);Check(drops==1&&cancels==3,"rebind cannot drop through replacement callbacks");
  input.OnPointerDown(e);RectTransformUtility.Available=false;input.OnPointerUp(e);RectTransformUtility.Available=true;Check(drops==1&&cancels==4,"failed release projection cancels magic ghost");
  allowed=false;input.OnPointerDown(e);input.OnPointerUp(e);Check(drops==1&&cancels==4,"unavailable spell cannot begin");allowed=true;
  input.OnPointerDown(e);typeof(NativeClassMagicPointer).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,new object[]{false});input.OnPointerUp(e);Check(drops==1&&cancels==5,"focus loss cancels without teaching magic");
  input.Configure(frame,()=>{bind();return true;},()=>true,(p,end)=>drops++,()=>cancels++);input.OnPointerDown(e);input.OnPointerUp(e);Check(drops==1,"reentrant begin rebind cannot capture a new gesture");
 }
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
  var captured=new NativeTutorialDrag();bool current=true;int moved=0,dropped=0,aborted=0;point.button=PointerEventData.InputButton.Left;
  captured.Configure(screen,(p,end)=>{if(end)dropped++;else moved++;},onCancel:()=>aborted++,continueAllowed:()=>current);
  captured.OnBeginDrag(point);current=false;captured.OnDrag(point);Check(aborted==1&&dropped==0,"ownership invalidated during movement cancels without drop");current=true;captured.OnDrag(point);captured.OnEndDrag(point);Check(moved==1&&dropped==0,"restored data cannot revive a cancelled pointer gesture");
  captured.OnBeginDrag(point);captured.OnEndDrag(point);Check(dropped==1,"a fresh begin may commit with current ownership");
  captured.OnBeginDrag(point);current=false;captured.OnEndDrag(point);Check(aborted==2&&dropped==1,"ownership invalidated at release cancels before commit");
  current=true;captured.Configure(screen,(p,end)=>{if(end)dropped++;},onCancel:()=>aborted++,continueAllowed:()=>{Disable(captured);return true;});captured.OnBeginDrag(point);captured.OnEndDrag(point);Check(aborted==3&&dropped==1,"reentrant disable during validation cannot commit after cancellation");
  captured.Configure(screen,(p,end)=>{if(end)dropped++;},onCancel:()=>aborted++,continueAllowed:()=>{captured.Configure(screen,(p,end)=>{if(end)dropped+=10;});return true;});captured.OnBeginDrag(point);captured.OnEndDrag(point);Check(aborted==4&&dropped==1,"validation-time rebinding cannot dispatch the old release to a new owner");
  WindowLifecycle();ClassMagicLifecycle();
  Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlay=false,scope="Production item/window drag lifecycle and shared release-clock input with isolated projection, clock source and Mono lifetime; not Unity EventSystem, GameObject destruction or physical input."}));
 }
}
