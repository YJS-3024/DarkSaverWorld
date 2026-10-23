using System;
using UnityEngine;

namespace DarkSaver.Combat
{
    // userunit.dll 100085F0..10008B69; only pattern14 records with positive Area draw a caster beam.
    public sealed class NativeThrustBeamEffects : ScriptableObject
    {
        [SerializeField] int[] magicIds=Array.Empty<int>(),lifetimes=Array.Empty<int>();
        [SerializeField] Sprite[] pieces=Array.Empty<Sprite>();
        [SerializeField] bool doubleArms;
        [SerializeField] NativeThrustBeamEffects sideArms;
        public int MagicCount=>magicIds.Length;
        public int PartCount=>sideArms!=null?9:doubleArms?6:3;
        public bool DoubleArms=>doubleArms;
        public void InitializePlasma(int[] ids,int[] durations,NativeThrustBeamEffects center,NativeThrustBeamEffects sides)
        {
            if(center==null || center.PartCount!=3 || sides==null || !sides.DoubleArms)throw new ArgumentException("Missing plasma beam components.");
            Initialize(ids,durations,center.pieces);sideArms=sides;
        }
        public void Initialize(int[] ids,int[] durations,Sprite[] source,bool useDoubleArms=false)
        {
            if(ids==null || durations==null || ids.Length==0 || ids.Length!=durations.Length
                || Array.Exists(durations,x=>x<=0 || x>1000) || source==null || source.Length!=6 || Array.Exists(source,x=>x==null))
                throw new ArgumentException("Invalid thrust beam data.");
            magicIds=(int[])ids.Clone();lifetimes=(int[])durations.Clone();pieces=(Sprite[])source.Clone();doubleArms=useDoubleArms;sideArms=null;
        }
        public bool TryGetLifetime(int id,out int lifetime)
        {int index=Array.IndexOf(magicIds,id);lifetime=index<0?0:lifetimes[index];return index>=0;}
        // Negative codes follow PGSMAGIC.X. The native default (including nonnegative X) is right.
        public bool TryGetPart(int id,int counter,int direction,int part,out Sprite sprite,out Vector2 offset,out Vector2 size)
        {
            sprite=null;offset=size=default;
            if(!TryGetLifetime(id,out int life) || counter<0 || counter>=life || part<0 || part>=PartCount)return false;
            if(sideArms!=null && part>=3)return sideArms.GetGeometry(counter,direction,part-3,out sprite,out offset,out size);
            return GetGeometry(counter,direction,part,out sprite,out offset,out size);
        }
        bool GetGeometry(int counter,int direction,int part,out Sprite sprite,out Vector2 offset,out Vector2 size)
        {
            sprite=null;offset=size=default;
            // Native double-arm right/default uses inverted source rectangles; DManage clips them to empty.
            if(doubleArms && direction!=-2 && direction!=-3 && direction!=-4)return false;
            int arm=part/3;part%=3;
            int strips=17*(counter/5+1),length=strips*2;
            bool vertical=direction==-3 || direction==-4;
            int index;
            if(direction==-2)
            {
                index=part==0?2:part==1?1:0;
                offset=new Vector2(part==0?-7:part==1?-14-length*.5f:-11-length,0);
            }
            else if(direction==-3)
            {
                index=part==0?5:part==1?4:3;
                offset=new Vector2(0,part==0?11:part==1?14+length*.5f:11+length);
            }
            else if(direction==-4)
            {
                index=part==0?3:part==1?4:5;
                offset=new Vector2(0,part==0?-10:part==1?-15-length*.5f:-20-length);
            }
            else
            {
                index=part==0?0:part==1?1:2;
                offset=new Vector2(part==0?10:part==1?15+length*.5f:24+length,0);
            }
            sprite=pieces[index];size=sprite.rect.size;
            if(part==1)size=vertical?new Vector2(size.x,length):new Vector2(length,size.y);
            if(doubleArms)offset+=vertical?new Vector2(arm==0?-32:32,0):new Vector2(0,arm==0?-32:32);
            return true;
        }
    }
}
