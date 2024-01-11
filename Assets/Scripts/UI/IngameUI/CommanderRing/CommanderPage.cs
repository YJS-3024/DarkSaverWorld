using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UI.Extension;
using UnityEngine;
using UnityEngine.Serialization;

public class CommanderPage : MonoBehaviour
{
    [SerializeField] private ButtonEx btnMove;
    [SerializeField] private ButtonEx btnItem;
    [SerializeField] private ButtonEx btnAttack;
    [SerializeField] private ButtonEx btnMagicSkill;
    [SerializeField] private ButtonEx btnJobSkill;
    [SerializeField] private ButtonEx btnRest;
    [SerializeField] private ButtonEx btnOperation;
    [SerializeField] private ButtonEx btnOption;

    public RectTransform Rect => (RectTransform)transform;

    public void Awake()
    {
        gameObject.SetActive(false);

        btnMove.onClick.AddListener(OnClick_Move);
        btnMove.ButtonString = TableManager.I.String.GetString("Action_Move");

        btnItem.onClick.AddListener(OnClick_UseItem);
        btnItem.ButtonString = TableManager.I.String.GetString("Action_UseItem");

        btnAttack.onClick.AddListener(OnClick_Attack);
        btnAttack.ButtonString = TableManager.I.String.GetString("Action_Attack");

        btnMagicSkill.onClick.AddListener(OnClick_MagicSkill);
        btnMagicSkill.ButtonString = TableManager.I.String.GetString("Action_Magic");

        btnJobSkill.onClick.AddListener(OnClick_JobSkill);
        btnJobSkill.ButtonString = TableManager.I.String.GetString("Action_SpecialAttack");

        btnRest.onClick.AddListener(OnClick_Rest);
        btnRest.ButtonString = TableManager.I.String.GetString("Action_Rest");

        btnOperation.onClick.AddListener(OnClick_Operation);
        btnOperation.ButtonString = TableManager.I.String.GetString("Action_Operation");

        btnOption.onClick.AddListener(OnClick_Option);
        btnOption.ButtonString = TableManager.I.String.GetString("Action_Option");
    }

    private void OnClick_Move()
    {
        PlayerManager.I.MainPlayer.CharAction = eCharAction.Move;
        var pos = PlayerManager.I.MainPlayer.transform.position;
        var range = PlayerManager.I.MainPlayer.MoveRange;

        PlayerManager.I.CreatePlates(pos, eCharAction.Move, range);

        gameObject.SetActive(false);
    }

    private void OnClick_UseItem()
    {
        gameObject.SetActive(false);
    }

    private void OnClick_Attack()
    {
        PlayerManager.I.MainPlayer.CharAction = eCharAction.Attack;
        var pos = PlayerManager.I.MainPlayer.transform.position;
        var range = PlayerManager.I.MainPlayer.AttackRange;

        PlayerManager.I.CreatePlates(pos, eCharAction.Attack, range);

        gameObject.SetActive(false);
    }

    private void OnClick_MagicSkill()
    {
        gameObject.SetActive(false);
    }

    private void OnClick_JobSkill()
    {
        gameObject.SetActive(false);
    }

    private void OnClick_Rest()
    {
        gameObject.SetActive(false);
    }

    private void OnClick_Operation()
    {
        gameObject.SetActive(false);
    }

    private void OnClick_Option()
    {
        gameObject.SetActive(false);
    }
}
