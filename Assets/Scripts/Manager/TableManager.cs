using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TableManager : MonoSingleton<TableManager>
{
    private TableLoader loader = new TableLoader();

    public TableManager_Item Item { get; } = new TableManager_Item();
    public TableManager_String String { get; }  = new TableManager_String();
    public TableManager_Monster Monster { get; }  = new TableManager_Monster();

    protected override void Destroy()
    {
    }

    public override bool Initialize()
    {
        loader.LoadTable("Data_Item", ref Item.DicItemDatas);
        loader.LoadTable("Data_String", ref String.DicStringDatas);
        loader.LoadTable("Data_Monster", ref Monster.DicMonsterDatas);

        return true;
    }
}
