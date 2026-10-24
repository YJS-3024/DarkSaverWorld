using System;
using System.Collections.Generic;

namespace DarkSaver.Core
{
    public static class FieldCatalog
    {
        // Native layouts; display names and inter-region routes are authored.
        public static readonly IReadOnlyList<TownDefinition> All=Array.AsReadOnly(new[] {
            Field("1010","카오시아 외곽",160,160),Field("1011","북쪽 구릉지",60,60),Field("1012","북동쪽 들판",40,40),
            Field("1020","강변 평원",100,100),Field("1030","설원과 사막",100,100),Field("1040","황혼 습지",100,100),
            Field("1050","황무지 협곡",30,30),Field("1060","빙하 경계",80,80),Field("1070","작은 분지",20,15),
            Field("1080","외딴 공터",30,30),Field("1090","동쪽 고개",20,15),Field("1100","깊은 들판",24,20),
            Field("1110","고대 유적지",80,80),Field("1111","유적 외곽",50,50),Field("1200","갈림길 분지",20,20),
            Field("1300","먼 들판",20,20),Field("1400","남쪽 공터",20,15),Field("1410","외곽 공터",20,15)
        });
        private static TownDefinition Field(string id,string title,int width,int height) =>
            new TownDefinition("WorldField"+id,title,"World"+id+"_Map",id=="1010"?"Field_Runtime":"Field"+id+"_Runtime",id,width,height,false);
        public static bool TryGet(string key,out TownDefinition field)
        {foreach(var row in All)if(row.Key==key){field=row;return true;}field=null;return false;}
        public static bool IsPair(string map,string runtime)
        {foreach(var row in All)if(row.MapScene==map && row.RuntimeScene==runtime)return true;return false;}
        public static bool IsMap(string map)
        {foreach(var row in All)if(row.MapScene==map)return true;return false;}
    }
    [Serializable] public sealed class WorldRoute
    {
        public string fromKey,toKey,spawnId,targetSpawnId;
        public float portalX,portalY,spawnX,spawnY;
        public bool legacy;
    }
    [Serializable] public sealed class WorldRouteContent
    {
        public int version;
        public string provenance;
        public WorldRoute[] routes;
        public static bool Resolve(string key,out TownDefinition region)
        {
            if(TownCatalog.TryGet(key,out region) || FieldCatalog.TryGet(key,out region) || NativeInteriorCatalog.TryGet(key,out region))return true;
            if(KaosiaFacilityCatalog.TryGet(key,out var facility)){region=facility.Region;return true;}
            if(ShopInteriorCatalog.TryGet(key,out var shop)){region=shop.Region;return true;}return false;
        }
        public bool IsValid
        {
            get
            {
                if(version!=1 || routes==null || routes.Length==0)return false;
                var keys=new HashSet<string>();var spawns=new HashSet<string>();
                foreach(var row in routes)
                {
                    if(row==null || !Resolve(row.fromKey,out var from) || !Resolve(row.toKey,out _) || row.fromKey==row.toKey
                        || !keys.Add(row.fromKey+":"+row.toKey) || !spawns.Add(row.fromKey+":"+row.spawnId) || string.IsNullOrEmpty(row.spawnId) || string.IsNullOrEmpty(row.targetSpawnId)
                        || float.IsNaN(row.portalX) || float.IsNaN(row.portalY) || float.IsNaN(row.spawnX) || float.IsNaN(row.spawnY)
                        || row.portalX<0 || row.portalX>=from.Width || row.portalY<0 || row.portalY>=from.Height
                        || row.spawnX<0 || row.spawnX>=from.Width || row.spawnY<0 || row.spawnY>=from.Height)return false;
                }
                foreach(var row in routes)
                {
                    float dx=row.portalX-row.spawnX,dy=row.portalY-row.spawnY;
                    if(!row.legacy && dx*dx+dy*dy<9)return false;
                    bool reverse=false;
                    foreach(var other in routes)
                        if(other.fromKey==row.toKey && other.toKey==row.fromKey && other.spawnId==row.targetSpawnId
                            && other.targetSpawnId==row.spawnId){reverse=true;break;}
                    if(!reverse)return false;
                }
                var visited=new HashSet<string>{"Kaosia"};bool changed=true;
                while(changed)
                {changed=false;foreach(var row in routes)if(visited.Contains(row.fromKey) && visited.Add(row.toKey))changed=true;}
                return visited.Count==TownCatalog.All.Count+FieldCatalog.All.Count;
            }
        }
    }
}
