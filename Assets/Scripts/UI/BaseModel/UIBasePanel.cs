using skfksky1004.DevKit.UI;

public abstract class UIBasePanel : UIBase, IUIType
{
    public abstract string UIPanelName();

    public abstract UIType GetUIType();
}