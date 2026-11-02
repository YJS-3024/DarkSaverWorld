using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class SoldierActionEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunSoldierActionChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static Fixture Soldiers(bool scenario=false,int mode=2,int hp=50,uint state=16)
    {
        var f=Members(scenario,true);var n=f.Notice();n.units[0].troopOrder=mode;n.units[0].commandRange=3;var actor=n.units.Single(u=>u.id==1);actor.hp=hp;actor.nativeState=state;
        Check(f.server.ReceiveBattleUnits(n).Accepted,"received live AI source/parent/range/target context");return f;
    }
    private static TrainingUnitsNotice SoldierResult(Fixture f)
    {
        var r=f.View.battle.pending;var n=MemberResult(f);var actor=n.units.Single(u=>u.id==r.source);actor.nativeState=r.aiStateAfter;
        if(r.action==TrainingAction.SoldierMove){actor.x=r.x;actor.y=r.y;}
        else actor.ap=0;return n;
    }
    private static void CheckSoldierActions(bool scenario)
    {
        var f=Soldiers(scenario);string before=State(f.View.record);int calls=0;
        Check(f.server.ExecuteBattleSoldierAction(1,null).Failure==WorldFailure.RulesUnavailable&&State(f.View.record)==before,"missing AI authority changes nothing");
        Check(f.server.RequestBattleSoldierAction(1,Id(),n=>{calls++;return 0;},0).Accepted,"native ready soldier attack queued");var r=f.View.battle.pending;
        Check(r.automatic&&r.action==TrainingAction.Attack&&r.source==1&&r.target==400&&r.nativeOrder==2&&r.attackCandidates.Length==1&&r.attackCandidates[0].life==r.targetLife&&calls==1,"auto attack captures mode/identity/candidate draw once");
        Check(State(f.View.record)==before&&f.View.battle.Unit(1).ap==20&&f.View.battle.Unit(1).nativeState==16,"AI decision does not predict resource/control outcome");
        r.attackCandidates[0].id=999;Check(f.View.battle.pending.attackCandidates[0].id==400,"candidate snapshots are independent");
        var notice=SoldierResult(f);Check(f.server.ReceiveBattleUnits(notice).Accepted&&f.View.battle.Unit(1).ap==0&&f.View.battle.Unit(1).nativeState==0&&f.View.battle.Unit(400).hp==40,"native AP zero/ready-bit clear with explicit target damage");
        Check(!f.server.ReceiveBattleUnits(notice).Accepted,"duplicate AI result does not hit twice");
        Check(f.server.AdvanceBattleAutomation((q,v)=>throw new Exception(),n=>0,0)==null,"spent AP does not run another action or recharge itself");
        foreach(var entry in new[]{(2,29),(4,19),(8,44)})
        {
            var rest=Soldiers(scenario,entry.Item1,entry.Item2);Check(rest.server.AdvanceBattleAutomation((q,v)=>SoldierResult(rest),n=>throw new Exception("rest must not draw a target"),0)?.Accepted==true,"native HP threshold selects rest "+entry.Item1);
            Check(rest.View.battle.Unit(1).hp==50&&rest.View.battle.Unit(1).ap==0&&rest.View.battle.Unit(400).hp==50,"rest uses approved health and native AP reset");
        }
        var idle=Soldiers(scenario,state:0);before=State(idle.View.record);int sends=0;
        Check(idle.server.AdvanceBattleAutomation((q,v)=>{sends++;return SoldierResult(idle);},n=>0,0)==null&&idle.View.battle.Unit(1).nativeState==48&&idle.View.battle.Unit(1).ap==20&&State(idle.View.record)==before&&sends==0,"full AP prepares client flags on a no-packet tick without writing save/health/AP");
        calls=0;Check(idle.server.AdvanceBattleAutomation((q,v)=>SoldierResult(idle),n=>{calls++;return 0;},0)?.Accepted==true&&calls==2,"queued movement inside range probes then draws the actual target separately");
        if(scenario)Check(f.server.ExecuteTrainingSoldierAction(1,(q,v)=>SoldierResult(f)).Failure==WorldFailure.InvalidStage,"training AI entry point cannot run in scenario");
    }
    private static void CheckSoldierMovementAndHint()
    {
        var moving=RegroupFixture();Check(moving.server.AdvanceBattleAutomation((q,v)=>SoldierResult(moving),n=>0,0)?.Accepted==true,"automatic move uses full native GetMovePos pipeline");
        Check(moving.View.battle.motions.Length==1&&!moving.View.moving&&moving.View.battle.Unit(1).nativeState==0,"approved automatic movement animates without commander input lock");
        int sends=0;Check(moving.server.AdvanceBattleAutomation((q,v)=>{sends++;return SoldierResult(moving);},n=>0,0)==null&&sends==0,"moving member has no duplicate automated request");moving.server.AdvanceWorld(100000);
        var hint=Soldiers(mode:2,hp:40,state:48);var n=hint.Notice();var actor=n.units.Single(u=>u.id==1);actor.attackTargets=Array.Empty<int>();actor.movementBudget=0;Check(hint.server.ReceiveBattleUnits(n).Accepted,"known no-enemy/no-movement context");string before=State(hint.View.record);
        Check(hint.server.AdvanceBattleAutomation((q,v)=>throw new Exception(),k=>0,0)==null&&hint.View.battle.Unit(1).nativeState==0&&State(hint.View.record)==before,"native failed attack-move sets rest hint and clears client movement flags, no packet");
        Check(hint.server.AdvanceBattleAutomation((q,v)=>throw new Exception(),k=>0,0)==null&&hint.View.battle.Unit(1).nativeState==48,"next full-AP tick prepares queued flags");
        Check(hint.server.AdvanceBattleAutomation((q,v)=>{Check(q.action==TrainingAction.Rest&&q.aiHintBefore==99&&q.aiHintAfter==-1,"client-local forced-rest hint consumed before normal attack threshold");return SoldierResult(hint);},k=>0,0)?.Accepted==true,"forced rest works above normal60 percent threshold");
        Check(hint.View.battle.Unit(1).hp==50&&hint.View.battle.Unit(1).ap==0,"forced rest still requires explicit approved health");
    }
    private static void CheckSoldierRejections()
    {
        foreach(string bad in new[]{"ap","flags","hp","commander","mode","candidate-permission","xp-revision","request","session","decline"})
        {
            var f=Soldiers();string before=State(f.View.record);Check(f.server.RequestBattleSoldierAction(1,Id(),n=>0,0).Accepted,"queue malformed AI result");var n=SoldierResult(f);var actor=n.units.Single(u=>u.id==1);
            switch(bad){case "ap":actor.ap=1;break;case "flags":actor.nativeState=16;break;case "hp":actor.hp--;break;case "commander":n.units[0].ap--;break;case "mode":n.units[0].troopOrder=8;break;case "candidate-permission":actor.attackTargets=Array.Empty<int>();break;case "xp-revision":n.experience=456;n.expectedGrowthRevision=99;break;case "request":n.request=Id();break;case "session":n.session=Id();break;case "decline":n.approved=false;break;}
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==before&&f.View.battle.Unit(1).ap==20,"malformed AI approval no side effects "+bad);
        }
        var unknown=Soldiers();var changed=unknown.Notice();changed.units.Single(u=>u.id==1).attackTargets=null;Check(unknown.server.ReceiveBattleUnits(changed).Accepted,"unknown enemy eligibility");string record=State(unknown.View.record);
        Check(unknown.server.AdvanceBattleAutomation((q,v)=>throw new Exception(),n=>0,0)==null&&unknown.View.battle.Unit(1).nativeState==16&&State(unknown.View.record)==record,"unknown enemies are not an empty list, rest, move, or control-flag mutation");
        var direct=Soldiers(mode:1);Check(direct.server.AdvanceBattleAutomation((q,v)=>throw new Exception(),n=>0,0)==null&&direct.View.battle.Unit(1).nativeState==16,"direct mode is not auto-controlled");
        var rng=Soldiers();Check(rng.server.AdvanceBattleAutomation((q,v)=>throw new Exception(),n=>n,0)==null&&rng.View.battle.pending==null&&rng.View.battle.Unit(1).nativeState==16,"invalid selector cannot silently pick an enemy or clear state");
        var fault=Soldiers();int requests=0;Check(fault.server.AdvanceBattleAutomation((q,v)=>{requests++;throw new Exception();},n=>0,0)?.Failure==WorldFailure.RulesUnavailable&&fault.View.battle.pending==null,"failed auto authority releases input");
        Check(fault.server.AdvanceBattleAutomation((q,v)=>{requests++;return SoldierResult(fault);},n=>0,0)==null&&requests==1,"failed decision not repeated every frame");Check(fault.server.ReceiveBattleUnits(fault.Notice()).Accepted,"new revision unlocks another decision");Check(fault.server.AdvanceBattleAutomation((q,v)=>SoldierResult(fault),n=>0,0)?.Accepted==true,"AI can retry after explicit update");
        foreach(int source in new[]{0,10,11,400}){var f=Soldiers();Check(!f.server.RequestBattleSoldierAction(source,Id(),n=>0,0).Accepted,"AI cannot control another commander/player "+source);}
        var generic=Soldiers();Check(generic.server.RequestBattleAction(TrainingAction.SoldierMove,-1,0,0,Id()).Failure==WorldFailure.InvalidInput,"generic actions cannot bypass native AI move planner");
    }
    private static void CheckSoldierSave()
    {
        var f=Soldiers(true);Check(f.server.RequestBattleSoldierAction(1,Id(),n=>0,0).Accepted,"scenario auto action");var n=SoldierResult(f);string before=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattleUnits(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle.Unit(1).ap==20&&f.View.battle.Unit(400).hp==50,"failed AI save preserves source/enemy/request");f.store.fail=false;Check(f.server.ReceiveBattleUnits(n).Accepted,"exact AI result save retry");
        var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Unit(1).ap==0&&restored.WorldSnapshot().battle.Unit(400).hp==40&&restored.WorldSnapshot().battle.pending==null,"reconnect restores approved AI result without repeated attack");
    }
    internal static void RunSoldierActionChecks(string path)
    {
        root=path;CheckSoldierActions(false);CheckSoldierActions(true);CheckSoldierMovementAndHint();CheckSoldierRejections();CheckSoldierSave();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Actual automatic source-aware requests/atomic approvals/AI AP and flag postconditions, client-only no-packet state, queued target draws, forced-rest hint and movement, failures and resume; native planner independently differential-checked."}));
    }
}
