using System;
using System.Collections.Generic;

namespace DarkSaver.Core
{
    // Native2000/2100 interiors; Kaosia association and entrance thresholds are single-player choices.
    public sealed class ShopInteriorDefinition
    {
        public readonly string Key,Title,ReturnSpawn;
        public readonly bool Weapons;
        public readonly int AdditionalPosition;
        public readonly int SourceBuildingIndex;
        public readonly float DoorX,DoorY,ReturnX,ReturnY;
        public readonly TownDefinition Region,TownContext;
        public const int Width=20,Height=15;
        public ShopInteriorDefinition(string key,string title,bool weapons,int sourceIndex,float doorX,float doorY,float returnX,float returnY,string spawn,int additionalPosition=0,string native=null)
        {
            Key=key;Title=title;Weapons=weapons;AdditionalPosition=additionalPosition;SourceBuildingIndex=sourceIndex;DoorX=doorX;DoorY=doorY;ReturnX=returnX;ReturnY=returnY;ReturnSpawn=spawn;
            Region=new TownDefinition(key,title,key+"_Map",key+"_Runtime",native ?? (weapons?"2000":"2100"),Width,Height,false);
            TownContext=new TownDefinition("Kaosia",title,Region.MapScene,Region.RuntimeScene,Region.NativeMap,Width,Height,false);
        }
        public bool OffersPosition(long position)=>AdditionalPosition!=0 ? position==AdditionalPosition : Weapons ? position==1 || position==2 : position==3 || position==4 || position==5 || position==8;
    }
    public static class ShopInteriorCatalog
    {
        public static readonly IReadOnlyList<ShopInteriorDefinition> All=Array.AsReadOnly(new[]{
            new ShopInteriorDefinition("KaosiaWeaponShop","카오시아 무기점",true,108,48.5f,47.5f,44.5f,47.5f,"FromWeaponShop"),
            new ShopInteriorDefinition("KaosiaArmorShop","카오시아 방어구점",false,191,48.5f,20.5f,48.5f,17.5f,"FromArmorShop")
        });
        private static readonly ShopInteriorDefinition Accessory=new ShopInteriorDefinition("KaosiaAccessoryShop","카오시아 장신구점",false,247,48.5f,10.5f,48.5f,2.5f,"FromKaosiaAccessoryShop",6,"2200");
        private static readonly ShopInteriorDefinition MagicTools=new ShopInteriorDefinition("KaosiaMagicTools","카오시아 마법도구·소모품점",false,224,28.5f,14.5f,28.5f,9.5f,"FromKaosiaMagicTools",7,"2300");
        public static IEnumerable<ShopInteriorDefinition> ShopsWithFacilities
        {get{foreach(var row in All)yield return row;yield return Accessory;yield return MagicTools;}}
        public const float EntryX=10.5f,EntryY=3.5f,ExitX=10.5f,ExitY=.5f;
        public static IEnumerable<WorldRoute> RoutesFrom(string key)
        {
            foreach(var shop in All)
            {
                if(key=="Kaosia")yield return new WorldRoute {fromKey=key,toKey=shop.Key,portalX=shop.DoorX,portalY=shop.DoorY,
                    spawnX=shop.ReturnX,spawnY=shop.ReturnY,spawnId=shop.ReturnSpawn,targetSpawnId="ShopEntry"};
                if(key==shop.Key)yield return new WorldRoute {fromKey=key,toKey="Kaosia",portalX=ExitX,portalY=ExitY,
                    spawnX=EntryX,spawnY=EntryY,spawnId="ShopEntry",targetSpawnId=shop.ReturnSpawn};
            }
        }
        public static bool TryGet(string key,out ShopInteriorDefinition shop)
        {foreach(var row in ShopsWithFacilities)if(row.Key==key){shop=row;return true;}shop=null;return false;}
        public static bool IsPair(string map,string runtime)
        {foreach(var row in ShopsWithFacilities)if(row.Region.MapScene==map && row.Region.RuntimeScene==runtime)return true;return false;}
    }
}
