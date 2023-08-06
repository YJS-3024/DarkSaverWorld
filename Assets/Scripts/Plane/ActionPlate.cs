using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActionPlate : MonoBehaviour
{
    [SerializeField] public MeshRenderer meshRenderer;

    private void Awake()
    {
        if (meshRenderer is null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var plate = hit.collider.GetComponent<ActionPlate>();
                if (plate == this)
                {
                    switch (PlayerManager.I.PlayerChar.CharAction)
                    {
                        case eCharAction.Move:
                        {
                            PlayerManager.I.ActionPlate.ClearPlate();
                            
                            SetMeshRenderColor();
                            
                            var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            var pos = TilemapManager.I.GetNode_WorldPos(worldPos);
                            Move(pos.centerPos);
                            break;
                        }
                    }        
                }
            }
            else 
            {
                //  행동칸 밖 클릭
                if (PlayerManager.I.ActionPlate.IsCreatedPlate())
                {
                    PlayerManager.I.ActionPlate.ClearPlate();
                }
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
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            var strColor = GetActionPlateColor();
            if (ColorUtility.TryParseHtmlString(strColor, out var color))
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                meshRenderer.material = material;
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
