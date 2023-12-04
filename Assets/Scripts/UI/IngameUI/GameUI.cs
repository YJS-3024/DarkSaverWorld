using System;
using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private RectTransform commanderRect;

    [SerializeField] private Button btnMove;
    [SerializeField] private Button btnAttack;
    [SerializeField] private Button btnRecess;
    [SerializeField] private Button btnOption;

    public void Awake()
    {
        commanderRect.gameObject.SetActive(false);

        btnMove.onClick.AddListener(OnClick_Move);
    }

    public void SetCommander(Vector3 worldPos)
    {
        //  commanderRect의 렉트 앵커가 min,max가 모두 0이여야한다.
        var screenPoint = Camera.main.WorldToScreenPoint(worldPos);
        var uiPosX = screenPoint.x - (UIManager.I.CanvasScale.x * 0.5f);
        var uiPosY = screenPoint.y - (UIManager.I.CanvasScale.y * 0.5f);
        commanderRect.localPosition = new Vector2(uiPosX, uiPosY);

        commanderRect.gameObject.SetActive(!commanderRect.gameObject.activeSelf);
    }

    private void OnClick_Move()
    {
        PlayerManager.I.PlayerChar.CharAction = eCharAction.Move;
        var pos = PlayerManager.I.PlayerChar.transform.position;
        PlayerManager.I.CreateMovePlates(pos);

        commanderRect.gameObject.SetActive(false);
    }
}
