using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DarkSaver.Original.UI
{
    public sealed class NativeWindowDragInput:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        private RectTransform reference,target;private Rect region;private Func<bool> allowed;private int pointer;
        private readonly NativeWindowDrag state=new NativeWindowDrag();
        public void Configure(RectTransform screen,RectTransform frame,Rect title,Func<bool> canMove=null)
        {state.Cancel();reference=screen;target=frame;region=title;allowed=canMove;}
        private bool Point(PointerEventData e,out int x,out int y)
        {x=y=0;if(reference==null||target==null||!target.gameObject.activeInHierarchy||allowed?.Invoke()==false||!RectTransformUtility.ScreenPointToLocalPointInRectangle(reference,e.position,e.pressEventCamera,out var p))return false;x=Mathf.RoundToInt(p.x);y=Mathf.RoundToInt(-p.y);return true;}
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||state.Active&&e.pointerId!=pointer)return;
            state.Cancel();if(!Point(e,out int x,out int y))return;pointer=e.pointerId;
            state.Press(Mathf.RoundToInt(target.anchoredPosition.x),Mathf.RoundToInt(-target.anchoredPosition.y),x,y,Mathf.RoundToInt(region.xMin),Mathf.RoundToInt(region.yMin),Mathf.RoundToInt(region.xMax),Mathf.RoundToInt(region.yMax));
            if(state.Active){e.useDragThreshold=false;target.SetAsLastSibling();}
        }
        public void OnBeginDrag(PointerEventData e)=>Move(e);
        public void OnDrag(PointerEventData e)=>Move(e);
        private void Move(PointerEventData e)
        {
            if(!state.Active||e.pointerId!=pointer||e.button!=PointerEventData.InputButton.Left)return;
            if(!Point(e,out int x,out int y)){state.Cancel();return;}
            var at=state.Move(Mathf.RoundToInt(target.anchoredPosition.x),Mathf.RoundToInt(-target.anchoredPosition.y),x,y);target.anchoredPosition=new Vector2(at.X,-at.Y);
        }
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer&&e.button==PointerEventData.InputButton.Left)state.Cancel();}
        public void OnEndDrag(PointerEventData e){if(e.pointerId==pointer)state.Cancel();}
        private void OnDisable()=>state.Cancel();
    }
}
