using System.Collections.Generic;
using TMPro;

public class MagicSkillPopup : UIBasePopup, IUIType
{
    public MagicScrollView scrollView;
    public TextMeshProUGUI popupTitle;
    
    public override void Created()
    {
        popupTitle.SetText($"{GetUIType()}");
    }

    public override void Show()
    {
        var magicList = new List<MagicScrollData>();
        var dataList = TableManager.I.Skill.GetSkillList();
        foreach (var data in dataList)
        {
            magicList.Add(new MagicScrollData(data));
        }
        
        // var list = new List();
        scrollView.InitScroll(magicList);
    }

    public override void Hide()
    {
        scrollView.ClearItems();
    }

    public override bool IsProcessEscape()
    {
        return true;
    }

    public UIType GetUIType() => UIType.MagicSkillPopup;
}
