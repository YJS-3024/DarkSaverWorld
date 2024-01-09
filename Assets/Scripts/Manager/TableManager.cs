using Table;

namespace Table
{
    public interface Table_Interface
    {
        public bool IsLoadSuccess { get; set; }
    }
}

public partial class TableManager : MonoSingleton<TableManager>
{
    private TableLoader loader = new TableLoader();

    public TableManager_CharLevel CharLevel { get; } = new TableManager_CharLevel();
    public TableManager_CharJob CharJob { get; } = new TableManager_CharJob();
    public TableManager_Item Item { get; } = new TableManager_Item();
    public TableManager_String String { get; }  = new TableManager_String();
    public TableManager_Monster Monster { get; }  = new TableManager_Monster();

    protected override void Destroy()
    {
    }

    public override bool Initialize()
    {
        CharLevel.IsLoadSuccess = loader.LoadTable("Data_CharLevel", ref CharLevel.DicCharLevelDatas);
        CharJob.IsLoadSuccess = loader.LoadTable("Data_CharJob", ref CharJob.DicCharJobDatas);
        Item.IsLoadSuccess = loader.LoadTable("Data_Item", ref Item.DicItemDatas);
        String.IsLoadSuccess = loader.LoadTable("Data_String", ref String.DicStringDatas);
        Monster.IsLoadSuccess = loader.LoadTable("Data_Monster", ref Monster.DicMonsterDatas);

        return true;
    }
}
