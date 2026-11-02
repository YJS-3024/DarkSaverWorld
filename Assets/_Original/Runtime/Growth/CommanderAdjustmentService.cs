using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply SelectAdjustmentCommander(int index,string identity,long revision)
        {
            var f=CommanderReady();if(f!=WorldFailure.None)return WorldResult(f);
            var roster=world.Record.roster;
            if(index<0||index>=(roster?.rows.Length??1)||revision!=(roster?.revision??0)
                ||identity!=(roster==null?worldCharacter:roster.rows[index].id))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingCommander!=null||world.PendingItemUse!=null||world.PendingClassOptions!=null||world.PendingClassChange!=null||world.PendingStatAdjustment!=null
                ||world.StatDraft?.Dirty==true||world.PendingMagicLoadout!=null||world.MagicDraft?.Dirty==true)return WorldResult(WorldFailure.Busy);
            if(index>0&&(roster.rows[index].profile?.growth==null||roster.rows[index].profile.inventory==null))return WorldResult(WorldFailure.RulesUnavailable);
            var target=index==0?null:new CommanderAdjustmentTarget{index=index,identity=identity,rosterRevision=revision};
            if(CommanderAdjustmentTarget.Same(target,world.AdjustmentTarget))return WorldResult(WorldFailure.None);
            world.ClearClassChange();world.ClearStatAdjustment();world.ClearMagicLoadout();world.GrowthSession=System.Guid.NewGuid().ToString("N");world.AdjustmentTarget=target;
            return WorldResult(WorldFailure.None);
        }
        private WorldFailure CommitAdjustment(WorldRecord projected,WorldRecord baseline,string notice)
        {
            if(!world.AdjustmentTargetMatches)return WorldFailure.StaleRequest;
            if(world.AdjustmentTarget==null)
            {if(projected.roster!=null)projected.roster.rows[0].job=projected.growth.job;var f=CommitWorld(projected,baseline);if(f==WorldFailure.None)world.Record=projected;return f;}
            if(!TempleRevivalOffer.SameState(world.AdjustmentRecord,baseline))return WorldFailure.StaleRequest;
            if(world.Record.roster.revision==long.MaxValue)return WorldFailure.StaleRequest;
            var next=world.Record.Copy();var target=world.AdjustmentTarget;var row=next.roster.rows[target.index];
            row.profile=CommanderProfile.Capture(projected);row.job=projected.growth.job;next.roster.revision++;next.roster.lastNotice=notice;
            var result=CommitWorld(next,world.Record);if(result!=WorldFailure.None)return result;
            world.Record=next;target.rosterRevision=next.roster.revision;return WorldFailure.None;
        }
    }
}
