using GlobalEnum;
using UnityEngine;

public class CommandPanel : MonoBehaviour
{


    public void OnClick_Move()
    {
        var pos = PlayerManager.I.MainPlayer.transform.position;
        var range = PlayerManager.I.MainPlayer.MoveRange;

        PlayerManager.I.CreatePlates(pos, eCharAction.Move, range);

        UIManager.I.GameUI.gameObject.SetActive(false);
    }

    public void OnClick_UseItem()
    {
    }

    public void OnClick_Attack()
    {
        var pos = PlayerManager.I.MainPlayer.transform.position;
        var range = PlayerManager.I.MainPlayer.AttackRange;

        PlayerManager.I.CreatePlates(pos, eCharAction.Attack, range);

        UIManager.I.GameUI.gameObject.SetActive(false);
    }

    public void OnClick_Magic()
    {
        EventManager.I.CallEvent("Move_MagicSkillPage")?.Invoke();

        // UIManager.I.GameUI.gameObject.SetActive(false);
    }

    public void OnClick_JobSkill()
    {
    }

    public void OnClick_Rest()
    {
        UIManager.I.GameUI.gameObject.SetActive(false);
    }

    public void OnClick_Operation()
    {
    }

    public void OnClick_Option()
    {
    }
}
