using GlobalEnum;
using UnityEngine;

public class MagicSkillPage : MonoBehaviour
{
    public void OnClick_SelectSkill(int skillId)
    {
        var pos = PlayerManager.I.MainPlayer.transform.position;
        PlayerManager.I.CreatePlates(pos, skillId);

        UIManager.I.GameUI.gameObject.SetActive(false);
    }
}
