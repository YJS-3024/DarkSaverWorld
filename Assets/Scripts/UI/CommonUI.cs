using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CommonUI : MonoBehaviour
{
    public Transform GameParentTf;

    public Transform PanelParentTf;

    public Transform PopupParentTf;

    public Transform OtherParentTf;

    public Transform LoadingParentTf;

    public Transform TopParentTf;

    public Canvas MainCanvas { get; private set; }
    public CanvasScaler CanvasScaler { get; private set; }
    public Vector2 CanvasScale { get; private set; } = Vector2.zero;

    private GameUI _gameUI = null;
    public GameUI GameUI
    {
        get
        {
            if (_gameUI is null)
            {
                CreateGameUI();
            }

            return _gameUI;
        }
    }

    private TopUI _topUI = null;
    public TopUI TopUI
    {
        get
        {
            if (_topUI is null)
            {
                CreateTopUI();
            }

            return _topUI;
        }
    }

    private LoadingUI _loadingUI;

    public LoadingUI LoadingUI
    {
        get
        {
            if (_loadingUI is null)
            {
                _loadingUI = GetComponentInChildren<LoadingUI>(true);
            }
            
            return _loadingUI;
        }
    }

    private void Awake()
    {
        if (MainCanvas is null)
        {
            MainCanvas = GetComponentInChildren<Canvas>();
        }

        if (CanvasScaler == null)
        {
            CanvasScaler = MainCanvas.GetComponent<CanvasScaler>();
        }

        if (CanvasScale == Vector2.zero)
        {
            var rectTf = (RectTransform)MainCanvas.transform;
            CanvasScale = new Vector2(rectTf.sizeDelta.x, rectTf.sizeDelta.y);
        }
    }

    private void CreateGameUI()
    {
        var path = UiUtil.GetPanelPath(UIType.GameUI);
        var prefab = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, path);
        if (prefab != null)
        {
            //  게임 오브젝트 셋팅
            var go = Instantiate(prefab.gameObject, GameParentTf);
            go.layer = LayerMask.NameToLayer("UI");

            //  UI Rect 셋팅
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = Vector3.zero;
            rect.localScale = Vector3.one;

            //  캔버스 생성 및 셋팅
            if (go.TryGetComponent<Canvas>(out var canvas) == false)
            {
                canvas = go.AddComponent<Canvas>();
                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<CanvasGroup>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)UIType.TopUI;
            canvas.sortingLayerName = SortingLayer.layers
                .Where(x => x.name.Equals("UIPanel"))
                .Select(x => x.name)
                .FirstOrDefault();

            _gameUI = go.GetComponent<GameUI>();
        }
    }

    private void CreateTopUI()
    {
        if (_topUI != null)
            return;

        var path = UiUtil.GetPanelPath(UIType.TopUI);
        var prefab = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, path);
        if (prefab != null)
        {
            //  게임 오브젝트 셋팅
            var go = Instantiate(prefab.gameObject, TopParentTf);
            go.layer = LayerMask.NameToLayer("UI");

            //  UI Rect 셋팅
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = Vector3.zero;
            rect.localScale = Vector3.one;

            //  캔버스 생성 및 셋팅
            if (go.TryGetComponent<Canvas>(out var canvas) == false)
            {
                canvas = go.AddComponent<Canvas>();
                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<CanvasGroup>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)UIType.TopUI;
            canvas.sortingLayerName = SortingLayer.layers
                .Where(x => x.name.Equals("UIPanel"))
                .Select(x => x.name)
                .FirstOrDefault();
            
            _topUI = go.GetComponent<TopUI>();
        }
    }
}
