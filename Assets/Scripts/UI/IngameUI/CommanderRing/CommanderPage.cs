using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UI.Extension;
using UnityEngine;
using UnityEngine.Serialization;

public class CommanderPage : MonoBehaviour
{
    [SerializeField] private CommandButton btnMove;
    [SerializeField] private CommandButton btnItem;
    [SerializeField] private CommandButton btnAttack;
    [SerializeField] private CommandButton btnMagicSkill;
    [SerializeField] private CommandButton btnJobSkill;
    [SerializeField] private CommandButton btnRest;
    [SerializeField] private CommandButton btnOperation;
    [SerializeField] private CommandButton btnOption;

    public RectTransform Rect => (RectTransform)transform;

    public void Awake()
    {
        gameObject.SetActive(false);

        btnMove.SetCommand(TableManager.I.String.GetString("Action_Move"), OnClick_Move);
        btnItem.SetCommand(TableManager.I.String.GetString("Action_UseItem"), OnClick_UseItem);
        btnAttack.SetCommand(TableManager.I.String.GetString("Action_Attack"), OnClick_Attack);
        btnMagicSkill.SetCommand(TableManager.I.String.GetString("Action_Magic"), OnClick_MagicSkill);
        btnJobSkill.SetCommand(TableManager.I.String.GetString("Action_SpecialAttack"), OnClick_JobSkill);
        btnRest.SetCommand(TableManager.I.String.GetString("Action_Rest"), OnClick_Rest);
        btnOperation.SetCommand(TableManager.I.String.GetString("Action_Operation"), OnClick_Operation);
        btnOption.SetCommand(TableManager.I.String.GetString("Action_Option"), OnClick_Option);
    }

    public void OnClick_Move()
    {
        gameObject.SetActive(false);
    }

    private void OnClick_UseItem()
    {
        gameObject.SetActive(false);
    }

    private void OnClick_Attack()
    {
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
