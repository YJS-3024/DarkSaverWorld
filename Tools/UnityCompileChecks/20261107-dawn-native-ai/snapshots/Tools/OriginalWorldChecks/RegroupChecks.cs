using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;

internal static class RegroupEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunRegroupChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static void CheckRegroupPlanner()
    {
        var terrain=new int[810];for(int i=2;i<810;i+=3)terrain[i]=1;
        var map=new TutorialMap{width=12,height=12,attributes=new int[144],terrain=terrain,abilities=Array.Empty<TutorialAbility>()};
        var catalog=new OriginalCombatCatalog{version=3,jobs=new[]{new OriginalCombatJob{job=100,rawMovementType=0}}};
        var leader=new TrainingUnit{id=0,job=100,life=1,x=5,y=5,hp=100,maxHp=100,canCommand=true,commandRange=3,troopOrder=8};
        var actor=new TrainingUnit{id=1,job=100,life=2,x=1,y=5,hp=10,maxHp=10,ap=20,maxAp=20,canMove=true,movementBudget=6,nativeState=0x20};
        var units=new[]{leader,actor};var blocked=new HashSet<TutorialTile>();
        Func<BattleRegroupPlan> plan=()=>OriginalRegroupMovement.Plan(map,catalog,units,actor.id,blocked);
        var p=plan();Check(p!=null&&p.x==5&&p.y==4,"native nearest tie uses MINIMUM remaining score, then X/Y scan; not cheapest west neighbor");
        Check(p.member==1&&p.memberLife==2&&p.commanderLife==1&&p.path.Last()==new TutorialTile(5,4),"source/parent identity and compressed path captured");
        blocked.Add(new TutorialTile(5,4));p=plan();Check(p!=null&&p.x==5&&p.y==6,"blocked facility/portal cell excluded, next native tie retained");blocked.Clear();
        foreach(int mode in new[]{2,4,8}){leader.troopOrder=mode;Check(plan()!=null,"outside search radius native pure return mode "+mode);}
        leader.troopOrder=1;Check(plan()==null,"direct mode does not wander automatically");leader.troopOrder=8;
        foreach(uint state in new uint[]{1,0x10,0x11,0x21,0x80}){actor.nativeState=state;Check(plan()==null,"moving/action/ghost native state blocked "+state);}
        actor.nativeState=0;actor.ap=19;Check(plan()==null,"idle soldier waits for full AP without local recharge");actor.ap=20;Check(plan()!=null,"native full-AP idle branch eligible");
        actor.nativeState=0x20;actor.ap=0;Check(plan()!=null,"queued native movement branch does not invent a second AP threshold");
        actor.nativeState=null;Check(plan()==null,"unknown native AI flags are not defaulted");actor.nativeState=0x20;
        actor.movementBudget=null;Check(plan()==null,"unknown movement budget withheld");actor.movementBudget=6;
        leader.canCommand=null;Check(plan()==null,"unknown control permission withheld");leader.canCommand=true;
        catalog.version=2;Check(plan()==null,"old catalog without explicit movement type does not invent infantry");actor.movementType=0;Check(plan()!=null,"explicit movement type works with legacy catalog");catalog.version=3;actor.movementType=null;
        actor.movementType=10;Check(plan()==null,"invalid movement category rejected");actor.movementType=null;
        actor.id=9;Check(plan()!=null,"ninth member included");actor.id=11;leader.id=10;Check(plan()==null,"another player's army is never auto-controlled");actor.id=1;leader.id=0;
        actor.x=3;leader.commandRange=1;leader.troopOrder=2;actor.hp=10;
        Check(plan()==null,"GetMovePos attack-mode branch inside radius2 is not replaced with pure follow");actor.hp=4;Check(plan()!=null,"below half HP takes verified nearest-position branch");
        actor.hp=10;leader.troopOrder=8;Check(plan()!=null,"native defense mode3 uses nearest helper regardless of attack-mode branch");
        actor.hp=0;Check(plan()==null,"dead follower does not request movement");actor.hp=10;leader.hp=0;Check(plan()==null,"dead leader does not generate invented wandering fallback");leader.hp=100;
        actor.x=1;leader.commandRange=3;actor.movementType=3;for(int i=11;i<810;i+=30)terrain[i]=99;
        Check(plan()==null,"naval movement uses its own blocked terrain column");actor.movementType=1;Check(plan()!=null,"another movement category remains traversable");
        actor.movementType=0;
        var blocker=new TrainingUnit{id=400,x=2,y=5,hp=10};units=new[]{leader,actor,blocker};
        for(int y=0;y<12;y++)for(int x=0;x<12;x++)if(y!=5)blocked.Add(new TutorialTile(x,y));
        Check(plan()==null,"enemy at corridor blocks native group/50 traversal");blocker.id=2;
        Check(plan()!=null,"same group/50 ally can be traversed but not selected as destination");
    }
    private static Fixture RegroupFixture(bool scenario=false)
    {
        var f=new Fixture(scenario:scenario);var notice=f.Notice();var leader=notice.units[0];leader.canCommand=true;leader.commandRange=3;leader.troopOrder=8;
        var map=f.definition.Map(f.View.record.map);var blocked=new HashSet<TutorialTile>(map.npcs.Select(n=>new TutorialTile(n.x,n.y)));
        foreach(var portal in map.portals)foreach(var c in portal.cells)blocked.Add(c.Tile);
        TrainingUnit member=null;
        for(int x=1;x<map.map.width&&member==null;x++)for(int y=1;y<map.map.height&&member==null;y++)
        {
            var candidate=f.Unit(1,100,x,y);candidate.nativeState=0x20;candidate.movementBudget=6;
            if(map.map.Terrain(candidate.Tile,2)<=0||map.map.Terrain(candidate.Tile,2)==99||blocked.Contains(candidate.Tile)||notice.units.Any(u=>u.Tile==candidate.Tile))continue;
            if(OriginalRegroupMovement.Plan(map.map,f.definition.combatCatalog,notice.units.Concat(new[]{candidate}).ToArray(),1,blocked)!=null)member=candidate;
        }
        Check(member!=null,"find return route on actual decoded training/scenario terrain, no map override");notice.units=notice.units.Concat(new[]{member}).ToArray();
        Check(f.server.ReceiveBattleUnits(notice).Accepted,"explicit owned army/range/AP/AI flags");return f;
    }
    private static TrainingUnitsNotice RegroupResult(Fixture f)
    {
        var request=f.View.battle.pending;var notice=f.Notice();notice.request=request.id;var member=notice.units.Single(u=>u.id==request.source);
        member.x=request.x;member.y=request.y;member.direction=TutorialGeometry.FacingRow(request.movePath.Last().X-request.movePath.First().X,request.movePath.Last().Y-request.movePath.First().Y);
        member.nativeState&=~0x30u;member.ap=7;return notice; // Explicit authority output, never a production AP formula.
    }
    private static void CheckRegroupLifecycle(bool scenario)
    {
        var f=RegroupFixture(scenario);var before=f.View.battle.Unit(1).Copy();string record=State(f.View.record);int calls=0;
        if(scenario)Check(f.server.ExecuteTrainingRegroup(1,(r,v)=>RegroupResult(f)).Failure==WorldFailure.InvalidStage&&State(f.View.record)==record,"training-specific wrapper cannot run in a scenario");
        Check(f.server.AdvanceBattleRegroup(null)==null&&State(f.View.record)==record&&f.View.battle.pending==null,"missing authority produces no predictive move/request");
        Check(f.server.RequestBattleRegroup(1,Id()).Accepted,"queue source-aware return request");var request=f.View.battle.pending;
        Check(request.action==TrainingAction.Regroup&&request.source==1&&request.target==0&&request.sourceLife==before.life&&request.movePath.Length>0&&State(f.View.record)==record,"request records path/lives without moving or spending AP");
        var path=request.movePath.ToArray();request.movePath[0]=new TutorialTile(999,999);Check(f.View.battle.pending.movePath.SequenceEqual(path),"returned pending path deeply copied");
        var n=RegroupResult(f);Check(f.server.ReceiveBattleUnits(n).Accepted,"approved return movement saved atomically");
        Check(f.View.record.x==f.View.battle.Commander.x&&f.View.record.y==f.View.battle.Commander.y&&f.View.battle.Unit(1).ap==7,"source member updated with explicit AP, commander not moved");
        var motion=f.View.battle.motions.Single();Check(motion.unit==1&&motion.x==before.x&&motion.y==before.y,"approved member begins at original render cell, no teleport");
        f.server.AdvanceWorld(80);var mid=f.View.battle.motions.Single();Check(mid.x!=motion.x||mid.y!=motion.y,"member actually advances along native quantized path");
        Check(!f.View.moving,"member animation does not lock commander movement or world inputs");
        Check(f.server.RequestBattleRegroup(1,Id()).Failure==WorldFailure.Busy,"in-motion member cannot be queued again");
        Check(f.server.AdvanceBattleRegroup((r,v)=>{calls++;return RegroupResult(f);})==null&&calls==0,"automatic pump skips an already moving member");
        f.server.AdvanceWorld(100000);Check(f.View.battle.motions.Length==0,"finished movement removed from render state");
        Check(!f.server.ReceiveBattleUnits(n).Accepted,"duplicate return approval rejected");
        Check(f.View.record.inventory.Length==0&&f.View.record.growth.experience==123,"return does not generate items/XP");
        var auto=RegroupFixture(scenario);Check(auto.server.AdvanceBattleRegroup((r,v)=>{calls++;return RegroupResult(auto);})?.Accepted==true,"automatic queue dispatches to existing authority without manual UI input");
        var failed=RegroupFixture(scenario);int attempts=0;
        Check(failed.server.AdvanceBattleRegroup((r,v)=>{attempts++;throw new Exception();})?.Failure==WorldFailure.RulesUnavailable&&failed.View.battle.pending==null,"authority exception releases queue");
        Check(failed.server.AdvanceBattleRegroup((r,v)=>{attempts++;return RegroupResult(failed);})==null&&attempts==1,"failed AI decision is not spammed every frame");
        Check(failed.server.ReceiveBattleUnits(failed.Notice()).Accepted,"new authoritative revision resets decision attempts");
        Check(failed.server.AdvanceBattleRegroup((r,v)=>RegroupResult(failed))?.Accepted==true,"retry after authoritative update");
    }
    private static void CheckRegroupRejections()
    {
        foreach(string bad in new[]{"target","commander-x","commander-ap","member-hp","member-mp","life","range","mode","state","movement-type","extra-unit","missing-unit","experience","session","revision","request","decline"})
        {
            var f=RegroupFixture();string record=State(f.View.record);Check(f.server.RequestBattleRegroup(1,Id()).Accepted,"queue malformed outcome fixture");var n=RegroupResult(f);var unit=n.units.Single(u=>u.id==1);
            switch(bad)
            {
                case "target":unit.x++;break;case "commander-x":n.units[0].x--;break;case "commander-ap":n.units[0].ap--;break;
                case "member-hp":unit.hp--;break;case "member-mp":unit.mp++;unit.maxMp++;break;case "life":unit.life++;break;
                case "range":n.units[0].commandRange++;break;case "mode":n.units[0].troopOrder=2;break;case "state":unit.nativeState=0x80;break;
                case "movement-type":unit.movementType=10;break;
                case "extra-unit":n.units=n.units.Concat(new[]{f.Unit(2,100,unit.x,unit.y+1)}).ToArray();break;case "missing-unit":n.units=n.units.Where(u=>u.id!=400).ToArray();break;
                case "experience":n.experience=999;n.expectedGrowthRevision=f.View.record.growth.revision;break;
                case "session":n.session=Id();break;case "revision":n.expectedRevision--;break;case "request":n.request=Id();break;case "decline":n.approved=false;break;
            }
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==record&&f.View.battle.motions.Length==0,"reject mixed/stale movement without side effects "+bad);
        }
        var regular=RegroupFixture();Check(regular.server.RequestBattleAction(TrainingAction.Regroup,0,0,0,Id()).Failure==WorldFailure.InvalidInput,"generic commander action cannot bypass member planner");
        foreach(int id in new[]{0,10,11,400,-1,999})Check(regular.server.RequestBattleRegroup(id,Id()).Failure==WorldFailure.RulesUnavailable,"source outside controlled group rejected "+id);
        var cancel=RegroupFixture();Check(cancel.server.RequestBattleRegroup(1,Id()).Accepted,"pending return before unsolicited correction");var pending=cancel.View.battle.pending.id;
        Check(cancel.server.ReceiveBattleUnits(cancel.Notice()).Accepted&&cancel.View.battle.pending==null,"independent snapshot invalidates pending decision");
        Check(cancel.server.CancelBattleAction(pending).Failure==WorldFailure.StaleRequest,"old pending ID cannot be canceled/replayed");
    }
    private static void CheckRegroupSaveAndInterrupt()
    {
        var f=RegroupFixture(true);Check(f.server.RequestBattleRegroup(1,Id()).Accepted,"scenario movement request");var n=RegroupResult(f);string record=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattleUnits(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==record&&f.View.battle.motions.Length==0&&f.View.battle.pending!=null,"failed disk write preserves unit/pending and starts no movement");f.store.fail=false;
        Check(f.server.ReceiveBattleUnits(n).Accepted,"exact failed-save approval retry");var destination=f.View.battle.Unit(1).Tile;
        var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Unit(1).Tile==destination&&restored.WorldSnapshot().battle.Unit(1).ap==7&&restored.WorldSnapshot().battle.motions.Length==0&&restored.WorldSnapshot().battle.pending==null,"reconnect keeps approved destination/AP without replaying motion/request");
        var death=f.Notice();death.units.Single(u=>u.id==1).hp=0;Check(f.server.ReceiveBattleUnits(death).Accepted&&f.View.battle.motions.Length==0,"death cancels stale movement animation");
        var vanish=RegroupFixture();Check(vanish.server.ExecuteBattleRegroup(1,(r,v)=>RegroupResult(vanish)).Accepted,"moving removal fixture");var removed=vanish.Notice();removed.units=removed.units.Where(u=>u.id!=1).ToArray();
        Check(vanish.server.ReceiveBattleUnits(removed).Accepted&&vanish.View.battle.motions.Length==0,"removed member leaves no stale body motion");
        var training=RegroupFixture();Check(training.server.ExecuteTrainingRegroup(1,(r,v)=>RegroupResult(training)).Accepted,"training wrapper return accepted");restored=new EntryAccountService(training.store);new EntryClient(restored).OpenLocalProfile();
        Check(restored.BeginWorld(training.definition).Accepted&&restored.WorldSnapshot().record.map==1000&&restored.WorldSnapshot().battle==null,"training interruption still returns to entrance, no resumable invented battle");
    }
    internal static void RunRegroupChecks(string path)
    {
        root=path;CheckRegroupPlanner();CheckRegroupLifecycle(false);CheckRegroupLifecycle(true);CheckRegroupRejections();CheckRegroupSaveAndInterrupt();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Native verified pure regroup branch, remaining-score ties/group50 terrain traversal, source-aware authority request/atomic approval, automatic pump, rendered motion snapshots and interruption. Separate combat-target MoveAttack/inside-defense AI not substituted."}));
    }
}
