using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : BasePanel
{
    [SerializeField] private Button btnChar;
    [SerializeField] private Button btnInven;
    [SerializeField] private Button btnMsg;

    public override void CreatedPanel()
    {
        btnChar.onClick.AddListener(OnClick_OpenChar);
        btnInven.onClick.AddListener(OnClick_OpenInventory);
        btnMsg.onClick.AddListener(OnClick_Msg);
    }

    private void OnClick_Msg()
    {
        UIManager.I.ShowConfirmPopup("테스트 타이틀", "테스트 설명", () =>
        {
            UIManager.I.HidePopup();
        });
    }

    private void OnClick_OpenChar()
    {
        UIManager.I.ShowPanel(UIType.CharPanel);
    }

    private void OnClick_OpenInventory()
    {
        UIManager.I.ShowPanel(UIType.InventoryPanel);
    }

    public override void ShowPanel()
    {
    }

    public override void HidePanel()
    {
    }

    public override bool IsProcessEscape()
    {
        return true;
    }

    public override string GetPanelName() => string.Empty;

    public override UIType GetPanelType() => UIType.MainPanel;
}
