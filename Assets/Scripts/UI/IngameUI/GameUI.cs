using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] private RectTransform commanderRect;

    public void Awake()
    {
        commanderRect.gameObject.SetActive(false);
    }

    public void SetCommander(Vector3 worldPos)
    {
        //  commanderRect의 렉트 앵커가 min,max가 모두 0이여야한다.
        var screenPoint = Camera.main.WorldToScreenPoint(worldPos);
        var uiPosX = screenPoint.x / Screen.width * UIManager.I.CanvasScale.x;
        var uiPosY = screenPoint.y / Screen.height * UIManager.I.CanvasScale.y;
        commanderRect.anchoredPosition = new Vector2(uiPosX,uiPosY);;

        commanderRect.gameObject.SetActive(!commanderRect.gameObject.activeSelf);
    }

}
