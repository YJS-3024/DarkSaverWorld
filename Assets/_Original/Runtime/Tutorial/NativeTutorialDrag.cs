using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkSaver.Original.Tutorial
{
    public sealed class NativeTutorialDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform reference;
        private Action<Vector2, bool> callback;
        private Func<bool> canBegin;
        private bool dragging;
        public void Configure(RectTransform screen, Action<Vector2, bool> action, Func<bool> allowed = null)
        { reference = screen; callback = action; canBegin = allowed; }
        public void OnBeginDrag(PointerEventData e)
        { dragging = e.button == PointerEventData.InputButton.Left && (canBegin == null || canBegin()); if (dragging) Dispatch(e, false); }
        public void OnDrag(PointerEventData e) { if (dragging) Dispatch(e, false); }
        public void OnEndDrag(PointerEventData e) { if (dragging) Dispatch(e, true); dragging = false; }
        private void Dispatch(PointerEventData e, bool end)
        {
            if (reference != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(reference, e.position, e.pressEventCamera, out var p))
                callback?.Invoke(new Vector2(p.x, -p.y), end);
        }
        private void OnDisable() { dragging = false; }
    }
}
