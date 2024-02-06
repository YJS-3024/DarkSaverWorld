using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.EventSystems;
using Utility;


public partial class InputManager : MonoSingleton<InputManager>
{
    private readonly RaycastHit2D[] _results = new RaycastHit2D[10];


    public iInputData InputData { get; set; } = null;

    public override bool Initialize()
    {
        return true;
    }

    protected override void Destroy()
    {
    }

    void Update()
    {
        //터치 했을시
        if (Input.touchCount > 0)
        {
            var touch = Input.touches.First();
            if (touch.phase == TouchPhase.Began)
            {
                var mousePos = Camera.main.ScreenPointToRay(touch.position);
                var size = Physics2D.RaycastNonAlloc(mousePos.origin, mousePos.direction, _results);
                if (size <= 0)
                    return;

                InputData?.OnClick_Player(_results);
                // SetClick_Enemy(hit);
                InputData?.OnClick_ActionPlate(_results);
            }
        }
        else
        {
            //마우스 클릭시
            if (Input.GetMouseButtonUp(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                var mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
                var size = Physics2D.RaycastNonAlloc(mousePos.origin, mousePos.direction, _results);
                if (size <= 0)
                    return;

                InputData?.OnClick_Player(_results);
                // SetClick_Enemy(hit);
                InputData?.OnClick_ActionPlate(_results);
            }
        }
    }
}


public class BattleInputData : iInputData
{
    public void OnClick_Ground(RaycastHit2D[] hit)
    {
    }

    public void OnClick_Player(RaycastHit2D[] hit)
    {
        var playerChar = hit
            .Where(x => x.collider.gameObject.layer == (int)eLayer.MainPlayer)
            .Select(x => x.collider.GetComponent<PlayerChar>())
            .FirstOrDefault();

        if (playerChar is null)
            return;

        // if (playerChar.charStatus.IsPossibleAction == false)
        //     return;

        if (playerChar.CharCommand != eCharCommand.None)
        {
            var sceneData = SceneController.I.CurSceneData;
            if (sceneData is BattleScene battleScene)
            {
                battleScene.ClearPlates();
            }

            playerChar.CharCommand = eCharCommand.None;
        }
        else
        {
            switch (playerChar.CharCommand)
            {
                case eCharCommand.None:
                default:
                {
                    UIManager.I.GameUI.SetCommander(playerChar.transform.position);
                    break;
                }
            }
        }
    }

    public void OnClick_Enemy(RaycastHit2D[] hit)
    {
        //     var enemy = hit
        //         .Select(x=>x.collider.GetComponent<EnemyChar>())
        //         .FirstOrDefault();
        //
        //     if (enemy is null)
        //         return;
        //
        //     var mainPlayer = PlayerManager.I.MainPlayer;
        //     switch (mainPlayer.CharAction)
        //     {
        //         case eCharAction.Attack:
        //         {
        //             var targetNode = TilemapManager.I.GetNode_WorldPos(enemy.transform.position);
        //             PlayerManager.I.MainPlayer.Attack(targetNode);
        //             break;
        //         }
        //         case eCharAction.None:
        //         default:
        //         {
        //             break;
        //         }
        //     }
    }

    public void OnClick_ActionPlate(RaycastHit2D[] hit)
    {
        var actionPlate = hit
            .Select(x => x.collider.GetComponent<ActionPlate>())
            .FirstOrDefault(x => x != null);

        if (actionPlate is null)
            return;

        actionPlate.ClickedPlate(actionPlate.transform.position);
    }
}

public class FieldInputData : iInputData
{
    public void OnClick_Ground(RaycastHit2D[] hit)
    {
    }

    public void OnClick_Player(RaycastHit2D[] hit)
    {
    }

    public void OnClick_Enemy(RaycastHit2D[] hit)
    {
    }

    public void OnClick_ActionPlate(RaycastHit2D[] hit)
    {
    }
}