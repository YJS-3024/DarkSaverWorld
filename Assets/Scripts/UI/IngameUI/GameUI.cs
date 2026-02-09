using System;
using UnityEngine;


public class GameUI : MonoBehaviour
{
    public enum CommanderPageType
    {
        PageFirst,
        PageUseItem,
        PageMagicSkill,
        PageJobSkill,
    }

    [SerializeField] private CommanderPage commanderPage;

    private CommanderPageType _curPage = CommanderPageType.PageFirst;

    public void SetCommander(Vector3 worldPos)
    {

        _curPage = CommanderPageType.PageFirst;

        //  commanderRect의 렉트 앵커가 min,max가 모두 0이여야한다.
        var screenPoint = Camera.main.WorldToScreenPoint(worldPos);
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            UIManager.I.GameUI.transform as RectTransform, 
            screenPoint, 
            CameraManager.I.UICamera, 
            out var localPos
        );
        commanderPage.Rect.localPosition = localPos;

        // var uiPosX = Screen.width - screenPoint.x;// - (UIManager.I.CanvasScale.x * 0.5f);
        // var uiPosY = Screen.height - screenPoint.y;// - (UIManager.I.CanvasScale.y * 0.5f);
        // commanderPage.Rect.localPosition = new Vector2(uiPosX, uiPosY);

        commanderPage.Rect.gameObject.SetActive(!commanderPage.Rect.gameObject.activeSelf);
    }
}