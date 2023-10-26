using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class BasePopup : MonoBehaviour
{
    [SerializeField] protected Image imgBackground;
    [SerializeField] protected GameObject goPopup;

    protected RectTransform RectTransform => (RectTransform)transform;


    public abstract void ShowPopup();
    public abstract void HidePopup();

    public void SetActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
}
