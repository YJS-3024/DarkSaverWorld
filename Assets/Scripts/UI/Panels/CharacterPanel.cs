public class CharacterPanel : UIBasePanel
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

    public override string UIPanelName() => "CharacterPanel";

    public override UIType GetUIType()=>UIType.CharacterPanel;
}
