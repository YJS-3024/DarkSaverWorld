using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TableManager : MonoSingleton<TableManager>
{
    private TableLoader loader = new TableLoader();

    private Dictionary<int, ItemData> dicItemDatas = new Dictionary<int, ItemData>();
    private Dictionary<int, StringData> dicStringDatas = new Dictionary<int, StringData>();

    protected override void Destroy()
    {
    }

    public override bool Initialize()
    {
        loader.LoadTable("Data_Item", ref dicItemDatas);
        loader.LoadTable("Data_String", ref dicStringDatas);

        return true;
    }
}
