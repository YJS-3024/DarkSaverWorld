using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DarkSaver.Original.UI
{
    public sealed class NativeBattleWindowPointer:MonoBehaviour,IPointerDownHandler
    {
        private Action<int,int,int> pressed;
        public void Configure(Action<int,int,int> action)=>pressed=action;
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var p))
                pressed?.Invoke(Mathf.FloorToInt(p.x),Mathf.FloorToInt(-p.y),unchecked((int)(Time.unscaledTimeAsDouble*1000)));
        }
    }
}
