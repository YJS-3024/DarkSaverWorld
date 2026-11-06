using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkSaver.Original.Tutorial
{
    public sealed class NativeTutorialDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform reference;
        private Action<Vector2, bool> callback;
        private Func<bool> canBegin,canContinue;
        private Action cancelled;
        private bool dragging;
        public void Configure(RectTransform screen, Action<Vector2, bool> action, Func<bool> allowed = null,Action onCancel=null,Func<bool> continueAllowed=null)
        {Cancel();reference = screen; callback = action; canBegin = allowed;cancelled=onCancel;canContinue=continueAllowed;}
        public void OnBeginDrag(PointerEventData e)
        { Cancel();dragging = e.button == PointerEventData.InputButton.Left && (canBegin == null || canBegin()); if (dragging&&!Dispatch(e, false))Cancel(); }
        public void OnDrag(PointerEventData e) { if (Continue()&&!Dispatch(e, false))Cancel(); }
        public void OnEndDrag(PointerEventData e) { if (!Continue())return;dragging=false;if(!Dispatch(e, true))cancelled?.Invoke(); }
        private bool Continue()
        {
            if(!dragging)return false;if(canContinue==null)return true;
            bool valid=canContinue();if(!dragging)return false;if(!valid){Cancel();return false;}return true;
        }
        private bool Dispatch(PointerEventData e, bool end)
        {
            if (reference != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(reference, e.position, e.pressEventCamera, out var p))
            {callback?.Invoke(new Vector2(p.x, -p.y), end);return true;}return false;
        }
        private void Cancel(){if(!dragging)return;dragging=false;cancelled?.Invoke();}
        private void OnDisable()=>Cancel();
    }
}
