using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DarkSaver.Original.UI
{
    // Capture the first left press on the message layer. The press remains owned
    // by this event target when hidden, so its later release cannot click through.
    public sealed class NativeWorldMessageInput : MonoBehaviour,IPointerDownHandler
    {
        private Action dismiss;
        public void Configure(Action action){dismiss=action;}
        public void OnPointerDown(PointerEventData e)
        {if(e.button!=PointerEventData.InputButton.Left)return;e.Use();dismiss?.Invoke();}
    }
}
