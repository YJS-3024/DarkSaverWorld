using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply GetCharacterGrowth()
        {var f=ReadyWorld();return WorldResult(f);}
        private WorldFailure CommitGrowth(WorldRecord next,WorldRecord expectedState=null)
        {
            bool changed=next.growth?.revision!=world.Record.growth?.revision;
            if(next.growth!=null&&next.scenarioBattle!=null)
            {
                next.scenarioBattle=next.scenarioBattle.Copy();var saved=next.scenarioBattle.units?.FirstOrDefault(u=>u.id==0);
                if(saved!=null){saved.nativeExperience=next.growth.experience<=uint.MaxValue?(uint?)next.growth.experience:null;if(changed)SyncNativeGrowthXp(saved,next.growth.experience);}
            }
            var failure=CommitWorld(next,expectedState);if(failure!=WorldFailure.None)return failure;
            world.Record=next;
            var commander=world.Combat?.Units.FirstOrDefault(u=>u.id==0);
            if(commander!=null&&next.growth!=null){commander.nativeExperience=next.growth.experience<=uint.MaxValue?(uint?)next.growth.experience:null;if(changed)SyncNativeGrowthXp(commander,next.growth.experience);}
            // Warehouse drafts own item moves only. A later confirmation must not undo received growth/health.
            if(world.Draft!=null)
            {
                world.Draft.growth=next.growth?.Copy();world.Draft.level=next.level;world.Draft.healthKnown=next.healthKnown;
                world.Draft.hp=next.hp;world.Draft.maxHp=next.maxHp;world.Draft.mp=next.mp;world.Draft.maxMp=next.maxMp;
            }
            return WorldFailure.None;
        }
        private static void SyncNativeGrowthXp(TrainingUnit unit,ulong experience)
        {if(unit.nativeInfo==null)return;if(experience<=uint.MaxValue)unit.nativeInfo.ExperienceSet((uint)experience);else unit.nativeInfo=null;}
        private WorldFailure GrowthNotice(string character,string session,string id,bool approved,long expectedRevision,bool battleMotion=false)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(!approved||character!=worldCharacter||session!=world.GrowthSession||!Guid.TryParseExact(id,"N",out _)
                ||expectedRevision!=(world.Record.growth?.revision??0)||expectedRevision==long.MaxValue
                ||world.Record.growth?.lastNotice==id)return WorldFailure.StaleRequest;
            if(world.Moving&&(!battleMotion||world.Combat==null)||world.PendingPortal!=null||world.PhaseSavePending||world.Combat?.Pending!=null)return WorldFailure.Busy;
            return WorldFailure.None;
        }
        public WorldReply ReceiveCharacterExperience(CharacterExperienceNotice notice)
        {
            if(notice==null)return WorldResult(WorldFailure.InvalidInput);
            var f=GrowthNotice(notice.character,notice.session,notice.id,notice.approved,notice.expectedRevision,true);if(f!=WorldFailure.None)return WorldResult(f);
            if(world.Combat!=null){f=ReadyBattle(true);if(f!=WorldFailure.None)return WorldResult(f);}
            if(world.Record.growth==null)return WorldResult(WorldFailure.RulesUnavailable);
            var next=world.Record.Copy();next.growth.experience=notice.experience;next.growth.revision++;next.growth.lastNotice=notice.id;
            // The native client consumes an absolute XP value. No inferred XP threshold, free healing or auto promotion.
            return WorldResult(CommitGrowth(next));
        }
        public WorldReply ReceiveCharacterGrowth(CharacterGrowthNotice notice)
        {
            if(notice==null||notice.state==null)return WorldResult(WorldFailure.InvalidInput);
            var f=GrowthNotice(notice.character,notice.session,notice.id,notice.approved,notice.expectedRevision,notice.operation==CharacterGrowthOperation.LevelChanged);if(f!=WorldFailure.None)return WorldResult(f);
            if(world.Definition.growthCatalog==null)return WorldResult(WorldFailure.RulesUnavailable);
            var state=notice.state;
            if(!CharacterGrowthRecord.Valid(state)||state.revision!=notice.expectedRevision+1||state.lastNotice!=notice.id
                ||!world.Definition.growthCatalog.Accepts(state)||!Enum.IsDefined(typeof(CharacterGrowthOperation),notice.operation)
                ||notice.health!=null&&(!notice.health.Valid||world.Record.ghost&&notice.health.hp!=0))return WorldResult(WorldFailure.InvalidInput);
            var previous=world.Record.growth;
            if(notice.operation==CharacterGrowthOperation.LevelChanged&&(previous==null||state.job!=previous.job||state.totalLevel<=previous.totalLevel||state.classLevel<=previous.classLevel)
                ||notice.operation==CharacterGrowthOperation.ClassChanged&&(previous==null||state.job==previous.job))return WorldResult(WorldFailure.InvalidInput);
            if(world.Combat!=null)
            {
                // Level results can arrive during combat; class changes retain the manual, out-of-battle path.
                if(notice.operation!=CharacterGrowthOperation.LevelChanged)return WorldResult(WorldFailure.Busy);
                var units=world.Combat.Units.Select(u=>u.Copy()).ToArray();var commander=units.First(u=>u.id==0);
                if(commander.hp==0&&notice.health?.hp>0)return WorldResult(WorldFailure.InvalidInput);
                if(state.stats!=null)commander.effectiveStats=state.stats.Copy();
                if(commander.nativeInfo!=null){commander.nativeInfo.fields[3]=state.totalLevel;if(state.nextExperience.HasValue)commander.nativeInfo.fields[5]=unchecked((int)state.nextExperience.Value);}
                if(notice.health!=null){commander.hp=notice.health.hp;commander.maxHp=notice.health.maxHp;commander.mp=notice.health.mp;commander.maxMp=notice.health.maxMp;}
                if(notice.health!=null&&commander.nativeInfo!=null){var healthFields=commander.nativeInfo.fields;healthFields[7]=commander.maxHp;healthFields[8]=commander.hp;healthFields[9]=commander.maxMp;healthFields[10]=commander.mp;}
                return ReceiveBattleUnits(new TrainingUnitsNotice {approved=true,id=notice.id,character=notice.character,session=world.CampaignSession,
                    expectedRevision=world.Combat.Revision,expectedGrowthRevision=notice.expectedRevision,experience=state.experience,growthState=state.Copy(),units=units});
            }
            var next=world.Record.Copy();next.growth=state.Copy();next.level=state.totalLevel;
            if(notice.health!=null)
            {next.healthKnown=true;next.hp=notice.health.hp;next.maxHp=notice.health.maxHp;next.mp=notice.health.mp;next.maxMp=notice.health.maxMp;}
            return WorldResult(CommitGrowth(next));
        }
        private static bool SameGrowthStats(CharacterGrowthStats a,CharacterGrowthStats b)
            =>a!=null&&b!=null&&a.attack==b.attack&&a.defense==b.defense&&a.magicAttack==b.magicAttack&&a.magicDefense==b.magicDefense
                &&(a.nativeValues==null?b.nativeValues==null:b.nativeValues!=null&&a.nativeValues.SequenceEqual(b.nativeValues));
    }
}
