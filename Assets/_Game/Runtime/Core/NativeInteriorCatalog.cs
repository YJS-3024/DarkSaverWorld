using System;
using System.Collections.Generic;
namespace DarkSaver.Core
{
    // Native indoor geometry/visual identification; town associations are not recovered.
    public static class NativeInteriorCatalog
    {
        public static readonly IReadOnlyList<TownDefinition> All=Array.AsReadOnly(new[]{
            new TownDefinition("Interior1500","무기·방패 벽면 실내; 시설 미확정 (1500)","Interior1500_Map","Interior1500_Runtime","1500",20,15,false),
            new TownDefinition("Interior2000","무기점 (2000)","Interior2000_Map","Interior2000_Runtime","2000",20,15,false),
            new TownDefinition("Interior2010","주거/사무형 실내; 역할 미확정 (2010)","Interior2010_Map","Interior2010_Runtime","2010",20,15,false),
            new TownDefinition("Interior2100","방어구점 (2100)","Interior2100_Map","Interior2100_Runtime","2100",20,15,false),
            new TownDefinition("Interior2110","용병상점 (2110)","Interior2110_Map","Interior2110_Runtime","2110",20,15,false),
            new TownDefinition("Interior2120","무기점 (2120)","Interior2120_Map","Interior2120_Runtime","2120",20,15,false),
            new TownDefinition("Interior2130","방어구점 (2130)","Interior2130_Map","Interior2130_Runtime","2130",20,15,false),
            new TownDefinition("Interior2140","주점형 실내 (2140)","Interior2140_Map","Interior2140_Runtime","2140",20,15,false),
            new TownDefinition("Interior2150","무기점 (2150)","Interior2150_Map","Interior2150_Runtime","2150",20,15,false),
            new TownDefinition("Interior2160","방어구점 (2160)","Interior2160_Map","Interior2160_Runtime","2160",20,15,false),
            new TownDefinition("Interior2170","장신구점 (2170)","Interior2170_Map","Interior2170_Runtime","2170",20,15,false),
            new TownDefinition("Interior2180","소모품점 (2180)","Interior2180_Map","Interior2180_Runtime","2180",20,15,false),
            new TownDefinition("Interior2190","장비 진열 실내; 간판 없음 (2190)","Interior2190_Map","Interior2190_Runtime","2190",20,15,false),
            new TownDefinition("Interior2200","장신구점 (2200)","Interior2200_Map","Interior2200_Runtime","2200",20,15,false),
            new TownDefinition("Interior2300","소모품점 (2300)","Interior2300_Map","Interior2300_Runtime","2300",20,15,false),
            new TownDefinition("Interior2400","은행 (2400)","Interior2400_Map","Interior2400_Runtime","2400",20,15,false),
            new TownDefinition("Interior2510","창고 (2510)","Interior2510_Map","Interior2510_Runtime","2510",20,15,false),
            new TownDefinition("Interior2600","교회형 실내 (2600)","Interior2600_Map","Interior2600_Runtime","2600",20,20,false),
            new TownDefinition("Interior2700","주점형 실내 (2700)","Interior2700_Map","Interior2700_Runtime","2700",20,20,false),
            new TownDefinition("Interior2800","서가·마법 문양 실내; 역할 미확정 (2800)","Interior2800_Map","Interior2800_Runtime","2800",20,20,false),
            new TownDefinition("Interior2900","용병상점 (2900)","Interior2900_Map","Interior2900_Runtime","2900",20,15,false),
            new TownDefinition("Interior3000","복합 사무/주거형 실내; 역할 미확정 (3000)","Interior3000_Map","Interior3000_Runtime","3000",30,30,false)
        });
        public static bool TryGet(string key,out TownDefinition region)
        {foreach(var row in All)if(row.Key==key){region=row;return true;}region=null;return false;}
        public static bool IsMap(string map)
        {foreach(var row in All)if(row.MapScene==map)return true;return false;}
        public static bool IsPair(string map,string runtime)
        {foreach(var row in All)if(row.MapScene==map && row.RuntimeScene==runtime)return true;return false;}
    }
}
