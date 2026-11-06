using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class BattleTickEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunBattleTickChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static Fixture TickFixture(bool scenario=false)
    {
        var f=Soldiers(scenario);var n=f.Notice();foreach(var u in n.units){u.nativeActive=true;u.nativeEnabled=true;u.ap=u.id==0?20:u.id==1?5:0;}
        Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit active/enabled native record metadata, never derived from HP");return f;
    }
    private static BattleTickNotice TickNotice(Fixture f,byte[] packet,int unit=1)=>new BattleTickNotice{id=Id(),character=f.View.characterId,session=f.View.campaignSession,expectedRevision=f.View.battle.revision,unitLife=f.View.battle.Unit(unit)?.life??0,approved=true,packet=packet};
    private static void CheckTickCodec()
    {
        foreach(int id in new[]{0,1,999,2000,2999})foreach(int ap in new[]{short.MinValue,-1,0,1,20,short.MaxValue})foreach(int mask in new[]{0,1,16,17,255,256,65535})
        {
            var raw=OriginalBattleTicks.MenuPacket(id,ap,mask);Check(raw.Length==8&&raw[0]==0x5c&&raw[1]==0x52&&OriginalBattleTicks.TryDecode(raw,out var p)&&!p.pulse&&p.unit==id&&p.ap==ap&&p.menu==mask,"native little-endian signed AP/unsigned mask round trip");
        }
        Check(OriginalBattleTicks.TryDecode(OriginalBattleTicks.PulsePacket(),out var pulse)&&pulse.pulse,"native5C04 has no payload");
        foreach(var raw in new[]{Array.Empty<byte>(),new byte[]{0x5c},new byte[]{0x5c,4,0},new byte[]{0,4},new byte[]{0x5c,0x52,0},new byte[]{0x5c,0x52,255,255,0,0,0,0}})Check(!OriginalBattleTicks.TryDecode(raw,out _),"malformed/truncated/wrong-scope packets rejected");
        Check(OriginalBattleTicks.MenuPacket(3000,0,0)==null&&OriginalBattleTicks.MenuPacket(0,32768,0)==null&&OriginalBattleTicks.MenuPacket(0,0,65536)==null,"encoder does not silently truncate invalid inputs");
        Check(OriginalBattleTicks.CommandFlags(1)==2&&OriginalBattleTicks.CommandFlags(2)==16&&OriginalBattleTicks.CommandFlags(4)==8&&OriginalBattleTicks.CommandFlags(8)==32&&OriginalBattleTicks.CommandFlags(16)==1&&OriginalBattleTicks.CommandFlags(32)==4&&OriginalBattleTicks.CommandFlags(255)==255,"native10C bit permutation");
    }
    private static void CheckTickLifecycle(bool scenario)
    {
        var f=TickFixture(scenario);var before=f.View.battle.units.Select(u=>u.Copy()).ToArray();var n=TickNotice(f,OriginalBattleTicks.PulsePacket());
        Check(f.server.ReceiveBattleTicks(n).Accepted&&f.View.battle.Commander.ap==20&&f.View.battle.Unit(1).ap==6&&f.View.battle.Unit(400).ap==1,"global pulse increments active records once, saturates received per-unit maximum");
        Check(f.View.battle.units.All(u=>u.hp==before.Single(x=>x.id==u.id).hp&&u.mp==before.Single(x=>x.id==u.id).mp&&u.Tile==before.Single(x=>x.id==u.id).Tile),"pulse never restores health/mana or moves units");
        Check(!f.server.ReceiveBattleTicks(n).Accepted&&f.View.battle.Unit(1).ap==6,"duplicate pulse cannot increment twice");
        foreach(int value in new[]{-32768,3,32767,3}){var menu=TickNotice(f,OriginalBattleTicks.MenuPacket(1,value,17));Check(f.server.ReceiveBattleTicks(menu).Accepted&&f.View.battle.Unit(1).ap==Math.Max(0,Math.Min(value,20))&&f.View.battle.Unit(1).nativeMenuMask==17&&f.View.battle.Unit(1).nativeState==48,"menu AP is an absolute clamped value, not a cost delta");}
        Check(!f.server.RequestBattleMemberAction(1,TrainingAction.Rest,-1,0,0,Id()).Accepted,"known menu17 denies manual rest while ownership remains intact");
        var injured=f.Notice();injured.units.Single(u=>u.id==1).hp=20;Check(f.server.ReceiveBattleUnits(injured).Accepted,"explicit injured state");
        Check(f.server.ReceiveBattleTicks(TickNotice(f,OriginalBattleTicks.MenuPacket(1,15,1))).Accepted,"native attack-ready bit does not fabricate a manual rest icon");
        Check(f.server.AdvanceBattleAutomation((r,v)=>{Check(r.action==TrainingAction.Rest,"native auto-rest can run through attack-ready branch despite manual rest icon disabled");return SoldierResult(f);},i=>0,0)?.Accepted==true,"automatic permissions and player menu permissions remain separate");
        if(scenario)Check(f.server.ReceiveTrainingTicks(TickNotice(f,OriginalBattleTicks.PulsePacket())).Failure==WorldFailure.InvalidStage,"training tick adapter checks map kind");
    }
    private static void CheckTickUnknownsAndFailures()
    {
        var unknown=Soldiers();string state=State(unknown.View.record);Check(unknown.server.ReceiveBattleTicks(TickNotice(unknown,OriginalBattleTicks.PulsePacket())).Failure==WorldFailure.RulesUnavailable&&State(unknown.View.record)==state,"unknown native activation is not guessed from HP");
        var f=TickFixture();var n=f.Notice();var enemy=n.units.Single(u=>u.id==400);enemy.nativeActive=false;enemy.nativeEnabled=null;Check(f.server.ReceiveBattleUnits(n).Accepted,"inactive records need no guessed enabled flag");
        Check(f.server.ReceiveBattleTicks(TickNotice(f,OriginalBattleTicks.PulsePacket())).Accepted&&f.View.battle.Unit(400).ap==0,"inactive record is not incremented");
        n=f.Notice();n.units.Single(u=>u.id==1).nativeEnabled=false;Check(f.server.ReceiveBattleUnits(n).Accepted,"known disabled record");int ap=f.View.battle.Unit(1).ap;Check(f.server.ReceiveBattleTicks(TickNotice(f,OriginalBattleTicks.PulsePacket())).Accepted&&f.View.battle.Unit(1).ap==ap,"disabled record not incremented");
        foreach(string bad in new[]{"life","session","revision","id","decline","packet"})
        {
            var g=TickFixture();string before=State(g.View.record);var note=TickNotice(g,OriginalBattleTicks.MenuPacket(1,20,255));
            switch(bad){case "life":note.unitLife++;break;case "session":note.session=Id();break;case "revision":note.expectedRevision--;break;case "id":note.id="bad";break;case "decline":note.approved=false;break;case "packet":note.packet=new byte[]{0x5c,0x82};break;}
            Check(!g.server.ReceiveBattleTicks(note).Accepted&&State(g.View.record)==before,"bad tick ingress has no partial effects "+bad);
        }
        var pending=TickFixture();Check(pending.server.RequestBattleMemberAction(1,TrainingAction.Attack,400,0,0,Id()).Accepted,"pending action before independent pulse");var old=SoldierResult(pending);
        Check(pending.server.ReceiveBattleTicks(TickNotice(pending,OriginalBattleTicks.PulsePacket())).Accepted&&pending.View.battle.pending==null&&!pending.server.ReceiveBattleUnits(old).Accepted,"independent revision invalidates old local transaction without applying its damage/cost");
    }
    private static void CheckTickSave()
    {
        var f=TickFixture(true);var n=TickNotice(f,OriginalBattleTicks.MenuPacket(1,7,0));string before=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattleTicks(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle.Unit(1).ap==5&&f.View.battle.Unit(1).nativeMenuMask==null,"failed tick save preserves counter/unknown mask");f.store.fail=false;
        Check(f.server.ReceiveBattleTicks(n).Accepted,"exact tick save retry");var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();
        Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Unit(1).ap==7&&restored.WorldSnapshot().battle.Unit(1).nativeMenuMask==0&&restored.WorldSnapshot().battle.Unit(1).nativeActive==true,"zero menu and activation metadata restore without becoming unknown/default");
    }
    internal static void RunBattleTickChecks(string path)
    {
        root=path;CheckTickCodec();CheckTickLifecycle(false);CheckTickLifecycle(true);CheckTickUnknownsAndFailures();CheckTickSave();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Native5C04 global counter pulse and5C52 absolute AP/menu codec and local authority ingress; explicit runtime activation, signed clamps/bit maps, separate manual/AI permissions, replay/lifetime/atomic save. No guessed period or maximum."}));
    }
}
