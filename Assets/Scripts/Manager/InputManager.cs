using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoSingleton<InputManager>
{
    public InputUI InputUI { get; private set; }

    public override bool Initialize()
    {
        if (InputUI == null)
        {
            InputUI = FindFirstObjectByType<InputUI>();
            InputUI.gameObject.SetActive(false);
        }
        
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
            var touch = Input.touches.First();
            if (touch.phase == TouchPhase.Began)
            {
                var mousePos = Camera.main.ScreenPointToRay(touch.position);
                var hit = Physics2D.RaycastAll(mousePos.origin, mousePos.direction);

                if(hit.Length <= 0)
                    return;

                SetClick_Hit(hit);
            }
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

                SetClick_Hit(hit);
            }
        }
    }

    private void SetClick_Hit(RaycastHit2D[] hit)
    {
        SetClick_Player(hit);
        // SetClick_Enemy(hit);
        SetClick_ActionPlate(hit);
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
            .Select(x => x.collider.GetComponent<ActionPlate>())
            .FirstOrDefault(x => x != null);

        if (actionPlate is null)
            return;

        actionPlate.ClickedPlate(actionPlate.transform.position);
    }

    // private void SetClick_Enemy(RaycastHit2D[] hit)
    // {
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
    // }
}