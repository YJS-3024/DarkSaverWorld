using System.Collections;
using System.Linq;
using GlobalEnum;
using UnityEngine;

public partial class BattleScene : BaseScene
{
    // Start is called before the first frame update
    private IEnumerator Start()
    {
        if (GameSystem.I is null)
        {
            yield return new WaitUntil(()=>GameSystem.I.Initialize());
        }

        yield return new WaitUntil(() => CameraManager.I.Initialize());
        
        PlayerManager.I.CreatePlayer(true);
    }

    public override SceneType SceneType() => GlobalEnum.SceneType.Scene_Battle;
}

public partial class BattleScene
{
    public override void ClickEvent(Vector2 screenPosition)
    {
        base.ClickEvent(screenPosition);
        
        var mousePos = Camera.main.ScreenPointToRay(screenPosition);
        var hit = Physics2D.RaycastAll(mousePos.origin, mousePos.direction);

        if(hit.Length <= 0)
            return;

        SetClick_Player(hit);
        // SetClick_Enemy(hit);
        SetClick_ActionPlate(hit);
    }

    private void SetClick_Player(RaycastHit2D[] hit)
    {
        var playerChar = hit
            .Where(x=>x.collider.gameObject.layer == (int)eLayer.MainPlayer)
            .Select(x=>x.collider.GetComponent<PlayerUnit>())
            .FirstOrDefault();

        if (playerChar is null)
            return;

        if (playerChar.CharAction != eCharAction.None)
        {
            PlayerManager.I.ClearPlates();
            playerChar.CharAction = eCharAction.None;
        }
        else
        {
            switch (playerChar.CharAction)
            {
                case eCharAction.None:
                default:
                {
                    UIManager.I.GameUI.SetCommander(playerChar.transform.position);
                    break;
                }
            }   
        }
    }

    private void SetClick_ActionPlate(RaycastHit2D[] hit)
    {
        var actionPlate = hit
            .Select(x => x.collider.GetComponent<ActionPlate>())
            .FirstOrDefault(x => x != null);

        if (actionPlate is null)
            return;

        actionPlate.ClickedPlate(actionPlate.transform.position);
    }
}
