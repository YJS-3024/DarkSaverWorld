using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

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
        switch (PlayerManager.I.PlayerChar.CharAction)
        {
            case eCharAction.Move:
            {
                PlayerManager.I.ActionPlate.ClearPlate();

                SetMeshRenderColor();

                var node = TilemapManager.I.GetNode_WorldPos(pos);
                Move(node.centerPos);
                break;
            }
        }
    }

    private void Move(Vector3 pos)
    {
        var charPos = TilemapManager.I.Path.MoveListLength > 0
            ? TilemapManager.I.Path.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(PlayerManager.I.PlayerChar.transform.position).centerPos;

        var nodes = TilemapManager.I.Path.FindPath(charPos, pos, true);
        if (nodes != null)
        {
            PlayerManager.I.PlayerChar.StartMove(nodes);
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
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                spriteRenderer.material = material;
            }
        }
    }

    private string GetActionPlateColor()
    {
        var mainPlayer = PlayerManager.I.PlayerChar;
        switch (mainPlayer.CharAction)
        {
            case eCharAction.Move:
            case eCharAction.None:
            default:
            {
                return "ffffff";
            }
        }
    }
}
