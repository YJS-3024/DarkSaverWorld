using System;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed class MercenaryInfoIconCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry { public int jobId, sheetId; public Sprite icon; }
        [SerializeField] private Entry[] entries=Array.Empty<Entry>();
        public int Count=>entries.Length;
        public void Initialize(Entry[] source)
        {
            if(source==null || source.Length==0)throw new ArgumentException("Empty mercenary info icons");
            var copy=new Entry[source.Length];
            for(int i=0;i<source.Length;i++) {
                var e=source[i];
                if(e==null || e.jobId<200 || (e.sheetId!=200 && e.sheetId!=207) || e.icon==null
                    || e.icon.texture.width!=32 || e.icon.texture.height!=32 || e.icon.rect!=new Rect(0,0,32,32))
                    throw new ArgumentException("Invalid converted mercenary icon");
                copy[i]=new Entry {jobId=e.jobId,sheetId=e.sheetId,icon=e.icon};
            }
            Array.Sort(copy,(a,b)=>a.jobId.CompareTo(b.jobId));
            for(int i=1;i<copy.Length;i++)if(copy[i-1].jobId==copy[i].jobId)throw new ArgumentException("Duplicate mercenary job");
            entries=copy;
        }
        public bool TryGet(int jobId,out Sprite icon)
        {
            foreach(var e in entries)if(e.jobId==jobId){icon=e.icon;return icon!=null;}
            icon=null;return false;
        }
    }
}
