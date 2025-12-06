using System;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public class ActionPlate : MonoBehaviour
{
    [SerializeField] public SpriteRenderer spriteRenderer;

    private TileMap tileMap => SceneController.I.CurTileMap;
    
    private void Awake()
    {
        if (spriteRenderer is null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void ClickedPlate(Vector3 pos)
    {
        SetMeshRenderColor();

        var scene = SceneController.I.CurSceneData;
        if (scene is BattleScene battleScene)
        {
            case eCharAction.Move:
            {
                var node = tileMap.GetNode_WorldPos(pos);
                Move(node.centerPos);
                break;
            }
            case eCharAction.Attack:
            {
                var node = tileMap.GetNode_WorldPos(pos);
                PlayerManager.I.MainPlayer.Attack(node);
                break;
            }
            case eCharAction.Magic:
            {
                var magicSkillId = 1;
                var list = new List<PlanePathNode>
                {
                    tileMap.GetNode_WorldPos(pos)
                };

                PlayerManager.I.MainPlayer.MagicSkill(magicSkillId, list);
                break;
            }
        }

        _onClicked?.Invoke(pos);

        // switch (PlayerManager.I.MainPlayer.CharAction)
        // {
        //     case eCharAction.Move:
        //     {
        //         var node = TilemapManager.I.GetNode_WorldPos(pos);
        //         Move(node.centerPos);
        //         break;
        //     }
        //     case eCharAction.Attack:
        //     {
        //         var node = TilemapManager.I.GetNode_WorldPos(pos);
        //         PlayerManager.I.MainPlayer.Attack(node);
        //         break;
        //     }
        //     case eCharAction.Magic_Attack:
        //     case eCharAction.Magic_Buff:
        //     {
        //         var list = new List<PlanePathNode>
        //         {
        //             TilemapManager.I.GetNode_WorldPos(pos)
        //         };
        //
        //         PlayerManager.I.MainPlayer.MagicSkill(PlayerManager.I.SelectSkillId ,list);
        //         break;
        //     }
        // }
    }

    public void SetPlate(eCharCommand commandType, Action<Vector3> onClickAction)
    {
        _onClicked = onClickAction;
        _commandType = commandType;

        SetMeshRenderColor();
    }

    private void Move(Vector3 pos)
    {
        var charPos = PlayerManager.I.MainPlayer.CharPath.MoveListLength > 0
            ? PlayerManager.I.MainPlayer.CharPath.LastNode().centerPos
            : tileMap.GetNode_WorldPos(PlayerManager.I.MainPlayer.transform.position).centerPos;

        var nodes = SceneController.I.CurScene.Path.FindPath_IncludeFindEnemy(charPos, pos, true);
        if (nodes != null)
        {
            PlayerManager.I.MainPlayer.Move(nodes);
        }
    }

    private void SetMeshRenderColor()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            string strColor;
            switch (_commandType)
            {
                case eCharCommand.Attack:
                {
                    strColor = "#ff0000";
                    break;
                }
                case eCharCommand.Magic_Attack:
                case eCharCommand.Magic_Buff:
                {
                    strColor = "#ffff00";
                    break;
                }
                case eCharCommand.Move:
                case eCharCommand.None:
                default:
                {
                    strColor = "#ffffff";
                    break;
                }
            }
            if (ColorUtility.TryParseHtmlString(strColor, out var color))
            {
                color.a = 0.4f;
                spriteRenderer.color = color;
                // spriteRenderer.sprite = material;
            }
        }
    }
}
