using System;
using System.Collections.Generic;
namespace DarkSaver.Core
{
    public sealed class ScenarioDefinition
    {
        public readonly string Key,Title,MapScene,RuntimeScene,PrerequisiteKey;public readonly int Width,Height;
        public ScenarioDefinition(string key,string title,string map,string runtime,int width,int height,string prerequisite=null){Key=key;Title=title;MapScene=map;RuntimeScene=runtime;Width=width;Height=height;PrerequisiteKey=prerequisite ?? (key=="01" ? "" : (int.Parse(key)-1).ToString("00"));}
    }
    public static class ScenarioCatalog
    {
        public static readonly IReadOnlyList<ScenarioDefinition> All=Array.AsReadOnly(new[]{
            new ScenarioDefinition("01","브르고스궁","Brugos01_Map","Brugos01_Runtime",40,40),
            new ScenarioDefinition("02","자모라 요새","Map_02_자모라 요새","Scenario02_Runtime", 40,40),
            new ScenarioDefinition("03","에트나 화산동굴","Map_03_에트나 화산동굴","Scenario03_Runtime", 50,50),
            new ScenarioDefinition("04","알카디아","Map_04_알카디아","Scenario04_Runtime", 40,40),
            new ScenarioDefinition("05","브린힐트 국경","Map_05_브린힐트 국경","Scenario05_Runtime", 50,50),
            new ScenarioDefinition("06","이름없는 마을","Map_06_이름없는 마을","Scenario06_Runtime", 40,40),
            new ScenarioDefinition("07","사그라하스 신전","Map_07_사그라하스 신전","Scenario07_Runtime", 40,40),
            new ScenarioDefinition("08","사군토 항구","Map_08_사군토 항구","Scenario08_Runtime", 42,42),
            new ScenarioDefinition("09","시시리오섬 해안","Map_09_시시리오섬 해안","Scenario09_Runtime", 50,50),
            new ScenarioDefinition("10","달라이 호수","Map_10_달라이 호수","Scenario10_Runtime", 50,50),
            new ScenarioDefinition("11","아케론 오아시스","Map_11_아케론 오아시스","Scenario11_Runtime", 60,60),
            new ScenarioDefinition("12","아멘호텝 왕릉","Map_12_아멘호텝 왕릉","Scenario12_Runtime", 40,40),
            new ScenarioDefinition("13","아멘호텝 왕릉 안","Map_13_아멘호텝 왕릉 안","Scenario13_Runtime", 40,40),
            new ScenarioDefinition("14","스케리아 마을","Map_14_스케리아 마을","Scenario14_Runtime", 60,60),
            new ScenarioDefinition("15","이름없는 마을","Map_15_이름없는 마을","Scenario15_Runtime", 60,60),
            new ScenarioDefinition("16","발할라 평원","Map_16_발할라 평원","Scenario16_Runtime", 40,40),
            new ScenarioDefinition("17","비공정 앞","Map_17_비공정 앞","Scenario17_Runtime", 40,40),
            // Explicit single-player side branches after17; no original server unlock inference.
            new ScenarioDefinition("33","야누스","Map_33_야누스","Scenario33_Runtime",40,50,"17"),
            new ScenarioDefinition("330","추가 전장 330","Map_330_추가 전장 330","Scenario330_Runtime",20,15,"17"),
            new ScenarioDefinition("331","추가 전장 331","Map_331_추가 전장 331","Scenario331_Runtime",20,15,"330"),
            new ScenarioDefinition("332","추가 전장 332","Map_332_추가 전장 332","Scenario332_Runtime",20,15,"331"),
            new ScenarioDefinition("333","추가 전장 333","Map_333_추가 전장 333","Scenario333_Runtime",20,15,"332"),
            new ScenarioDefinition("404","추가 전장 404","Map_404_추가 전장 404","Scenario404_Runtime",20,15,"17"),
            new ScenarioDefinition("405","추가 전장 405","Map_405_추가 전장 405","Scenario405_Runtime",20,15,"404"),
            new ScenarioDefinition("406","추가 전장 406","Map_406_추가 전장 406","Scenario406_Runtime",20,15,"405")
        });
        public static bool TryGet(string key,out ScenarioDefinition scenario){foreach(var row in All)if(row.Key==key || "Scenario"+row.Key==key){scenario=row;return true;}scenario=null;return false;}
        public static bool CanEnter(string key,Func<string,bool> isCleared) =>
            isCleared!=null && TryGet(key,out var row) && (row.Key=="01" || !isCleared(row.Key) && isCleared(row.PrerequisiteKey));
        public static bool IsProgressKey(string key)=>key!="01" && TryGet(key,out var row) && row.Key==key;
        public static bool IsPair(string map,string runtime){foreach(var row in All)if(row.MapScene==map && row.RuntimeScene==runtime)return true;return false;}
        public static bool IsMap(string map){foreach(var row in All)if(row.MapScene==map)return true;return false;}
    }
}
