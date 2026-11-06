using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class BattleLifecycleEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunBattleLifecycleChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static Fixture LifecycleFixture(bool scenario=false,bool near=false)
    {var f=Members(scenario,near);var n=f.Notice();foreach(var u in n.units){u.nativeState=16;u.nativeActive=true;u.nativeEnabled=true;}Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit native lifecycle metadata");return f;}
    private static BattleLifecycleNotice LifecycleNotice(Fixture f,BattleLifecycleKind kind,int unit=1,int unused=0)
        =>new BattleLifecycleNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,unitLife=f.View.battle.Unit(unit)?.life??0,approved=true,packet=OriginalBattleLifecycle.Packet(kind,unit,unused)};
    private static void CheckLifecycleCodec()
    {
        foreach(int unit in new[]{0,1,999,2000,2999})foreach(int unused in new[]{-32768,-1,0,1,32767})
        {var bytes=OriginalBattleLifecycle.Packet(BattleLifecycleKind.Death,unit,unused);Check(OriginalBattleLifecycle.TryDecode(bytes,out var p)&&p.unit==unit&&p.unused==unused&&p.kind==BattleLifecycleKind.Death,"5C07 retains the unused signed WORD without assigning it meaning");}
        foreach(int unit in new[]{0,1,999,2999})Check(OriginalBattleLifecycle.TryDecode(OriginalBattleLifecycle.Packet(BattleLifecycleKind.Remove,unit),out var p)&&p.kind==BattleLifecycleKind.Remove&&p.unit==unit,"5C03 single-unit removal packet");
        foreach(var bytes in new[]{Array.Empty<byte>(),new byte[]{0x5c},new byte[]{0x5c,3,0},new byte[]{0x5c,3,0,0,0},new byte[]{0x5c,7,0,0},new byte[]{0x5c,7,255,255,0,0},new byte[]{0x5c,5,0,0}})Check(!OriginalBattleLifecycle.TryDecode(bytes,out _),"malformed lifecycle application packet rejected");
        Check(OriginalBattleLifecycle.Packet((BattleLifecycleKind)9,0)==null&&OriginalBattleLifecycle.Packet(BattleLifecycleKind.Death,3000)==null&&OriginalBattleLifecycle.Packet(BattleLifecycleKind.Remove,0,1)==null,"strict lifecycle encoder domains");
    }
    private static void CheckDeathPlayback()
    {
        foreach(int job in new[]{100,302,700,799})
        {
            var e=new BattleDeathEffect{unit=0,life=1,job=job};int end=job/100==7?1:19;
            for(int step=0;step<=end;step++)
            {
                Check(e.Counter==(job/100==7?0:step)&&e.Expired==(step==end),"native corpse ending counter "+job);
                var frames=e.Frames(160,160);
                if(step<end)Check(frames.Length==2&&frames[0].atlasY==(step<8?32:step%4)*32&&frames[1].atlasX==step/3*32&&frames[1].width==31,"native corpse row and31px spark source");
                else Check(frames.Length==0,"completed corpse leaves no visible source frame");
                e.Advance(40,0);
            }
        }
        var held=new BattleDeathEffect{unit=1,life=1,job=100,held=true};held.Advance(500,1080);Check(held.Counter==0&&held.held&&held.Frames(0,0).Length==0,"HP/MP display flag40 holds native death update/render");held.Advance(580,580);Check(!held.held&&held.Counter==0,"clearing numeric counter does not also advance corpse in the same update");held.Advance(40,0);Check(held.Counter==1,"next update begins death");held.Advance(1080,1080);Check(held.Counter==1,"new numeric slot pauses an existing corpse without resetting164");
        var audio=new DeathSoundPlayback();Check(audio.Update(new[]{held}).Length==1&&audio.Update(new[]{held}).Length==0,"native death sound1 begins once");held.Advance(10000,0);var cues=audio.Update(new[]{held});Check(cues.Length==1&&cues[0].stop&&cues[0].sound==1,"death sound1 stops at retirement");Check(audio.Update(new[]{held}).Length==0,"completed corpse cannot replay sound");
        Check(new DeathSoundPlayback().Update(new[]{new BattleDeathEffect{unit=0,life=1,job=700,age=40}}).Length==0,"native7xx skips death sound");
        var replaced=new DeathSoundPlayback();Check(replaced.Update(new[]{new BattleDeathEffect{unit=1,life=1,job=100,age=40}}).Length==1,"old lifetime sound starts");
        var replacement=replaced.Update(new[]{new BattleDeathEffect{unit=1,life=2,job=100,age=40}});Check(replacement.Length==2&&replacement[0].stop&&!replacement[1].stop,"old lifetime cleanup precedes new sound1 start; replacement is not cut off");
    }
    private static void CheckLifecycleIngress(bool scenario)
    {
        var f=LifecycleFixture(scenario);var before=f.View.battle.Unit(1);var death=LifecycleNotice(f,BattleLifecycleKind.Death);Check(f.server.ReceiveBattleLifecycle(death).Accepted,"explicit native death notice");
        var actor=f.View.battle.Unit(1);Check(actor.nativeState==144&&actor.hp==before.hp&&actor.mp==before.mp&&actor.ap==before.ap&&!actor.ghost&&!f.View.record.ghost,"death flag alone does not invent HP0, costs, XP or ghost state");
        Check(!OriginalCommandRange.Living(actor)&&f.View.battle.deaths.Single().Counter==0,"dead runtime actor cannot accept new commands");
        Check(!f.server.ReceiveBattleLifecycle(death).Accepted,"same death notice is idempotently rejected");f.server.AdvanceWorld(120);
        Check(f.View.battle.deaths.Single().Counter==3,"corpse updates independently of server health");
        Check(f.server.ReceiveBattleLifecycle(LifecycleNotice(f,BattleLifecycleKind.Death)).Accepted&&f.View.battle.deaths.Single().Counter==3,"second distinct death flag does not reset164");
        Check(f.server.ReceiveBattleLifecycle(LifecycleNotice(f,BattleLifecycleKind.Remove)).Accepted&&f.View.battle.Unit(1).nativeActive==true,"dmKillChar preserves an actor already marked dying");
        f.server.AdvanceWorld(10000);Check(f.View.battle.Unit(1).nativeEnabled==false&&f.View.battle.Unit(1).nativeState==0&&f.View.battle.deaths.Single().Expired,"verified client retirement clears runtime state/enabled, no automatic respawn");
        var other=f.View.battle.Unit(400);Check(f.server.ReceiveBattleLifecycle(LifecycleNotice(f,BattleLifecycleKind.Remove,400)).Accepted&&f.View.battle.Unit(400).nativeActive==false&&f.View.battle.Unit(400).hp==other.hp,"ordinary removal disables active without forging death/damage");
        if(scenario)Check(f.server.ReceiveTrainingLifecycle(LifecycleNotice(f,BattleLifecycleKind.Remove)).Failure==WorldFailure.InvalidStage,"training lifecycle adapter map guard");
        var wait=LifecycleFixture(scenario);Check(wait.server.ReceiveBattleFeedback(FeedbackNotice(wait,BattleFeedbackKind.Health,1,0)).Accepted,"HP0 arrives separately before death");Check(wait.server.ReceiveBattleLifecycle(LifecycleNotice(wait,BattleLifecycleKind.Death)).Accepted&&wait.View.battle.deaths.Single().held,"death held by live HP numeric slot");
        wait.server.AdvanceWorld(1080);Check(wait.View.battle.deaths.Single().Counter==0&&!wait.View.battle.deaths.Single().held&&wait.View.battle.Unit(1).nativeEnabled==true,"numeric27 clears before corpse update starts");wait.server.AdvanceWorld(40);Check(wait.View.battle.deaths.Single().Counter==1,"corpse first update after HP display");
        Check(wait.server.ReceiveBattleFeedback(FeedbackNotice(wait,BattleFeedbackKind.Mana,1,0)).Accepted&&wait.View.battle.deaths.Single().held,"new MP display pauses corpse immediately");wait.server.AdvanceWorld(1080);Check(wait.View.battle.deaths.Single().Counter==1&&!wait.View.battle.deaths.Single().held,"MP slot consumes no corpse updates");wait.server.AdvanceWorld(720);Check(wait.View.battle.Unit(1).nativeEnabled==false&&wait.View.battle.deaths.Single().Expired,"remaining18 updates retire corpse");
    }
    private static void CheckLifecycleOccupancy()
    {
        foreach(bool zeroHp in new[]{false,true})
        {
            var f=LifecycleFixture();var target=f.View.battle.Unit(400);var map=f.definition.Map(f.View.record.map);
            if(zeroHp)Check(f.server.ReceiveBattleFeedback(FeedbackNotice(f,BattleFeedbackKind.Health,400,0)).Accepted,"separate HP0 before retirement occupancy");
            Check(OriginalCommandRange.OccupiesTile(f.View.battle.Unit(400))&&OriginalMemberCommands.Route(map,f.definition.combatCatalog,f.View.battle.units,1,target.x,target.y)==null,"active/enabled corpse tile remains occupied even after HP0");
            Check(f.server.ReceiveBattleLifecycle(LifecycleNotice(f,BattleLifecycleKind.Death,400)).Accepted,"corpse occupies until native retirement");f.server.AdvanceWorld(2000);
            Check(!OriginalCommandRange.OccupiesTile(f.View.battle.Unit(400))&&OriginalMemberCommands.Route(map,f.definition.combatCatalog,f.View.battle.units,1,target.x,target.y)?.Count>0,"retired body cannot leave an invisible movement blocker");
            Check(f.server.RequestBattleMemberAction(1,TrainingAction.Move,-1,target.x,target.y,Id()).Accepted&&f.server.ReceiveBattleUnits(MemberResult(f)).Accepted,"approved walk into retired unit tile, without changing corpse HP");
            Check(f.View.battle.Unit(400).hp==(zeroHp?0:target.hp),"tile retirement is not an implicit HP update");
        }
        var removed=LifecycleFixture();var victim=removed.View.battle.Unit(400);Check(removed.server.ReceiveBattleLifecycle(LifecycleNotice(removed,BattleLifecycleKind.Remove,400)).Accepted&&!OriginalCommandRange.OccupiesTile(removed.View.battle.Unit(400)),"5C03 frees a tile immediately even if HP is positive");
    }
    private static void CheckAppearanceLifecycle()
    {
        var f=LifecycleFixture(true);var n=f.Notice();var unit=n.units.Single(u=>u.id==1);unit.nativeAppearanceJob=701;
        Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.battle.Unit(1).job==100&&OriginalBattleLifecycle.AppearanceKey(f.View.battle.Unit(1))==701,"explicit appearance key does not change class or movement type");
        Check(f.server.ReceiveBattleLifecycle(LifecycleNotice(f,BattleLifecycleKind.Death)).Accepted&&f.View.battle.deaths.Single().LargeJob,"death7xx branch uses resolved appearance key rather than class");
        f.server.AdvanceWorld(40);Check(f.View.battle.Unit(1).nativeEnabled==false&&f.View.battle.Unit(1).job==100,"large appearance retires on first update without class change");
        var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Unit(1).nativeAppearanceJob==701,"received appearance persists independently of class");
        foreach(int invalid in new[]{-1,0,1000}){var g=LifecycleFixture();var bad=g.Notice();bad.units.Single(u=>u.id==1).nativeAppearanceJob=invalid;Check(!g.server.ReceiveBattleUnits(bad).Accepted,"unknown/unresolved appearance cannot create a substitute sprite");}
    }
    private static void CheckLifecycleFailures()
    {
        foreach(string problem in new[]{"session","revision","life","character","id","decline","length"})
        {
            var f=LifecycleFixture();var n=LifecycleNotice(f,BattleLifecycleKind.Death);switch(problem){case "session":n.session=Id();break;case "revision":n.expectedRevision--;break;case "life":n.unitLife++;break;case "character":n.character="bad";break;case "id":n.id="bad";break;case "decline":n.approved=false;break;case "length":n.packet=new byte[]{0x5c,7,1,0};break;}
            string state=State(f.View.record);Check(!f.server.ReceiveBattleLifecycle(n).Accepted&&State(f.View.record)==state&&f.View.battle.deaths.Length==0,"invalid lifecycle notice is atomic "+problem);
        }
        var unknown=Members();var note=LifecycleNotice(unknown,BattleLifecycleKind.Death,0);Check(unknown.server.ReceiveBattleLifecycle(note).Failure==WorldFailure.RulesUnavailable,"unknown native state flags are not manufactured");
        foreach(string problem in new[]{"null","enum","life","not-dead","remove-active","duplicate"})
        {
            var f=LifecycleFixture();var n=f.Notice();var e=new BattleLifecycleEvent{kind=BattleLifecycleKind.Death,unit=1,life=1};n.units.Single(u=>u.id==1).nativeState|=128;n.lifecycleEvents=new[]{e};
            switch(problem){case "null":n.lifecycleEvents=new BattleLifecycleEvent[]{null};break;case "enum":e.kind=(BattleLifecycleKind)9;break;case "life":e.life++;break;case "not-dead":n.units.Single(u=>u.id==1).nativeState=0;break;case "remove-active":e.kind=BattleLifecycleKind.Remove;break;case "duplicate":n.lifecycleEvents=new[]{e,e};break;}
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&f.View.battle.deaths.Length==0,"malformed local lifecycle event "+problem);
        }
        var save=LifecycleFixture(true);var approval=LifecycleNotice(save,BattleLifecycleKind.Death);string before=State(save.View.record);save.store.fail=true;Check(save.server.ReceiveBattleLifecycle(approval).Failure==WorldFailure.StorageUnavailable&&State(save.View.record)==before&&save.View.battle.deaths.Length==0,"failed death save emits no corpse/audio");save.store.fail=false;Check(save.server.ReceiveBattleLifecycle(approval).Accepted,"exact death notice retry after save recovery");
        var restored=new EntryAccountService(save.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(save.definition).Accepted&&restored.WorldSnapshot().battle.Unit(1).nativeState==144&&restored.WorldSnapshot().battle.deaths.Length==0,"reconnect keeps received state but does not replay corpse/audio");
    }
    internal static void RunBattleLifecycleChecks(string path)
    {
        root=path;CheckLifecycleCodec();CheckDeathPlayback();CheckLifecycleIngress(false);CheckLifecycleIngress(true);CheckAppearanceLifecycle();CheckLifecycleOccupancy();CheckLifecycleFailures();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Original5C03 remove/5C07 death ingress, numeric-gated corpse playback, client runtime retirement, sound1 cues, separate HP/ghost/respawn outcomes and atomic save/replay/lifetime guards."}));
    }
}
