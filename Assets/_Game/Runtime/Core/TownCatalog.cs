using System;
using System.Collections.Generic;

namespace DarkSaver.Core
{
    public sealed class TownDefinition
    {
        public readonly string Key, Title, MapScene, RuntimeScene, NativeMap;
        public readonly int Width, Height;
        public readonly bool SourceIdentityConfirmed;
        public TownDefinition(string key,string title,string map,string runtime,string native,int width,int height,bool confirmed)
        { Key=key;Title=title;MapScene=map;RuntimeScene=runtime;NativeMap=native;Width=width;Height=height;SourceIdentityConfirmed=confirmed; }
    }
    public static class TownCatalog
    {
        // Names are documented. Additional native-map/name bindings are authored
        // single-player choices until original region routing is recovered.
        public static readonly IReadOnlyList<TownDefinition> All = Array.AsReadOnly(new[] {
            new TownDefinition("Kaosia","카오시아","Kaosia_Map","Town_Runtime","1000",43,43,true),
            new TownDefinition("Belpueus","벨퓌어스","Belpueus_Map","Belpueus_Runtime","1002",40,40,false),
            new TownDefinition("Sisirio","시시리오","Sisirio_Map","Sisirio_Runtime","1003",30,30,false),
            new TownDefinition("Castlelim","캐슬림","Castlelim_Map","Castlelim_Runtime","1004",60,60,false)
        });
        public static bool TryGet(string key,out TownDefinition town)
        { foreach(var row in All)if(row.Key==key){town=row;return true;}town=null;return false; }
        public static bool IsPair(string map,string runtime)
        { foreach(var row in All)if(row.MapScene==map && row.RuntimeScene==runtime)return true;return false; }
        public static bool IsMap(string map)
        { foreach(var row in All)if(row.MapScene==map)return true;return false; }
        public static bool IsRuntime(string runtime)
        { foreach(var row in All)if(row.RuntimeScene==runtime)return true;return false; }
    }
}
