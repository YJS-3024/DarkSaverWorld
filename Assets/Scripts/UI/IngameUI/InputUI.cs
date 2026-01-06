using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
{
    public bool IsPressed { get; set; } = false;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
        SceneController.I.CurScene.ClickEvent(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (IsPressed)
        {
            SceneController.I.CurScene.PressEvent(eventData.position);
        }
    }
}
