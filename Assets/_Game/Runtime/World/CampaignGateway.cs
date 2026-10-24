using System.Collections.Generic;
using DarkSaver.Core;
using UnityEngine;
namespace DarkSaver.World
{
    public static class CampaignGateway
    {
        private static CampaignEntranceData content;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()=>content=null;
        private static CampaignEntranceData Content
        {
            get
            {
                if(content!=null)return content;
                var source=Resources.Load<TextAsset>("Scenarios/Shared/Entrances");
                var value=source==null?null:JsonUtility.FromJson<CampaignEntranceData>(source.text);
                return content=value!=null && value.IsValid?value:null;
            }
        }
        public static CampaignEntrance Find(string key)
        {if(Content!=null)foreach(var row in Content.entries)if(row.key==key)return row;return null;}
        public static IEnumerable<WorldRoute> RoutesFrom(string region)
        {
            if(Content==null)yield break;
            foreach(var entry in Content.entries)
            {
                if(entry.fieldKey==region)yield return new WorldRoute {fromKey=region,toKey="Scenario"+entry.key,portalX=entry.portalX,portalY=entry.portalY,
                    spawnX=entry.spawnX,spawnY=entry.spawnY,spawnId="ScenarioReturn_"+entry.key,targetSpawnId="ScenarioEntry"};
                if("Scenario"+entry.key==region)yield return new WorldRoute {fromKey=region,toKey=entry.fieldKey,portalX=entry.returnX,portalY=entry.returnY,
                    spawnX=entry.entryX,spawnY=entry.entryY,spawnId="ScenarioEntry",targetSpawnId="ScenarioReturn_"+entry.key};
            }
        }
    }
}
