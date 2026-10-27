using System;
using System.Collections.Generic;
using DarkSaver.Core;
namespace DarkSaver.Events
{
    public enum TutorialRangeMode { None, Move, Attack, Magic }
    public readonly struct TutorialRangeCell
    {
        public TutorialRangeCell(int x,int y,bool target){X=x;Y=y;Target=target;}
        public int X {get;} public int Y {get;} public bool Target {get;}
    }
    // Display only. Never advances the script, spends AP, moves a unit or resolves damage.
    public static class OriginalTutorialRanges
    {
        // UserUnit::dmSetChar's temporary record -> character +0x3c/+0x30.
        // These tutorial defaults are not class movement stats or general server rules.
        public const int MovementBudget=12, AttackRadius=1, TroopColumn=0;
        public static bool TryBuild(NativeTutorialProgress state,int actorId,TutorialRangeMode mode,int width,int height,
            IEnumerable<(int x,int y)> suppliedArea,int magicRange,Func<TutorialActor,bool> targetAllowed,
            out TutorialRangeCell[] cells)
        {
            cells=Array.Empty<TutorialRangeCell>();
            if(state==null || !state.IsValid || width<=0 || height<=0 || width>200 || height>200)return false;
            if(mode==TutorialRangeMode.None)return true;
            if(mode!=TutorialRangeMode.Move && mode!=TutorialRangeMode.Attack && mode!=TutorialRangeMode.Magic)return false;
            var actor=Array.Find(state.actors,a=>a.id==actorId);
            if(actor==null || actor.dead || actor.removed || actor.x>=width || actor.y>=height)return false;
            var area=new HashSet<(int,int)>();
            if(mode==TutorialRangeMode.Magic)
            {
                if(magicRange<0 || magicRange>short.MaxValue)return false;
                // GameCtl positive magic.Range -> UserUnit::SetArea, a Manhattan diamond.
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                    if(Math.Abs(x-actor.x)+Math.Abs(y-actor.y)<=magicRange)area.Add((x,y));
            }
            else
            {
                if(suppliedArea==null)return false;
                foreach(var p in suppliedArea)
                {if(p.x<0 || p.y<0 || p.x>=width || p.y>=height)return false;area.Add(p);}
            }
            if(mode!=TutorialRangeMode.Move && targetAllowed==null)return false;
            var targets=new HashSet<(int,int)>();
            if(mode!=TutorialRangeMode.Move)
                foreach(var unit in state.actors)
                    if(!unit.dead && !unit.removed && unit.id!=actorId && targetAllowed(unit))targets.Add((unit.x,unit.y));
            var result=new List<TutorialRangeCell>();
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                if(area.Contains((x,y)))result.Add(new TutorialRangeCell(x,y,targets.Contains((x,y))));
            cells=result.ToArray();return true;
        }
    }
}
