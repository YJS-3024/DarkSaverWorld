using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TopUI : MonoBehaviour
{
    [SerializeField] private Button btnBack;
    [SerializeField] private Text txtPanelName;

    private void Awake()
    {
        btnBack.onClick.AddListener(OnClick_Escape);
    }

    private void OnClick_Escape()
    {
        UIManager.I.OnEscape();
    }

    public void SetPanelName(string strPanelName)
    {
        if (txtPanelName != null)
        {
            txtPanelName.text = strPanelName;
        }
    }
}
