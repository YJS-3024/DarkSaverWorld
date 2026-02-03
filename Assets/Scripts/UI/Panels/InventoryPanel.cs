public class InventoryPanel : UIBasePanel
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
    
    public override string UIPanelName() => "InventoryPanel";

    public override UIType GetUIType() => UIType.InventoryPanel;
}