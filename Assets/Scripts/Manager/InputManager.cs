using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.EventSystems;

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
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                var mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
                var hit = Physics2D.RaycastAll(mousePos.origin, mousePos.direction);

                if(hit.Length <= 0)
                    return;

                SetClick_Player(hit);
                SetClick_ActionPlate(hit);
            }
        }
    }

    private void SetClick_Player(RaycastHit2D[] hit)
    {
        var playerChar = hit
            .Where(x=>x.collider.gameObject.layer == (int)eLayer.MainPlayer)
            .Select(x=>x.collider.GetComponent<PlayerChar>())
            .FirstOrDefault();

        if (playerChar is null)
            return;

        if (playerChar.CharAction != eCharAction.None)
        {
            PlayerManager.I.ClearPlates();
            playerChar.CharAction = eCharAction.None;
        }
        else
        {
            switch (playerChar.CharAction)
            {
                case eCharAction.None:
                default:
                {
                    UIManager.I.GameUI.SetCommander(playerChar.transform.position);
                    break;
                }
            }   
        }
    }

    private void SetClick_ActionPlate(RaycastHit2D[] hit)
    {
        var actionPlate = hit
            .Select(x=>x.collider.GetComponent<ActionPlate>())
            .FirstOrDefault();

        if (actionPlate is null)
            return;

        actionPlate.ClickedPlate(actionPlate.transform.position);
    }

    private void TestPlayerState()
    {
        var mainPlayer = PlayerManager.I.MainPlayer;

        var action = eCharAction.None;
        if (Input.GetKeyUp(KeyCode.Alpha1)) mainPlayer.CharAction = (eCharAction.Move);
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