using System;
using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.EventSystems;
using Utility;


public class InputManager : MonoSingleton<InputManager>
{

    public override bool Initialize()
    {
        return true;
    }

    protected override void Destroy()
    {
    }

    private void Update()
    {
        //터치 했을시
        if (Input.touchCount > 0)
        {
            var touch = Input.touches.First();

            var mousePos = Camera.main.ScreenPointToRay(touch.position);
            var arr = Physics2D.RaycastAll(mousePos.origin, mousePos.direction);
            if (arr.Length <= 0)
                return;

            if (touch.phase == TouchPhase.Began)
            {
                var playerChar = arr
                    .Where(x => x.collider.gameObject.layer == (int)eLayer.MainPlayer)
                    .Select(x => x.collider.GetComponent<PlayerChar>())
                    .FirstOrDefault();
                if (playerChar != null)
                {
                    OnClick_Player(arr);
                }

                // SetClick_Enemy(hit);

                var actionPlate = arr
                    .Select(x => x.collider.GetComponent<ActionPlate>())
                    .FirstOrDefault(x => x != null);
                if (actionPlate != null)
                {
                    OnClick_ActionPlate(arr);
                }
            }

            if (touch.phase == TouchPhase.Stationary)
            {
                OnClick_Ground(arr);
            }
        }
        else
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            var mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
            var arr = Physics2D.RaycastAll(mousePos.origin, mousePos.direction);
            if (arr.Length <= 0)
                return;

            //마우스 클릭시
            if (Input.GetMouseButtonUp(0))
            {
                OnClick_Player(arr);
                // SetClick_Enemy(hit);
                OnClick_ActionPlate(arr);
            }

            if (Input.GetMouseButton(0))
            {
                OnClick_Ground(arr);
            }
        }
    }


    public void OnClick_Ground(RaycastHit2D[] hit)
    {
        switch (SceneController.I.CurSceneType)
        {
            case SceneType.Scene_Title:
                break;
            case SceneType.Scene_Village:
            case SceneType.Scene_Field:
            {
                var ground = hit.First(x =>
                    x.collider != null &&
                    x.collider.gameObject.layer == (int)eLayer.Field_Board);

                var node = TilemapManager.I.GetNode_WorldPos(ground.point);
                var charPos = PlayerManager.I.MainPlayer.CharPath.MoveListLength > 0
                    ? PlayerManager.I.MainPlayer.CharPath.LastNode().centerPos
                    : TilemapManager.I.GetNode_WorldPos(PlayerManager.I.MainPlayer.transform.position).centerPos;

                var nodes = PlayerManager.I.MainPlayer.Path.FindPath_IncludeFindEnemy(charPos, node.centerPos, true);
                if (nodes != null)
                {
                    PlayerManager.I.MainPlayer.Move(nodes);
                }

                break;
            }
            case SceneType.Scene_Battle:
                break;
        }
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

        switch (SceneController.I.CurSceneType)
        {
            case SceneType.Scene_Title:
                break;
            case SceneType.Scene_Village:
                break;
            case SceneType.Scene_Field:
                break;
            case SceneType.Scene_Battle:
            {
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
                break;
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
        switch (SceneController.I.CurSceneType)
        {
            case SceneType.Scene_Title:
                break;
            case SceneType.Scene_Village:
                break;
            case SceneType.Scene_Field:
                break;
            case SceneType.Scene_Battle:
            {
                var actionPlate = hit
                    .Select(x => x.collider.GetComponent<ActionPlate>())
                    .FirstOrDefault(x => x != null);

                if (actionPlate is null)
                    return;

                actionPlate.ClickedPlate(actionPlate.transform.position);

                break;
            }
        }
    }
}