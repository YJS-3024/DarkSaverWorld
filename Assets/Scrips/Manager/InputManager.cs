using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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
            Input_Touch();
        }
        else
        {
            //마우스 클릭시
            if (Input.GetMouseButtonDown(0))
            {
                Input_Mouse();
            }
        }
    }

    public void Input_Mouse()
    {
        // 바로 이동
        // Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // var endNode = PlaneManager.I.GetNode_WorldPos(worldPos);
        // if (endNode == null ||
        //     PlaneManager.I.IsMoveAble(endNode.indexX, endNode.indexY) == false)
        //     return;
        //
        // var mainPlayer = PlayerManager.I.MainPlayer;
        // if (mainPlayer != null)
        // {
        //     mainPlayer.transform.position = endNode.centerPos + (Vector2.one * 0.5f);
        // }

        // 패스 이동
        var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var charPos = PathManager.I.MoveListLength > 0
            ? (Vector3)(PathManager.I.LastNode().centerPos + (Vector2.one * 0.5f))
            : PlayerManager.I.MainPlayer.transform.position;

        var nodes = PathManager.I.FindPath(charPos, worldPos, true);
        if (nodes != null)
        {
            PlayerManager.I.MainPlayer.StartMove(nodes);
        }
    }

    public void Input_Touch()
    {

    }
}
