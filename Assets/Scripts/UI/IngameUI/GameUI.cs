using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class CommandUtility
{
    
}

public class GameUI : MonoBehaviour
{
    public enum CommanderPageType
    {
        None,
        PageFirst,
        PageUseItem,
        PageMagicSkill,
        PageJobSkill,
    }

    [SerializeField] private CommandPanel commanderPage;
    [SerializeField] private MagicSkillPage magicSkillPage;

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private BaseScrollPool scrollPool;

    private readonly List<CommandButton> _activeButtonList = new List<CommandButton>();

    private CommanderPageType _curPage = CommanderPageType.None;

    private void Awake()
    {
        EventManager.I.AddEvent("Move_MagicSkillPage", OnMove_MagicSkillPage);
    }

    private void OnDisable()
    {
        ClearScrollItem();
        _curPage = CommanderPageType.None;
    }

    public void SetCommander(Vector3 worldPos)
    {
        if (_curPage != CommanderPageType.None)
        {
            ShowCommand(CommanderPageType.None);
            return;
        }

        _curPage = CommanderPageType.PageFirst;

        //  commanderRect의 렉트 앵커가 min,max가 모두 0이여야한다.
        var screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        var uiPosX = screenPoint.x - (UIManager.I.CanvasScale.x * 0.5f);
        var uiPosY = screenPoint.y - (UIManager.I.CanvasScale.y * 0.5f);
        // commanderPage.Rect.anchoredPosition = new Vector2(uiPosX, uiPosY);
        //
        // commanderPage.Rect.gameObject.SetActive(!commanderPage.Rect.gameObject.activeSelf);

        ShowCommand(CommanderPageType.PageFirst);
        gameObject.SetActive(true);
    }

    private void ShowCommand(CommanderPageType type)
    {
        ClearScrollItem();

        switch (type)
        {
            case CommanderPageType.PageFirst:
            {
                var list = new[] { "Move", "Attack", "Magic", "Rest"};
                foreach (var command in list)
                {
                    var item = scrollPool.PopItem(scrollRect.content);
                    if (item is CommandButton btn)
                    {
                        var strName = TableManager.I.String.GetString($"Action_{command}");
                        btn.SetCommand(strName, () =>
                        {
                            var func = commanderPage.GetType().GetMethod($"OnClick_{command}");
                            func?.Invoke(commanderPage, null);
                        });
                        btn.SetActiveButton(true);
                        btn.gameObject.SetActive(true);

                        _activeButtonList.Add(btn);
                    }
                }

                gameObject.SetActive(true);
                break;
            }
            case CommanderPageType.PageUseItem: break;
            case CommanderPageType.PageMagicSkill:
            {
                var skills = TableManager.I.Skill.GetSkillList();
                if (skills.Count == 0)
                    return;

                foreach (var skillData in skills)
                {
                    var item = scrollPool.PopItem(scrollRect.content);
                    if (item is CommandButton btn)
                    {
                        btn.SetCommand_Skill(skillData.SkillID, magicSkillPage.OnClick_SelectSkill);
                        btn.SetActiveButton(true);
                        btn.gameObject.SetActive(true);

                        _activeButtonList.Add(btn);
                    }
                }
                break;
            }
            case CommanderPageType.PageJobSkill: break;
            default:
            {
                gameObject.SetActive(false);
                break;
            }
        }

        _curPage = type;
    }

    private void ClearScrollItem()
    {
        if (_activeButtonList.Count > 0)
        {
            foreach (var item in _activeButtonList)
                scrollPool.PushItem(item);
            _activeButtonList.Clear();
        }
    }

    private void OnMove_MagicSkillPage()
    {
        ShowCommand(CommanderPageType.PageMagicSkill);
    }
}