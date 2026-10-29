using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DarkSaver.UI
{
    // Input routing shared by native artwork buttons, tooltip hits and item drags.
    public sealed class CanvasUiEvents:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,IDragHandler,IEndDragHandler,IInitializePotentialDragHandler,ISubmitHandler,ICancelHandler
    {
        public string Tooltip;
        public bool IsHovered {get;private set;}
        public bool SuppressClick;
        public Action Enter,Exit,Submit,Cancel;
        public Action<PointerEventData> Down,Move,Up;
        public void OnPointerEnter(PointerEventData data){IsHovered=true;Enter?.Invoke();}
        public void OnPointerExit(PointerEventData data){IsHovered=false;Exit?.Invoke();}
        public void OnPointerDown(PointerEventData data){SuppressClick=false;Down?.Invoke(data);}
        public void OnPointerUp(PointerEventData data)=>Up?.Invoke(data);
        public void OnInitializePotentialDrag(PointerEventData data){data.useDragThreshold=false;}
        public void OnDrag(PointerEventData data)=>Move?.Invoke(data);
        public void OnEndDrag(PointerEventData data){}
        public void OnSubmit(BaseEventData data)=>Submit?.Invoke();
        public void OnCancel(BaseEventData data)=>Cancel?.Invoke();
        private void OnDisable(){IsHovered=false;SuppressClick=false;Exit?.Invoke();Cancel?.Invoke();}
    }
}
