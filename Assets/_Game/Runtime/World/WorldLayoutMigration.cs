using UnityEngine;
using DarkSaver.Core;

namespace DarkSaver.World
{
    public static class WorldLayoutMigration
    {
        private static WorldLayoutResumeData content;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()=>content=null;
        public static bool AppliesTo(string map)=>map=="Kaosia_Map" || map=="Belpueus_Map" || map=="World1010_Map";
        public static bool TryResolve(string map,float x,float y,out float resolvedX,out float resolvedY)
        {
            resolvedX=x;resolvedY=y;
            if(!AppliesTo(map))return false;
            if(content==null) {
                var asset=Resources.Load<TextAsset>("World/LateLayoutResume");
                if(asset==null)return false;
                content=JsonUtility.FromJson<WorldLayoutResumeData>(asset.text);
            }
            if(content==null || content.version!=1 || content.maps==null)return false;
            foreach(var entry in content.maps)if(entry!=null && entry.mapScene==map)
            {
                // Kaosia was restored from 61x61 to the user-confirmed 43x43 map.
                // Resolve old positive coordinates in memory; never rewrite the saved snapshot.
                if(map=="Kaosia_Map" && entry.IsValid && x>=0 && y>=0
                    && !float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(y) && !float.IsInfinity(y))
                {
                    x=Mathf.Min(x,entry.width-.5f);y=Mathf.Min(y,entry.height-.5f);
                }
                return entry.TryResolve(x,y,out resolvedX,out resolvedY);
            }
            return false;
        }
    }
}
