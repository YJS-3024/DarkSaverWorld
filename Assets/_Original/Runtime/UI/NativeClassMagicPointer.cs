using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DarkSaver.Original.UI
{
    //4646A0 starts the32px cursor-centered icon on press, without a uGUI drag threshold.
    public sealed class NativeClassMagicPointer:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        private RectTransform reference;private Func<bool> begin,valid;private Action<Vector2,bool> action;private Action cancel;
        private bool captured;private int pointer,generation;
        public void Configure(RectTransform frame,Func<bool> canBegin,Func<bool> stillValid,Action<Vector2,bool> callback,Action cancelled)
        {Cancel();generation++;reference=frame;begin=canBegin;valid=stillValid;action=callback;cancel=cancelled;}
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||captured&&pointer!=e.pointerId)return;Cancel();int version=generation;
            bool allowed=begin?.Invoke()!=false;if(version!=generation||!allowed)return;
            captured=true;pointer=e.pointerId;e.useDragThreshold=false;e.eligibleForClick=false;if(!Dispatch(e,false))Cancel();
        }
        public void OnBeginDrag(PointerEventData e)=>Move(e,false);
        public void OnDrag(PointerEventData e)=>Move(e,false);
        public void OnPointerUp(PointerEventData e)=>Move(e,true);
        public void OnEndDrag(PointerEventData e)=>Move(e,true);
        private void Move(PointerEventData e,bool end)
        {
            if(!captured||pointer!=e.pointerId||e.button!=PointerEventData.InputButton.Left)return;int version=generation;
            bool current=valid?.Invoke()!=false;if(version!=generation||!captured)return;if(!current){Cancel();return;}
            if(end)captured=false;if(!Dispatch(e,end)){if(end)cancel?.Invoke();else Cancel();}
        }
        private bool Dispatch(PointerEventData e,bool end)
        {if(reference==null||!RectTransformUtility.ScreenPointToLocalPointInRectangle(reference,e.position,e.pressEventCamera,out var p))return false;action?.Invoke(new Vector2(p.x,-p.y),end);return true;}
        private void Cancel(){generation++;if(!captured)return;captured=false;cancel?.Invoke();}
        private void OnDisable()=>Cancel();
        private void OnApplicationFocus(bool focused){if(!focused)Cancel();}
    }
}
