using System;
using System.Linq;

namespace DarkSaver.Original.World
{
    [Serializable] public sealed class MagicEffectStrip
    {
        public int x,y,width,height,stepX,stepY,count,surface,rawCount;
        public MagicEffectStrip Copy()=>(MagicEffectStrip)MemberwiseClone();
        public bool Valid=>(surface==0||surface==1)&&width>0&&height>0&&count>0&&count<=64&&Enumerable.Range(0,count).All(i=>x+(long)i*stepX>=0&&y+(long)i*stepY>=0
            &&x+(long)i*stepX+width<=999&&y+(long)i*stepY+height<=(surface==1?999:1098));
    }
    [Serializable] public sealed class MagicProjectilePattern
    {
        public int id,type,sound1,sound2;public MagicEffectStrip travel,impact,charge;
        public bool HasCharge=>new[]{2,3,4,7,8,9,13,21,22}.Contains(type);
        public bool Valid=>type==16||type==0&&(id==11||id==64||id==65)||new[]{1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,18,19,20,21,22,24}.Contains(type)&&impact?.Valid==true
            &&(!new[]{1,2,3,4,10,11,13,14,24}.Contains(type)||travel?.Valid==true)&&(!HasCharge||charge?.Valid==true);
        public MagicProjectilePattern Copy()=>new MagicProjectilePattern{id=id,type=type,sound1=sound1,sound2=sound2,travel=travel?.Copy(),impact=impact?.Copy(),charge=charge?.Copy()};
    }
    [Serializable] public sealed class MagicProjectileCatalog
    {
        public MagicProjectilePattern[] patterns;public int[] magic,areaMagic,groupMagic,skills;
        public MagicMenuEntry[] shapes;
        public MagicProjectilePattern Find(int pattern)=>patterns?.FirstOrDefault(p=>p.id==pattern);
        public bool SupportsSkill(MagicMenuEntry metadata)=>SupportsMember(metadata,skills);
        public bool SupportsEffect(MagicMenuEntry metadata)=>Supports(metadata)||SupportsSkill(metadata);
        public bool Supports(MagicMenuEntry metadata)=>SupportsMember(metadata,magic);
        private bool SupportsMember(MagicMenuEntry metadata,int[] ids)
        {
            if(metadata==null||ids?.Contains(metadata.id)!=true||Find(metadata.pattern)?.Valid!=true)return false;
            var shape=shapes?.FirstOrDefault(s=>s.id==metadata.id);
            return shape!=null&&shape.pattern==metadata.pattern&&shape.team==metadata.team&&shape.unit==metadata.unit&&shape.distance==metadata.distance&&shape.range==metadata.range;
        }
    }
    public static class OriginalBattleMagicMenu
    {
        // GameCtl early10009460 / late10009520: up,down,left,right,UL,DL,UR,DR,center.
        private static readonly int[] X={0,0,-32,32,-32,-32,32,32,0},Y={-32,32,0,0,-32,32,-32,32,0};
        public static int OffsetX(int slot)=>slot>=0&&slot<X.Length?X[slot]:throw new ArgumentOutOfRangeException(nameof(slot));
        public static int OffsetY(int slot)=>slot>=0&&slot<Y.Length?Y[slot]:throw new ArgumentOutOfRangeException(nameof(slot));
    }
}
