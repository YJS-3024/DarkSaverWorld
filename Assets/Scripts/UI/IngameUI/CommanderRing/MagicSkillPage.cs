using UnityEngine;

public class MagicSkillPage : MonoBehaviour
{
    public void OnClick_SelectSkill(int skillId)
    {
        var skillData = TableManager.I.Skill.GetSkill(skillId);
        if (skillData is null)
            return;

        var mainPlayer = PlayerManager.I.MainPlayer;
        mainPlayer.MagicSkill(skillId);
    }
}
