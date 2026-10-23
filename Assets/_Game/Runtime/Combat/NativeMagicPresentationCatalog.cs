using System;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed class NativeMagicPresentationCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            [SerializeField] int id,pattern,family,area,frames1,frames2,sound1,sound2;
            public int Id=>id;public int Pattern=>pattern;public int Family=>family;
            public int Sound1=>sound1;public int Sound2=>sound2;
            public bool SharedCaster=>family==2 || family==3 || family==4 || family==7 || family==8 || family==9 || family==13 || family==21 || family==22;
            public int CasterLifetime=>SharedCaster?10:family==6 || family==15 || family==16 || family==19?area*5
                :family==14?3*area*frames2+4:family==18?22:family==20?area*2:family==12?(frames1+1<3?8:frames1+1)*4:0;
            public Entry(int magicId,int patternId,int type,int radius,int f1,int f2,int s1,int s2)
            {id=magicId;pattern=patternId;family=type;area=radius;frames1=f1;frames2=f2;sound1=s1;sound2=s2;}
        }
        [SerializeField] Entry[] entries=Array.Empty<Entry>();
        public int Count=>entries.Length;
        public Entry GetAt(int index)=>entries[index];
        public void Initialize(Entry[] source){if(source==null || Array.Exists(source,x=>x==null))throw new ArgumentException();entries=(Entry[])source.Clone();}
        public bool TryGet(int id,out Entry entry){foreach(var e in entries)if(e.Id==id){entry=e;return true;}entry=null;return false;}
    }
}
