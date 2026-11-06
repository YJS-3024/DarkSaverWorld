using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkSaver.Original.World
{
    // Saver4119B0: signed32 clock delta must be strictly greater than30.
    // One repeat per input poll; a delayed poll never replays missed repeats.
    internal struct NativePointerRepeatClock
    {
        public const int ThresholdMilliseconds=30;
        internal int LastTick { get; private set; }
        internal int Repeats { get; private set; }
        private bool active;
        internal void Begin(int now){active=true;LastTick=now;Repeats=0;}
        internal void Reset(){active=false;LastTick=Repeats=0;}
        internal bool TryRepeat(int now)
        {
            if(!active||unchecked(now-LastTick)<=ThresholdMilliseconds)return false;
            LastTick=now;Repeats=unchecked(Repeats+1);return true;
        }
    }

    public sealed class NativeMapPointer : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IInitializePotentialDragHandler
    {
        private Action<int,int> clicked;
        private Func<bool> continuousAllowed;
        private bool held,dragged;private int lastX,lastY;
        private PointerEventData heldPointer;
        private NativePointerRepeatClock repeat;
        private static int ClockMilliseconds=>unchecked((int)(long)(Time.realtimeSinceStartupAsDouble*1000.0));
        private readonly List<RaycastResult> hits=new List<RaycastResult>();
        public void Configure(Action<int,int> callback) { clicked = callback; }
        public void ConfigureContinuous(Func<bool> allowed){continuousAllowed=allowed;}
        public void OnInitializePotentialDrag(PointerEventData data)
        {
            if(continuousAllowed?.Invoke()==true)data.useDragThreshold=false;
            else data.pointerDrag=null; // Battle grid retains click-on-release semantics.
        }
        public void OnPointerDown(PointerEventData data)
        {
            if(data.button!=PointerEventData.InputButton.Left)return;
            held=true;dragged=false;heldPointer=data;repeat.Begin(ClockMilliseconds);
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.pressEventCamera,out var point))
            {
                lastX=Mathf.FloorToInt(point.x/32);lastY=Mathf.FloorToInt(-point.y/32);
                if(continuousAllowed?.Invoke()==true){dragged=true;clicked?.Invoke(lastX,lastY);}
            }
        }
        public void OnPointerUp(PointerEventData data){if(data.button==PointerEventData.InputButton.Left){held=false;heldPointer=null;repeat.Reset();}}
        private void OnDisable(){held=dragged=false;heldPointer=null;repeat.Reset();}
        private void Update()
        {
            if(!held)return;
            if(!Input.GetMouseButton(0)){held=false;heldPointer=null;repeat.Reset();return;}
            if(heldPointer!=null)OnDrag(heldPointer); // Camera scrolling can change the tile under a stationary cursor.
        }
        public void OnDrag(PointerEventData data)
        {
            if(!held||data.button!=PointerEventData.InputButton.Left)return;
            heldPointer=data;
            if(!repeat.TryRepeat(ClockMilliseconds)||continuousAllowed?.Invoke()!=true)return;
            var events=EventSystem.current;if(events==null)return;
            hits.Clear();events.RaycastAll(data,hits);
            if(hits.Count==0||hits[0].gameObject!=gameObject)return;
            if(!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform,data.position,data.pressEventCamera))return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.pressEventCamera,out var point))
            {
                int x=Mathf.FloorToInt(point.x/32),y=Mathf.FloorToInt(-point.y/32);
                if(x==lastX&&y==lastY)return;
                lastX=x;lastY=y;dragged=true;clicked?.Invoke(x,y);
            }
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            if(dragged){dragged=false;return;}
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.pressEventCamera,out var point))
                clicked?.Invoke(Mathf.FloorToInt(point.x/32),Mathf.FloorToInt(-point.y/32));
        }
    }
}
