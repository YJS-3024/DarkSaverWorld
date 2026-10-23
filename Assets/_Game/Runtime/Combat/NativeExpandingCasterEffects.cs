using System;
using UnityEngine;

namespace DarkSaver.Combat
{
    // userunit.dll 10006820..10006A63 (caster family14).
    public sealed class NativeExpandingCasterEffects : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            [SerializeField] int magicId,area;
            [SerializeField] Sprite[] frames;
            public int Id=>magicId;
            public int FrameCount=>frames.Length;
            public int Lifetime=>3*area*frames.Length+4;
            public Entry(int id,int radius,Sprite[] source)
            {
                if(id<=0 || radius<0 || radius>32 || source==null || source.Length==0 || source.Length>64 || Array.Exists(source,x=>x==null))
                    throw new ArgumentException("Invalid expanding caster data.");
                magicId=id;area=radius;frames=(Sprite[])source.Clone();
            }
            public Sprite Frame(int counter)=>counter<0 || counter>=Lifetime?null:frames[counter%frames.Length];
            public Vector2 PixelSize(int counter)
            {
                var sprite=Frame(counter);if(sprite==null)return Vector2.zero;
                // Signed integer /2 precedes the final doubling; odd dimensions must not be rounded up.
                return new Vector2(2*((counter+(int)sprite.rect.width-32)/2+3*counter),
                    2*((counter+(int)sprite.rect.height-32)/2+3*counter));
            }
        }
        [SerializeField] Entry[] entries=Array.Empty<Entry>();
        public int MagicCount=>entries.Length;
        public void Initialize(Entry[] source)
        {
            if(source==null || source.Length==0 || Array.Exists(source,x=>x==null))throw new ArgumentException("Missing expanding caster entries.");
            entries=(Entry[])source.Clone();
        }
        public bool TryGet(int id,out Entry entry)
        {foreach(var e in entries)if(e.Id==id){entry=e;return true;}entry=null;return false;}
    }
}
