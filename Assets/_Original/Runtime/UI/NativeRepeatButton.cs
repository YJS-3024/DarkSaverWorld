using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DarkSaver.Original.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class NativeRepeatButton : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,IPointerExitHandler
    {
        private Action repeat;private bool down,over;private double last;
        public void Configure(Action action){repeat=action;}
        public void OnPointerDown(PointerEventData e)
        {if(e.button!=PointerEventData.InputButton.Left||!GetComponent<Button>().IsInteractable())return;down=over=true;last=Time.realtimeSinceStartupAsDouble*1000;repeat?.Invoke();}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)down=false;}
        public void OnPointerEnter(PointerEventData e){over=true;}
        public void OnPointerExit(PointerEventData e){over=false;}
        private void OnDisable(){down=over=false;}
        private void Update()
        {
            double now=Time.realtimeSinceStartupAsDouble*1000;
            // Native input-loop boundary is strictly greater than30ms, with at most one dispatch per poll.
            if(down&&over&&now-last>30&&GetComponent<Button>().IsInteractable()){last=now;repeat?.Invoke();}
        }
    }
}
