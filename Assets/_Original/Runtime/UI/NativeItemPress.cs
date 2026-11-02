using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DarkSaver.Original.UI
{
    // The original item caption exists from pointer-down until release, not only
    // after uGUI's drag threshold has been crossed.
    public sealed class NativeItemPress : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
    {
        private Action<bool> changed;private bool pressed;
        public void Configure(Action<bool> value){Release();changed=value;}
        public void OnPointerDown(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;pressed=true;changed?.Invoke(true);}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Release();}
        private void OnDisable()=>Release();
        private void Release(){if(!pressed)return;pressed=false;changed?.Invoke(false);}
    }
}
