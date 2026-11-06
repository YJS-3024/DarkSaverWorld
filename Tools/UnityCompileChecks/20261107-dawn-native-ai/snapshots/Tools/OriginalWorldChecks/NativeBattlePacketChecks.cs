using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class NativeBattlePacketEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunNativeBattlePacketChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static NativeBattleNotice NativeNotice(Fixture f,byte[] bytes,int source=1,int target=400)=>new NativeBattleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,unitLife=f.View.battle.Unit(source)?.life??0,targetLife=f.View.battle.Unit(target)?.life??0,approved=true,packet=bytes};
    private static void CheckNativeRequestPackets()
    {
        foreach(int source in new[]{0,1,999,2000,2999})foreach(int target in new[]{400,401})foreach(int a in new[]{0,127,255,32767,32768,65535})foreach(int b in new[]{0,32768,65535})
        {
            var r=new TrainingActionRequest{action=TrainingAction.Attack,source=source,target=target,sourceTerrain=a,targetTerrain=b};var bytes=r.NativePacket;
            Check(bytes.Length==10&&OriginalBattleRequests.TryDecode(bytes,out var p)&&p.source==source&&p.target==target&&p.sourceTerrain==a&&p.targetTerrain==b,"outgoing native attack four WORD roundtrip");
            Check(!OriginalBattleFeedback.TryDecode(bytes,out _),"outgoing10-byte attack must not be consumed as8-byte incoming effect");bytes[2]=123;Check(r.NativePacket[2]==(byte)source,"packet byte snapshots cannot alter captured request fields");
        }
        foreach(int source in new[]{0,1,999,2999}){var r=new TrainingActionRequest{action=TrainingAction.Rest,source=source};Check(r.NativePacket.Length==4&&r.NativePacket[1]==11&&OriginalBattleRequests.TryDecode(r.NativePacket,out var p)&&p.source==source&&p.action==TrainingAction.Rest,"native5C0B rest request");}
        foreach(var action in new[]{TrainingAction.Move,TrainingAction.CastMagic,TrainingAction.UseSkill,TrainingAction.UseItem,TrainingAction.TroopOrder,TrainingAction.Regroup,TrainingAction.SoldierMove})Check(new TrainingActionRequest{action=action}.NativePacket==null,"unsupported or unprepared action has no fabricated packet "+action);
        foreach(var r in new[]{new TrainingActionRequest{action=TrainingAction.Rest,source=-1},new TrainingActionRequest{action=TrainingAction.Attack,source=0,target=0,sourceTerrain=0,targetTerrain=0},new TrainingActionRequest{action=TrainingAction.Attack,source=0,target=400,sourceTerrain=-1,targetTerrain=0},new TrainingActionRequest{action=TrainingAction.Attack,source=0,target=400,sourceTerrain=65536,targetTerrain=0}})Check(r.NativePacket==null,"invalid request does not silently truncate fields");
        Check(!OriginalBattleRequests.TryDecode(OriginalBattleFeedback.Packet(BattleFeedbackKind.Attack,1,0,400),out _),"incoming attack cannot be used as outgoing action");
        var f=Members();Check(f.server.RequestBattleMemberAction(1,TrainingAction.Attack,400,0,0,Id()).Accepted,"source-aware member action queued");var pending=f.View.battle.pending;Check(OriginalBattleRequests.TryDecode(pending.NativePacket,out var sent)&&sent.source==1&&sent.target==400&&sent.sourceTerrain==pending.sourceTerrain&&sent.targetTerrain==pending.targetTerrain,"existing authority receives bytes of captured member action and terrain");Check(f.server.ReceiveBattleUnits(MemberResult(f)).Accepted,"optional packet bytes preserve existing authority approval");
    }
    private static void CheckUnifiedIngress(bool scenario)
    {
        var f=LifecycleFixture(scenario);
        var hp=NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Health,0,150),0);Check(f.server.ReceiveBattlePacket(hp).Accepted&&f.View.record.hp==150,"unified5C05 HP route");Check(!f.server.ReceiveBattlePacket(hp).Accepted,"shared replay protection across ingress routes");
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Mana,0,40),0)).Accepted&&f.View.record.mp==40,"unified5C06 MP route");
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Miss,0,0),0)).Accepted&&f.View.battle.numberEffects.Single().kind==BattleFeedbackKind.Miss&&f.View.record.hp==150&&f.View.record.mp==40,"unified5C0F explicit MISS replaces numeric slot without health/mana change");
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleTicks.MenuPacket(1,5,17))).Accepted&&f.View.battle.Unit(1).ap==5,"unified5C52 AP/menu route");
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleTicks.PulsePacket())).Accepted&&f.View.battle.Unit(1).ap==6,"unified5C04 pulse route, no local timer");
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleFeedback.Packet(BattleFeedbackKind.Attack,1,7,400))).Accepted&&f.View.battle.attackEffects.Single(e=>e.unit==1).flag==7,"unified5C82 attack-display route");
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleLifecycle.Packet(BattleLifecycleKind.Death,1))).Accepted&&f.View.battle.deaths.Length==1,"unified5C07 death route");
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,OriginalBattleLifecycle.Packet(BattleLifecycleKind.Remove,400),400)).Accepted&&f.View.battle.Unit(400).nativeActive==false,"unified5C03 removal route");
        if(scenario)Check(f.server.ReceiveTrainingPacket(NativeNotice(f,OriginalBattleTicks.PulsePacket())).Failure==WorldFailure.InvalidStage,"unified training-only ingress guard");
        foreach(var bytes in new[]{new byte[]{0x5c,0x82,1,0,144,1,0,0,0,0},new byte[]{0x5c,4,0},new byte[]{0x5c,7,1,0},new byte[]{0x5c,5,1,0},new byte[]{0x5c,0x52,1,0},new byte[]{0x5c,3,1,0,0},new byte[]{0,4},new byte[]{0x5c}})
        {string before=State(f.View.record);Check(!f.server.ReceiveBattlePacket(NativeNotice(f,bytes)).Accepted&&State(f.View.record)==before,"unified malformed/direction-confused packet has no partial effects");}
        Check(f.server.ReceiveBattlePacket(NativeNotice(f,new byte[]{0x5c,0xff})).Failure==WorldFailure.RulesUnavailable,"unsupported subtype is not guessed");
    }
    private static void CheckBodyBounds()
    {
        foreach(int size in new[]{32,64})foreach(int direction in new[]{0,1,2,3})foreach(bool moving in new[]{false,true})foreach(double clock in new[]{0.0,124,125,332,333,666,999})
        {
            var frame=NativeBodyFrames.Body(size*3,size*4,direction,clock,moving,160,160);Check(frame.width==size&&frame.height==size&&frame.x==160+(32-size)/2&&NativeBodyFrames.Fits(frame,size*3,size*4),"full-size body pixels and centered large-body anchor");
        }
        Check(NativeBodyFrames.Body(100,128,0,0,false,0,0)==null&&NativeBodyFrames.Body(96,128,4,0,false,0,0)==null,"unknown sheet geometry/direction does not invent a crop");
        Check(!NativeBodyFrames.Fits(new BattleFeedbackFrame{atlasX=0,atlasY=1024,width=32,height=32},96,128),"native invalid corpse source rect cannot smear clamped final texture row");
        Check(!NativeBodyFrames.Fits(new BattleFeedbackFrame{atlasX=int.MaxValue,atlasY=0,width=32,height=32},96,128),"crop bounds cannot overflow integer addition");
    }
    private static NativeBattleNotice ExperienceNotice(Fixture f,int unit,uint value)
    {
        var n=NativeNotice(f,OriginalBattleExperience.Packet(unit,value),unit);
        if(unit==0)n.expectedGrowthRevision=f.View.record.growth?.revision;return n;
    }
    private static void CheckNativeExperience(bool scenario)
    {
        foreach(int unit in new[]{0,1,400})
        {
            var f=LifecycleFixture(scenario);int classLevel=f.View.record.growth.classLevel,totalLevel=f.View.record.growth.totalLevel;int[] magic=f.View.record.growth.magic.ToArray(),skills=f.View.record.growth.skills.ToArray();
            foreach(uint value in new uint[]{500,5,0,uint.MaxValue,uint.MaxValue})
            {
                ulong commanderXp=f.View.record.growth.experience;long growthRevision=f.View.record.growth.revision;var before=f.View.battle.units.Select(u=>u.Copy()).ToArray();
                var n=ExperienceNotice(f,unit,value);Check(f.server.ReceiveBattlePacket(n).Accepted&&f.View.battle.Unit(unit).nativeExperience==value,"native5C53 replaces target absolute XP including decreases/zero/max in training/scenario "+scenario+" unit"+unit);
                Check(f.View.record.growth.experience==(unit==0?value:commanderXp)&&f.View.record.growth.revision==growthRevision+(unit==0?1:0),"only commander XP packet updates the global growth record");
                Check(f.View.record.growth.classLevel==classLevel&&f.View.record.growth.totalLevel==totalLevel&&f.View.record.growth.magic.SequenceEqual(magic)&&f.View.record.growth.skills.SequenceEqual(skills),"absolute XP receipt never calculates a level/class or grants abilities");
                foreach(var old in before)
                {
                    var current=f.View.battle.Unit(old.id).Copy();if(old.id==unit)current.nativeExperience=old.nativeExperience;
                    Check(OriginalMemberCommands.Same(old,current),"native XP update preserves every other unit value and every other unit");
                }
                string saved=State(f.View.record);Check(!f.server.ReceiveBattlePacket(n).Accepted&&State(f.View.record)==saved,"duplicate absolute-XP delivery does not apply twice");
            }
            if(scenario)
            {
                var reloaded=new EntryAccountService(f.store);new EntryClient(reloaded).OpenLocalProfile();Check(reloaded.BeginWorld(f.definition).Accepted&&reloaded.WorldSnapshot().battle.Unit(unit).nativeExperience==uint.MaxValue,"scenario per-unit XP survives isolated service recreation");
            }
        }
        foreach(string failure in new[]{"life","session","revision","growth-revision","missing-growth-context","length","unknown-unit","approval"})
        {
            var f=LifecycleFixture(scenario);var n=ExperienceNotice(f,0,500);string before=State(f.View.record);long revision=f.View.battle.revision;
            if(failure=="life")n.unitLife++;if(failure=="session")n.session=Id();if(failure=="revision")n.expectedRevision++;
            if(failure=="growth-revision")n.expectedGrowthRevision++;if(failure=="missing-growth-context")n.expectedGrowthRevision=null;
            if(failure=="length")n.packet=n.packet.Take(7).ToArray();if(failure=="unknown-unit")n.packet=OriginalBattleExperience.Packet(999,500);if(failure=="approval")n.approved=false;
            Check(!f.server.ReceiveBattlePacket(n).Accepted&&State(f.View.record)==before&&f.View.battle.revision==revision,"invalid native XP has no partial record/unit update "+failure);
        }
        var failed=LifecycleFixture(scenario);var incoming=ExperienceNotice(failed,0,500);string state=State(failed.View.record);var caller=failed.View.battle.Unit(0).Copy();failed.store.fail=true;
        Check(failed.server.ReceiveBattlePacket(incoming).Failure==WorldFailure.StorageUnavailable&&State(failed.View.record)==state&&OriginalMemberCommands.Same(caller,failed.View.battle.Unit(0)),"failed native XP save rolls back both growth and per-unit XP");
        failed.store.fail=false;Check(failed.server.ReceiveBattlePacket(incoming).Accepted&&failed.View.record.growth.experience==500,"native absolute XP retries after storage recovery");
        var interleaved=LifecycleFixture(scenario);var earlierDelivery=ExperienceNotice(interleaved,0,500);long unchangedBattle=interleaved.View.battle.revision;
        Check(interleaved.server.ReceiveCharacterExperience(new CharacterExperienceNotice{approved=true,id=Id(),character=interleaved.View.characterId,
            session=interleaved.View.growthSession,expectedRevision=interleaved.View.record.growth.revision,experience=5000}).Accepted,"independent profile XP can arrive without a battle action");
        Check(interleaved.View.battle.revision==unchangedBattle&&interleaved.View.battle.Commander.nativeExperience==5000
            &&!interleaved.server.ReceiveBattlePacket(earlierDelivery).Accepted&&interleaved.View.record.growth.experience==5000,"growth revision protects a newer XP value even when battle revision did not change");
        if(scenario)
        {
            var restored=new EntryAccountService(interleaved.store);new EntryClient(restored).OpenLocalProfile();
            Check(restored.BeginWorld(interleaved.definition).Accepted&&restored.WorldSnapshot().record.growth.experience==5000
                &&restored.WorldSnapshot().battle.Commander.nativeExperience==5000,"profile XP synchronization also updates the durable scenario commander snapshot");
        }
        var supplied=LifecycleFixture(scenario);var external=supplied.Notice();external.experience=500;external.expectedGrowthRevision=supplied.View.record.growth.revision;
        external.units.First(u=>u.id==0).nativeExperience=null;string payload=State(external.units);supplied.store.fail=true;
        Check(supplied.server.ReceiveBattleUnits(external).Failure==WorldFailure.StorageUnavailable&&State(external.units)==payload,"failed XP normalization never mutates a caller-owned retry payload");
        supplied.store.fail=false;Check(supplied.server.ReceiveBattleUnits(external).Accepted&&State(external.units)==payload&&supplied.View.battle.Commander.nativeExperience==500,"accepted XP normalization owns its copied units and leaves the caller snapshot intact");
        foreach(var bytes in new[]{Array.Empty<byte>(),new byte[]{0x5c,0x53},new byte[]{0x5c,0x53,0,0,0,0,0},new byte[]{0x5c,0x53,0,0,0,0,0,0,0},new byte[]{0x5c,0x53,255,255,0,0,0,0},new byte[]{0x5c,0x53,184,11,0,0,0,0}})
            Check(!OriginalBattleExperience.TryDecode(bytes,out _,out _),"strict XP packet length/signed unit domain");
    }
    private static void CheckNativeStatus(bool scenario)
    {
        foreach(int unit in new[]{0,1,400})foreach(bool active in new[]{false,true})foreach(uint flags in new uint[]{0,1,2,4,0xffff,0x80000000,0xffff0001,uint.MaxValue})
        {
            var f=LifecycleFixture(scenario);var setup=f.Notice();var target=setup.units.First(u=>u.id==unit);target.nativeActive=active;target.nativeStatusFlags=0xabcdef01;
            Check(f.server.ReceiveBattleUnits(setup).Accepted,"explicit status baseline/active flag");
            var expected=f.View.battle.Unit(unit).Copy();if(active)expected.nativeStatusFlags=OriginalNativeUnits.StatusFlags(flags);
            var hp=f.View.record.hp;var mp=f.View.record.mp;var xp=f.View.record.growth.experience;var gold=f.View.record.gold;
            var packet=OriginalBattleStatus.Packet(unit,flags);Check(OriginalBattleStatus.TryDecode(packet,out var id,out var value)&&id==unit&&value==flags,"13 signed unit and uint flags roundtrip");
            var n=NativeNotice(f,packet,unit);
            Check(f.server.ReceiveBattlePacket(n).Accepted&&State(f.View.battle.Unit(unit))==State(expected),"13 updates only active runtime104 flags and preserves every other unit field");
            Check(f.View.record.hp==hp&&f.View.record.mp==mp&&f.View.record.growth.experience==xp&&f.View.record.gold==gold,"13 does not predict duration, HP/MP, damage, XP or currency");
            string state=State(f.View.record);Check(!f.server.ReceiveBattlePacket(n).Accepted&&State(f.View.record)==state,"13 replay rejected after one application");
        }
        foreach(string error in new[]{"approval","owner","session","revision","life","length","unit","active-unknown"})
        {
            var f=LifecycleFixture(scenario);var n=NativeNotice(f,OriginalBattleStatus.Packet(1,1),1);
            if(error=="approval")n.approved=false;if(error=="owner")n.character=Id();if(error=="session")n.session=Id();if(error=="revision")n.expectedRevision++;if(error=="life")n.unitLife++;
            if(error=="length")n.packet=n.packet.Take(7).ToArray();if(error=="unit")n.packet=OriginalBattleStatus.Packet(2999,1);
            if(error=="active-unknown"){var setup=f.Notice();setup.units.First(u=>u.id==1).nativeActive=null;Check(f.server.ReceiveBattleUnits(setup).Accepted,"explicit unknown active metadata");n.expectedRevision=f.View.battle.revision;}
            string state=State(f.View.record);Check(!f.server.ReceiveBattlePacket(n).Accepted&&State(f.View.record)==state,"13 bad metadata/bytes preserve state: "+error);
        }
        var ignored=LifecycleFixture(scenario);var retired=ignored.Notice();retired.units.First(u=>u.id==1).nativeActive=false;
        Check(ignored.server.ReceiveBattleUnits(retired).Accepted,"explicit inactive status target");
        Check(ignored.server.RequestBattleAction(TrainingAction.Rest,-1,0,0,Id()).Accepted,"unrelated pending action precedes ignored13");
        var noop=NativeNotice(ignored,OriginalBattleStatus.Packet(1,uint.MaxValue),1);string unchanged=State(ignored.View.record),pending=State(ignored.View.battle);ignored.store.fail=true;
        Check(ignored.server.ReceiveBattlePacket(noop).Accepted&&State(ignored.View.record)==unchanged&&State(ignored.View.battle)==pending,"inactive13 needs no save and preserves pending action/revision and all presentation state");
        Check(!ignored.server.ReceiveBattlePacket(noop).Accepted&&State(ignored.View.battle)==pending,"ignored13 is still deduplicated without canceling the pending command");
        var badIgnored=NativeNotice(ignored,OriginalBattleStatus.Packet(1,0),1);badIgnored.session=Id();
        Check(ignored.server.ReceiveBattlePacket(badIgnored).Failure==WorldFailure.StaleRequest&&State(ignored.View.battle)==pending,"inactive13 cannot bypass the original adapter session guard");
        ignored.store.fail=false;
        var durable=LifecycleFixture(scenario);var update=NativeNotice(durable,OriginalBattleStatus.Packet(0,0x81),0);var before=State(durable.View.record);durable.store.fail=true;
        Check(durable.server.ReceiveBattlePacket(update).Failure==WorldFailure.StorageUnavailable&&State(durable.View.record)==before,"13 failed durable update is atomic");
        durable.store.fail=false;Check(durable.server.ReceiveBattlePacket(update).Accepted&&durable.View.battle.Commander.nativeStatusFlags==0x140,"13 retries the same notice after storage recovery");
        if(scenario)
        {var reconnect=new EntryAccountService(durable.store);new EntryClient(reconnect).OpenLocalProfile();Check(reconnect.BeginWorld(durable.definition).Accepted&&reconnect.WorldSnapshot().battle.Commander.nativeStatusFlags==0x140,"13 scenario display state restores through account reload");}
        foreach(var bytes in new[]{Array.Empty<byte>(),new byte[]{0x5c,0x13},new byte[]{0x5c,0x13,0,0,0,0,0},new byte[]{0x5c,0x13,0,0,0,0,0,0,0},new byte[]{0x5c,0x13,255,255,0,0,0,0},new byte[]{0x5c,0x53,0,0,0,0,0,0}})
            Check(!OriginalBattleStatus.TryDecode(bytes,out _,out _),"13 malformed length/type/negative unit is rejected");
    }
    internal static void RunNativeBattlePacketChecks(string path)
    {
        root=path;CheckNativeRequestPackets();CheckUnifiedIngress(false);CheckUnifiedIngress(true);CheckBodyBounds();CheckNativeExperience(false);CheckNativeExperience(true);CheckNativeStatus(false);CheckNativeStatus(true);
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Verified native Attack/Rest outgoing bytes, independent direction-specific5C82 lengths, one local ingress including13 status flags, replay/length/atomic-state guards and32/64px source bounds. Movement hints have a separate comparison; other unprepared actions generate no packet."}));
    }
}
