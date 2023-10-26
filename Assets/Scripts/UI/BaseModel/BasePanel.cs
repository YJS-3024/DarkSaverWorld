using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasePanel : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    protected RectTransform RectTransform => (RectTransform)transform;

    private void Start()
    {
        if (canvasGroup is null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    protected void SetPanelAlpha(float value)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = value;
        }
    }

    public void SetActive(bool bActive, GameObject targetGo = null)
    {
        if (targetGo == null)
            targetGo = this.gameObject;

        if (targetGo != null)
        {
            targetGo.SetActive(bActive);
        }
    }

    /// <summary>
    /// 패널 생성때 셋팅
    /// </summary>
    public abstract void CreatedPanel();
    /// <summary>
    /// 패널 활성화시 셋팅
    /// </summary>
    public abstract void ShowPanel();
    /// <summary>
    /// 패널 비활성화시 셋팅
    /// </summary>
    public abstract void HidePanel();

    /// <summary>
    /// 백버튼 처리
    /// </summary>
    /// <returns></returns>
    public abstract bool IsProcessEscape();

    /// <summary>
    /// 탑UI에 보여질 패널의 이름
    /// </summary>
    /// <returns></returns>
    public abstract string GetPanelName();
    /// <summary>
    /// 패널의 타입
    /// </summary>
    /// <returns></returns>
    public abstract UIType GetPanelType();
}
