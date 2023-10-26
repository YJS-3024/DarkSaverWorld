using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MessagePopup : BasePopup
{
    [SerializeField] private Text txtTitle;
    [SerializeField] private Text txtDescription;

    [SerializeField] private Text txtCheckBox;
    [SerializeField] private Toggle tglCheckBox;

    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    private bool _isOn = false;

    /// <summary>
    /// 일반 메세지 팝업
    /// </summary>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="onConfirm"></param>
    /// <param name="onCancel"></param>
    /// <param name="strConfirm"></param>
    /// <param name="strCancel"></param>
    public void ShowConfirmPopup(string title, string description,
        Action onConfirm, Action onCancel = null,
        string strConfirm = "", string strCancel = "")
    {
        SetTitle(title);
        SetDescription(description);

        SetButton(btnConfirm, onConfirm, strConfirm);
        SetButton(btnCancel, onCancel, strCancel);
    }

    /// <summary>
    /// 일반 메세지 팝업
    /// </summary>
    /// <param name="title"></param>
    /// <param name="onConfirm"></param>
    public void ShowMsgPopup(string title, Action onConfirm)
    {
        SetTitle(title);
        SetDescription(string.Empty);

        SetButton(btnCancel, onConfirm);
    }

    /// <summary>
    /// 체크 박스 팝업
    /// </summary>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="checkBoxLabel"></param>
    /// <param name="onCheck"></param>
    public void ShowCheckPopup(string title, string description, string checkBoxLabel, Action<bool> onCheck)
    {
        SetTitle(title);
        SetDescription(description);
        SetCheckBox(tglCheckBox, checkBoxLabel);

        SetButton(btnCancel, () => onCheck.Invoke(_isOn));
    }

    public override void ShowPopup() { }

    public override void HidePopup()
    {
        tglCheckBox?.onValueChanged.RemoveAllListeners();
        btnConfirm?.onClick.RemoveAllListeners();
        btnCancel?.onClick.RemoveAllListeners();
    }

    private void SetTitle(string strTitle)
    {
        if(txtTitle is null)
            return;

        var isActive = !string.IsNullOrEmpty(strTitle);
        txtTitle.gameObject.SetActive(isActive);
        txtTitle.text = strTitle;
    }

    private void SetDescription(string strDescription)
    {
        if(txtDescription is null)
            return;

        var isActive = !string.IsNullOrEmpty(strDescription);
        txtDescription.gameObject.SetActive(isActive);
        txtDescription.text = strDescription;
    }

    private void SetButton(Button targetButton, Action onAction, string strButtonName= "")
    {
        if(targetButton is null)
            return;

        targetButton.onClick.AddListener(() =>
        {
            onAction?.Invoke();
        });

        // targetButton.text = strButtonName
    }

    private void SetCheckBox(Toggle toggle, string strCheckBox)
    {
        if (toggle is null)
            return;

        toggle.onValueChanged.AddListener((isOn) =>
        {
            _isOn = isOn;
        });

        txtCheckBox.text = strCheckBox;
    }
}
