using System;
using GlobalEnum;
using UI.Extension;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
    [SerializeField] private UseItemPage useItemPage;
    [SerializeField] private MagicSkillPage magicSkillPage;
    [SerializeField] private JobSkillPage jobSkillPage;

    private CommanderPageType _curPage = CommanderPageType.PageFirst;

    public void SetCommander(Vector3 worldPos)
    {
        _curPage = CommanderPageType.PageFirst;

        //  commanderRect의 렉트 앵커가 min,max가 모두 0이여야한다.
        var screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        var uiPosX = screenPoint.x - (UIManager.I.CanvasScale.x * 0.5f);
        var uiPosY = screenPoint.y - (UIManager.I.CanvasScale.y * 0.5f);
        commanderPage.Rect.anchoredPosition = new Vector2(uiPosX, uiPosY);

        commanderPage.Rect.gameObject.SetActive(!commanderPage.Rect.gameObject.activeSelf);
    }
}