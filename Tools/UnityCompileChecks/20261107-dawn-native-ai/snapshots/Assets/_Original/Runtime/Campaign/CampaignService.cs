using System;
using System.Linq;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ContinueCampaignPresentation()
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(world.Record.scenarioVisit?.nativeEnd?.stage==NativeScenarioEndRecord.Notice)return CloseNativeScenarioEndNotice();
            if(world.NativeDialogue.Visible)
            {world.Failure=WorldFailure.None;if(world.Presentation?.Snapshot().nativeEvent==true)world.Presentation.Continue();else world.NativeDialogue.Continue();return WorldResult(world.Failure);}
            if(world.Presentation==null)return WorldResult(WorldFailure.InvalidStage);
            world.Failure=WorldFailure.None;world.Presentation.Continue();return WorldResult(world.Failure);
        }
        public CampaignCommand[] DrainCampaignEffects()
        { if (world == null) return Array.Empty<CampaignCommand>(); var result = world.Effects.ToArray(); world.Effects.Clear(); return result; }
        public WorldReply RequestCampaignClear()
        {var f=ReadyWorld();return WorldResult(f==WorldFailure.None?WorldFailure.RulesUnavailable:f);}
        public WorldReply ReceiveCampaignClear(CampaignClearNotice notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            var map=world.Definition.campaign?.Map(world.Record.map);
            if(notice==null||!notice.approved||notice.map!=world.Record.map||notice.session!=world.CampaignSession||!Guid.TryParseExact(notice.id,"N",out _)||world.CampaignNotices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            if(map?.kind!="scenario"||world.Moving||world.SceneLocked||world.Record.campaignPhase=="outro"||world.Record.campaignPhase=="complete")return WorldResult(WorldFailure.InvalidStage);
            if(world.Combat?.Pending!=null||world.Record.campaignProgress?.events.Any(e=>!e.completed)==true)return WorldResult(WorldFailure.Busy);
            var next=world.Record.Copy();next.campaignPhase="outro";next.scenarioBattle=null;if(next.scenarioVisit!=null){next.scenarioVisit.nativeEnd=null;next.scenarioVisit.nativeOutroSuffix=world.CurrentScenarioScriptSuffix;}f=CommitWorld(next);
            if(f==WorldFailure.None){world.Record=next;world.Combat=null;world.PendingRetreat=null;world.NativeDialogue.Clear();world.CampaignNotices.Add(notice.id);world.SetPresentation(world.ScenarioOutro());}
            return WorldResult(f);
        }
        public WorldReply CampaignUnitAction(int unit)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            if (world.PresentationLocked) return WorldResult(WorldFailure.Busy);
            return WorldResult(world.Definition.campaign?.Map(world.Record.map)?.units.Any(u => u.unit == unit) == true ? WorldFailure.RulesUnavailable : WorldFailure.InvalidInput);
        }
        public WorldReply EnterCampaignMap(int target)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            var map = world.Definition.campaign?.Map(target); if (map == null) return WorldResult(WorldFailure.InvalidInput);
            if (world.Moving || world.Kind != FacilityKind.None || world.PresentationLocked) return WorldResult(WorldFailure.Busy);
            var authored=world.Map(world.Record.map).portals.FirstOrDefault(p=>p.id==world.PendingPortal&&p.targetMap==target&&!string.IsNullOrEmpty(p.gateway));
            if(authored!=null)return EnterWorldPortal(authored.id,world.Map(target).training);
            var link = world.Definition.campaign.connections.FirstOrDefault(c => c.fromMap == world.Record.map && c.toMap == target
                && !string.IsNullOrWhiteSpace(c.evidence) && c.cells.Any(p => p.x == world.Record.x && p.y == world.Record.y));
            if (link == null) return WorldResult(WorldFailure.RulesUnavailable);
            var next = world.Record.Copy(); next.map = target; next.x = link.x; next.y = link.y; next.campaignPhase = map.kind == "scenario" ? "intro" : null;
            if (!world.Map(target).map.Contains(new DarkSaver.Original.Tutorial.TutorialTile(next.x, next.y))) return WorldResult(WorldFailure.InvalidInput);
            f=PrepareScenarioVisit(next);if(f!=WorldFailure.None)return WorldResult(f);
            f = CommitWorld(next); if (f == WorldFailure.None) world.Relocate(next); return WorldResult(f);
        }
        private bool ValidateCampaignMapChange(CampaignCommand command)
        {
            int expected=command.name=="CHANGESPRITE"?6:7;
            if(command.args.Length!=expected||command.args.Any(v=>!int.TryParse(v,out _))||world.Definition.campaign?.HasNativeMaps!=true){world.Failure=WorldFailure.RulesUnavailable;return false;}
            try
            {
                var steps=(world.Record.campaignProgress?.steps??Array.Empty<CampaignScriptStep>()).Concat(new[]{new CampaignScriptStep{command=command.raw,scene=command.scene}}).ToArray();
                world.Definition.campaign.ApplyChanges(command.scene,steps);return true;
            }
            catch(System.IO.InvalidDataException){world.Failure=WorldFailure.InvalidInput;return false;}
            catch(InvalidOperationException){world.Failure=WorldFailure.RulesUnavailable;return false;}
        }
        private int? ExecuteCampaignScript(CampaignCommand command)
        {
            string key=command.context+":"+command.line;
            var previous=world.Record.campaignProgress?.steps.FirstOrDefault(s=>s.key==key);
            if(previous!=null)return previous.result;
            var next=world.Record.Copy();
            var step=new CampaignScriptStep{key=key,command=command.raw,scene=command.scene,result=1,packet=OriginalCampaignScript.Packet(command)};
            NativeMagicPacket directedMagic=null;TrainingUnitsNotice directedUnits=null;
            switch(command.name)
            {
                case "FIRSTCLEAR":step.result=(next.clearedScenarios??Array.Empty<int>()).Contains(next.map)?0:1;break;
                case "SAVECLEAR":next.clearedScenarios=(next.clearedScenarios??Array.Empty<int>()).Concat(new[]{next.map}).Distinct().ToArray();break;
                case "RANDOM":step.result=OriginalCampaignScript.Random(command.Int(0),unchecked((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()));break;
                case "CHANGESPRITE":case "CHANGETILE2":
                    if(!ValidateCampaignMapChange(command))return null;break;
                case "MAGIC":case "KILLCHAR1":case "KILLCHAR2":
                    var local=PrepareDirectedCommand(next,command,out directedMagic,out directedUnits);
                    if(local!=WorldFailure.None){world.Failure=local;return null;}
                    step.packet=directedMagic==null?null:OriginalNativeMagic.Request(directedMagic);
                    if(directedMagic!=null&&step.packet==null){world.Failure=WorldFailure.InvalidInput;return null;}break;
                default:if(step.packet==null){world.Failure=WorldFailure.RulesUnavailable;return null;}break;
            }
            var progress=next.campaignProgress??new CampaignProgressRecord{visit=next.scenarioVisit?.id??"legacy:"+next.map};
            if(progress.revision==long.MaxValue||step.packet!=null&&next.campaignDelivery?.revision==long.MaxValue){world.Failure=WorldFailure.StaleRequest;return null;}
            progress.steps=progress.steps.Concat(new[]{step}).ToArray();progress.revision++;next.campaignProgress=progress;
            if(step.packet!=null)
            {
                var delivery=next.campaignDelivery??new CampaignDeliveryRecord();
                delivery.requests=delivery.requests.Where(d=>!d.acknowledged||d.visit==progress.visit).Concat(new[]{new CampaignDelivery{id=progress.visit+":"+key,visit=progress.visit,map=next.map,command=step.command,packet=(byte[])step.packet.Clone()}}).ToArray();
                delivery.revision++;next.campaignDelivery=delivery;
            }
            var failure=CommitWorld(next);if(failure!=WorldFailure.None){world.Failure=failure;return null;}
            world.Record=next;
            if(directedUnits!=null)
            {
                var combat=world.Combat;ApplyLifecycle(combat,directedUnits);combat.Units=directedUnits.units;combat.Revision++;
                ClearPendingBattle(combat);combat.Notices.Add(directedUnits.id);
            }
            if(directedMagic!=null)ApplySelectedMagic(world.Combat,directedMagic,key);
            world.Failure=WorldFailure.None;return step.result;
        }
        private WorldFailure PrepareDirectedCommand(WorldRecord next,CampaignCommand command,out NativeMagicPacket magic,out TrainingUnitsNotice delta)
        {
            magic=null;delta=null;
            var native=next.campaignProgress?.events.FirstOrDefault(e=>e.id==world.ActiveCampaignEvent)?.native;
            if(native==null||command.context!="event:"+world.ActiveCampaignEvent||world.Combat==null)return WorldFailure.InvalidStage;
            if(command.name=="MAGIC")return SelectNativeMagic(command.Int(0),command.Int(1),command.Int(2),command.Int(3),next.campaignProgress.nativeLocalTeam,1,out magic);
            int id=command.Int(0);if(id<0)return WorldFailure.None; // Both original exports return immediately.
            var target=world.Combat.Units.FirstOrDefault(u=>u.id==id);if(target==null)return WorldFailure.RulesUnavailable;
            var kind=command.name=="KILLCHAR1"?BattleLifecycleKind.Remove:BattleLifecycleKind.Death;
            if(!OriginalBattleLifecycle.TryApply(target,kind,out var after))return WorldFailure.RulesUnavailable;
            if(world.Combat.Revision==long.MaxValue)return WorldFailure.StaleRequest;
            delta=new TrainingUnitsNotice{id=Guid.NewGuid().ToString("N"),units=world.Combat.Units.Select(u=>u.id==id?after:u.Copy()).ToArray(),
                lifecycleEvents=new[]{new BattleLifecycleEvent{kind=kind,unit=id,life=target.life}}};
            next.scenarioBattle=SaveScenarioBattle(world.Combat,delta,delta.units);return WorldFailure.None;
        }
        public WorldReply RetryCampaignPresentation()
        {var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);if(world.Presentation==null)return WorldResult(WorldFailure.InvalidStage);world.Failure=WorldFailure.None;world.Presentation.Retry();return WorldResult(world.Failure);}
        public CampaignScriptRequest[] PendingCampaignRequests()
        {
            if(ReadyWorld()!=WorldFailure.None)return Array.Empty<CampaignScriptRequest>();
            return (world.Record.campaignDelivery?.requests??Array.Empty<CampaignDelivery>()).Where(s=>!s.acknowledged)
                .Select(s=>new CampaignScriptRequest{id=s.id,character=worldCharacter,session=world.CampaignSession,visit=s.visit,
                    map=s.map,command=s.command,packet=(byte[])s.packet.Clone()}).ToArray();
        }
        public WorldReply AcknowledgeCampaignRequest(CampaignScriptRequest request)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(FindCampaignDelivery(request)==null)return WorldResult(WorldFailure.StaleRequest);
            var next=world.Record.Copy();AcknowledgeCampaignDelivery(next,request.id);
            f=CommitWorld(next);if(f==WorldFailure.None)world.Record=next;return WorldResult(f);
        }
        private CampaignDelivery FindCampaignDelivery(CampaignScriptRequest request)
        {
            var delivery=world.Record.campaignDelivery?.requests.FirstOrDefault(s=>s.id==request?.id);
            return delivery==null||delivery.acknowledged||request.character!=worldCharacter||request.session!=world.CampaignSession
                ||request.visit!=delivery.visit||request.map!=delivery.map||request.command!=delivery.command||request.packet==null
                ||!request.packet.SequenceEqual(delivery.packet)?null:delivery;
        }
        private static void AcknowledgeCampaignDelivery(WorldRecord next,string id)
        {
            next.campaignDelivery.requests.First(s=>s.id==id).acknowledged=true;
            next.campaignDelivery.requests=next.campaignDelivery.requests.Where(d=>!d.acknowledged||d.visit==next.scenarioVisit?.id).ToArray();next.campaignDelivery.revision++;
        }
        // Default in-process server policy for actual source-issued GETGENERAL.
        // Grant + durable receipt + request acknowledgement form a single commit.
        public WorldReply ProcessLocalScenarioRecruit(CampaignScriptRequest request)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(request?.packet==null||request.packet.Length!=3||request.packet[0]!=0x3c)return WorldResult(WorldFailure.InvalidInput);
            var input=request.Copy();var delivery=FindCampaignDelivery(input);
            if(delivery==null)return WorldResult(WorldFailure.StaleRequest);
            var commands=CampaignCommand.Parse(delivery.command);
            if(commands.Length!=1||commands[0].name!="GETGENERAL"||commands[0].args.Length!=1
                ||!int.TryParse(commands[0].args[0],out int job)||job<0||job>ushort.MaxValue
                ||!OriginalCampaignScript.Packet(commands[0]).SequenceEqual(input.packet)
                ||world.Definition.campaign?.Map(delivery.map)?.kind!="scenario")return WorldResult(WorldFailure.InvalidInput);
            if(world.Record.campaignDelivery.revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var next=world.Record.Copy();
            if(!(next.campaignRecruits??Array.Empty<CampaignRecruitReceipt>()).Any(r=>r.map==delivery.map&&r.job==job))
            {
                f=WorldOwner(out var owner);if(f!=WorldFailure.None)return WorldResult(f);
                var existing=world.CommanderJob==job?next.roster?.rows[0]:next.roster?.rows.Skip(1).FirstOrDefault(r=>r.job==job);
                bool initial=owner.character.job==job;
                bool primary=world.CommanderJob==job;
                string owned=existing?.id??(initial||primary?worldCharacter:null),notice=Guid.NewGuid().ToString("N");
                bool created=owned==null;
                if(created)
                {
                    var catalog=world.Definition.growthCatalog;var row=catalog?.Row(job,1);var title=catalog?.Job(job);
                    var stats=world.Definition.combatCatalog?.Job(job)?.baseStats;
                    if(title==null||row?.abilitiesResolved!=true||stats?.Valid!=true)return WorldResult(WorldFailure.RulesUnavailable);
                    var roster=next.roster??new CommanderRoster{rows=new[]{new OwnedCommander{id=worldCharacter,name=owner.character.name,job=world.CommanderJob,nativeRecord=0,deployment=1}}};
                    if(roster.rows.Length>=10)return WorldResult(WorldFailure.CommanderRosterFull);
                    if(roster.revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
                    var growth=new CharacterGrowthRecord{job=job,classLevel=1,totalLevel=1,revision=1,lastNotice=notice,
                        magic=row.magic?.Distinct().ToArray()??Array.Empty<int>(),skills=row.skills?.Distinct().ToArray()??Array.Empty<int>(),stats=stats.CombatStats(),
                        nextExperience=row.requiredExperience>=0&&row.requiredExperience<=uint.MaxValue?(uint?)row.requiredExperience:null};
                    if(!catalog.Accepts(growth))return WorldResult(WorldFailure.RulesUnavailable);
                    owned=Guid.NewGuid().ToString("N");
                    int nativeRecord=Enumerable.Range(1,65534).First(id=>roster.rows.All(r=>r.nativeRecord!=id));
                    roster.rows=roster.rows.Concat(new[]{new OwnedCommander{id=owned,name=title.name,job=job,nativeRecord=nativeRecord,deployment=2,
                        profile=new CommanderProfile{healthKnown=true,hp=stats.hp,maxHp=stats.hp,mp=Math.Max(0,stats.mp),maxMp=Math.Max(0,stats.mp),
                            growth=growth,level=1,inventory=Array.Empty<WorldOwnedItem>(),equipment=Array.Empty<WorldOwnedItem>()}}}).ToArray();
                    roster.revision++;roster.lastNotice=notice;next.roster=roster;
                }
                next.campaignRecruits=(next.campaignRecruits??Array.Empty<CampaignRecruitReceipt>()).Concat(new[]{new CampaignRecruitReceipt{
                    map=delivery.map,job=job,owned=owned,request=input.id,notice=notice,created=created}}).ToArray();
            }
            AcknowledgeCampaignDelivery(next,input.id);
            bool rosterChanged=(next.roster?.revision??0)!=(world.Record.roster?.revision??0);
            f=CommitWorld(next);if(f==WorldFailure.None){world.Record=next;if(rosterChanged)world.PendingCommander=null;}
            return WorldResult(f);
        }
        // Native15 is a synchronous broadcast with subject=-1, unlike interactive0D.
        // Keep the older typed queue API below for its explicit local contract.
        private WorldReply ReceiveNativeImmediateEvents(NativeBattleNotice notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);var input=notice?.Copy();
            if(input?.approved!=true||!OriginalCampaignScript.DecodeEvents(input.packet,out var numbers))return WorldResult(WorldFailure.InvalidInput);
            if(input.character!=worldCharacter||input.session!=world.CampaignSession||!Guid.TryParseExact(input.id,"N",out _)
                ||input.expectedRevision!=(world.Combat?.Revision??0)||world.CampaignNotices.Contains(input.id)
                ||world.Record.campaignProgress?.events.Any(e=>e.id.StartsWith(input.id+":",StringComparison.Ordinal))==true)return WorldResult(WorldFailure.StaleRequest);
            var map=world.Definition.campaign?.Map(world.Record.map);
            if(map?.kind!="scenario"||world.Record.campaignPhase!="explore"||world.Record.scenarioVisit?.nativeEnd?.Started==true
                ||world.Moving||world.Kind!=FacilityKind.None||world.PendingPortal!=null||world.Combat?.Pending!=null
                ||world.ActiveCampaignEvent!=null||world.Record.campaignProgress?.events.Any(e=>!e.completed)==true
                ||world.Presentation?.Snapshot().completed==false)return WorldResult(WorldFailure.Busy);
            if(numbers.Length==0){world.CampaignNotices.Add(input.id);return WorldResult(WorldFailure.None);}
            var bodies=OriginalCampaignScript.Events(map.events);var next=world.Record.Copy();
            var progress=next.campaignProgress??new CampaignProgressRecord{visit=next.scenarioVisit?.id??"legacy:"+next.map};
            if(progress.revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var steps=progress.steps.ToList();
            for(int i=0;i<numbers.Length;i++)
            {
                if(!bodies.TryGetValue(numbers[i],out var body))continue; // Original F710 ignores an absent event flag.
                // Defer unsupported pre-ownership operations before applying any mutation.
                if(!OriginalCampaignScript.TryImmediateChanges(body,out var changes))return WorldResult(WorldFailure.RulesUnavailable);
                foreach(var command in changes)
                {
                    int expected=command.name=="CHANGESPRITE"?6:7;
                    if(command.args.Length!=expected||command.args.Any(v=>!int.TryParse(v,out _)))return WorldResult(WorldFailure.InvalidInput);
                    if(world.Definition.campaign.HasNativeMaps!=true)return WorldResult(WorldFailure.RulesUnavailable);
                    steps.Add(new CampaignScriptStep{key="native15:"+input.id+":"+i+":"+command.line,command=command.raw,scene=next.map,result=1});
                }
            }
            progress.steps=steps.ToArray();progress.events=progress.events.Concat(numbers.Select((n,i)=>new CampaignEventRecord{id=input.id+":"+i,number=n,completed=true})).ToArray();
            progress.revision++;next.campaignProgress=progress;
            // Validate all mutations together before writing any journal or runtime geometry.
            try
            {
                if(steps.Any(s=>s.scene==next.map&&(s.command.StartsWith("CHANGESPRITE ",StringComparison.Ordinal)||s.command.StartsWith("CHANGETILE2 ",StringComparison.Ordinal))))
                {if(world.Definition.campaign.HasNativeMaps!=true)return WorldResult(WorldFailure.RulesUnavailable);world.Definition.campaign.ApplyChanges(next.map,steps.ToArray());}
            }
            catch(System.IO.InvalidDataException){return WorldResult(WorldFailure.InvalidInput);}
            catch(InvalidOperationException){return WorldResult(WorldFailure.RulesUnavailable);}
            f=CommitWorld(next);if(f==WorldFailure.None){world.Record=next;world.CampaignNotices.Add(input.id);}
            // No dialogue, timer, owner-only packet request, XP/gold/item grant or scene clear.
            return WorldResult(f);
        }
        private WorldFailure PrepareDirectedStart(WorldRecord next,out CampaignEventRecord started)
        {
            started=null;var progress=next.campaignProgress;
            if(world.ActiveCampaignEvent!=null||progress==null)return WorldFailure.None;
            var pending=progress.events.FirstOrDefault(e=>!e.completed);
            if(pending?.native==null)return WorldFailure.None;
            var bodies=OriginalCampaignScript.Events(world.Definition.campaign.Map(next.map).events);
            if(!bodies.TryGetValue(pending.number,out var body))
            {pending.completed=true;progress.nativeBlocked=true;return WorldFailure.None;}
            var native=pending.native;
            if(native.fingerprint!=OriginalCampaignScript.Fingerprint(string.Join("\n",body.Select(c=>c.raw))))return WorldFailure.RulesUnavailable;
            if(native.subject>=0)
            {
                var actor=world.Combat?.Units.FirstOrDefault(u=>u.id==native.subject);
                if(actor==null)return WorldFailure.StaleRequest;
                if(!actor.nativeState.HasValue)return WorldFailure.RulesUnavailable;
                if(!native.started)
                {
                    if(actor.nativeInfo?.Valid==true)native.team=actor.nativeInfo.fields[24];
                    else if(native.providedTeamKnown&&native.revision==world.Combat.Revision)native.team=native.providedTeam;
                    else return WorldFailure.RulesUnavailable;
                    // F710 looks up the current received record by unit ID at
                    // dequeue time. The local life guard belongs to receipt,
                    // not a fabricated lifetime pointer inside the native FIFO.
                    native.life=actor.life;
                }
            }
            else native.team=-1;
            native.started=true;progress.nativeBlocked=false;started=pending;return WorldFailure.None;
        }
        private void StartDirectedPresentation(CampaignEventRecord pending)
        {
            if(pending==null)return;
            var native=pending.native;var body=OriginalCampaignScript.Events(world.Definition.campaign.Map(world.Record.map).events)[pending.number];
            bool Moving(int id)
            {
                var actor=world.Combat?.Units.FirstOrDefault(u=>u.id==id);
                if(actor==null){world.Failure=WorldFailure.StaleRequest;return true;}
                if(!actor.nativeState.HasValue){world.Failure=WorldFailure.RulesUnavailable;return true;}
                // Movement receipts persist the completed position/state;
                // their in-flight animation remains in the combat session.
                return Movement(world.Combat,id)?.Moving==true||(actor.nativeState.Value&1)!=0;
            }
            world.SetPresentation(string.Join("\n",body.Select(c=>c.raw)),"event:"+pending.id,null,
                new CampaignNativeExecution{subject=native.subject,team=native.team,localTeam=()=>world.Record.campaignProgress.nativeLocalTeam.Value,
                    moving=Moving,resultVisible=()=>world.Record.scenarioVisit?.nativeEnd?.stage==NativeScenarioEndRecord.Result,message=ResolveDirectedMessage,dialogue=world.NativeDialogue});
            world.ActiveCampaignEvent=pending.id;
        }
        private CampaignMessage ResolveDirectedMessage(CampaignCommand command)
        {
            var failure=WorldOwner(out var owner);if(failure!=WorldFailure.None){world.Failure=failure;return null;}
            var rows=world.CommanderView()?.rows;
            string Name(int slot)=>slot==0?(rows?.FirstOrDefault()?.name??owner.character.name):slot<(rows?.Length??0)?rows[slot].name??"":"";
            int? Job(int slot)=>slot==0?world.CommanderJob:slot<(rows?.Length??0)?rows[slot].job:0;
            var proof=new NativeBattleNotice{localTeam=world.Record.campaignProgress.nativeLocalTeam,expectedTeamRevision=world.CurrentNativeTeamRevision};
            string ownName=null;
            if(world.TryNativeWinner(proof,out var bytes))ownName=bytes.Length==0?"":world.Definition.nativeText?.Invoke(bytes);
            if(OriginalCampaignMessage.TryResolve(command,Name,Job,ownName,out var result))return result;
            world.Failure=WorldFailure.RulesUnavailable;return null;
        }
        private WorldReply ReceiveNativeDirectedEvent(NativeBattleNotice notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);var input=notice?.Copy();
            if(input?.approved!=true||!OriginalDirectedEvents.TryDecode(input.packet,out int number,out int subject))return WorldResult(WorldFailure.InvalidInput);
            var progress=world.Record.campaignProgress;
            if(input.character!=worldCharacter||input.session!=world.CampaignSession||!Guid.TryParseExact(input.id,"N",out _)
                ||input.expectedRevision!=(world.Combat?.Revision??0)||progress?.nativeNotices?.Contains(input.id)==true)return WorldResult(WorldFailure.StaleRequest);
            if(world.Definition.campaign?.Map(world.Record.map)?.kind!="scenario"||world.Record.scenarioVisit==null||world.Record.campaignPhase!="explore"
                ||world.Record.scenarioVisit.nativeEnd?.Started==true||world.Kind!=FacilityKind.None||world.PendingPortal!=null
                ||progress?.events.Any(e=>!e.completed&&e.native==null)==true
                ||world.ActiveCampaignEvent==null&&world.Presentation?.Snapshot().completed==false)return WorldResult(WorldFailure.Busy);
            if(!input.localTeam.HasValue||input.localTeam<0||input.localTeam>=20)return WorldResult(WorldFailure.RulesUnavailable);
            var bodies=OriginalCampaignScript.Events(world.Definition.campaign.Map(world.Record.map).events);
            bool exists=number>=0&&bodies.TryGetValue(number,out _);
            bool append=number>=0&&(progress?.events.Count(e=>!e.completed&&e.id!=world.ActiveCampaignEvent)??0)<100;
            if(append&&exists&&!OriginalDirectedEvents.Supports(bodies[number],subject))return WorldResult(WorldFailure.RulesUnavailable);
            var actor=world.Combat?.Units.FirstOrDefault(u=>u.id==subject);
            if(append&&exists&&subject>=0)
            {
                if(actor==null)return WorldResult(WorldFailure.RulesUnavailable);
                if(actor.life!=input.unitLife)return WorldResult(WorldFailure.StaleRequest);
                if(!actor.nativeState.HasValue||actor.nativeInfo?.Valid!=true&&!input.sourceTeam.HasValue)return WorldResult(WorldFailure.RulesUnavailable);
                if(actor.nativeInfo?.Valid==true&&input.sourceTeam.HasValue&&actor.nativeInfo.fields[24]!=input.sourceTeam)return WorldResult(WorldFailure.InvalidInput);
            }
            else if(subject==-1&&input.unitLife!=0)return WorldResult(WorldFailure.InvalidInput);
            var next=world.Record.Copy();var p=next.campaignProgress??new CampaignProgressRecord{visit=next.scenarioVisit.id};
            if(p.revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            next.campaignProgress=p;p.nativeLocalTeam=input.localTeam;p.nativeBlocked=false;
            // A full original100-slot FIFO drops the appended item, then still pumps an idle head.
            if(append)
                p.events=p.events.Concat(new[]{new CampaignEventRecord{id=input.id+":native",number=number,native=new NativeCampaignEvent{
                    subject=subject,life=subject<0?0:input.unitLife,providedTeam=input.sourceTeam??0,providedTeamKnown=input.sourceTeam.HasValue,
                    revision=input.expectedRevision,fingerprint=exists?OriginalCampaignScript.Fingerprint(string.Join("\n",bodies[number].Select(c=>c.raw))):""}}}).ToArray();
            p.nativeNotices=(p.nativeNotices??Array.Empty<string>()).Concat(new[]{input.id}).ToArray();p.revision++;
            f=PrepareDirectedStart(next,out var started);if(f!=WorldFailure.None)return WorldResult(f);
            f=CommitWorld(next);if(f==WorldFailure.None){world.Record=next;StartDirectedPresentation(started);}
            return WorldResult(f);
        }
        public WorldReply ReceiveCampaignEvents(CampaignEventsNotice notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(notice==null||!notice.approved||notice.character!=worldCharacter||notice.session!=world.CampaignSession||notice.visit!=world.Record.scenarioVisit?.id
                ||notice.map!=world.Record.map||!Guid.TryParseExact(notice.id,"N",out _))return WorldResult(WorldFailure.StaleRequest);
            var map=world.Definition.campaign?.Map(notice.map);
            if(world.NativeEventPending)return WorldResult(WorldFailure.Busy);
            if(map?.kind!="scenario"||world.Record.campaignPhase=="outro"||world.Record.campaignPhase=="complete"||world.Moving||world.Kind!=FacilityKind.None||world.Combat?.Pending!=null)return WorldResult(WorldFailure.InvalidStage);
            if(!OriginalCampaignScript.DecodeEvents(notice.packet,out var numbers))return WorldResult(WorldFailure.InvalidInput);
            var bodies=OriginalCampaignScript.Events(map.events);
            if(numbers.Any(n=>!bodies.ContainsKey(n)))return WorldResult(WorldFailure.InvalidInput);
            if(world.Record.campaignProgress?.events.Any(e=>e.id.StartsWith(notice.id+":",StringComparison.Ordinal))==true)return WorldResult(WorldFailure.StaleRequest);
            var next=world.Record.Copy();var progress=next.campaignProgress??new CampaignProgressRecord{visit=next.scenarioVisit?.id??"legacy:"+next.map};
            if(progress.events.Count(e=>!e.completed&&e.id!=world.ActiveCampaignEvent)+numbers.Length>100)return WorldResult(WorldFailure.Busy);
            progress.events=progress.events.Concat(numbers.Select((n,i)=>new CampaignEventRecord{id=notice.id+":"+i,number=n})).ToArray();
            progress.revision++;next.campaignProgress=progress;f=CommitWorld(next);if(f==WorldFailure.None)world.Record=next;return WorldResult(f);
        }
        private bool AdvanceCampaignEvents(double ms)
        {
            world.Presentation?.Advance(ms);
            if(world.ActiveCampaignEvent!=null&&world.Presentation?.Snapshot().completed==true)
            {
                if(world.Record.campaignProgress.revision==long.MaxValue){world.Failure=WorldFailure.StaleRequest;return false;}
                var next=world.Record.Copy();var ended=next.campaignProgress.events.First(e=>e.id==world.ActiveCampaignEvent);ended.completed=true;next.campaignProgress.revision++;
                if(ended.native!=null)
                {
                    string active=world.ActiveCampaignEvent;world.ActiveCampaignEvent=null;
                    var prepared=PrepareDirectedStart(next,out var nativeStart);world.ActiveCampaignEvent=active;
                    if(prepared!=WorldFailure.None){world.Failure=prepared;return false;}
                    var saved=CommitWorld(next);world.PhaseSavePending=saved!=WorldFailure.None;
                    if(saved!=WorldFailure.None){world.Failure=saved;return false;}
                    world.Record=next;world.ActiveCampaignEvent=null;StartDirectedPresentation(nativeStart);return true;
                }
                var f=CommitWorld(next);world.PhaseSavePending=f!=WorldFailure.None;
                if(f!=WorldFailure.None){world.Failure=f;return false;}world.Record=next;world.ActiveCampaignEvent=null;
            }
            if(world.ActiveCampaignEvent==null&&world.Record.campaignPhase=="explore"&&(world.Presentation==null||world.Presentation.Snapshot().completed))
            {
                var pending=world.Record.campaignProgress?.events.FirstOrDefault(e=>!e.completed);
                if(pending!=null)
                {
                    if(pending.native!=null)
                    {
                        if(world.Record.campaignProgress.nativeBlocked)return true;
                        var next=world.Record.Copy();var prepared=PrepareDirectedStart(next,out var nativeStart);
                        if(prepared!=WorldFailure.None){world.Failure=prepared;return false;}
                        if(next.campaignProgress.revision==long.MaxValue){world.Failure=WorldFailure.StaleRequest;return false;}
                        next.campaignProgress.revision++;
                        var saved=CommitWorld(next);if(saved!=WorldFailure.None){world.Failure=saved;return false;}
                        world.Record=next;StartDirectedPresentation(nativeStart);return true;
                    }
                    var body=OriginalCampaignScript.Events(world.Definition.campaign.Map(world.Record.map).events)[pending.number];
                    var actors=world.Combat!=null?world.Combat.Units.Select(u=>new CampaignActor{unit=u.id,job=u.job,sourceJob=u.id==0?"COM0":u.job.ToString(),x=u.x,y=u.y,renderX=u.x,renderY=u.y,direction=u.direction,dead=u.hp==0}).ToArray():
                        world.Definition.campaign.Map(world.Record.map).units.Select(u=>new CampaignActor{unit=u.unit,job=u.job,x=u.x,y=u.y,renderX=u.x,renderY=u.y,direction=u.direction})
                        .Concat(new[]{new CampaignActor{unit=0,job=world.CommanderJob,sourceJob="COM0",x=world.Record.x,y=world.Record.y,renderX=world.Record.x,renderY=world.Record.y,direction=world.Record.direction}}).ToArray();
                    world.SetPresentation(string.Join("\n",body.Select(c=>c.raw)),"event:"+pending.id,actors);world.ActiveCampaignEvent=pending.id;
                }
            }
            return true;
        }

    }
}
