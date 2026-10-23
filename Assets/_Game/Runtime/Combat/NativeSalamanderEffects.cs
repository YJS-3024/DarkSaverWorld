using System;
using UnityEngine;

namespace DarkSaver.Combat
{
    // userunit.dll 100081D0..100085EF. Coordinates below use native screen Y (down).
    public sealed class NativeSalamanderEffects : ScriptableObject
    {
        [SerializeField] int[] magicIds = Array.Empty<int>(), lifetimes = Array.Empty<int>();
        [SerializeField] Sprite[] flames = Array.Empty<Sprite>(), heads = Array.Empty<Sprite>();
        public int MagicCount => magicIds.Length;
        public void Initialize(int[] ids, int[] durations, Sprite[] trail, Sprite[] body)
        {
            if(ids==null || durations==null || ids.Length==0 || ids.Length!=durations.Length
                || Array.Exists(durations,x=>x<=0 || x>1000) || trail==null || trail.Length!=3
                || body==null || body.Length!=6 || Array.Exists(trail,x=>x==null) || Array.Exists(body,x=>x==null))
                throw new ArgumentException("Invalid salamander visual data.");
            magicIds=(int[])ids.Clone();lifetimes=(int[])durations.Clone();
            flames=(Sprite[])trail.Clone();heads=(Sprite[])body.Clone();
        }
        public bool TryGetLifetime(int id,out int counters)
        {
            int index=Array.IndexOf(magicIds,id);counters=index<0?0:lifetimes[index];return index>=0;
        }
        public bool TryGetPiece(int id,int counter,Vector2Int nativeDelta,int slot,out Sprite sprite,out Vector2 pixels)
        {
            sprite=null;pixels=default;
            if(!TryGetLifetime(id,out int life) || counter<0 || counter>=life || slot<0 || slot>4)return false;
            int head=counter+2,index;
            if(slot==4)
            {
                // Original prioritizes X; a purely downward vector shares the left-facing pair.
                int direction=nativeDelta.x>0?0:nativeDelta.x==0 && nativeDelta.y<0?2:1;
                sprite=heads[direction*2+head%2];index=head;
            }
            else
            {
                index=Math.Max(0,head-3)+slot;if(index>head)return false;
                sprite=flames[index%3];
            }
            // IDIV truncates the step before multiplying by the trail index.
            int x=nativeDelta.x/life*index,y=nativeDelta.y/life*index;
            float w=sprite.rect.width,h=sprite.rect.height;
            pixels=new Vector2(x+w*.5f-(int)(w+1)/2,-y-h*.5f+(int)(h+1)/2);
            return true;
        }
    }
}
