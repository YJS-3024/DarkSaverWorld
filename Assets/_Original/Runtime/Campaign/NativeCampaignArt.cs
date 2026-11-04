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
        public Texture2D[] combineMasks;
        public TextAsset mapMusicTable;
        private OriginalMapMusic mapMusic;
        public OriginalMapMusic MapMusic=>mapMusic??(mapMusic=OriginalMapMusic.Parse(mapMusicTable?.text));
        public string[] musicPaths;
        public AudioClip[] musicClips;
        public AudioClip Music(string path)
        {int at=musicPaths==null?-1:Array.IndexOf(musicPaths,(path??"").Replace('\\','/').ToLowerInvariant());return at<0||musicClips==null||at>=musicClips.Length?null:musicClips[at];}
        public Texture2D CombineMask(int archive,int index)
        {int at=(archive==1?0:5)+index;return combineMasks!=null&&at>=0&&at<combineMasks.Length?combineMasks[at]:null;}
        public Texture2D[] faces; public int[] faceIds;
        public Texture2D noFace;
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
                cached.ConfigureNativeMaps(ApplyChanges);
                foreach (var map in cached.maps) { map.intro = Script(map.introIndex); map.outro = Script(map.outroIndex); map.events = Script(map.eventIndex); }
                return cached;
            }
        }
        public bool IsConfigured => Definition?.maps?.Length > 0 && Definition.connections != null && maps != null && graphics?.Length == graphicIds?.Length
            && pictures?.Length == pictureIds?.Length && scripts != null && faces != null && faces.Length == faceIds?.Length
            &&mapMusicTable!=null&&MapMusic.Count>0&&combineMasks?.Length==8&&combineMasks.All(m=>m!=null)&&musicPaths!=null&&musicClips?.Length==musicPaths.Length&&musicClips.All(m=>m!=null)
            && (!Definition.maps.Any(m=>m.world.sourceVersion=="Saver_1.2") || legacyGraphics!=null&&legacyGraphicIds!=null&&legacyGraphics.Length>0&&legacyGraphics.Length==legacyGraphicIds.Length&&legacyGraphics.All(g=>g!=null));
        public GatewayMapCatalog GatewayMaps => gatewayMaps==null?null:JsonUtility.FromJson<GatewayMapCatalog>(gatewayMaps.text);
        public WorldWindowDefinition GatewayWindow => gatewayWindowDefinition==null?null:JsonUtility.FromJson<WorldWindowDefinition>(gatewayWindowDefinition.text);
        public WorldDefinition Merge(WorldDefinition world)
        {
            if(Definition?.maps==null||Definition.maps.Length==0||Definition.connections==null)throw new System.IO.InvalidDataException("맵 카탈로그가 연결되지 않았습니다.");
            if(gatewaySettings==null||GatewayMaps==null)throw new System.IO.InvalidDataException("UserGateways/GatewayMaps 입구 설정이 연결되지 않았습니다.");
            world=Definition.Merge(world);
            if(gatewaySettings!=null)
            {
                var settings=ScenarioGatewayData.Decode(gatewaySettings.bytes);
                if(settings.applyToGame&&settings.gates.Any(g=>g.enabled&&!g.IsWalk)&&(gatewayWindow==null||GatewayWindow==null))throw new System.IO.InvalidDataException("NPC 방 목록 원본 UI 자료가 없습니다.");
                ScenarioGatewayData.Apply(settings,GatewayMaps,world);
            }
            return world;
        }
        private CampaignChangedMap ApplyChanges(int id,CampaignScriptStep[] steps)
        {
            var changes=(steps??Array.Empty<CampaignScriptStep>()).Where(s=>s.scene==id).Select(s=>new CampaignCommand(s.command,0))
                .Where(c=>c.name=="CHANGESPRITE"||c.name=="CHANGETILE2").Select(c=>new Maps.NativeMapDecoder.MapChange{name=c.name,args=c.args.Select((v,i)=>c.Int(i)).ToArray()}).ToArray();
            var result=Maps.NativeMapDecoder.ApplyChanges(MapBytes(id),rid=>Graphic(rid,id),changes,Definition.Map(id).world.map.attributes);
            return new CampaignChangedMap{bytes=result.bytes,attributes=result.attributes,renderAttributes=result.renderAttributes};
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
        public Texture2D Face(int id) { if(id<0)return null;int i=faceIds==null?-1:Array.IndexOf(faceIds,id);return i<0||i>=(faces?.Length??0)?noFace:faces[i]; }
    }
}
