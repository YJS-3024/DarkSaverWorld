using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private Image imgProgress;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void SetProgress(float curValue)
    {
        imgProgress.fillAmount = curValue;
    }
}
