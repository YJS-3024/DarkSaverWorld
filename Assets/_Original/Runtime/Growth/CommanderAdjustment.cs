using System;
namespace DarkSaver.Original.World
{
    // Typed ownership context, not an inferred native class/stat/magic wire layout.
    public sealed class CommanderAdjustmentTarget
    {
        public int index;public string identity;public long rosterRevision;
        public CommanderAdjustmentTarget Copy()=>(CommanderAdjustmentTarget)MemberwiseClone();
        public static bool Same(CommanderAdjustmentTarget a,CommanderAdjustmentTarget b)=>a==null?b==null:b!=null&&a.index==b.index&&a.identity==b.identity&&a.rosterRevision==b.rosterRevision;
    }
    internal sealed partial class WorldSession
    {
        public CommanderAdjustmentTarget AdjustmentTarget;
        public bool AdjustmentTargetMatches=>AdjustmentTarget==null||Record.roster!=null&&AdjustmentTarget.rosterRevision==Record.roster.revision
            &&AdjustmentTarget.index>0&&AdjustmentTarget.index<Record.roster.rows.Length&&Record.roster.rows[AdjustmentTarget.index].id==AdjustmentTarget.identity;
        public CommanderProfile AdjustmentProfile=>AdjustmentTarget!=null&&AdjustmentTargetMatches?Record.roster.rows[AdjustmentTarget.index].profile:null;
        public WorldRecord AdjustmentRecord
        {
            get
            {
                if(AdjustmentTarget==null)return Record;
                if(AdjustmentProfile==null)return null;
                var r=Record.Copy();AdjustmentProfile.Apply(r);r.roster=null;return r;
            }
        }
        public int AdjustmentJob=>AdjustmentTarget==null?CommanderJob:AdjustmentTargetMatches?Record.roster.rows[AdjustmentTarget.index].job:-1;
    }
}
