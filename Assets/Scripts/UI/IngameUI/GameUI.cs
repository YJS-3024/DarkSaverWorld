using System;
using GlobalEnum;
using UI.Extension;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private RectTransform commanderRect;

    [SerializeField] private ButtonEx btnMove;
    [SerializeField] private ButtonEx btnItem;
    [SerializeField] private ButtonEx btnAttack;
    [SerializeField] private ButtonEx btnMagic;
    [SerializeField] private ButtonEx btnSpecialAttack;
    [SerializeField] private ButtonEx btnRest;
    [SerializeField] private ButtonEx btnOperation;
    [SerializeField] private ButtonEx btnOption;

    public void Awake()
    {
        commanderRect.gameObject.SetActive(false);

        btnMove.onClick.AddListener(OnClick_Move);
        btnMove.ButtonString = TableManager.I.String.GetString("Action_Move");

        btnItem.onClick.AddListener(OnClick_UseItem);
        btnItem.ButtonString = TableManager.I.String.GetString("Action_UseItem");

        btnAttack.onClick.AddListener(OnClick_Attack);
        btnAttack.ButtonString = TableManager.I.String.GetString("Action_Attack");

        btnMagic.onClick.AddListener(OnClick_Magic);
        btnMagic.ButtonString = TableManager.I.String.GetString("Action_Magic");

        btnSpecialAttack.onClick.AddListener(OnClick_SpecialAttack);
        btnSpecialAttack.ButtonString = TableManager.I.String.GetString("Action_SpecialAttack");

        btnRest.onClick.AddListener(OnClick_Rest);
        btnRest.ButtonString = TableManager.I.String.GetString("Action_Rest");

        btnOperation.onClick.AddListener(OnClick_Operation);
        btnOperation.ButtonString = TableManager.I.String.GetString("Action_Operation");

        btnOption.onClick.AddListener(OnClick_Option);
        btnOption.ButtonString = TableManager.I.String.GetString("Action_Option");
    }

    public void SetCommander(Vector3 worldPos)
    {
        //  commanderRect의 렉트 앵커가 min,max가 모두 0이여야한다.
        var screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        var uiPosX = screenPoint.x - (UIManager.I.CanvasScale.x * 0.5f);
        var uiPosY = screenPoint.y - (UIManager.I.CanvasScale.y * 0.5f);
        commanderRect.anchoredPosition = new Vector2(uiPosX, uiPosY);

        commanderRect.gameObject.SetActive(!commanderRect.gameObject.activeSelf);
    }

    private void OnClick_Move()
    {
        PlayerManager.I.MainPlayer.CharAction = eCharAction.Move;
        var pos = PlayerManager.I.MainPlayer.transform.position;
        PlayerManager.I.CreatePlates(pos);

        commanderRect.gameObject.SetActive(false);
    }

    private void OnClick_UseItem()
    {
        commanderRect.gameObject.SetActive(false);
    }

    private void OnClick_Attack()
    {
        PlayerManager.I.MainPlayer.CharAction = eCharAction.Attack;
        var pos = PlayerManager.I.MainPlayer.transform.position;
        PlayerManager.I.CreatePlates(pos);

        commanderRect.gameObject.SetActive(false);
    }

    private void OnClick_Magic()
    {
        commanderRect.gameObject.SetActive(false);
    }

    private void OnClick_SpecialAttack()
    {
        commanderRect.gameObject.SetActive(false);
    }

    private void OnClick_Rest()
    {
        commanderRect.gameObject.SetActive(false);
    }

    private void OnClick_Operation()
    {
        commanderRect.gameObject.SetActive(false);
    }

    private void OnClick_Option()
    {
        commanderRect.gameObject.SetActive(false);
    }
}