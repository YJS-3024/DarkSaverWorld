using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ReceivePlayerIdentity(PlayerIdentityNotice notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);var current=world.Record.identity;
            if(notice==null||!notice.approved||notice.character!=worldCharacter||notice.session!=world.CommanderSession
                ||!Guid.TryParseExact(notice.id,"N",out _)||notice.id==current?.id||notice.expectedRevision!=(current?.revision??0)||notice.expectedRevision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            if(world.Moving||world.PendingPortal!=null||world.PresentationLocked||world.Combat!=null||world.Map(world.Record.map).training)return WorldResult(WorldFailure.Busy);
            if(!notice.age.HasValue&&!notice.gender.HasValue&&notice.spouse==null)return WorldResult(WorldFailure.RulesUnavailable);
            var next=world.Record.Copy();next.identity=new PlayerIdentityRecord{id=notice.id,revision=notice.expectedRevision+1,age=notice.age??current?.age,gender=notice.gender??current?.gender,spouse=notice.spouse??current?.spouse};
            if(!PlayerIdentityRecord.Valid(next.identity))return WorldResult(WorldFailure.InvalidInput);
            f=CommitWorld(next,world.Record);if(f==WorldFailure.None)world.Record=next;return WorldResult(f);
        }
    }
}
