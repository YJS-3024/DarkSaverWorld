using System;
using DarkSaver.Core;
using UnityEngine;

namespace DarkSaver.World
{
    [Serializable] public sealed class OriginalInteriorStaffEntry
    {
        public string nativeMap,texture,reference,evidence;
        public int appearanceId,frame,pixelX,pixelY;
    }
    [Serializable] public sealed class OriginalInteriorStaffData
    {
        public int version;
        public OriginalInteriorStaffEntry[] entries;
    }

    // Original-first Kaosia presentation, with separately marked user-requested exceptions.
    public static class KaosiaOriginalPresentation
    {
        public static bool IsTown(string key)=>key=="Kaosia";
        public static bool IsInterior(string key,out string nativeMap)
        {
            nativeMap=null;
            if(ShopInteriorCatalog.TryGet(key,out var shop))nativeMap=shop.Region.NativeMap;
            else if(KaosiaFacilityCatalog.TryGet(key,out var facility))nativeMap=facility.Region.NativeMap;
            else if(NativeInteriorCatalog.TryGet(key,out var interior))nativeMap=interior.NativeMap;
            return nativeMap=="2000" || nativeMap=="2100" || nativeMap=="2200" || nativeMap=="2300"
                || nativeMap=="2400" || nativeMap=="2510" || nativeMap=="2600" || nativeMap=="2800";
        }
        public static bool IsRegion(string key)=>IsTown(key) || IsInterior(key,out _);
        public static OriginalInteriorStaffData LoadStaff()
        {
            var source=Resources.Load<TextAsset>("Town/OriginalInteriorStaff");
            var data=source==null?null:JsonUtility.FromJson<OriginalInteriorStaffData>(source.text);
            if(data==null || data.version!=1 || data.entries==null)
                throw new InvalidOperationException("Original indoor staff resources unavailable.");
            return data;
        }
    }
}
