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
            // Click_Move(Input.touches.FirstOrDefault().position);
        }
        else
        {
            //마우스 클릭시
            if (Input.GetMouseButtonUp(0))
            {
                var mainPlayer = PlayerManager.I.PlayerChar;
                
                var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                
                var targetPos = TilemapManager.I.GetNode_WorldPos(worldPos)?.centerPos;
                var mainCharPos = PlayerManager.I.PlayerChar.GetNodePos;
                if (mainCharPos == targetPos)
                {
                    switch (mainPlayer.CharAction)
                    {
                        case eCharAction.None:
                        {
                            break;
                        }
                        case eCharAction.Move:
                        {
                            PlayerManager.I.CreateMovePlates(mainCharPos);
                            break;
                        }
                    }
                }
                else
                {
                    // PlayerManager.I.ActionPlate.ClearPlate();
                }
            }
        }
    }
    
    public void Click_Move(Vector2 screenPos)
    {
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