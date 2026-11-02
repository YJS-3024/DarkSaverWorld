using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed class NativeMovementPacket
    {
        public bool failed;
        public int unit,x,y;
    }
    public sealed class NativeMovementPath
    {
        public TutorialTile[] points;
        public int[] directions;
        public int axisAfter,cardinalSteps;
    }
    public static class OriginalNativeMovement
    {
        public static bool TryDecode(byte[] bytes,out NativeMovementPacket packet)
        {
            packet=null;if(bytes==null||bytes.Length!=8||bytes[0]!=0x5c||bytes[1]!=1&&bytes[1]!=14)return false;
            int Word(int at)=>(short)(bytes[at]|bytes[at+1]<<8);
            packet=new NativeMovementPacket{failed=bytes[1]==14,unit=Word(2),x=Word(4),y=Word(6)};return true;
        }
        public static byte[] Packet(bool failed,int unit,int x,int y)
        {
            if(unit<short.MinValue||unit>short.MaxValue||x<short.MinValue||x>short.MaxValue||y<short.MinValue||y>short.MaxValue)return null;
            return new[]{(byte)0x5c,(byte)(failed?14:1),(byte)unit,(byte)(unit>>8),(byte)x,(byte)(x>>8),(byte)y,(byte)(y>>8)};
        }
        // Native D2E0 add-head backtracking, then D740 adjacent perpendicular compression.
        // This phase is separate from the five-point outgoing hint phase10016170.
        public static NativeMovementPath Path(TutorialMap map,OriginalCombatCatalog catalog,TrainingUnit[] units,int source,TutorialTile start,TutorialTile end,int axis)
        {
            if(map==null||units==null||units.Any(u=>u==null)||axis<0||axis>1||map.width>200||map.height>200||!map.Contains(start)||!map.Contains(end))return null;
            var actor=units.FirstOrDefault(u=>u.id==source)?.Copy();int? kind=OriginalMemberCommands.MovementType(actor,catalog);
            if(actor==null||actor.nativeActive==false||!kind.HasValue||!OriginalRegroupMovement.ValidMovementType(kind)||!actor.movementBudget.HasValue||actor.movementBudget<0||actor.movementBudget>100000)return null;
            actor.x=start.X;actor.y=start.Y;
            var occupied=units.Where(u=>u.id!=source&&OriginalCommandRange.OccupiesTile(u)).GroupBy(u=>u.Tile).ToDictionary(g=>g.Key,g=>g.FirstOrDefault(u=>u.id/50!=source/50)?.id??g.First().id);
            occupied[start]=source;
            var scores=OriginalRegroupMovement.Reachable(map,actor,actor.movementBudget.Value+7,kind.Value,occupied,new HashSet<TutorialTile>());
            if(scores[end.Y*map.width+end.X]<0)return null;
            var backwards=new List<TutorialTile>();int x=end.X,y=end.Y,phase=axis,steps=0;
            int Score(int tx,int ty)=>tx>=0&&ty>=0&&tx<map.width&&ty<map.height?scores[ty*map.width+tx]:-1;
            while(steps<=map.width*map.height)
            {
                int current=Score(x,y);int[] values={Score(x-1,y),Score(x+1,y),Score(x,y-1),Score(x,y+1)};
                int maximum=Math.Max(current,values.Max());scores[y*map.width+x]=-64;
                int d=-1;foreach(int candidate in phase==0?new[]{0,1,2,3}:new[]{2,3,0,1})if(values[candidate]==maximum){d=candidate;break;}
                if(d<0)
                {
                    if(maximum!=current||x!=start.X||y!=start.Y)return null;
                    backwards.Reverse();var points=new List<TutorialTile>();var codes=new List<int>();var cursor=start;
                    for(int i=0;i<backwards.Count;i++)
                    {
                        var next=backwards[i];
                        if(i+1<backwards.Count&&Math.Abs(backwards[i+1].X-cursor.X)==1&&Math.Abs(backwards[i+1].Y-cursor.Y)==1)next=backwards[++i];
                        int dx=next.X-cursor.X,dy=next.Y-cursor.Y;
                        codes.Add(dx<0?(dy<0?5:dy>0?6:1):dx>0?(dy<0?7:dy>0?8:2):dy<0?3:4);points.Add(next);cursor=next;
                    }
                    return new NativeMovementPath{points=points.ToArray(),directions=codes.Concat(new[]{9}).ToArray(),axisAfter=phase,cardinalSteps=steps};
                }
                backwards.Add(new TutorialTile(x,y));if(d==0)x--;else if(d==1)x++;else if(d==2)y--;else y++;
                phase=phase==0?1:0;steps++;
                if(current<0||x<0||y<0||x>=map.width||y>=map.height)return null;
            }
            return null;
        }
    }
}
