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
    internal static void RunNativeMagicChecks(string path)
    {
        root=path;CheckNativeMagicInput(false);CheckNativeMagicInput(true);CheckNativeMagicGuards();CheckNativeClientCastTiming();CheckAllNativeMagicNotices();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Native magic direction-specific wire formats, explicit outgoing mode/signature, incoming same-team echo/zero/negative guards, target lives, environmental source, no guessed resource result and durable replay/reconnect contracts."}));
    }
}
