using UnityEngine;
using UnityEngine.EventSystems;

public class InputUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SceneController.I.CurScene.ClickEvent(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
    }
}
