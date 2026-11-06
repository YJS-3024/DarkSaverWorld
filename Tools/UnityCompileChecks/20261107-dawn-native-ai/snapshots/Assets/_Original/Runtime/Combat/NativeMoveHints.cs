using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed class NativeMoveHint
    {
        public TutorialTile[] points;
        public int axisBefore,axisAfter;
    }
    public static class OriginalNativeMoveHints
    {
        // Original DD30 emits destination and up to four reverse cardinal neighbours,
        // not a path length or the complete displayed/diagonally-compressed route.
        public static NativeMoveHint Calculate(TutorialMap map,OriginalCombatCatalog catalog,TrainingUnit[] units,int source,int x,int y,int axis)
        {
            if(map==null||units==null||units.Any(u=>u==null)||axis<0||axis>1||map.width>200||map.height>200
                ||x<=0||y<=0||x>=200||y>=200||!map.Contains(new TutorialTile(x,y)))return null;
            var actor=units.FirstOrDefault(u=>u.id==source);int? kind=OriginalMemberCommands.MovementType(actor,catalog);
            if(actor==null||actor.nativeActive==false||!actor.movementBudget.HasValue||actor.movementBudget<0||actor.movementBudget>100000||!kind.HasValue||!OriginalRegroupMovement.ValidMovementType(kind))return null;
            var occupied=units.Where(OriginalCommandRange.OccupiesTile).GroupBy(u=>u.Tile).ToDictionary(g=>g.Key,g=>g.FirstOrDefault(u=>u.id/50!=source/50)?.id??g.First().id);
            var scores=OriginalRegroupMovement.Reachable(map,actor,actor.movementBudget.Value+7,kind.Value,occupied,new HashSet<TutorialTile>());
            if(scores[y*map.width+x]<0)return null;
            int phase=axis;var result=new List<TutorialTile>{new TutorialTile(x,y)};
            int Score(int tx,int ty)=>tx>=0&&ty>=0&&tx<map.width&&ty<map.height?scores[ty*map.width+tx]:-1;
            while(result.Count<5)
            {
                int current=Score(x,y),left=Score(x-1,y),right=Score(x+1,y),up=Score(x,y-1),down=Score(x,y+1);
                int maximum=Math.Max(current,Math.Max(Math.Max(left,right),Math.Max(up,down)));scores[y*map.width+x]=-64;
                int direction=-1;int[] values={left,right,up,down};int[] order=phase==0?new[]{0,1,2,3}:new[]{2,3,0,1};
                foreach(int d in order)if(values[d]==maximum){direction=d;break;}
                if(direction<0)
                {
                    if(maximum!=current||x!=actor.x||y!=actor.y)return null;
                    while(result.Count<5)result.Add(new TutorialTile(-1,-1));
                    return new NativeMoveHint{points=result.ToArray(),axisBefore=axis,axisAfter=phase};
                }
                if(direction==0)x--;else if(direction==1)x++;else if(direction==2)y--;else y++;
                result.Add(new TutorialTile(x,y));
                if(result.Count==5)return new NativeMoveHint{points=result.ToArray(),axisBefore=axis,axisAfter=phase};
                phase=phase==0?1:0;
                if(x<0||y<0||x>=map.width||y>=map.height||current<0)return null;
            }
            return null;
        }
        public static byte[] Packet(int source,TutorialTile[] points)
        {
            if(source<0||source>=3000||points==null||points.Length!=5)return null;
            bool ended=false;
            for(int i=0;i<5;i++)
            {
                var p=points[i];if(p.X==-1&&p.Y==-1){if(i==0)return null;ended=true;continue;}
                if(ended||p.X<0||p.Y<0||p.X>=200||p.Y>=200)return null;
            }
            var bytes=new byte[24];bytes[0]=0x5c;bytes[1]=0x43;bytes[2]=(byte)source;bytes[3]=(byte)(source>>8);
            for(int i=0;i<5;i++){int at=4+i*4;bytes[at]=(byte)points[i].X;bytes[at+1]=(byte)(points[i].X>>8);bytes[at+2]=(byte)points[i].Y;bytes[at+3]=(byte)(points[i].Y>>8);}return bytes;
        }
        public static bool TryDecode(byte[] bytes,out int source,out TutorialTile[] points)
        {
            source=-1;points=null;if(bytes==null||bytes.Length!=24||bytes[0]!=0x5c||bytes[1]!=0x43)return false;
            int id=bytes[2]|bytes[3]<<8;var p=new TutorialTile[5];
            for(int i=0;i<5;i++){int at=4+i*4;p[i]=new TutorialTile((short)(bytes[at]|bytes[at+1]<<8),(short)(bytes[at+2]|bytes[at+3]<<8));}
            if(Packet(id,p)==null)return false;source=id;points=p;return true;
        }
    }
}
