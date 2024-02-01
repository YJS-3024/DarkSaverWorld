using GlobalEnum;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CommandPanel : MonoBehaviour
{
    public void OnClick_Move()
    {
        var pos = PlayerManager.I.MainPlayer.transform.position;
        var range = PlayerManager.I.MainPlayer.MoveRange;

        var sceneData = SceneController.I.CurSceneData;
        if (sceneData is BattleScene battleScene)
        {
            battleScene.CreatePlates(pos, eCharCommand.Move, range, (vec) =>
            {
                var node = TilemapManager.I.GetNode_WorldPos(vec);
                var charPos = PlayerManager.I.MainPlayer.CharPath.MoveListLength > 0
                    ? PlayerManager.I.MainPlayer.CharPath.LastNode().centerPos
                    : TilemapManager.I.GetNode_WorldPos(PlayerManager.I.MainPlayer.transform.position).centerPos;

                var nodes = PlayerManager.I.MainPlayer.Path.FindPath_IncludeFindEnemy(charPos, node.centerPos, true);
                if (nodes != null)
                {
                    PlayerManager.I.MainPlayer.Move(nodes);
                }
            });
        }

        UIManager.I.GameUI.gameObject.SetActive(false);
    }

    public void OnClick_UseItem()
    {
    }

    public void OnClick_Attack()
    {
        var pos = PlayerManager.I.MainPlayer.transform.position;
        var range = PlayerManager.I.MainPlayer.AttackRange;

        var sceneData = SceneController.I.CurSceneData;
        if (sceneData is BattleScene battleScene)
        {
            battleScene.CreatePlates(pos, eCharCommand.Attack, range, (vec) =>
            {
                var node = TilemapManager.I.GetNode_WorldPos(pos);
                PlayerManager.I.MainPlayer.Attack(node);
            });
        }

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
