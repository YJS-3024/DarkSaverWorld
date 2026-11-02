using System;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed class BattleMagicTarget
    {
        public int id,x,y;public long life;public bool wasDead;
        public BattleMagicTarget Copy()=>(BattleMagicTarget)MemberwiseClone();
    }
    public static class BattleMagicArea
    {
        // userunit SetArea: positive weights only; never use this weight as damage.
        public static long Weight(int centerX,int centerY,int x,int y,int area)=>area<0?0:Math.Max(0,(long)area+1-Math.Abs((long)x-centerX)-Math.Abs((long)y-centerY));
        public static BattleMagicTarget[] Targets(TrainingUnit[] units,int[] permitted,int centerX,int centerY,int area,bool allowDead=false)
            =>units.Where(u=>(u.hp>0||allowDead)&&permitted.Contains(u.id)&&Weight(centerX,centerY,u.x,u.y,area)>0)
                .OrderBy(u=>u.x).ThenBy(u=>u.y).Select(Capture).ToArray();
        // CheckMagicTarget Unit8: truncate ID/10, then slots0..8 in ID order, never slot9.
        public static BattleMagicTarget[] GroupTargets(TrainingUnit[] units,int[] permitted,int selected,bool allowDead=false)
            =>selected<0?Array.Empty<BattleMagicTarget>():units.Where(u=>u.id>=selected/10*10&&u.id-selected/10*10<9&&(u.hp>0||allowDead)&&permitted.Contains(u.id))
                .OrderBy(u=>u.id).Select(Capture).ToArray();
        public static BattleMagicTarget Capture(TrainingUnit unit)=>new BattleMagicTarget{id=unit.id,life=unit.life,x=unit.x,y=unit.y,wasDead=unit.hp==0};
    }
}
