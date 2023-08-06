using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnum;
using UnityEngine;

public enum UIType
{
    None = 0,

    //  Panel
    MainPanel,
    TopPanel,

    //  Popup
    Popup = 100,
    ProductPopup,
    TestAdsPopup,

    //  Message
    Message = 200,
    WarningMessage,
    ConfirmMessage,

    Count,
}

public class UIManager : MonoSingleton<UIManager>
{
    private GameObject uiGameObject;
    private Canvas UIPanel;
    private Canvas UIPopup;
    private Canvas UITop;

    private Dictionary<UIType, BasePanel> _panelUI = new Dictionary<UIType, BasePanel>();
    private Dictionary<UIType, BasePopup> _popupUI = new Dictionary<UIType, BasePopup>();
    
    protected override void Destroy()
    {
        
    }

    public override bool Initialize()
    {
        if (UIPanel is null)
        {
            var prefab = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, "UI");
            if (prefab != null)
            {
                uiGameObject = Instantiate(prefab, transform);
                uiGameObject.transform.localPosition = Vector3.zero;
                uiGameObject.transform.localScale = Vector3.one;
            }
        }

        if (uiGameObject != null)
        {
            var list = uiGameObject.GetComponentsInChildren<Canvas>().ToList();
            UIPanel = list.Find(x => x.name == "Panel");
            UIPopup = list.Find(x => x.name == "Popup");
            UITop = list.Find(x => x.name == "Top");
        }

        return true;
    }
}
