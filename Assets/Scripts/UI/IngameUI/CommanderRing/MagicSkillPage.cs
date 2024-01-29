using GlobalEnum;
using UnityEngine;

public class MagicSkillPage : MonoBehaviour
{
    public void OnClick_SelectSkill(int skillId)
    {
        var skillData = TableManager.I.Skill.GetSkill(skillId);
        if (skillData is null)
            return;

        var actionType = skillData.SkillType == 1
            ? eCharAction.Magic_Attack
            : eCharAction.Magic_Buff;

        PlayerManager.I.SelectSkillId = skillId;
        // var mainPlayer = PlayerManager.I.MainPlayer;
        // mainPlayer.MagicSkill(skillId);

        var pos = PlayerManager.I.MainPlayer.transform.position;
        var range = skillData.SkillRange;
        PlayerManager.I.CreatePlates(pos, actionType, range);

        UIManager.I.GameUI.gameObject.SetActive(false);
    }
}
