using System;
using System.Collections.Generic;
namespace DarkSaver.Core
{
    // Native exterior/interior resources. Routing, bank/warehouse roles of unsigned
    // duplicate buildings and doorway thresholds are authored single-player choices.
    public sealed class KaosiaFacilityDefinition
    {
        public readonly TownDefinition Region;
        public readonly int BuildingIndex,BuildingGroup;
        public readonly float DoorX,DoorY,ReturnX,ReturnY;
        public readonly string ReturnSpawn;
        public KaosiaFacilityDefinition(string key,string title,string native,int height,int building,int group,float dx,float dy,float rx,float ry)
        {
            Region=new TownDefinition(key,title,key+"_Map",key+"_Runtime",native,20,height,false);
            BuildingIndex=building;BuildingGroup=group;DoorX=dx;DoorY=dy;ReturnX=rx;ReturnY=ry;ReturnSpawn="From"+key;
        }
    }
    public static class KaosiaFacilityCatalog
    {
        public static readonly IReadOnlyList<KaosiaFacilityDefinition> All=Array.AsReadOnly(new[]{
            new KaosiaFacilityDefinition("KaosiaAccessoryShop","카오시아 장신구점","2200",15,247,8,48.5f,10.5f,48.5f,2.5f),
            new KaosiaFacilityDefinition("KaosiaBank","카오시아 은행","2400",15,240,13,11.5f,5.5f,11.5f,2.5f),
            new KaosiaFacilityDefinition("KaosiaWarehouse","카오시아 창고","2510",15,197,9,13.5f,19.5f,13.5f,16.5f),
            new KaosiaFacilityDefinition("KaosiaTemple","카오시아 신전","2600",20,135,1,32.5f,40.5f,32.5f,37.5f),
            new KaosiaFacilityDefinition("KaosiaMagicTools","카오시아 마법도구·소모품점","2300",15,224,2,28.5f,14.5f,28.5f,9.5f)
        });
        public static bool TryGet(string key,out KaosiaFacilityDefinition facility)
        {foreach(var row in All)if(row.Region.Key==key){facility=row;return true;}facility=null;return false;}
        public static bool IsPair(string map,string runtime)
        {foreach(var row in All)if(row.Region.MapScene==map && row.Region.RuntimeScene==runtime)return true;return false;}
        public static bool IsMap(string map)
        {foreach(var row in All)if(row.Region.MapScene==map)return true;return false;}
        public static IEnumerable<WorldRoute> RoutesFrom(string key)
        {
            foreach(var row in All)
            {
                if(key=="Kaosia")yield return new WorldRoute {fromKey=key,toKey=row.Region.Key,portalX=row.DoorX,portalY=row.DoorY,
                    spawnX=row.ReturnX,spawnY=row.ReturnY,spawnId=row.ReturnSpawn,targetSpawnId="FacilityEntry"};
                if(key==row.Region.Key)yield return new WorldRoute {fromKey=key,toKey="Kaosia",portalX=10.5f,portalY=.5f,
                    spawnX=10.5f,spawnY=3.5f,spawnId="FacilityEntry",targetSpawnId=row.ReturnSpawn};
            }
        }
    }
}
