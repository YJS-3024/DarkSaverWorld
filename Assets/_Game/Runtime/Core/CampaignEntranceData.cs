using System;
using System.Collections.Generic;
namespace DarkSaver.Core
{
    [Serializable] public sealed class CampaignEntrance
    {
        public string key,fieldKey;
        public float portalX,portalY,spawnX,spawnY,entryX,entryY,returnX,returnY;
    }
    [Serializable] public sealed class CampaignEntranceData
    {
        public int version;public string provenance;public CampaignEntrance[] entries;
        public bool IsValid
        {
            get
            {
                if(version!=1 || entries==null || entries.Length!=17)return false;
                var keys=new HashSet<string>();
                foreach(var entry in entries)
                {
                    if(entry==null || !keys.Add(entry.key) || !ScenarioCatalog.TryGet(entry.key,out var scenario) || !FieldCatalog.TryGet(entry.fieldKey,out var region))return false;
                    bool Inside(float x,float y,int width,int height)=>!float.IsNaN(x)&&!float.IsNaN(y)&&x>=0&&y>=0&&x<width&&y<height;
                    if(!Inside(entry.portalX,entry.portalY,region.Width,region.Height)||!Inside(entry.spawnX,entry.spawnY,region.Width,region.Height)
                        ||!Inside(entry.entryX,entry.entryY,scenario.Width,scenario.Height)||!Inside(entry.returnX,entry.returnY,scenario.Width,scenario.Height))return false;
                    float dx=entry.portalX-entry.spawnX,dy=entry.portalY-entry.spawnY;
                    if(dx*dx+dy*dy<9)return false;
                    dx=entry.entryX-entry.returnX;dy=entry.entryY-entry.returnY;if(dx*dx+dy*dy<9)return false;
                }
                return true;
            }
        }
    }
}
