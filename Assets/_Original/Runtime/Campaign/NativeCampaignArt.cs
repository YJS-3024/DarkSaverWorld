using System;
using System.Linq;
using UnityEngine;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Campaign
{
    public sealed class NativeCampaignArt : ScriptableObject
    {
        public TextAsset catalog;
        public TextAsset[] maps, graphics, scripts;
        public TextAsset[] legacyGraphics;
        public int[] legacyGraphicIds;
        public int[] graphicIds, pictureIds;
        public Texture2D[] pictures;
        public Texture2D[] faces; public int[] faceIds;
        public TextAsset gatewaySettings,gatewayMaps,gatewayWindowDefinition;
        public Texture2D gatewayWindow;
        private CampaignDefinition cached;
        public CampaignDefinition Definition
        {
            get
            {
                if (cached != null) return cached;
                if (catalog == null) return null;
                cached = JsonUtility.FromJson<CampaignDefinition>(catalog.text);
                foreach (var map in cached.maps) { map.intro = Script(map.introIndex); map.outro = Script(map.outroIndex); map.events = Script(map.eventIndex); }
                return cached;
            }
        }
        public bool IsConfigured => Definition?.maps?.Length > 0 && Definition.connections != null && maps != null && graphics?.Length == graphicIds?.Length
            && pictures?.Length == pictureIds?.Length && scripts != null && faces != null && faces.Length == faceIds?.Length
            && (!Definition.maps.Any(m=>m.world.sourceVersion=="Saver_1.2") || legacyGraphics!=null&&legacyGraphicIds!=null&&legacyGraphics.Length>0&&legacyGraphics.Length==legacyGraphicIds.Length&&legacyGraphics.All(g=>g!=null));
        public GatewayMapCatalog GatewayMaps => gatewayMaps==null?null:JsonUtility.FromJson<GatewayMapCatalog>(gatewayMaps.text);
        public WorldWindowDefinition GatewayWindow => gatewayWindowDefinition==null?null:JsonUtility.FromJson<WorldWindowDefinition>(gatewayWindowDefinition.text);
        public WorldDefinition Merge(WorldDefinition world)
        {
            world=Definition.Merge(world);
            if(gatewaySettings!=null)
            {
                var settings=ScenarioGatewayData.Decode(gatewaySettings.bytes);
                if(settings.applyToGame&&settings.gates.Any(g=>g.enabled&&!g.IsWalk)&&(gatewayWindow==null||GatewayWindow==null))throw new System.IO.InvalidDataException("NPC 방 목록 원본 UI 자료가 없습니다.");
                ScenarioGatewayData.Apply(settings,GatewayMaps,world);
            }
            return world;
        }
        public byte[] MapBytes(int id)
        { var map = Definition.Map(id); return map == null ? null : maps[map.nativeIndex].bytes; }
        public byte[] Graphic(int id)
        { int i = Array.IndexOf(graphicIds, id); if (i < 0) throw new InvalidOperationException("Native GRS is missing: " + id); return graphics[i].bytes; }
        public byte[] Graphic(int id,int mapId)
        {
            if(Definition.Map(mapId)?.world.sourceVersion!="Saver_1.2")return Graphic(id);
            int i=legacyGraphicIds==null?-1:Array.IndexOf(legacyGraphicIds,id);
            if(i<0||legacyGraphics==null||i>=legacyGraphics.Length||legacyGraphics[i]==null)throw new InvalidOperationException("Legacy GRS is missing for map "+mapId+": "+id);
            return legacyGraphics[i].bytes;
        }
        public Texture2D Picture(int job)
        { var info = Definition.jobs.FirstOrDefault(j => j.job == job); int i = info == null ? -1 : Array.IndexOf(pictureIds, info.picture); return i < 0 ? null : pictures[i]; }
        public string Script(int index) => index < 0 || index >= scripts.Length ? "" : scripts[index].text;
        public Texture2D Face(int id) { int i = Array.IndexOf(faceIds, id); return i < 0 || i >= faces.Length ? null : faces[i]; }
    }
}
