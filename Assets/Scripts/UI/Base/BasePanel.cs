using System.Collections;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;

public abstract class BasePanel : MonoBehaviour
{
    public abstract void ShowPanel();
    public abstract void HidePanel();
    
    public abstract ePanelType GetPanelType();

    public virtual bool GetIsProcessEscape()
    {
        return true;
    }
}
