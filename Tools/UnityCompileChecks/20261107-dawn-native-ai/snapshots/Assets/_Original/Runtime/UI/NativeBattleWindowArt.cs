using System;
using UnityEngine;
using DarkSaver.Original.Tutorial;
namespace DarkSaver.Original.UI
{
    [Serializable] public sealed class NativeBattleWindowCatalog
    {public WaitingWindowDefinition[] windows;}
    public sealed class NativeBattleWindowArt:ScriptableObject
    {
        public TextAsset layout;
        public Texture2D[] full,minimized;
        public int[] mapIds;
        public Texture2D[] minimaps;
        public TextAsset[] briefings;
        public Texture2D markers;
        private NativeBattleWindowCatalog catalog;
        public NativeBattleWindowCatalog Catalog=>catalog??(catalog=layout==null?null:JsonUtility.FromJson<NativeBattleWindowCatalog>(layout.text));
        public int Index(int scene)=>Catalog?.windows==null?-1:Array.FindIndex(Catalog.windows,w=>w.scene==scene);
        public Texture2D Minimap(int map){int i=mapIds==null?-1:Array.IndexOf(mapIds,map);return i>=0&&i<(minimaps?.Length??0)?minimaps[i]:null;}
        public string Briefing(int map){int i=mapIds==null?-1:Array.IndexOf(mapIds,map);return i>=0&&i<(briefings?.Length??0)?briefings[i]?.text:null;}
    }
}
