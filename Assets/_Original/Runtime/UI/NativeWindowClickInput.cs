using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DarkSaver.Original.UI
{
    // Per-window native release sequence. Unity supplies an unscaled millisecond
    // clock adapter; its process-time phase is not a Windows timing measurement.
    public sealed class NativeWindowClickInput : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
    {
        private NativeWindowClickClock clock;private Action consecutive;private bool armed;
        public void Configure(NativeWindowClickClock value,Action callback=null){armed=false;clock=value;consecutive=callback;}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)armed=true;}
        public void OnPointerUp(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!armed)return;armed=false;
            if(clock?.Release(unchecked((int)(Time.unscaledTimeAsDouble*1000)))==true)consecutive?.Invoke();
        }
        private void OnDisable(){armed=false;}
    }
}
