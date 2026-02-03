using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPanel : UIBasePanel
{
    public override void Created()
    {
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

    public override string UIPanelName() => "TestPanel";

    public override UIType GetUIType() => UIType.TestPanel;

}
