using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkSaver.Original.World
{
    [Serializable] public sealed class OriginalCommandRangeItem
    {
        public int item,rawCommandRange,equipType;
        public string name;
    }
    // Derived from one received snapshot. Null means unknown/unavailable, never an invented radius.
    public sealed class BattleCommandRange
    {
        public int commander,member;
        public long commanderLife,memberLife;
        public int? range;
        public long? distance;
        public bool? inside,regroup;
        public int? regroupSearchRadius;
        public BattleCommandRange Copy()=>(BattleCommandRange)MemberwiseClone();
    }
    [Flags] public enum CommandRangeEdges { None=0,Top=1,Bottom=2,Left=4,Right=8 }
    public static class OriginalCommandRange
    {
        public static int CommanderId(int unit)=>unit/10*10;
        public static long Distance(int x,int y,int otherX,int otherY)=>Math.Abs((long)x-otherX)+Math.Abs((long)y-otherY);
        public static bool Contains(int x,int y,int radius,int testX,int testY)=>Distance(x,y,testX,testY)<=radius;
        public static bool Living(TrainingUnit unit)=>unit!=null&&unit.hp>0&&!unit.ghost&&unit.nativeActive!=false&&unit.nativeEnabled!=false&&((unit.nativeState??0)&0x80)==0;
        public static bool OccupiesTile(TrainingUnit unit)
        {
            if(unit==null||unit.nativeActive==false||unit.nativeEnabled==false)return false;
            // Native mainmap is populated by +0/+4, including a dying HP0 actor until retirement.
            // Old snapshots without these flags retain their existing HP/ghost fallback.
            return unit.nativeActive==true&&unit.nativeEnabled==true||unit.hp>0||unit.ghost;
        }
        public static TrainingUnit DisplayCommander(TrainingUnit[] units,int hoveredUnit)
        {
            if(units==null||hoveredUnit<0||hoveredUnit>999||!units.Any(u=>u.id==hoveredUnit))return null;
            var leader=units.FirstOrDefault(u=>u.id==CommanderId(hoveredUnit));
            return Living(leader)&&leader.commandRange>0?leader:null;
        }
        public static BattleCommandRange[] Evaluate(TrainingUnit[] units)
        {
            if(units==null)return Array.Empty<BattleCommandRange>();
            var result=new List<BattleCommandRange>();
            foreach(var unit in units.Where(u=>u.id>=0&&u.id<=999&&u.id%10!=0).OrderBy(u=>u.id))
            {
                var leader=units.FirstOrDefault(u=>u.id==CommanderId(unit.id));
                var state=new BattleCommandRange{commander=CommanderId(unit.id),member=unit.id,commanderLife=leader?.life??0,memberLife=unit.life,range=leader?.commandRange};
                if(Living(unit)&&Living(leader)&&leader.commandRange.HasValue)
                {
                    state.distance=Distance(leader.x,leader.y,unit.x,unit.y);
                    state.inside=state.distance<=leader.commandRange.Value;
                    // SoldierAIMove: outside -> seek a position near the leader in modes2/4/8.
                    // This is NOT a blanket attack prohibition; SoldierAIAction is called afterwards.
                    if(leader.troopOrder.HasValue)
                    {
                        state.regroup=state.inside==false&&(leader.troopOrder.Value&(2|4|8))!=0;
                        if(state.regroup==true)state.regroupSearchRadius=Math.Max(2,leader.commandRange.Value);
                    }
                }
                result.Add(state);
            }
            return result.ToArray();
        }
        // SetArea positive cells with neighbor changes. No obstacle/path or attack-range substitution.
        public static CommandRangeEdges Edges(int centerX,int centerY,int radius,int x,int y,int width,int height)
        {
            if(radius<1||x<0||y<0||x>=width||y>=height||!Contains(centerX,centerY,radius,x,y))return CommandRangeEdges.None;
            CommandRangeEdges edges=CommandRangeEdges.None;
            if(y==0||!Contains(centerX,centerY,radius,x,y-1))edges|=CommandRangeEdges.Top;
            if(y==height-1||!Contains(centerX,centerY,radius,x,y+1))edges|=CommandRangeEdges.Bottom;
            if(x==0||!Contains(centerX,centerY,radius,x-1,y))edges|=CommandRangeEdges.Left;
            if(x==width-1||!Contains(centerX,centerY,radius,x+1,y))edges|=CommandRangeEdges.Right;
            return edges;
        }
    }
}
