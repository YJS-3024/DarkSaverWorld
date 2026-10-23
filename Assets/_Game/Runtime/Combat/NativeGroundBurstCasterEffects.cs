using System;
using UnityEngine;

namespace DarkSaver.Combat
{
    // userunit.dll 10009A20..10009C9F; setarea8 10001930.
    public sealed class NativeGroundBurstCasterEffects : ScriptableObject
    {
        [SerializeField] int[] magicIds=Array.Empty<int>(),areas=Array.Empty<int>();
        [SerializeField] NativeTargetEffects frames;
        public int MagicCount=>magicIds.Length;
        public void Initialize(int[] ids,int[] sourceAreas,NativeTargetEffects source)
        {
            if(ids==null || sourceAreas==null || ids.Length==0 || ids.Length!=sourceAreas.Length || source==null
                || Array.Exists(sourceAreas,x=>x<0 || x>32))throw new ArgumentException("Invalid ground burst caster data.");
            foreach(int id in ids)if(!source.TryGet(id,out var p) || !p.RandomGround)throw new ArgumentException("Missing ground burst frames.");
            magicIds=(int[])ids.Clone();areas=(int[])sourceAreas.Clone();frames=source;
        }
        public bool TryGet(int id,out int area,out NativeTargetEffects.Pattern pattern)
        {
            area=0;pattern=null;int index=Array.IndexOf(magicIds,id);
            if(index<0 || frames==null || !frames.TryGet(id,out pattern))return false;
            area=areas[index];return true;
        }
        public static bool Contains(int area,int dx,int dy)
        {
            long distance=Math.Abs((long)dx)+Math.Abs((long)dy),radius=(long)area+1;
            return area>=0 && distance<radius && (distance&1)!=(radius&1);
        }
        public static int NextCounter(int counter,int initialRandom)
        {
            if(counter<0 || initialRandom<0 || initialRandom>4)throw new ArgumentOutOfRangeException();
            return (counter==0?initialRandom:counter)+1; // Unlike the target renderer, caster never pauses.
        }
        public static Sprite Frame(NativeTargetEffects.Pattern pattern,int counter,int jitter)
        {
            if(pattern==null || !pattern.RandomGround || counter<0 || counter>=pattern.LifetimeCounters || jitter<0 || jitter>2)return null;
            int frame=(counter/2+jitter-1)%pattern.FrameCount;
            // Both native sheets start at X=0: signed remainder -1 is wholly outside the source.
            return frame<0?null:pattern.Frame(frame*2);
        }
    }
}
