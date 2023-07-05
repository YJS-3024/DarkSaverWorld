using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InputManager : MonoSingleton<InputManager>
{
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
            Input_Move(Input.touches.FirstOrDefault().position);
        }
        else
        {
            //마우스 클릭시
            if (Input.GetMouseButtonUp(0))
            {
                Input_Move(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                Input_Move(Input.mousePosition);
            }
        }
    }

    public void Input_Move(Vector3 screenPos)
    {
        // 패스 이동
        var worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        var charPos = TilemapManager.I.Path.MoveListLength > 0
            ? TilemapManager.I.Path.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(PlayerManager.I.MainPlayer.transform.position).centerPos;

        var nodes = TilemapManager.I.Path.FindPath(charPos, worldPos, true);
        if (nodes != null)
        {
            PlayerManager.I.MainPlayer.StartMove(nodes);
        }
    }

    // public void Input_Attack(Vector3 screenPos)
    // {
    //     var worldPos = Camera.main.ScreenToWorldPoint(screenPos);
    // }

    public void Touch_Move(Vector3 screenPos)
    {
        var worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        var charPos = TilemapManager.I.Path.MoveListLength > 0
            ? (Vector3)(TilemapManager.I.Path.LastNode().centerPos + (Vector2.one * 0.5f))
            : PlayerManager.I.MainPlayer.transform.position;

        var nodes = TilemapManager.I.Path.FindPath(charPos, worldPos, true);
        if (nodes != null)
        {
            PlayerManager.I.MainPlayer.StartMove(nodes);
        }
    }
}
