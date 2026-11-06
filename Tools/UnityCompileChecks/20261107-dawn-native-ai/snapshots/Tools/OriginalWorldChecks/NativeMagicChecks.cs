using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
using DarkSaver.Original.Entry;
internal static class NativeMagicEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunNativeMagicChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static NativeBattleNotice MagicPacket(Fixture f,int source=400,int magic=101,params int[] targets)
    {
        var target=f.View.battle.Unit(1);return new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,approved=true,
            unitLife=source>=0?f.View.battle.Unit(source).life:0,localTeam=0,sourceTeam=source==0?0:8,
            targetLives=targets.Distinct().Select(id=>new BattleLifeRecord{id=id,life=f.View.battle.Unit(id).life}).ToArray(),
            packet=OriginalNativeMagic.Notice(new NativeMagicPacket{magic=magic,source=source,x=target.x,y=target.y,targets=targets})};
    }
    private static void CheckNativeMagicInput(bool scenario)
    {
        var f=LifecycleFixture(scenario);var hp=f.View.battle.Unit(1).hp;var source=f.View.battle.Unit(400).Copy();var record=f.View.record.Copy();
        var incoming=MagicPacket(f,400,101,1);Check(f.server.ReceiveBattlePacket(incoming).Accepted,"native5C89 incoming magic list accepted");
        Check(f.View.battle.magicEffects.Any(e=>e.targetUnit==1&&e.magic==101&&e.sourceUnit==400),"received enemy cast applies existing source/target effect slot");
        Check(f.View.battle.Unit(1).hp==hp&&f.View.battle.Unit(400).ap==source.ap&&f.View.battle.Unit(400).mp==source.mp&&f.View.record.growth.experience==record.growth.experience,"visual magic packet never manufactures damage/resources/XP");
        int effects=f.View.battle.magicEffects.Length;Check(!f.server.ReceiveBattlePacket(incoming).Accepted&&f.View.battle.magicEffects.Length==effects,"duplicate magic packet does not start a second effect");
        Check(f.server.ReceiveBattlePacket(MagicPacket(f,400,601,1)).Accepted&&f.View.battle.magicEffects.Any(e=>e.role==BattleMagicEffectRole.ChargeOnly&&e.sourceUnit==400),"native lightning adds its confirmed caster charge slot");
        var ghost=LifecycleFixture(scenario);Check(ghost.server.ReceiveBattlePacket(MagicPacket(ghost,-1,101,1)).Accepted,"native negative source supports received target-only environmental effect");
        Check(ghost.View.battle.magicEffects.Length==1&&ghost.View.battle.magicEffects[0].sourceX==-1&&ghost.View.battle.magicEffects[0].sourceY==-1&&ghost.View.battle.magicEffects[0].role==BattleMagicEffectRole.TargetOnly,"missing source uses native(-1,-1), no fabricated caster or charge");
        var duplicate=LifecycleFixture(scenario);Check(duplicate.server.ReceiveBattlePacket(MagicPacket(duplicate,400,101,1,1)).Accepted&&duplicate.View.battle.magicEffects.Count(e=>e.targetUnit==1&&e.role!=BattleMagicEffectRole.ChargeOnly)==1,"repeated target writes one native target slot");
        foreach(var value in new[]{MagicPacket(duplicate,0,101,1),MagicPacket(duplicate,400,101),MagicPacket(duplicate,400,-1,1)})
        {
            value.expectedRevision=duplicate.View.battle.revision;int count=duplicate.View.battle.magicEffects.Length;
            Check(duplicate.server.ReceiveBattlePacket(value).Accepted&&duplicate.View.battle.magicEffects.Length==count,"same-team echo/zero targets/negative magic adds no visual event");
        }
    }
    private static void CheckDirectNativeMagic(bool scenario)
    {
        var f=LifecycleFixture(scenario);var n=MagicPacket(f,0,101,1);n.packet[1]=0x67;n.localTeam=n.sourceTeam=null;
        var before=f.View.battle.Commander.Copy();var gold=f.View.record.gold;var xp=f.View.record.growth.experience;
        var manager=new LocalServerManager(f.server);
        Check(manager.Notify_BattlePacket(n).Accepted,"67 enters common manager without requiring the89 team-echo metadata");
        Check(f.View.battle.magicEffects.Any(e=>e.magic==101&&e.sourceUnit==0&&e.targetUnit==1),"67 same-team direct effect reaches native visual slots");
        Check(f.View.battle.Commander.hp==before.hp&&f.View.battle.Commander.mp==before.mp&&f.View.battle.Commander.ap==before.ap&&f.View.record.gold==gold&&f.View.record.growth.experience==xp,"67 visual delivery grants no damage/resources/gold/XP");
        string oldEffects=State(f.View.battle.magicEffects);
        var status=new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,unitLife=f.View.battle.Commander.life,approved=true,packet=OriginalBattleStatus.Packet(0,0x81)};
        Check(manager.Notify_BattlePacket(status).Accepted&&f.View.battle.Commander.nativeStatusFlags==0x140&&State(f.View.battle.magicEffects)==oldEffects,"13 through manager changes condition icons without resetting an existing67 effect");
        int effects=f.View.battle.magicEffects.Length;Check(!manager.Notify_BattlePacket(n).Accepted&&f.View.battle.magicEffects.Length==effects,"67 replay does not recreate effects");
        var empty=LifecycleFixture(scenario);
        foreach(var notice in new[]{MagicPacket(empty,0,101),MagicPacket(empty,0,-1,1)})
        {notice.packet[1]=0x67;notice.localTeam=notice.sourceTeam=null;notice.expectedRevision=empty.View.battle.revision;Check(empty.server.ReceiveBattlePacket(notice).Accepted&&empty.View.battle.magicEffects.Length==0,"67 zero list/negative magic follows DB20 no-effect path");}
        var missing=LifecycleFixture(scenario);var environmental=MagicPacket(missing,-1,101,1);environmental.packet[1]=0x67;environmental.localTeam=environmental.sourceTeam=null;
        Check(missing.server.ReceiveBattlePacket(environmental).Accepted&&missing.View.battle.magicEffects.All(e=>e.role==BattleMagicEffectRole.TargetOnly&&e.sourceX==-1&&e.sourceY==-1),"67 negative source creates target-only effect without invented caster");
        foreach(string invalid in new[]{"approval","owner","session","revision","life","target-life","length","unknown-target"})
        {
            var bad=LifecycleFixture(scenario);var proof=MagicPacket(bad,0,101,1);proof.packet[1]=0x67;proof.localTeam=proof.sourceTeam=null;
            if(invalid=="approval")proof.approved=false;if(invalid=="owner")proof.character=Id();if(invalid=="session")proof.session=Id();if(invalid=="revision")proof.expectedRevision++;
            if(invalid=="life")proof.unitLife++;if(invalid=="target-life")proof.targetLives[0].life++;if(invalid=="length")proof.packet=proof.packet.Take(5).ToArray();
            if(invalid=="unknown-target"){proof.packet[11]=0xff;proof.packet[12]=0x7f;}
            string state=State(bad.View.record);Check(!bad.server.ReceiveBattlePacket(proof).Accepted&&State(bad.View.record)==state&&bad.View.battle.magicEffects.Length==0,"67 rejected proof preserves state and effects: "+invalid);
        }
        var failed=LifecycleFixture(scenario);var retry=MagicPacket(failed,0,101,1);retry.packet[1]=0x67;retry.localTeam=retry.sourceTeam=null;
        string original=State(failed.View.record);failed.store.fail=true;
        Check(failed.server.ReceiveBattlePacket(retry).Failure==WorldFailure.StorageUnavailable&&State(failed.View.record)==original&&failed.View.battle.magicEffects.Length==0,"67 save refusal leaves no partial presentation");
        failed.store.fail=false;Check(failed.server.ReceiveBattlePacket(retry).Accepted,"67 same proof retries after save failure");
        Check(OriginalNativeMagic.Notice(new NativeMagicPacket{magic=101,targets=new[]{1}},0x66)==null,"67 support does not silently alias unrelated opcodes");
    }
    private static void CheckNativeMagicGuards()
    {
        foreach(int mode in new[]{1,2})foreach(int magic in new[]{101,21101})foreach(int count in new[]{0,1,255})
        {
            var p=new NativeMagicPacket{magic=magic,mode=mode,source=0,x=-1,y=-1,signature=uint.MaxValue,targets=Enumerable.Range(0,count).ToArray()};var bytes=OriginalNativeMagic.Request(p);
            Check(bytes.Length==16+2*count&&OriginalNativeMagic.TryRequest(bytes,out var request)&&request.magic==magic&&request.mode==mode&&request.signature==uint.MaxValue&&request.targets.SequenceEqual(p.targets),"outgoing general/special mode/check value/count byte roundtrip");
            Check(!OriginalNativeMagic.TryNotice(bytes,out _),"outgoing magic cannot be mistaken for incoming same-opcode message");
            var incoming=OriginalNativeMagic.Notice(p);Check(OriginalNativeMagic.TryNotice(incoming,out var read)&&read.targets.SequenceEqual(p.targets)&&!OriginalNativeMagic.TryRequest(incoming,out _),"incoming format excludes request mode and DWORD and remains direction-specific");
        }
        Check(OriginalNativeMagic.Request(new NativeMagicPacket{mode=0,magic=101,targets=Array.Empty<int>()})==null&&OriginalNativeMagic.Notice(new NativeMagicPacket{targets=new int[256]})==null,"unknown mode and overflowing target count are not truncated");
        foreach(int token in new[]{0,255,32768,65535})
        {
            var bytes=OriginalNativeMagic.Request(new NativeMagicPacket{item=300,itemToken=token,magic=3001,source=0,x=1,y=2,signature=uint.MaxValue,targets=new[]{0}});
            Check(bytes.Length==21&&OriginalNativeMagic.TryRequest(bytes,out var item)&&item.item==300&&item.itemToken==token&&item.magic==3001&&item.signature==uint.MaxValue,"separate5C91 item request preserves explicit unsigned token and DWORD");
            Check(OriginalBattleRequests.TryDecode(bytes,out var action)&&action.action==TrainingAction.UseItem&&action.itemToken==token,"common outgoing decoder identifies item transaction without reusing local slot");
        }
        foreach(string failure in new[]{"life","target-life","session","revision","team","unknown-target","length"})
        {
            var f=LifecycleFixture();var packet=MagicPacket(f,400,101,1);string before=State(f.View.record);
            if(failure=="life")packet.unitLife++;if(failure=="target-life")packet.targetLives[0].life++;if(failure=="session")packet.session=Id();if(failure=="revision")packet.expectedRevision++;if(failure=="team")packet.localTeam=null;
            if(failure=="unknown-target")packet.packet=OriginalNativeMagic.Notice(new NativeMagicPacket{magic=101,source=400,targets=new[]{2999}});if(failure=="length")packet.packet=packet.packet.Take(packet.packet.Length-1).ToArray();
            Check(!f.server.ReceiveBattlePacket(packet).Accepted&&State(f.View.record)==before&&f.View.battle.magicEffects.Length==0,"reject malformed/stale/unknown-team magic without state/effect changes "+failure);
        }
        var durable=LifecycleFixture(true);var n=MagicPacket(durable,400,101,1);string state=State(durable.View.record);durable.store.fail=true;
        Check(durable.server.ReceiveBattlePacket(n).Failure==WorldFailure.StorageUnavailable&&State(durable.View.record)==state&&durable.View.battle.magicEffects.Length==0,"failed magic notice save applies no effects");durable.store.fail=false;Check(durable.server.ReceiveBattlePacket(n).Accepted,"same magic event can retry after durable failure");
        var restore=new DarkSaver.Original.Entry.EntryAccountService(durable.store);new DarkSaver.Original.Entry.EntryClient(restore).OpenLocalProfile();Check(restore.BeginWorld(durable.definition).Accepted&&restore.WorldSnapshot().battle.magicEffects.Length==0,"reconnect does not replay old cast");
        var forged=LifecycleFixture();var notice=forged.Notice();notice.nativeMagic=MagicPacket(forged,400,101,1);notice.units[0].hp--;Check(!forged.server.ReceiveBattleUnits(notice).Accepted,"raw magic proof cannot authorize damage in the final snapshot");
        var forgedXp=LifecycleFixture();var xpNotice=forgedXp.Notice();xpNotice.nativeMagic=MagicPacket(forgedXp,400,101,1);xpNotice.units.First(u=>u.id==400).nativeExperience=500;
        Check(!forgedXp.server.ReceiveBattleUnits(xpNotice).Accepted&&!forgedXp.View.battle.Unit(400).nativeExperience.HasValue,"visual magic proof cannot smuggle a per-unit XP update");
        var queued=new Fixture();Check(queued.server.RequestBattleAction(TrainingAction.CastMagic,400,0,0,Id(),101).Accepted,"existing cast queue captures original validation signature");
        var pending=queued.View.battle.pending;var raw=pending.NativeMagicPacket(2);
        Check(OriginalNativeMagic.TryRequest(raw,out var decoded)&&decoded.signature==OriginalNativeMagic.Signature(queued.definition.magicMenu.Find(101))&&decoded.source==0&&decoded.targets.SequenceEqual(pending.magicTargets.Select(t=>t.id)),"explicit native mode builds exact captured spell and candidate list without guessing server result");
        Check(pending.NativeMagicPacket(0)==null&&OriginalNativeMagic.TryRequest(pending.NativePacket,out var standard)&&standard.mode==1,"confirmed GameCtl manual call uses parameter0 and writes mode1; invalid explicit mode is rejected");
    }
    private static void CheckNativeClientCastTiming()
    {
        foreach(string outcome in new[]{"accept","deny","storage"})
        {
            var f=new Fixture();var manager=new LocalServerManager(f.server);string before=State(f.View.record);int observed=0;string effect=null;
            if(outcome=="storage")f.store.fail=true;
            manager.BattleAuthority=(r,v)=>
            {
                Check(v.magicEffects.Length>0&&State(f.View.record)==before,"original client cast visuals start before server result, with no predicted resources");
                observed=v.magicEffects.Length;effect=v.magicEffects[0].id;var result=MagicResult(f);if(outcome=="deny")result.approved=false;return result;
            };
            var reply=manager.Request_BattleMagic(101,400);
            Check(reply.Accepted==(outcome=="accept")&&f.View.battle.magicEffects.Length==observed&&f.View.battle.magicEffects.Any(e=>e.id==effect),"approval/denial/save failure does not duplicate or restart original locally displayed cast "+outcome);
            if(outcome!="accept")Check(State(f.View.record)==before&&f.View.battle.pending==null,"rejected cast preserves all authoritative values while transient animation completes");
            f.server.AdvanceWorld(100000);Check(f.View.battle.magicEffects.Length==0,"locally started cast retires after rejection or approval");
        }
        var item=ItemFixture();var owner=Owned(item);var itemManager=new LocalServerManager(item.server);string saved=State(item.View.record);
        itemManager.BattleAuthority=(r,v)=>
        {Check(v.magicEffects.Length>0&&State(item.View.record)==saved&&item.View.record.inventory.Any(i=>i.id==owner),"item target display starts before server consumption, no predicted item removal");return ItemResult(item);};
        Check(itemManager.Request_BattleItem(owner,400).Accepted&&!item.View.record.inventory.Any(i=>i.id==owner),"item is removed only by explicit approved result after its original local display");
        var unavailable=new Fixture();var empty=new LocalServerManager(unavailable.server);
        Check(empty.Request_BattleMagic(101,400).Failure==WorldFailure.RulesUnavailable&&unavailable.View.battle.magicEffects.Length==0&&unavailable.View.battle.pending==null,"unconfigured server result route does not fabricate a playable cast");
    }
    private static void CheckAllNativeMagicNotices()
    {
        foreach(int source in new[]{400,-1})
        {
            var f=LifecycleFixture();int hp=f.View.battle.Unit(1).hp,mp=f.View.battle.Commander.mp,ap=f.View.battle.Commander.ap;
            var rows=f.definition.magicMenu.entries.Where(m=>f.definition.magicProjectiles.SupportsEffect(m)).ToArray();Check(rows.Length==375,"all289 general and86 skill display records are in native notice scope");
            foreach(var row in rows)
            {
                var n=MagicPacket(f,source,row.id,1);n.packet=OriginalNativeMagic.Notice(new NativeMagicPacket{magic=row.id,source=source,x=short.MinValue,y=short.MaxValue,targets=new[]{1}});
                Check(f.server.ReceiveBattlePacket(n).Accepted,"incoming signed coordinates are not revalidated as an outgoing click "+row.id+" source"+source);
                var frames=BattleMagicPresentation.Frames(f.View.battle.magicEffects); // Exercise rendering calculations before cleanup, even for out-of-map coordinates.
                Check(f.View.battle.Unit(1).hp==hp&&f.View.battle.Commander.mp==mp&&f.View.battle.Commander.ap==ap,"every incoming visual remains separate from damage/costs "+row.id);
                int steps=0;
                while(f.View.battle.magicEffects.Length>0&&steps++<1024)
                {BattleMagicPresentation.Frames(f.View.battle.magicEffects);f.server.AdvanceWorld(40);}
                Check(steps<1024&&f.View.battle.magicEffects.Length==0,"every native notice frame calculates and retires without a stuck effect "+row.id);
            }
        }
    }
    private static Fixture LevelEffectFixture(bool scenario=false,int source=0,bool targets=true)
    {
        var f=LifecycleFixture(scenario,near:true);var n=f.Notice();var actor=n.units.First(u=>u.id==source);
        actor.magicRules=(actor.magicRules??Array.Empty<BattleMagicRule>()).Concat(new[]{new BattleMagicRule{magic=4001,enabled=true,targets=targets?new[]{source==0?400:0}:Array.Empty<int>()}}).ToArray();
        Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit original relation eligibility for level-effect request; no spell grant");return f;
    }
    private static NativeBattleNotice LevelEffectNotice(Fixture f,int opcode=0x64,int source=0)
    {
        var actor=f.View.battle.Unit(source);var targets=actor?.magicRules?.FirstOrDefault(r=>r.magic==4001)?.targets??Array.Empty<int>();
        return new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,unitLife=actor?.life??0,approved=true,
            targetLives=targets.Select(id=>new BattleLifeRecord{id=id,life=f.View.battle.Unit(id).life}).ToArray(),packet=new[]{(byte)0x5c,(byte)opcode,(byte)source,(byte)(source>>8)}};
    }
    private static void CheckNativeLevelEffects()
    {
        foreach(bool scenario in new[]{false,true})foreach(int source in new[]{0,400})foreach(int opcode in new[]{0x64,0x65})
        {
            var f=LevelEffectFixture(scenario,source);var n=LevelEffectNotice(f,opcode,source);var before=f.View.record.Copy();var commander=f.View.battle.Commander.Copy();
            var reply=f.server.ReceiveBattlePacket(n,out var packet);
            Check(reply.Accepted&&OriginalNativeMagic.TryRequest(packet,out var cast)&&cast.magic==4001&&cast.mode==1&&cast.source==source
                &&cast.targets.SequenceEqual(n.targetLives.Select(t=>t.id))&&cast.signature==OriginalNativeMagic.Signature(f.definition.magicMenu.Find(4001)),"5C64/65 produces original 4001 mode1 request in training/scenario "+scenario+" source"+source+" result="+reply.Failure+" packet="+(packet==null?"null":Convert.ToHexString(packet)));
            before.scenarioBattle=f.View.record.scenarioBattle?.Copy(); // Only durable delivery/revision bookkeeping may change.
            Check(State(f.View.record)==State(before)&&f.View.battle.magicEffects.Length==0&&f.View.battle.Commander.ap==commander.ap&&f.View.battle.Commander.mp==commander.mp,"level-effect command itself never awards levels/XP, starts visuals, or predicts costs/damage");
            Check(!f.server.ReceiveBattlePacket(n,out var duplicate).Accepted&&duplicate==null,"duplicate level-effect delivery does not resend followup");
        }
        foreach(string failure in new[]{"length","life","target-life","session","revision","approval","rules","flags","metadata"})
        {
            var f=LevelEffectFixture();var n=LevelEffectNotice(f);string before=State(f.View.record);long revision=f.View.battle.revision;
            if(failure=="length")n.packet=n.packet.Concat(new byte[]{0}).ToArray();if(failure=="life")n.unitLife++;if(failure=="target-life")n.targetLives[0].life++;
            if(failure=="session")n.session=Id();if(failure=="revision")n.expectedRevision++;if(failure=="approval")n.approved=false;
            if(failure=="rules"||failure=="flags")
            {var update=f.Notice();if(failure=="rules")update.units.First(u=>u.id==0).magicRules=null;else update.units.First(u=>u.id==400).nativeEnabled=null;Check(f.server.ReceiveBattleUnits(update).Accepted,"isolated unknown eligibility fixture");n.expectedRevision=revision=f.View.battle.revision;before=State(f.View.record);}
            if(failure=="metadata")f.definition.magicMenu.Find(4001).range=7;
            Check(!f.server.ReceiveBattlePacket(n,out var packet).Accepted&&packet==null&&State(f.View.record)==before&&f.View.battle.revision==revision,"invalid/unknown level-effect request has no partial save or outbound packet "+failure);
        }
        var durable=LevelEffectFixture(true);var notice=LevelEffectNotice(durable);string saved=State(durable.View.record);durable.store.fail=true;
        Check(durable.server.ReceiveBattlePacket(notice,out var delivered).Accepted&&delivered!=null&&State(durable.View.record)==saved,"pure original client control command needs no profile save, even if storage is unavailable");
        durable.store.fail=false;Check(!durable.server.ReceiveBattlePacket(notice,out var retried).Accepted&&retried==null,"repeated client command does not resend after storage recovers");
        var pending=LevelEffectFixture();pending.Queue();string request=pending.View.battle.pending.id;
        long pendingRevision=pending.View.battle.revision;
        Check(pending.server.ReceiveBattlePacket(LevelEffectNotice(pending),out _).Accepted&&pending.View.battle.pending?.id==request&&pending.View.battle.revision==pendingRevision,"level-effect receipt preserves an unrelated outstanding action and battle revision");
        var empty=LevelEffectFixture(targets:false);
        Check(empty.server.ReceiveBattlePacket(LevelEffectNotice(empty),out var none).Accepted&&none==null,"no eligible target means original CheckMagicTarget produces no magic request");
        var negative=LevelEffectFixture();
        Check(negative.server.ReceiveBattlePacket(LevelEffectNotice(negative,source:-1),out var ignored).Accepted&&ignored==null,"signed negative original unit ignores level-effect request");
        foreach(string callback in new[]{"none","logout","escape","new-revision","focus-logout","focus-revision","mutate-delivery"})
        {
            var f=LevelEffectFixture();var manager=new LocalServerManager(f.server);int sent=0,focused=0;
            var delivery=LevelEffectNotice(f);
            manager.Notify_BattlePacketRequested+=bytes=>{sent++;Check(OriginalNativeMagic.TryRequest(bytes,out var cast)&&cast.magic==4001,"manager emits original application bytes");};
            manager.Notify_BattleFocusRequested+=focus=>
            {
                focused++;Check(focus.unit==0&&focus.Matches(f.View)&&focus.x==f.View.battle.Commander.x&&focus.y==f.View.battle.Commander.y&&sent==0,"source focus precedes the 4001 request and owns immutable context");
                if(callback=="focus-logout")new EntryClient(f.server).Logout();else if(callback=="focus-revision")f.server.ReceiveBattleUnits(f.Notice());
            };
            manager.Notify_BattleChanged+=reply=>
            {if(callback=="logout")new EntryClient(f.server).Logout();else if(callback=="escape")f.server.EscapeTraining();else if(callback=="new-revision")f.server.ReceiveBattleUnits(f.Notice());
                else if(callback=="mutate-delivery"){delivery.packet[2]=1;delivery.packet[1]=0;reply.View.battle.Commander.x++;reply.View.characterId="mutated view";}};
            bool emits=callback=="none"||callback=="mutate-delivery";
            Check(manager.Notify_TrainingPacket(delivery).Accepted&&sent==(emits?1:0)
                &&focused==(emits||callback.StartsWith("focus-")?1:0),"stale battle/focus callback suppresses old level-effect followup; external DTO mutation cannot change accepted input "+callback);
        }
        foreach(var packet in new[]{Array.Empty<byte>(),new byte[]{0x5c,0x64},new byte[]{0x5c,0x65,0},new byte[]{0x5c,0x63,0,0},new byte[]{0x5c,0x64,0,0,0}})
            Check(!OriginalNativeMagic.TryLevelEffectNotice(packet,out _),"strict level-effect application length/opcode guard");
        var scenarioUi=LifecycleFixture(scenario:true);var setup=scenarioUi.Notice();
        foreach(var actor in setup.units.Where(u=>u.id==0||u.id==400))actor.magicRules=(actor.magicRules??Array.Empty<BattleMagicRule>()).Concat(new[]{new BattleMagicRule{magic=4001,enabled=true,targets=Array.Empty<int>()}}).ToArray();
        Check(scenarioUi.server.ReceiveBattleUnits(setup).Accepted,"isolated original scenario map/intro and explicit no-target relation fixture");
        var sceneManager=new LocalServerManager(scenarioUi.server);var sceneReceiver=new BattleFocusCheckHost(sceneManager,scenarioUi.definition);int packets=0;
        sceneManager.Notify_BattleFocusRequested+=sceneReceiver.Accept;sceneManager.Notify_BattlePacketRequested+=_=>packets++;
        var map=scenarioUi.definition.Map(scenarioUi.View.record.map).map;var actor0=scenarioUi.View.battle.Commander;var actor400=scenarioUi.View.battle.Unit(400);
        var ownViewport=NativeViewport.Centered(actor0.x,actor0.y,map.width,map.height);var enemyViewport=NativeViewport.Centered(actor400.x,actor400.y,map.width,map.height);
        Check(ownViewport.left!=enemyViewport.left||ownViewport.top!=enemyViewport.top,"large scenario fixture distinguishes commander and enemy camera origins");
        string scenarioState=State(scenarioUi.View.record);
        Check(sceneManager.Notify_BattlePacket(LevelEffectNotice(scenarioUi,source:400)).Accepted&&sceneReceiver.Paints==1&&sceneReceiver.Left==enemyViewport.left&&sceneReceiver.Top==enemyViewport.top&&packets==0,"production manager and UI receiver focus the announced enemy even when CheckMagicTarget has no targets");
        Check(sceneManager.Notify_BattlePacket(LevelEffectNotice(scenarioUi,source:0)).Accepted&&sceneReceiver.Paints==2&&sceneReceiver.Resets==1&&sceneReceiver.Left==ownViewport.left&&sceneReceiver.Top==ownViewport.top&&packets==0,"next commander focus updates viewport while retaining the same scenario dialog session");
        Check(State(scenarioUi.View.record)==scenarioState,"pure source camera/control path leaves the scenario save unchanged");
        var moving=LevelEffectFixture();
        Check(moving.server.ExecuteTrainingAction(TrainingAction.Move,-1,10,8,(request,view)=>
        {
            var result=moving.Notice();result.request=request.id;var actor=result.units.First(u=>u.id==0);actor.x=request.x;actor.y=request.y;return result;
        }).Accepted&&moving.View.moving,"moving control fixture starts a real approved path");
        moving.server.AdvanceWorld(40);var currentMotion=moving.View.battle.commanderMotion;
        var movingManager=new LocalServerManager(moving.server);NativeBattleFocus? announced=null;NativeMagicPacket movingCast=null;
        movingManager.Notify_BattleFocusRequested+=focus=>announced=focus;
        movingManager.Notify_BattlePacketRequested+=packet=>{Check(OriginalNativeMagic.TryRequest(packet,out movingCast),"moving control emits valid original magic bytes");};
        Check(movingManager.Notify_TrainingPacket(LevelEffectNotice(moving)).Accepted&&announced.HasValue&&announced.Value.x==currentMotion.tileX&&announced.Value.y==currentMotion.tileY
            &&movingCast!=null&&movingCast.x==currentMotion.tileX&&movingCast.y==currentMotion.tileY&&movingCast.y!=moving.View.battle.Commander.y&&moving.View.moving,"control camera and target calculation use the current native grid tile, not the approved destination or fractional rendering position");
        moving.server.AdvanceWorld(100000);Check(!announced.Value.Matches(moving.View),"tile crossing invalidates an old focus even if no new server revision arrived");
    }
    private static NativeBattleNotice RedirectNotice(Fixture f,int magic=101,int source=0)
    {
        var target=f.View.battle.Unit(400);return new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,approved=true,unitLife=source>=0?f.View.battle.Unit(source)?.life??0:0,
            packet=OriginalMagicRedirect.Packet(magic,source,target.x,target.y),targetLives=f.View.battle.units.Select(u=>new BattleLifeRecord{id=u.id,life=u.life}).ToArray()};
    }
    private static void CheckNativeRedirect(bool scenario)
    {
        var f=LifecycleFixture(scenario,near:true);var seed=f.Notice();
        seed.units.First(u=>u.id==0).magicRules=new[]{new BattleMagicRule{magic=101,enabled=true,targets=new[]{400}}};
        Check(f.server.ReceiveBattleUnits(seed).Accepted,"68 explicit relation rule without spell ownership/cost rules");
        var notice=RedirectNotice(f);string before=State(f.View.record);long revision=f.View.battle.revision;
        f.store.fail=true;
        var received=f.server.ReceiveBattlePacket(notice,out var packet);
        Check(received.Accepted&&OriginalNativeMagic.TryRequest(packet,out var cast)&&cast.mode==2&&cast.targets.SequenceEqual(new[]{400}),"68 computes original targets and returns mode2 bytes without saving: "+received.Failure+" / "+(packet==null?"null":Convert.ToHexString(packet)));
        Check(State(f.View.record)==before&&f.View.battle.revision==revision&&f.View.battle.magicEffects.Any(e=>e.targetUnit==400),"68 effect changes no resources, stored record or battle revision");
        Check(!f.server.ReceiveBattlePacket(notice,out var repeated).Accepted&&repeated==null,"68 duplicate control emits no duplicate request");f.store.fail=false;
        foreach(string bad in new[]{"approval","owner","session","revision","life","target-life","length","metadata","relation","flags"})
        {
            var test=LifecycleFixture(scenario,near:true);var n=RedirectNotice(test);
            if(bad=="approval")n.approved=false;if(bad=="owner")n.character=Id();if(bad=="session")n.session=Id();if(bad=="revision")n.expectedRevision++;
            if(bad=="life")n.unitLife++;if(bad=="target-life")n.targetLives.First(t=>t.id==400).life++;if(bad=="length")n.packet=n.packet.Take(9).ToArray();
            if(bad=="metadata")n.packet=OriginalMagicRedirect.Packet(32767,0,1,1);
            if(bad=="relation"||bad=="flags"||bad=="overlap")
            {
                var update=test.Notice();if(bad=="relation")update.units[0].magicRules=Array.Empty<BattleMagicRule>();
                if(bad=="flags")update.units.First(u=>u.id==400).nativeActive=null;
                if(bad=="overlap"){update.units[1].nativeEnabled=false;update.units[1].x=update.units[0].x;update.units[1].y=update.units[0].y;}
                Check(test.server.ReceiveBattleUnits(update).Accepted,"68 explicit invalid/unknown context fixture "+bad);n.expectedRevision=test.View.battle.revision;
            }
            string stored=State(test.View.record);Check(!test.server.ReceiveBattlePacket(n,out var output).Accepted&&output==null&&State(test.View.record)==stored&&test.View.battle.magicEffects.Length==0,"68 rejects and preserves state: "+bad);
        }
        var pending=LifecycleFixture(scenario,near:true);pending.Queue();var waiting=pending.View.battle.pending.id;var pendingRevision=pending.View.battle.revision;
        Check(pending.server.ReceiveBattlePacket(RedirectNotice(pending),out _).Accepted&&pending.View.battle.pending.id==waiting&&pending.View.battle.revision==pendingRevision,"68 preserves an unrelated pending player action");
        var empty=LifecycleFixture(scenario,near:true);var emptySeed=empty.Notice();emptySeed.units[0].magicRules[0].targets=Array.Empty<int>();Check(empty.server.ReceiveBattleUnits(emptySeed).Accepted,"68 empty relation fixture");
        Check(empty.server.ReceiveBattlePacket(RedirectNotice(empty),out var none).Accepted&&none==null&&empty.View.battle.magicEffects.Length==0,"68 failed target calculation creates no request or effect");
        var env=LifecycleFixture(scenario,near:true);Check(env.server.ReceiveBattlePacket(RedirectNotice(env,source:-1),out var environmental).Accepted&&OriginalNativeMagic.TryRequest(environmental,out var ep)&&ep.source==-1&&env.View.battle.magicEffects.Any(e=>e.sourceUnit==-1),"68 source-1 bypasses relation and preserves environmental source");
        var team=LifecycleFixture(scenario,near:true);var teamNotice=RedirectNotice(team,source:-2);teamNotice.localTeam=0;
        Check(team.server.ReceiveBattlePacket(teamNotice,out var local).Accepted&&OriginalNativeMagic.TryRequest(local,out var lp)&&lp.source==0,"68 source-2 uses explicitly supplied native local team");
        var unknown=LifecycleFixture(scenario,near:true);Check(unknown.server.ReceiveBattlePacket(RedirectNotice(unknown,source:-2),out _).Failure==WorldFailure.RulesUnavailable,"68 never guesses a missing native local team");
        var group=LifecycleFixture(scenario,near:true);int groupMagic=group.definition.magicMenu.entries.First(m=>m.unit==8&&group.definition.magicProjectiles.SupportsEffect(m)).id;
        var groupSeed=group.Notice();groupSeed.units.First(u=>u.id==400).nativeEnabled=false;var lastSlot=groupSeed.units.First(u=>u.id==400).Copy();lastSlot.id=409;lastSlot.nativeEnabled=true;lastSlot.nativeInfo=null;groupSeed.units=groupSeed.units.Concat(new[]{lastSlot}).ToArray();Check(group.server.ReceiveBattleUnits(groupSeed).Accepted,"68 disabled selected group fixture");
        Check(group.server.ReceiveBattlePacket(RedirectNotice(group,groupMagic),out var groupBytes).Accepted&&OriginalNativeMagic.TryRequest(groupBytes,out var gp)&&gp.targets.Length==0&&gp.x==-1&&gp.y==-1,"68 Unit8 empty group succeeds with original -1/-1 output coordinates");
        var retired=LifecycleFixture(scenario,near:true);var retiredSeed=retired.Notice();retiredSeed.units.First(u=>u.id==1).nativeEnabled=false;retiredSeed.units.First(u=>u.id==1).x=retiredSeed.units[0].x;retiredSeed.units.First(u=>u.id==1).y=retiredSeed.units[0].y;
        Check(retired.server.ReceiveBattleUnits(retiredSeed).Accepted&&retired.server.ReceiveBattlePacket(RedirectNotice(retired),out _).Accepted,"68 retired nonoccupant sharing a live tile does not block target calculation");
        var absent=LifecycleFixture(scenario,near:true);Check(absent.server.ReceiveBattlePacket(RedirectNotice(absent,source:-3),out _).Failure==WorldFailure.RulesUnavailable,"68 never fabricates the special700 relation owner");
        var special=LifecycleFixture(scenario,near:true);var specialSeed=special.Notice();var specialMap=special.definition.Map(special.View.record.map);var blocked=OriginalMemberCommands.StaticBlocked(specialMap);
        var tile=Enumerable.Range(1,specialMap.map.width-2).SelectMany(x=>Enumerable.Range(1,specialMap.map.height-2).Select(y=>new DarkSaver.Original.Tutorial.TutorialTile(x,y))).First(t=>!blocked.Contains(t)&&specialSeed.units.All(u=>u.Tile!=t)&&specialMap.map.Terrain(t,2)!=99);
        var owner=special.Unit(700,100,tile.X,tile.Y);owner.nativeActive=owner.nativeEnabled=true;owner.magicRules=new[]{new BattleMagicRule{magic=101,enabled=true,targets=new[]{400}}};specialSeed.units=specialSeed.units.Concat(new[]{owner}).ToArray();
        Check(special.server.ReceiveBattleUnits(specialSeed).Accepted&&special.server.ReceiveBattlePacket(RedirectNotice(special,source:-3),out var specialBytes).Accepted&&OriginalNativeMagic.TryRequest(specialBytes,out var specialCast)&&specialCast.source==700,"68 special-3 uses the explicitly received700 source/rule");
        var managerFixture=LifecycleFixture(scenario,near:true);var manager=new LocalServerManager(managerFixture.server);int sends=0,focus=0;
        manager.Notify_BattlePacketRequested+=bytes=>{sends++;Check(OriginalNativeMagic.TryRequest(bytes,out var p)&&p.mode==2,"68 manager sends complete mode2 packet");};manager.Notify_BattleFocusRequested+=_=>focus++;
        Check(manager.Notify_BattlePacket(RedirectNotice(managerFixture)).Accepted&&sends==1&&focus==0,"68 common manager sends followup without level-effect camera focus");
        bool nested=false;int nestedSends=0;var nestedFixture=LifecycleFixture(scenario,near:true);var nestedManager=new LocalServerManager(nestedFixture.server);
        nestedManager.Notify_BattleChanged+=reply=>{if(!nested){nested=true;Check(nestedManager.Notify_BattlePacket(RedirectNotice(nestedFixture)).Accepted,"68 reentrant newer control accepted");}};
        nestedManager.Notify_BattlePacketRequested+=_=>nestedSends++;
        Check(nestedManager.Notify_BattlePacket(RedirectNotice(nestedFixture)).Accepted&&nestedSends==1,"68 suppresses older followup after reentrant control without changing revision");
    }
    private static Fixture ReceivedMagicTeams(bool scenario=false,bool otherPlayer=false)
    {
        var f=LifecycleFixture(scenario,near:true);var packets=f.View.battle.units.Select(u=>
        {var p=UnitPacket(u);if(otherPlayer&&u.id==1)p.words[1]=1;return new NativeUnitDelivery{life=u.life,packet=OriginalNativeUnits.Packet(p)};}).ToArray();
        Check(f.server.ReceiveBattleUnitPackets(new NativeBattleUnitsNotice{id=Id(),approved=true,character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,
            expectedGrowthRevision=f.View.record.growth.revision,packets=packets}).Accepted,"actual51 records provide teams for native client controls");
        var clear=f.Notice();foreach(var u in clear.units)u.magicRules=null;Check(f.server.ReceiveBattleUnits(clear).Accepted,"no fabricated per-spell rules in received-team fixture");return f;
    }
    private static void CheckReceivedTeamMagicControls()
    {
        foreach(bool scenario in new[]{false,true})foreach(int source in new[]{0,400})foreach(int opcode in new[]{0x64,0x65})
        {
            var f=ReceivedMagicTeams(scenario);var n=LevelEffectNotice(f,opcode,source);
            int[] ids=source==0?new[]{400}:new[]{0,1};var expected=f.View.battle.units.Where(u=>ids.Contains(u.id)).OrderBy(u=>u.x).ThenBy(u=>u.y).Select(u=>u.id).ToArray();
            n.targetLives=expected.Select(id=>new BattleLifeRecord{id=id,life=f.View.battle.Unit(id).life}).ToArray();
            string before=State(f.View.record);long revision=f.View.battle.revision;f.store.fail=true;
            Check(f.server.ReceiveBattlePacket(n,out var bytes).Accepted&&OriginalNativeMagic.TryRequest(bytes,out var cast)&&cast.magic==4001&&cast.mode==1&&cast.targets.SequenceEqual(expected),"64/65 derives original target masks from51 teams without spell ownership/cost rules");
            Check(State(f.View.record)==before&&f.View.battle.revision==revision&&f.View.battle.Unit(source).magicRules==null,"received-team control does not invent/save permissions,XP,AP or money");
        }
        foreach(bool scenario in new[]{false,true})
        {
            var f=ReceivedMagicTeams(scenario);string before=State(f.View.record);var n=RedirectNotice(f);
            Check(f.server.ReceiveBattlePacket(n,out var bytes).Accepted&&OriginalNativeMagic.TryRequest(bytes,out var cast)&&cast.targets.SequenceEqual(new[]{400}),"68 selects original enemy from51 teams with no typed permission list");
            Check(State(f.View.record)==before&&f.View.battle.magicEffects.Any(e=>e.targetUnit==400),"68 native-team path only constructs its verified outgoing request/effect");
            var priority=ReceivedMagicTeams(scenario);var contradictory=priority.Notice();contradictory.units.First(u=>u.id==0).magicRules=new[]{new BattleMagicRule{magic=101,enabled=false,targets=new[]{0}}};
            Check(priority.server.ReceiveBattleUnits(contradictory).Accepted&&priority.server.ReceiveBattlePacket(RedirectNotice(priority),out var actual).Accepted&&OriginalNativeMagic.TryRequest(actual,out var original)&&original.targets.SequenceEqual(new[]{400}),"known native relation supersedes contradictory legacy policy for server-directed control");
        }
        var incomplete=ReceivedMagicTeams();var changed=incomplete.Notice();changed.units.First(u=>u.id==400).nativeInfo=null;Check(incomplete.server.ReceiveBattleUnits(changed).Accepted,"missing actual target team fixture");
        string state=State(incomplete.View.record);Check(incomplete.server.ReceiveBattlePacket(RedirectNotice(incomplete),out var absent).Failure==WorldFailure.RulesUnavailable&&absent==null&&State(incomplete.View.record)==state,"missing received team is not inferred from monster ID/palette");
        var mixed=ReceivedMagicTeams(otherPlayer:true);var wave=LevelEffectNotice(mixed);wave.localTeam=0;wave.targetLives=mixed.View.battle.units.Select(u=>new BattleLifeRecord{id=u.id,life=u.life}).ToArray();
        state=State(mixed.View.record);Check(mixed.server.ReceiveBattlePacket(wave,out var unresolved).Failure==WorldFailure.RulesUnavailable&&unresolved==null&&State(mixed.View.record)==state,"nearby other-player target remains unknown without actual local-player alliance slot");
        var point=RedirectNotice(mixed);point.localTeam=0;Check(mixed.server.ReceiveBattlePacket(point,out var known).Accepted&&OriginalNativeMagic.TryRequest(known,out var selected)&&selected.targets.SequenceEqual(new[]{400}),"unresolved relation outside actual spell area does not block a known target");
    }
    internal static void RunNativeMagicChecks(string path)
    {
        root=path;CheckNativeMagicInput(false);CheckNativeMagicInput(true);CheckDirectNativeMagic(false);CheckDirectNativeMagic(true);CheckNativeMagicGuards();CheckNativeClientCastTiming();CheckAllNativeMagicNotices();CheckNativeLevelEffects();CheckNativeRedirect(false);CheckNativeRedirect(true);CheckReceivedTeamMagicControls();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Native magic direction-specific wire formats, explicit outgoing mode/signature, incoming89 echo and67 direct-effect/zero/negative guards, target lives, environmental source, no guessed resource result and durable replay/reconnect contracts."}));
    }
}
