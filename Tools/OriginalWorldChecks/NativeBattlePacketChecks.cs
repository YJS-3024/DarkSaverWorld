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
    internal static void RunNativeBattlePacketChecks(string path)
    {
        root=path;CheckNativeRequestPackets();CheckUnifiedIngress(false);CheckUnifiedIngress(true);CheckBodyBounds();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Verified native Attack/Rest outgoing bytes, independent direction-specific5C82 lengths, one local ingress for eight native subtypes, replay/length/atomic-state guards and32/64px source bounds. Movement hints have a separate comparison; other unprepared actions generate no packet."}));
    }
}
