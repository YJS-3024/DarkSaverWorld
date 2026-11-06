using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private WorldFailure CommanderReady()
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return f;
            if(world.Map(world.Record.map).training||world.Definition.campaign?.Map(world.Record.map)?.kind=="scenario"||world.Combat!=null)return WorldFailure.InvalidStage;
            return world.Moving||world.PendingPortal!=null||world.Kind!=FacilityKind.None||world.PresentationLocked?WorldFailure.Busy:WorldFailure.None;
        }
        public WorldReply InspectCommander(int slot)
        {
            var f=CommanderReady();if(f!=WorldFailure.None)return WorldResult(f);
            int count=world.Record.roster?.rows.Length??1;
            if(slot<0||slot>=count)return WorldResult(WorldFailure.NotOwned);
            world.InspectedCommander=slot;return WorldResult(WorldFailure.None);
        }
        public WorldReply RequestCommanderOperation(CommanderOperation operation,int source,int target,string request)
        {
            var f=CommanderReady();if(f!=WorldFailure.None)return WorldResult(f);
            if(!Enum.IsDefined(typeof(CommanderOperation),operation)||!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            if(world.PendingCommander!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.roster?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var state=world.CommanderView();
            if(operation!=CommanderOperation.State)
            {
                if(state==null||source<0||source>=state.rows.Length)return WorldResult(WorldFailure.NotOwned);
                if(operation==CommanderOperation.ToggleDeployment&&(source==0||!state.rows[source].nativeRecord.HasValue||!state.rows[source].deployment.HasValue))return WorldResult(WorldFailure.RulesUnavailable);
                if(operation==CommanderOperation.Swap&&(source==target||target<0||target>=state.rows.Length))return WorldResult(WorldFailure.InvalidInput);
                if(operation==CommanderOperation.Swap&&(source==0||target==0))
                {
                    var primary=state.rows[source==0?target:source];
                    if(primary.profile?.inventory==null)return WorldResult(WorldFailure.RulesUnavailable);
                }
            }
            world.PendingCommander=new CommanderRequest{id=request,character=worldCharacter,session=world.CommanderSession,revision=revision,operation=operation,
                source=source,target=target,map=world.Record.map,x=world.Record.x,y=world.Record.y,baseline=CommanderProfile.Capture(world.Record).Fingerprint(),state=state};
            world.Applied.Add(request);return WorldResult(WorldFailure.None);
        }
        public WorldReply CancelCommanderOperation(string request)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(request==null||world.PendingCommander?.id!=request)return WorldResult(WorldFailure.StaleRequest);
            world.PendingCommander=null;return WorldResult(WorldFailure.None);
        }
        public WorldReply ExecuteCommanderOperation(CommanderOperation operation,int source,int target,string request,Func<CommanderRequest,WorldView,CommanderNotice> authority)
        {
            if(operation==CommanderOperation.State&&authority==null)return WorldResult(ReadyWorld()==WorldFailure.None?WorldFailure.RulesUnavailable:ReadyWorld());
            var queued=RequestCommanderOperation(operation,source,target,request);if(!queued.Accepted)return queued;
            var pending=world.PendingCommander.Copy();CommanderNotice notice;
            try
            {
                notice=authority?.Invoke(pending.Copy(),world.Snapshot());
                if(authority==null)
                {
                    // Proven native local edits only: no new ownership, HP, item or server permission rule.
                    notice=new CommanderNotice{approved=true,id=Guid.NewGuid().ToString("N"),request=pending.Copy(),state=OriginalCommanderPackets.Edit(pending)};
                }
            }
            catch{CancelCommanderOperation(pending.id);return WorldResult(WorldFailure.RulesUnavailable);}
            return notice==null?queued:ReceiveCommanderState(notice);
        }
        public WorldReply ReceiveCommanderState(CommanderNotice notice)
        {
            var f=CommanderReady();if(f!=WorldFailure.None)return WorldResult(f);
            var pending=world.PendingCommander;
            if(notice==null||!Guid.TryParseExact(notice.id,"N",out _)||!CommanderRequest.Same(pending,notice.request)||pending.character!=worldCharacter
                ||pending.session!=world.CommanderSession||pending.revision!=(world.Record.roster?.revision??0)||notice.id==world.Record.roster?.lastNotice)
                return WorldResult(WorldFailure.StaleRequest);
            if(!notice.approved){world.PendingCommander=null;return WorldResult(WorldFailure.InvalidInput);}
            if(pending.map!=world.Record.map||pending.x!=world.Record.x||pending.y!=world.Record.y||pending.baseline!=CommanderProfile.Capture(world.Record).Fingerprint())return WorldResult(WorldFailure.StaleRequest);
            if(notice.state?.rows==null||notice.state.rows.Length==0||notice.state.rows.Any(r=>!OwnedCommander.Valid(r)))return WorldResult(WorldFailure.InvalidInput);
            var state=notice.state.Copy();
            if(pending.operation==CommanderOperation.State){f=PreserveKnownCommanderData(state);if(f!=WorldFailure.None)return WorldResult(f);}
            if(pending.operation!=CommanderOperation.State)
            {
                var expected=OriginalCommanderPackets.Edit(pending);if(expected==null||!SameCommanderRows(expected,state))return WorldResult(WorldFailure.InvalidInput);
            }
            else if(world.Record.roster!=null&&(state.rows[0].id!=world.Record.roster.rows[0].id||state.rows[0].job!=world.CommanderJob))return WorldResult(WorldFailure.InvalidInput);
            else if(state.rows[0].job!=world.CommanderJob)return WorldResult(WorldFailure.InvalidInput);
            if(pending.operation==CommanderOperation.State&&state.rows[0].profile!=null&&!CommanderProfile.Same(state.rows[0].profile,CommanderProfile.Capture(world.Record)))return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();bool primaryChanged=pending.operation==CommanderOperation.Swap&&(pending.source==0||pending.target==0);
            if(primaryChanged)
            {
                if(state.rows[0].profile?.inventory==null)return WorldResult(WorldFailure.RulesUnavailable);
                state.rows[0].profile.Apply(next);
            }
            // Primary state has one canonical live location. Views capture it when needed.
            state.rows[0].profile=null;state.revision=pending.revision+1;state.lastNotice=notice.id;next.roster=state;
            if(!CommanderRoster.Valid(state)||state.rows.Any(r=>world.Definition.growthCatalog?.Job(r.job)==null&&world.Definition.campaign?.jobs?.Any(j=>j.job==r.job)!=true)
                ||state.rows.Any(r=>r.profile?.growth!=null&&world.Definition.growthCatalog?.Accepts(r.profile.growth)!=true)||!WorldRecord.Valid(next))return WorldResult(WorldFailure.InvalidInput);
            f=CommitWorld(next);if(f!=WorldFailure.None)return WorldResult(f);
            world.Record=next;world.PendingCommander=null;
            if(world.InspectedCommander>=state.rows.Length)world.InspectedCommander=0;
            if(primaryChanged)world.ResetCommanderContexts();return WorldResult(WorldFailure.None);
        }
        public WorldReply ReceiveCommanderRoster(CommanderRosterPush notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(notice==null||!notice.approved||notice.character!=worldCharacter||notice.session!=world.CommanderSession||notice.expectedRevision!=(world.Record.roster?.revision??0)
                ||notice.expectedRevision==long.MaxValue||!Guid.TryParseExact(notice.id,"N",out _)||notice.id==world.Record.roster?.lastNotice)return WorldResult(WorldFailure.StaleRequest);
            if(world.Moving||world.Combat?.Pending!=null)return WorldResult(WorldFailure.Busy);
            if(notice.state?.rows==null||notice.state.rows.Any(r=>!OwnedCommander.Valid(r)))return WorldResult(WorldFailure.InvalidInput);
            var state=notice.state.Copy();
            f=PreserveKnownCommanderData(state);if(f!=WorldFailure.None)return WorldResult(f);
            if(state.rows==null||state.rows.Length==0||state.rows.Any(r=>!OwnedCommander.Valid(r))||state.rows[0].job!=world.CommanderJob
                ||world.Record.roster!=null&&state.rows[0].id!=world.Record.roster.rows[0].id
                ||state.rows[0].profile!=null&&!CommanderProfile.Same(state.rows[0].profile,CommanderProfile.Capture(world.Record)))return WorldResult(WorldFailure.InvalidInput);
            state.rows[0].profile=null;state.revision=notice.expectedRevision+1;state.lastNotice=notice.id;
            if(!CommanderRoster.Valid(state)||state.rows.Any(r=>world.Definition.growthCatalog?.Job(r.job)==null&&world.Definition.campaign?.jobs?.Any(j=>j.job==r.job)!=true)
                ||state.rows.Any(r=>r.profile?.growth!=null&&world.Definition.growthCatalog?.Accepts(r.profile.growth)!=true))return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();next.roster=state;f=CommitWorld(next);if(f!=WorldFailure.None)return WorldResult(f);
            world.Record=next;world.PendingCommander=null;if(world.InspectedCommander>=state.rows.Length)world.InspectedCommander=0;
            return WorldResult(WorldFailure.None);
        }
        private WorldFailure PreserveKnownCommanderData(CommanderRoster state)
        {
            if(state?.rows==null||state.rows.Length==0)return WorldFailure.InvalidInput;
            foreach(var row in state.rows)
            {
                var previous=world.Record.roster?.rows.FirstOrDefault(r=>r.id==row.id);
                if(row.name==null)row.name=previous?.name;
                // A list-only update does not erase an already received personal record.
                if(row.profile==null&&previous?.profile!=null)
                {if(row.job!=previous.job)return WorldFailure.RulesUnavailable;row.profile=previous.profile.Copy();}
            }
            if(world.Record.roster==null&&state.rows[0].name==null)
            {var f=WorldOwner(out var owner);if(f!=WorldFailure.None)return f;state.rows[0].name=owner.character.name;}
            return WorldFailure.None;
        }
        public WorldReply EquipCommanderItem(int index,string identity,long revision,string owned,int slot,string request,int inventorySlot=-1)
        {
            var f=CommanderReady();if(f!=WorldFailure.None)return WorldResult(f);
            var roster=world.Record.roster;
            if(roster==null||index<0||index>=roster.rows.Length||roster.revision!=revision||roster.rows[index].id!=identity)return WorldResult(WorldFailure.StaleRequest);
            if(index==0)return EquipWorldItem(owned,slot,request,inventorySlot);
            if(world.PendingCommander!=null||world.PendingItemUse!=null||world.PendingClassChange!=null||world.PendingClassOptions!=null||world.PendingStatAdjustment!=null||world.StatDraft?.Dirty==true||world.PendingMagicLoadout!=null||world.MagicDraft?.Dirty==true)return WorldResult(WorldFailure.Busy);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request)||revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var row=roster.rows[index];if(row.profile?.inventory==null)return WorldResult(WorldFailure.RulesUnavailable);
            var personal=new WorldRecord();row.profile.Apply(personal);
            f=BuildEquippedRecord(personal,row.job,owned,slot,inventorySlot,out var equipped);if(f!=WorldFailure.None)return WorldResult(f);
            var next=world.Record.Copy();next.roster.rows[index].profile=CommanderProfile.Capture(equipped);next.roster.revision++;next.roster.lastNotice=request;
            if(!WorldRecord.Valid(next))return WorldResult(WorldFailure.InvalidInput);
            return ApplyWorld(next,request);
        }
        private static bool SameCommanderRows(CommanderRoster a,CommanderRoster b)=>a!=null&&b!=null&&a.alternateGate==b.alternateGate&&a.rows?.Length==b.rows?.Length
            &&a.rows.Select((r,i)=>r.id==b.rows[i].id&&r.name==b.rows[i].name&&r.job==b.rows[i].job&&r.nativeRecord==b.rows[i].nativeRecord&&r.deployment==b.rows[i].deployment&&r.nativeStatus==b.rows[i].nativeStatus&&CommanderProfile.Same(r.profile,b.rows[i].profile)).All(x=>x);
    }
}
