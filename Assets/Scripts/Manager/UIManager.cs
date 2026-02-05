using System;
using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.UI;

public enum UIType
{
    None,
    GameUI = 1,

    MainPanel = 10,
    CharacterPanel,
    InventoryPanel,

    TestPanel = 999,

    TopUI = 1000,

    MessagePopup = 2000,
    MagicSkillPopup,
}

/// <summary>
/// UI별 UI 타입 셋팅용 인터페이스
/// </summary>
public interface IUIType
{
    public UIType GetUIType();
}

public static class UiUtil
{
    public static string GetPanelPath(UIType type)
    {
        switch (type)
        {
            case UIType.GameUI : return "UI/Game/GameUI";
            case UIType.TopUI: return "UI/Game/TopUI";

            case UIType.MainPanel: return "UI/Panel/MainPanel";
            case UIType.CharacterPanel: return "UI/Panel/CharPanel";
            case UIType.InventoryPanel: return "UI/Panel/InvenPanel";

            case UIType.MessagePopup: return "UI/Popup/MessagePopup";
            case UIType.MagicSkillPopup: return "UI/Popup/MagicSkillPopup";
            case UIType.None:
            default:
                return string.Empty;
        }
    }
}

public partial class UIManager : MonoSingleton<UIManager>
{
    private CommonUI _commonUI;
    private readonly Dictionary<UIType, UIBasePanel> _dicPanels = new Dictionary<UIType, UIBasePanel>();
    private readonly Dictionary<UIType, UIBasePopup> _dicPopups = new Dictionary<UIType, UIBasePopup>();

    private readonly Stack<UIBasePanel> _panelHistory = new Stack<UIBasePanel>();
    private readonly Stack<UIBasePopup> _popupHistory = new Stack<UIBasePopup>();

    private UIType lastUIType = UIType.None;

    public CanvasScaler CanvasScaler => _commonUI.CanvasScaler;
    public GameUI GameUI => _commonUI.GameUI;
    public TopUI TopUI => _commonUI.TopUI;
    public LoadingUI LoadingUI => _commonUI.LoadingUI;
    public Vector2 CanvasScale => _commonUI.CanvasScale;

    protected override void Destroy(){ }

    public override bool Initialize()
    {
        if (_commonUI is null)
        {
            var go = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, "UI/CommonUI");
            var goCommonUI = Instantiate(go, transform);
            if (goCommonUI != null)
            {
                _commonUI = goCommonUI.GetComponent<CommonUI>();   
                _commonUI.gameObject.SetActive(true);
            }
        }

        // ShowPanel(UIType.MainPanel);
        
        return true;
    }

    #region [Panel]


    /// <summary>
    /// 패널 생성
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private UIBasePanel CreatePanel(UIType type)
    {
        if (_dicPanels.TryGetValue(type, out var panel))
            return panel;

        var path = UiUtil.GetPanelPath(type);
        var prefab = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, path);
        if (prefab != null)
        {
            //  패널 프리팹 생성
            var go = Instantiate(prefab.gameObject, _commonUI.PanelParentTf);
            go.layer = LayerMask.NameToLayer("UI");

            //  UI Rect
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = Vector3.zero;
            rect.localScale = Vector3.one;

            //  캔버스 셋팅
            if (go.TryGetComponent<Canvas>(out var canvas) == false)
            {
                canvas = go.AddComponent<Canvas>();

                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<CanvasGroup>();
            }

            var strUiLayer = type.ToString().Contains("Panel")
                ? "UIPanel"
                : "UIPopup";

            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)type;
            canvas.sortingLayerName = SortingLayer.layers
                .Where(x => x.name.Equals(strUiLayer))
                .Select(x => x.name)
                .FirstOrDefault();

            //  생성된 패널 등록
            var component = go.GetComponent<UIBasePanel>();
            if (component != null)
            {
                component.Created();
                _dicPanels.Add(type, component);
            }

            _dicPanels[type].SetActive(false);
            
            return _dicPanels[type];
        }

        return null;
    }

    /// <summary>
    /// 패널 열기
    /// </summary>
    /// <param name="type"></param>
    public void ShowPanel(UIType type)
    {
        if (_panelHistory.Count > 0)
        {
            var beforePanelType = lastUIType;
            _dicPanels[beforePanelType].SetActive(false);
        }

        if (_dicPanels.TryGetValue(type, out var panel) == false)
        {
            panel = CreatePanel(type);
        }

        if (panel is null)
            return;

        panel.Show();
        panel.SetActive(true);

        TopUI.SetPanelName(panel.UIPanelName());

        _panelHistory.Push(panel);

        lastUIType = panel.GetUIType();
    }

    /// <summary>
    /// 패널 닫기
    /// </summary>
    public void HidePanel()
    {
        if (_panelHistory.Peek().GetUIType() != UIType.MainPanel)
        {
            var curPanel = _panelHistory.Pop();
            curPanel.Hide();
            curPanel.SetActive(false);

            var beforePanel = _panelHistory.Peek();
            beforePanel.Show();
            beforePanel.SetActive(true);

            lastUIType = beforePanel.GetUIType();
        }
    }

    public UIBasePanel GetPanel()
    {
        return _panelHistory.Peek();
    }

    #endregion [Panel]





    #region [Popup]

    public UIBasePopup CreatePopup(UIType type)
    {
        if (_dicPopups.TryGetValue(type, out var popup))
            return popup;

        var path = UiUtil.GetPanelPath(type);
        var prefab = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, path);
        if (prefab != null)
        {

            //  팝업 프리팹 생성
            var go = Instantiate(prefab.gameObject, _commonUI.PopupParentTf);
            go.layer = LayerMask.NameToLayer("UI");

            //  UI Rect
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = Vector3.zero;
            rect.localScale = Vector3.one;

            //  생성 팝업 등록
            var component = go.GetComponent<UIBasePopup>();
            if (component != null)
            {
                _dicPopups.Add(type, component);
            }

            _dicPopups[type].SetActive(false);
            
            return _dicPopups[type];
        }

        return null;
    }

    public UIBasePopup ShowPopup(UIType type)
    {
        if (_dicPopups.TryGetValue(type, out var popup) == false)
        {
            popup = CreatePopup(type);
        }

        if (popup is null)
            return null;

        popup.Show();
        popup.SetActive(true);

        _popupHistory.Push(popup);

        // lastUIType = popup.GetPanelType();

        return popup;
    }

    public void HidePopup()
    {
        if (_popupHistory.Count > 0)
        {
            var popup = _popupHistory.Pop();
            if (popup != null)
            {
                popup.Hide();
                popup.SetActive(false);
            }
        }
    }

    #endregion [Popup]


    /// <summary>
    /// Escape
    /// </summary>
    public void OnEscape()
    {
        if (_popupHistory.Count > 0)
        {
            var popup = _popupHistory.Pop();
            if (popup != null)
            {
                HidePopup();
            }
        }
        else
        {
            var panel = _panelHistory.Peek();
            if (panel.IsProcessEscape())
            {
                HidePanel();
            }
        }
    }
}

public partial class UIManager
{
    public void ShowConfirmPopup(
        string title, string description,
        Action onConfirm, Action onCancel = null,
        string strConfirm = "", string strCancel = "")
    {
        var activePopup = ShowPopup(UIType.MessagePopup) as MessagePopup;
        if (activePopup != null)
        {
            activePopup.ShowConfirmPopup(title, description, onConfirm, onCancel, strConfirm, strCancel);
        }
    }
}
