using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private Image imgProgress;

    private Action _onComplete;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void SetProgressCompleteCallback(Action onComplete)
    {
        _onComplete = onComplete;
    }

    public void SetProgress(float curValue)
    {
        if (isActiveAndEnabled == false)
            gameObject.SetActive(true);

        imgProgress.fillAmount = curValue;
    }

    public void SetActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
}
