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
        public int[] graphicIds, pictureIds;
        public Texture2D[] pictures;
        public Texture2D[] faces; public int[] faceIds;
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
            && pictures?.Length == pictureIds?.Length && scripts != null && faces != null && faces.Length == faceIds?.Length;
        public WorldDefinition Merge(WorldDefinition world) => Definition.Merge(world);
        public byte[] MapBytes(int id)
        { var map = Definition.Map(id); return map == null ? null : maps[map.nativeIndex].bytes; }
        public byte[] Graphic(int id)
        { int i = Array.IndexOf(graphicIds, id); if (i < 0) throw new InvalidOperationException("Native GRS is missing: " + id); return graphics[i].bytes; }
        public Texture2D Picture(int job)
        { var info = Definition.jobs.FirstOrDefault(j => j.job == job); int i = info == null ? -1 : Array.IndexOf(pictureIds, info.picture); return i < 0 ? null : pictures[i]; }
        public string Script(int index) => index < 0 || index >= scripts.Length ? "" : scripts[index].text;
        public Texture2D Face(int id) { int i = Array.IndexOf(faceIds, id); return i < 0 || i >= faces.Length ? null : faces[i]; }
    }
}
