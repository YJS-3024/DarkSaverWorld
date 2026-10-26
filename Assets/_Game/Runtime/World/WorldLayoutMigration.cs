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
            foreach(var entry in content.maps)if(entry!=null && entry.mapScene==map)return entry.TryResolve(x,y,out resolvedX,out resolvedY);
            return false;
        }
    }
}
