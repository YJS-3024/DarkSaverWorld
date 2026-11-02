using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class NativeBattleResponseEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunNativeBattleResponseChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static NativeBattleResponse PacketResponse(Fixture f,params byte[][] packets)=>new NativeBattleResponse{id=Id(),request=f.View.battle.pending.id,approved=true,packets=packets};
    private static byte[] Ap(int unit,int value=0,int menu=0)=>OriginalBattleTicks.MenuPacket(unit,value,menu);
    private static byte[] Hp(int unit,int value)=>OriginalBattleFeedback.Packet(BattleFeedbackKind.Health,unit,value);
    private static byte[] Mp(int unit,int value)=>OriginalBattleFeedback.Packet(BattleFeedbackKind.Mana,unit,value);
    private static byte[] Strike(int source,int target,int flag=0)=>OriginalBattleFeedback.Packet(BattleFeedbackKind.Attack,source,flag,target);
    private static void QueueNative(Fixture f,TrainingAction action,int source=1)
    {Check(source==0?f.server.RequestBattleAction(action,action==TrainingAction.Attack?400:-1,0,0,Id()).Accepted:f.server.RequestBattleMemberAction(source,action,action==TrainingAction.Attack?400:-1,0,0,Id()).Accepted,"queue native response action");}
    private static void CheckNativeResponseAttack(bool scenario,int source)
    {
        var f=LifecycleFixture(scenario);
        if(source==0)
        {
            var n=f.Notice();var leader=n.units.Single(u=>u.id==0);var enemy=n.units.Single(u=>u.id==400);var map=f.definition.Map(f.View.record.map);var blocked=OriginalMemberCommands.StaticBlocked(map);
            var cells=new[]{(leader.x+1,leader.y),(leader.x-1,leader.y),(leader.x,leader.y+1),(leader.x,leader.y-1)};
            var at=cells.First(p=>map.map.Contains(new DarkSaver.Original.Tutorial.TutorialTile(p.Item1,p.Item2))&&!blocked.Contains(new DarkSaver.Original.Tutorial.TutorialTile(p.Item1,p.Item2))&&!n.units.Any(u=>u.id!=400&&u.x==p.Item1&&u.y==p.Item2)&&map.map.Terrain(new DarkSaver.Original.Tutorial.TutorialTile(p.Item1,p.Item2),2)!=99);
            enemy.x=at.Item1;enemy.y=at.Item2;Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit adjacent commander target fixture");
        }
        QueueNative(f,TrainingAction.Attack,source);var request=f.View.battle.pending;string before=State(f.View.record);long revision=f.View.battle.revision;
        var response=PacketResponse(f,Strike(source,400,2),Hp(400,0),Ap(source,0,0),OriginalBattleLifecycle.Packet(BattleLifecycleKind.Death,400));
        var prepared=OriginalBattleResponses.Prepare(request,f.View.battle,response);Check(prepared!=null&&State(f.View.record)==before&&f.View.battle.Unit(400).hp==50&&f.View.battle.attackEffects.Length==0,"preparing ordered bytes is side-effect free");
        Check(f.server.ReceiveNativeBattleResponse(response).Accepted&&f.View.battle.revision==revision+1&&f.View.battle.pending==null,"one action response advances one revision and clears its pending request");
        Check(f.View.battle.Unit(source).ap==0&&f.View.battle.Unit(source).nativeMenuMask==0&&f.View.battle.Unit(400).hp==0&&f.View.battle.Unit(400).nativeState==144,"absolute packet AP/menu/health/death state applied together");
        Check(f.View.battle.attackEffects.Length==2&&f.View.battle.numberEffects.Single().delta==-50&&f.View.battle.deaths.Single().held,"exactly one native attack pair, actual HP delta and numeric-gated death");
        Check(!f.View.record.ghost&&f.View.record.growth.experience==123,"no inferred ghost/XP/gold or kill formula");
        Check(!f.server.ReceiveNativeBattleResponse(response).Accepted&&f.View.battle.Unit(source).ap==0,"replay cannot spend resources or restart effects");
        f.server.AdvanceWorld(2000);Check(f.View.battle.Unit(400).nativeEnabled==false&&!OriginalCommandRange.OccupiesTile(f.View.battle.Unit(400)),"native packet response follows existing corpse retirement and occupancy");
    }
    private static void CheckNativeResponseRest(bool scenario,bool healthLast)
    {
        var f=LifecycleFixture(scenario);var state=f.Notice();var actor=state.units.Single(u=>u.id==1);actor.mp=3;actor.maxMp=10;Check(f.server.ReceiveBattleUnits(state).Accepted,"explicit rest MP context");QueueNative(f,TrainingAction.Rest);
        var response=healthLast?PacketResponse(f,Mp(1,10),Hp(1,50),Ap(1,7,16)):PacketResponse(f,Hp(1,50),Mp(1,10),Ap(1,7,16));
        Check(f.server.ReceiveNativeBattleResponse(response).Accepted&&f.View.battle.Unit(1).hp==50&&f.View.battle.Unit(1).mp==10&&f.View.battle.Unit(1).ap==7,"raw rest response has explicit HP/MP/AP, no local recovery formula");
        var effect=f.View.battle.numberEffects.Single();Check(effect.kind==(healthLast?BattleFeedbackKind.Health:BattleFeedbackKind.Mana)&&effect.delta==(healthLast?40:7),"shared numeric slot follows wire HP/MP order without stacking or losing resource updates");
    }
    private static void CheckNativeResponseNoInventedEffect()
    {
        foreach(bool miss in new[]{false,true})
        {
            var f=LifecycleFixture();QueueNative(f,TrainingAction.Attack);var packets=miss?new[]{OriginalBattleFeedback.Packet(BattleFeedbackKind.Miss,400,0),Ap(1)}:new[]{Ap(1)};
            Check(f.server.ReceiveNativeBattleResponse(PacketResponse(f,packets)).Accepted&&f.View.battle.Unit(400).hp==50&&f.View.battle.attackEffects.Length==0&&f.View.battle.impact==null,"no5C82 means no invented attack, unchanged HP means no inferred MISS");
            Check(f.View.battle.numberEffects.Length==(miss?1:0),"only native5C0F produces MISS");
        }
        var auto=Soldiers();var flags=auto.Notice();foreach(var u in flags.units){u.nativeActive=true;u.nativeEnabled=true;}Check(auto.server.ReceiveBattleUnits(flags).Accepted,"native automatic action metadata");
        Check(auto.server.ExecuteBattleSoldierAction(1,(r,v)=>OriginalBattleResponses.Prepare(r,v,new NativeBattleResponse{id=Id(),request=r.id,approved=true,packets=new[]{Strike(1,400),Hp(400,40),Ap(1)}}),n=>0,0).Accepted&&auto.View.battle.Unit(1).ap==0&&auto.View.battle.Unit(1).nativeState==0,"native auto attack shares packet approval and verified AP/AI state contract");
    }
    private static void CheckNativeResponseFailures()
    {
        foreach(string problem in new[]{"empty","unknown","truncated","outgoing","pulse","remove","other-unit","wrong-target","double-hp","double-menu","hp-over-max","bad-session-proof","changed-units","changed-events","unrequested-xp","missing-proof-pending"})
        {
            var f=LifecycleFixture();QueueNative(f,TrainingAction.Attack);var response=PacketResponse(f,Hp(400,40),Ap(1));
            switch(problem)
            {
                case "empty":response.packets=Array.Empty<byte[]>();break;case "unknown":response.packets=new[]{new byte[]{0x5c,0xff}};break;
                case "truncated":response.packets=new[]{new byte[]{0x5c,5,0}};break;case "outgoing":response.packets=new[]{f.View.battle.pending.NativePacket};break;
                case "pulse":response.packets=new[]{OriginalBattleTicks.PulsePacket()};break;case "remove":response.packets=new[]{OriginalBattleLifecycle.Packet(BattleLifecycleKind.Remove,400)};break;
                case "other-unit":response.packets=new[]{Hp(0,150)};break;case "wrong-target":response.packets=new[]{Strike(1,0)};break;
                case "double-hp":response.packets=new[]{Hp(400,40),Hp(400,30)};break;case "double-menu":response.packets=new[]{Ap(1),Ap(1)};break;
                case "hp-over-max":response.packets=new[]{Hp(400,51)};break;
            }
            string before=State(f.View.record);WorldReply result;
            if(problem.StartsWith("changed-")||problem=="unrequested-xp"||problem=="bad-session-proof"||problem=="missing-proof-pending")
            {
                var notice=OriginalBattleResponses.Prepare(f.View.battle.pending,f.View.battle,response);
                if(problem=="changed-units")notice.units.Single(u=>u.id==400).hp=1;
                if(problem=="changed-events")notice.feedbackEvents[0].value=1;
                if(problem=="unrequested-xp"){notice.experience=900;notice.expectedGrowthRevision=f.View.record.growth.revision;}
                if(problem=="bad-session-proof")notice.session="bad";
                if(problem=="missing-proof-pending")notice.request=null;
                result=f.server.ReceiveBattleUnits(notice);
            }
            else result=f.server.ReceiveNativeBattleResponse(response);
            Check(!result.Accepted&&State(f.View.record)==before&&f.View.battle.Unit(400).hp==50&&f.View.battle.Unit(1).ap==20&&f.View.battle.numberEffects.Length==0,"bad response never partially applies "+problem);
        }
        foreach(string problem in new[]{"bad-id","wrong-request","denied-payload","oversized","null-packet"})
        {
            var f=LifecycleFixture();QueueNative(f,TrainingAction.Rest);var response=PacketResponse(f,Ap(1));switch(problem){case "bad-id":response.id="bad";break;case "wrong-request":response.request=Id();break;case "denied-payload":response.approved=false;break;case "oversized":response.packets=Enumerable.Repeat(Ap(1),33).ToArray();break;case "null-packet":response.packets=new byte[][]{null};break;}
            Check(!f.server.ReceiveNativeBattleResponse(response).Accepted&&f.View.battle.Unit(1).ap==20,"invalid response context/size "+problem);
        }
        var deny=LifecycleFixture();QueueNative(deny,TrainingAction.Rest);var denied=new NativeBattleResponse{id=Id(),request=deny.View.battle.pending.id,approved=false};Check(!deny.server.ReceiveNativeBattleResponse(denied).Accepted&&deny.View.battle.pending==null&&deny.View.battle.Unit(1).ap==20,"explicit denial closes pending action without spending resources");
    }
    private static void CheckNativeResponseSave()
    {
        var f=LifecycleFixture(true);QueueNative(f,TrainingAction.Attack);var response=PacketResponse(f,Strike(1,400),Hp(400,40),Ap(1,7,17));string before=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveNativeBattleResponse(response).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle.pending!=null&&f.View.battle.attackEffects.Length==0,"atomic packet response waits for durable save");f.store.fail=false;
        Check(f.server.ReceiveNativeBattleResponse(response).Accepted,"identical response retries after storage recovery");var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Unit(1).ap==7&&restored.WorldSnapshot().battle.Unit(1).nativeMenuMask==17&&restored.WorldSnapshot().battle.Unit(400).hp==40&&restored.WorldSnapshot().battle.attackEffects.Length==0,"reconnect restores approved raw outcome, never response packets/effects");
        var copy=LifecycleFixture();QueueNative(copy,TrainingAction.Attack);var reply=PacketResponse(copy,Hp(400,40));var prepared=OriginalBattleResponses.Prepare(copy.View.battle.pending,copy.View.battle,reply);reply.packets[0][4]=1;Check(copy.server.ReceiveBattleUnits(prepared).Accepted&&copy.View.battle.Unit(400).hp==40,"prepared notice owns packet byte snapshots");
        var stale=LifecycleFixture();QueueNative(stale,TrainingAction.Attack);var old=PacketResponse(stale,Hp(400,40));Check(stale.server.ReceiveBattleTicks(TickNotice(stale,OriginalBattleTicks.PulsePacket())).Accepted,"independent notice advances old local transaction");Check(!stale.server.ReceiveNativeBattleResponse(old).Accepted&&stale.View.battle.Unit(400).hp==50,"stale native response does not attach to another action");
    }
    private static void CheckNativeResponseManager()
    {
        var f=LifecycleFixture();var manager=new LocalServerManager(f.server);int nativeCalls=0,changed=0;
        manager.NativeBattleAuthority=(r,v)=>{nativeCalls++;Check(r.NativePacket!=null&&r.NativePacket.Length==10,"manager native provider receives original action bytes");return new NativeBattleResponse{id=Id(),request=r.id,approved=true,packets=new[]{Hp(400,40),Ap(1)}};};
        manager.Notify_BattleChanged+=r=>changed++;
        Check(!manager.HasBattleAuthority(false)&&manager.HasBattleAuthorityFor(false,TrainingAction.Attack)&&manager.HasBattleAuthorityFor(false,TrainingAction.Rest)&&!manager.HasBattleAuthorityFor(false,TrainingAction.Move)&&!manager.HasBattleAuthorityFor(false,TrainingAction.CastMagic),"native-only authority exposes supported commands; no unsupported UI actions");
        Check(manager.Request_BattleMemberAttack(1,400).Accepted&&nativeCalls==1&&changed==1&&f.View.battle.Unit(400).hp==40,"real member manager request invokes provider and emits one post-commit notification");
        var typed=LifecycleFixture();var both=new LocalServerManager(typed.server);int typedCalls=0;
        both.NativeBattleAuthority=(r,v)=>throw new Exception("typed authority must win");both.BattleAuthority=(r,v)=>{typedCalls++;return MemberResult(typed);};
        Check(both.Request_BattleMemberAttack(1,400).Accepted&&typedCalls==1,"existing typed authority retains precedence");
        var training=LifecycleFixture();var trainer=new LocalServerManager(training.server);int trainingCalls=0;
        trainer.NativeTrainingAuthority=(r,v)=>{trainingCalls++;return new NativeBattleResponse{id=Id(),request=r.id,approved=true,packets=new[]{Hp(1,50),Ap(1)}};};
        Check(trainer.Request_TrainingMemberRest(1).Accepted&&trainingCalls==1&&training.View.battle.Unit(1).hp==50,"native training override uses training guard and shared atomic path");
        var exception=LifecycleFixture();var broken=new LocalServerManager(exception.server);broken.NativeBattleAuthority=(r,v)=>throw new Exception("fixture authority");string state=State(exception.View.record);
        Check(broken.Request_BattleMemberAttack(1,400).Failure==WorldFailure.RulesUnavailable&&exception.View.battle.pending==null&&State(exception.View.record)==state,"provider exception clears lock, never applies partial packet data");
        var durable=LifecycleFixture(true);var retry=new LocalServerManager(durable.server);NativeBattleResponse retained=null;int committed=0;
        retry.NativeBattleAuthority=(r,v)=>retained=new NativeBattleResponse{id=Id(),request=r.id,approved=true,packets=new[]{Hp(400,40),Ap(1,7,17)}};
        retry.Notify_BattleChanged+=r=>committed++;state=State(durable.View.record);durable.store.fail=true;long revision=durable.View.battle.revision;
        Check(retry.Request_BattleMemberAttack(1,400).Failure==WorldFailure.StorageUnavailable&&durable.View.battle.pending?.id==retained.request&&State(durable.View.record)==state&&committed==0,"synchronous native provider save failure retains request for verbatim retry and emits no commit event");
        durable.store.fail=false;
        Check(retry.Notify_BattleResponse(retained).Accepted&&durable.View.battle.pending==null&&durable.View.battle.revision==revision+1&&durable.View.battle.Unit(400).hp==40&&committed==1,"same native provider response can commit exactly once after storage recovers");
        Check(!retry.Notify_BattleResponse(retained).Accepted&&committed==1,"redelivery after successful retry cannot commit or notify again");
    }
    internal static void RunNativeBattleResponseChecks(string path)
    {
        root=path;foreach(bool scenario in new[]{false,true})foreach(int source in new[]{0,1})CheckNativeResponseAttack(scenario,source);
        foreach(bool scenario in new[]{false,true})foreach(bool healthLast in new[]{false,true})CheckNativeResponseRest(scenario,healthLast);
        CheckNativeResponseNoInventedEffect();CheckNativeResponseFailures();CheckNativeResponseSave();CheckNativeResponseManager();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Atomic basic attack/rest responses from verified raw packet sequences; existing validation/proof/save/retry/ownership contracts, shared HP/MP display order, native menu outcomes, explicit MISS/no invented effects, rejection and reconnect. Local response envelope is not original TCP batching."}));
    }
}
