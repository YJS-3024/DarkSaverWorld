using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkSaver.Original.World
{
    public sealed class NativeMapPointer : MonoBehaviour, IPointerClickHandler
    {
        private Action<int,int> clicked;
        public void Configure(Action<int,int> callback) { clicked = callback; }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.pressEventCamera,out var point))
                clicked?.Invoke(Mathf.FloorToInt(point.x/32),Mathf.FloorToInt(-point.y/32));
        }
    }
}
