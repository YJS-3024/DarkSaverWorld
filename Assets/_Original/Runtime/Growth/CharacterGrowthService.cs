using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply GetCharacterGrowth()
        {var f=ReadyWorld();return WorldResult(f);}
        private WorldFailure CommitGrowth(WorldRecord next,WorldRecord expectedState=null)
        {
            var failure=CommitWorld(next,expectedState);if(failure!=WorldFailure.None)return failure;
            world.Record=next;
            // Warehouse drafts own item moves only. A later confirmation must not undo received growth/health.
            if(world.Draft!=null)
            {
                world.Draft.growth=next.growth?.Copy();world.Draft.level=next.level;world.Draft.healthKnown=next.healthKnown;
                world.Draft.hp=next.hp;world.Draft.maxHp=next.maxHp;world.Draft.mp=next.mp;world.Draft.maxMp=next.maxMp;
            }
            return WorldFailure.None;
        }
        private WorldFailure GrowthNotice(string character,string session,string id,bool approved,long expectedRevision)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(!approved||character!=worldCharacter||session!=world.GrowthSession||!Guid.TryParseExact(id,"N",out _)
                ||expectedRevision!=(world.Record.growth?.revision??0)||expectedRevision==long.MaxValue
                ||world.Record.growth?.lastNotice==id)return WorldFailure.StaleRequest;
            if(world.Moving||world.PendingPortal!=null||world.PhaseSavePending||world.Combat?.Pending!=null)return WorldFailure.Busy;
            return WorldFailure.None;
        }
        public WorldReply ReceiveCharacterExperience(CharacterExperienceNotice notice)
        {
            if(notice==null)return WorldResult(WorldFailure.InvalidInput);
            var f=GrowthNotice(notice.character,notice.session,notice.id,notice.approved,notice.expectedRevision);if(f!=WorldFailure.None)return WorldResult(f);
            if(world.Record.growth==null)return WorldResult(WorldFailure.RulesUnavailable);
            var next=world.Record.Copy();next.growth.experience=notice.experience;next.growth.revision++;next.growth.lastNotice=notice.id;
            // The native client consumes an absolute XP value. No inferred XP threshold, free healing or auto promotion.
            return WorldResult(CommitGrowth(next));
        }
        public WorldReply ReceiveCharacterGrowth(CharacterGrowthNotice notice)
        {
            if(notice==null||notice.state==null)return WorldResult(WorldFailure.InvalidInput);
            var f=GrowthNotice(notice.character,notice.session,notice.id,notice.approved,notice.expectedRevision);if(f!=WorldFailure.None)return WorldResult(f);
            if(world.Definition.growthCatalog==null)return WorldResult(WorldFailure.RulesUnavailable);
            if(world.Combat!=null)return WorldResult(WorldFailure.Busy);
            var state=notice.state;
            if(!CharacterGrowthRecord.Valid(state)||state.revision!=notice.expectedRevision+1||state.lastNotice!=notice.id
                ||!world.Definition.growthCatalog.Accepts(state)||!Enum.IsDefined(typeof(CharacterGrowthOperation),notice.operation)
                ||notice.health!=null&&(!notice.health.Valid||world.Record.ghost&&notice.health.hp!=0))return WorldResult(WorldFailure.InvalidInput);
            var previous=world.Record.growth;
            if(notice.operation==CharacterGrowthOperation.LevelChanged&&(previous==null||state.job!=previous.job||state.totalLevel<=previous.totalLevel||state.classLevel<=previous.classLevel)
                ||notice.operation==CharacterGrowthOperation.ClassChanged&&(previous==null||state.job==previous.job))return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();next.growth=state.Copy();next.level=state.totalLevel;
            if(notice.health!=null)
            {next.healthKnown=true;next.hp=notice.health.hp;next.maxHp=notice.health.maxHp;next.mp=notice.health.mp;next.maxMp=notice.health.maxMp;}
            return WorldResult(CommitGrowth(next));
        }
    }
}
