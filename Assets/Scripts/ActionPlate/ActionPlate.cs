using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public class ActionPlate : MonoBehaviour
{
    [SerializeField] public SpriteRenderer spriteRenderer;

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

        PlayerManager.I.ActionPlate.ClearPlate();

        switch (PlayerManager.I.MainPlayer.CharAction)
        {
            case eCharAction.Move:
            {
                var node = TilemapManager.I.GetNode_WorldPos(pos);
                Move(node.centerPos);
                break;
            }
            case eCharAction.Attack:
            {
                var node = TilemapManager.I.GetNode_WorldPos(pos);
                PlayerManager.I.MainPlayer.Attack(node);
                break;
            }
            case eCharAction.Magic:
            {
                var magicSkillId = 1;
                var list = new List<PlanePathNode>
                {
                    TilemapManager.I.GetNode_WorldPos(pos)
                };

                PlayerManager.I.MainPlayer.MagicSkill(magicSkillId, list);
                break;
            }
        }
    }

    private void Move(Vector3 pos)
    {
        var charPos = PlayerManager.I.MainPlayer.CharPath.MoveListLength > 0
            ? PlayerManager.I.MainPlayer.CharPath.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(PlayerManager.I.MainPlayer.transform.position).centerPos;

        var nodes = PlayerManager.I.MainPlayer.Path.FindPath_IncludeFindEnemy(charPos, pos, true);
        if (nodes != null)
        {
            PlayerManager.I.MainPlayer.Move(nodes);
        }
    }

    public void SetMeshRenderColor()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            var strColor = GetActionPlateColor();
            if (ColorUtility.TryParseHtmlString(strColor, out var color))
            {
                color.a = 0.4f;
                spriteRenderer.color = color;
                // spriteRenderer.sprite = material;
            }
        }
    }

    private string GetActionPlateColor()
    {
        var mainPlayer = PlayerManager.I.MainPlayer;
        switch (mainPlayer.CharAction)
        {
            case eCharAction.Attack:
            {
                return "#ff0000";
            }
            case eCharAction.Magic:
            {
                return "#ffff00";
            }
            case eCharAction.Move:
            case eCharAction.None:
            default:
            {
                return "#ffffff";
            }
        }
    }
}
