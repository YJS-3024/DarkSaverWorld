using skfksky1004.DevKit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MagicScrollData : BaseScrollData
{
    public long SkillId;
    public short SkillType;
    public int SkillNameId;
    
    public MagicScrollData(SkillData data)
    {
        SkillId = data.SkillID;
        SkillType = data.SkillType;
        SkillNameId = data.SkillID;
    }
}

public class MagicScrollItem : BaseScrollItem
{
    [SerializeField] private Text txtMagicName;
    
    public override void UpdateItem(BaseScrollData data)
    {
        if(data is not MagicScrollData magicScrollData)
            return;
        
        txtMagicName.text = TableManager.I.String.GetString(magicScrollData.SkillNameId);   
    }
}
