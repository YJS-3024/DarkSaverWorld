using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
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
        TestPlayerState();

        //터치 했을시
        if (Input.touchCount > 0)
        {
            ClickTarget(Input.touches.FirstOrDefault().position);
        }
        else
        {
            //마우스 클릭시
            if (Input.GetMouseButtonUp(0))
            {
                var mainPlayer = PlayerManager.I.PlayerChar;
                if (mainPlayer.CharAction == eCharAction.Move)
                {
                    ClickTarget(Input.mousePosition);
                }
            }
        }
    }

    public void ClickTarget(Vector3 screenPos)
    {
        var worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        var charPos = TilemapManager.I.Path.MoveListLength > 0
            ? TilemapManager.I.Path.LastNode().centerPos
            : TilemapManager.I.GetNode_WorldPos(PlayerManager.I.PlayerChar.transform.position).centerPos;

        var nodes = TilemapManager.I.Path.FindPath(charPos, worldPos, true);
        if (nodes != null)
        {
            PlayerManager.I.PlayerChar.StartMove(nodes);
        }
    }

    private void TestPlayerState()
    {
        var mainPlayer = PlayerManager.I.PlayerChar;

        var action = eCharAction.None;
        if (Input.GetKeyUp(KeyCode.Alpha1)) mainPlayer.CharAction =(eCharAction.Move);
        else if (Input.GetKeyUp(KeyCode.Alpha2)) mainPlayer.CharAction = (eCharAction.UseItem);
        else if (Input.GetKeyUp(KeyCode.Alpha3)) mainPlayer.CharAction = (eCharAction.Attack);
        else if (Input.GetKeyUp(KeyCode.Alpha4)) mainPlayer.CharAction = (eCharAction.Magic);
        else if (Input.GetKeyUp(KeyCode.Alpha5)) mainPlayer.CharAction = (eCharAction.Attack_Special);
        else if (Input.GetKeyUp(KeyCode.Alpha6)) mainPlayer.CharAction = (eCharAction.Recess);
        else if (Input.GetKeyUp(KeyCode.Alpha7)) mainPlayer.CharAction = (eCharAction.Management);
        else if (Input.GetKeyUp(KeyCode.Alpha8)) mainPlayer.CharAction = (eCharAction.System_Option);
        else if (Input.GetKeyUp(KeyCode.Alpha9)) mainPlayer.CharAction = (eCharAction.Attack);
    }
}