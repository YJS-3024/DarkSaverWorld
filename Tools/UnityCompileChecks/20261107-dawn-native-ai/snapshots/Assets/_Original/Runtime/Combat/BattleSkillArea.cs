using System;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    // Pure native SetArea geometry, independent of authority permissions/costs/damage.
    public static class BattleSkillArea
    {
        public static bool Directional(int distance)=>distance==-1||distance==-3||distance==-4;
        public static bool ValidPoint(MagicMenuEntry m,TutorialTile caster,int x,int y,TutorialMap map)
            =>m!=null&&map!=null&&map.Contains(caster)&&(Directional(m.distance)?x>=-4&&x<=-1&&y==-1:m.distance==-2||m.distance==-5?x==caster.X&&y==caster.Y:m.distance==-6&&map.Contains(new TutorialTile(x,y)));
        public static long Weight(MagicMenuEntry m,TutorialTile caster,int x,int y,int tx,int ty)
        {
            long dx=Math.Abs((long)tx-caster.X),dy=Math.Abs((long)ty-caster.Y);
            if(m.distance==-6)
            {
                long vx=(long)x-caster.X,vy=(long)y-caster.Y,steps=Math.Max(Math.Abs(vx),Math.Abs(vy));if(steps==0)return 0;
                long ix=vx*100/steps,iy=vy*100/steps,fx=-50,fy=-50;
                for(long i=0;i<=steps;i++,fx+=ix,fy+=iy)
                    if(tx==caster.X+(fx+(ix>0?100:0))/100&&ty==caster.Y+(fy+(iy>0?100:0))/100)return 1;
                return 0;
            }
            if(Directional(m.distance))
            {
                bool horizontal=x!=-4&&x!=-3,positive=x!=-3&&x!=-2;
                long step=horizontal?(long)tx-caster.X:(long)ty-caster.Y;if(!positive)step=-step;
                long across=horizontal?(long)ty-caster.Y:(long)tx-caster.X;
                if(step<1||step>m.range+1)return 0;long remaining=m.range-step+1;
                if(m.distance==-1)return across==0?remaining:0;
                int perpendicular=horizontal?caster.Y:caster.X;
                if(across==1||across==-1&&perpendicular>1)return remaining-(m.distance==-4?1:0);
                return across==0&&m.distance==-4?remaining:0;
            }
            if(m.distance==-2)return dx==0||dy==0?Math.Max(0,m.range-dx-dy):0;
            if(m.distance==-5){long radius=(long)m.range+1,d=dx+dy;return d<radius&&(d&1)!=(radius&1)?1:0;}
            return BattleMagicArea.Weight(m.distance>0?x:caster.X,m.distance>0?y:caster.Y,tx,ty,m.range);
        }
        public static BattleMagicTarget[] Targets(MagicMenuEntry m,TrainingUnit caster,TrainingUnit[] units,int[] permitted,int x,int y,bool allowDead)
            =>units.Where(u=>(u.hp>0||allowDead)&&permitted.Contains(u.id)&&Weight(m,caster.Tile,x,y,u.x,u.y)>0).OrderBy(u=>u.x).ThenBy(u=>u.y).Select(BattleMagicArea.Capture).ToArray();
    }
}
