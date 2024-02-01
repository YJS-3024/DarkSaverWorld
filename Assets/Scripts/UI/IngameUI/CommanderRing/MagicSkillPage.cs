using System.Collections.Generic;
using UnityEngine;

public class MagicSkillPage : MonoBehaviour
{
    public void OnClick_SelectSkill(int skillId)
    {
        var pos = PlayerManager.I.MainPlayer.transform.position;

            var scene = SceneController.I.CurSceneData;
            if (scene is BattleScene battleScene)
            {
                battleScene.CreatePlates(pos, skillId, OnClickSkill);
            }

        UIManager.I.GameUI.gameObject.SetActive(false);
    }

    private void OnClickSkill(Vector3 pos)
    {
        var list = new List<PlanePathNode>
        {
            TilemapManager.I.GetNode_WorldPos(pos)
        };

        PlayerManager.I.MainPlayer.MagicSkill(PlayerManager.I.SelectSkillId ,list);
    }
}
