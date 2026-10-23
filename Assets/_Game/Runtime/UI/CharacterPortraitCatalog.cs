using System;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed class CharacterPortraitCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry { public int jobId; public Texture2D image; }
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private Texture2D noFace;
        public int Count => entries.Length;
        public Texture2D NoFace => noFace;
        public void Initialize(Entry[] source, Texture2D fallback)
        {
            if(source==null || fallback==null || fallback.width!=64 || fallback.height!=64) throw new ArgumentException("Invalid portrait catalog");
            var copy=new Entry[source.Length];
            for(int i=0;i<source.Length;i++) {
                var entry=source[i];
                if(entry==null || entry.jobId<100 || entry.jobId>169 || entry.image==null || entry.image.width!=64 || entry.image.height!=64)
                    throw new ArgumentException("Invalid portrait entry");
                copy[i]=new Entry {jobId=entry.jobId,image=entry.image};
            }
            Array.Sort(copy,(a,b)=>a.jobId.CompareTo(b.jobId));
            for(int i=1;i<copy.Length;i++) if(copy[i-1].jobId==copy[i].jobId) throw new ArgumentException("Duplicate portrait");
            entries=copy; noFace=fallback;
        }
        public bool TryGet(int jobId, out Texture2D image)
        {
            image=null; if(jobId<100 || jobId>169) return false;
            foreach(var entry in entries) if(entry.jobId==jobId) { image=entry.image; return image!=null; }
            image=noFace; return image!=null;
        }
    }
}
