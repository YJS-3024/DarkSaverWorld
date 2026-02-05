using GlobalEnum;
using UnityEngine;

public class CommanderPage : MonoBehaviour
{
    [SerializeField] private CommandButton btnMove;
    // [SerializeField] private CommandButton btnItem;
    [SerializeField] private CommandButton btnAttack;
    [SerializeField] private CommandButton btnMagicSkill;
    // [SerializeField] private CommandButton btnJobSkill;
    // [SerializeField] private CommandButton btnRest;
    // [SerializeField] private CommandButton btnOperation;
    // [SerializeField] private CommandButton btnOption;

    public RectTransform Rect => (RectTransform)transform;

    public void Awake()
    {
        gameObject.SetActive(false);

        btnMove.SetCommand(TableManager.I.String.GetString("Action_Move"), OnClick_Move);
        // btnItem.SetCommand(TableManager.I.String.GetString("Action_UseItem"), OnClick_UseItem);
        btnAttack.SetCommand(TableManager.I.String.GetString("Action_Attack"), OnClick_Attack);
        btnMagicSkill.SetCommand(TableManager.I.String.GetString("Action_Magic"), OnClick_MagicSkill);
        // btnJobSkill.SetCommand(TableManager.I.String.GetString("Action_SpecialAttack"), OnClick_JobSkill);
        // btnRest.SetCommand(TableManager.I.String.GetString("Action_Rest"), OnClick_Rest);
        // btnOperation.SetCommand(TableManager.I.String.GetString("Action_Operation"), OnClick_Operation);
        // btnOption.SetCommand(TableManager.I.String.GetString("Action_Option"), OnClick_Option);
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
        UIManager.I.ShowPopup(UIType.MagicSkillPopup);
        
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
