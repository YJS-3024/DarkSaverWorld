using UnityEngine;
using UnityEngine.UI;

public class MainPanel : UIBasePanel
{
    [SerializeField] private Button btnChar;
    [SerializeField] private Button btnInven;
    [SerializeField] private Button btnMsg;

    public override void Created()
    {
        btnChar.onClick.AddListener(OnClick_OpenChar);
        btnInven.onClick.AddListener(OnClick_OpenInventory);
        btnMsg.onClick.AddListener(OnClick_Msg);
    }

    private void OnClick_Msg()
    {
        UIManager.I.ShowConfirmPopup("�׽�Ʈ Ÿ��Ʋ", "�׽�Ʈ ����", () =>
        {
            UIManager.I.HidePopup();
        });
    }

    private void OnClick_OpenChar()
    {
        UIManager.I.ShowPanel(UIType.CharacterPanel);
    }

    private void OnClick_OpenInventory()
    {
        UIManager.I.ShowPanel(UIType.InventoryPanel);
    }

    public override void Show()
    {
    }

    public override void Hide()
    {
    }

    public override bool IsProcessEscape()
    {
        return true;
    }

    public override string UIPanelName() => "MainPanel";
    public override UIType GetUIType() => UIType.MainPanel;
}
