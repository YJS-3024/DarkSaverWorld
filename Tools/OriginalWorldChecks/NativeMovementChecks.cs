using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;
internal static class NativeMovementEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunNativeMovementChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static TutorialTile NativeDestination(Fixture f,int source,int minimum=1)
    {
        var actor=f.View.battle.Unit(source);var map=f.definition.Map(f.View.record.map);var blocked=OriginalMemberCommands.StaticBlocked(map);
        for(int radius=1;radius<9;radius++)for(int dx=-radius;dx<=radius;dx++)for(int dy=-radius;dy<=radius;dy++)
        {
            var point=new TutorialTile(actor.x+dx,actor.y+dy);
            if(!map.map.Contains(point)||blocked.Contains(point)||f.View.battle.units.Any(u=>OriginalCommandRange.OccupiesTile(u)&&u.Tile==point))continue;
            var path=OriginalNativeMovement.Path(map.map,f.definition.combatCatalog,f.View.battle.units,source,actor.Tile,point,0);
            if(path?.points.Length>=minimum)return point;
        }
        throw new Exception("No native destination on decoded map");
    }
    private static NativeBattleNotice MovePacket(Fixture f,int source,TutorialTile tile,bool failed=false)=>NativeNotice(f,OriginalNativeMovement.Packet(failed,source,tile.X,tile.Y),source);
    private static void CheckNativeMovementLifecycle(bool scenario,int source)
    {
        var f=LifecycleFixture(scenario);var origin=f.View.battle.Unit(source).Tile;var end=NativeDestination(f,source,2);var actor=f.View.battle.Unit(source).Copy();
        var move=MovePacket(f,source,end);long revision=f.View.battle.revision;
        Check(f.server.ReceiveBattlePacket(move).Accepted&&f.View.battle.Unit(source).Tile==end&&f.View.battle.revision==revision+1,"raw5C01 independent position notice commits once");
        Check(f.View.battle.Unit(source).hp==actor.hp&&f.View.battle.Unit(source).mp==actor.mp&&f.View.battle.Unit(source).ap==actor.ap,"movement has no guessed resource cost or recovery");
        if(source==0)Check(f.View.record.x==end.X&&f.View.renderX==origin.X&&f.View.renderY==origin.Y,"commander persists approved destination while rendering original start");
        else Check(f.View.battle.motions.Single(m=>m.unit==source).x==origin.X,"member presentation starts on original grid");
        f.server.AdvanceWorld(80);var before=f.View;double x=source==0?before.renderX:before.battle.motions.Single(m=>m.unit==source).x;double y=source==0?before.renderY:before.battle.motions.Single(m=>m.unit==source).y;
        Check(f.server.ReceiveBattlePacket(MovePacket(f,source,origin)).Accepted,"second raw move queues latest destination during active leg");
        var after=f.View;Check((source==0?after.renderX:after.battle.motions.Single(m=>m.unit==source).x)==x&&(source==0?after.renderY:after.battle.motions.Single(m=>m.unit==source).y)==y,"requeue preserves partial step without snapping or reversing early");
        f.server.AdvanceWorld(100000);Check(f.View.battle.Unit(source).Tile==origin&&(source==0?!f.View.moving:f.View.battle.motions.All(m=>m.unit!=source)),"queued route reaches latest destination and retires presentation");
        Check(!f.server.ReceiveBattlePacket(move).Accepted,"old movement packet cannot be replayed");
        Check(f.server.ReceiveBattlePacket(MovePacket(f,source,end)).Accepted,"new movement after completion");f.server.AdvanceWorld(80);
        Check(f.server.ReceiveBattlePacket(MovePacket(f,source,origin,true)).Accepted&&f.View.battle.Unit(source).Tile==origin&&(f.View.battle.Unit(source).nativeState.Value&0x21)==0x20,"5C0E snaps to explicit correction and sets queued20, clears moving1");
        Check(source==0?!f.View.moving:f.View.battle.motions.All(m=>m.unit!=source),"move failure clears only the corrected unit motion");
        if(scenario)
        {
            var resumed=new EntryAccountService(f.store);new EntryClient(resumed).OpenLocalProfile();
            Check(resumed.BeginWorld(f.definition).Accepted&&resumed.WorldSnapshot().battle.Unit(source).Tile==origin&&resumed.WorldSnapshot().battle.motions.Length==0&&!resumed.WorldSnapshot().moving,"reconnect restores approved correction without replaying queued animation");
        }
    }
    private static void CheckNativeMovementGuards()
    {
        foreach(bool failed in new[]{false,true})foreach(int unit in new[]{-32768,-1,0,2999,32767})foreach(int coordinate in new[]{-32768,-1,0,199,32767})
        {var bytes=OriginalNativeMovement.Packet(failed,unit,coordinate,coordinate);Check(OriginalNativeMovement.TryDecode(bytes,out var p)&&p.failed==failed&&p.unit==unit&&p.x==coordinate&&p.y==coordinate,"signed raw movement WORD codec preserves original reader values");}
        foreach(int size in new[]{0,2,7,9,24})Check(!OriginalNativeMovement.TryDecode(new byte[size],out _),"strict8-byte movement ingress length");
        var f=LifecycleFixture(true);var end=NativeDestination(f,1);var n=MovePacket(f,1,end);string state=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattlePacket(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==state&&f.View.battle.motions.Length==0,"failed durable move does not start motion or change state");f.store.fail=false;
        Check(f.server.ReceiveBattlePacket(n).Accepted,"same raw movement can retry after storage failure");
        var paused=new EntryAccountService(f.store);new EntryClient(paused).OpenLocalProfile();Check(paused.BeginWorld(f.definition).Accepted&&paused.WorldSnapshot().battle.Unit(1).Tile==end&&paused.WorldSnapshot().battle.motions.Length==0,"approved move reconnects at final tile without resumable flags");
        foreach(string bad in new[]{"life","revision","session","unit","outside","length"})
        {
            var fixture=LifecycleFixture();var notice=MovePacket(fixture,1,NativeDestination(fixture,1));state=State(fixture.View.record);
            if(bad=="life")notice.unitLife++;if(bad=="revision")notice.expectedRevision++;if(bad=="session")notice.session=Id();
            if(bad=="unit")notice.packet=OriginalNativeMovement.Packet(false,2999,1,1);if(bad=="outside")notice.packet=OriginalNativeMovement.Packet(false,1,-1,1000);if(bad=="length")notice.packet=new byte[]{0x5c,1};
            Check(!fixture.server.ReceiveBattlePacket(notice).Accepted&&State(fixture.View.record)==state&&fixture.View.battle.motions.Length==0,"reject invalid move atomically "+bad);
        }
        var inactive=LifecycleFixture();var update=inactive.Notice();update.units.Single(u=>u.id==1).nativeActive=false;Check(inactive.server.ReceiveBattleUnits(update).Accepted,"explicit inactive actor");
        var old=inactive.View.battle.Unit(1).Tile;Check(inactive.server.ReceiveBattlePacket(MovePacket(inactive,1,new TutorialTile(2,2))).Accepted&&inactive.View.battle.Unit(1).Tile==old&&inactive.View.battle.motions.Length==0,"MoveChar ignores inactive actor");
        var zero=LifecycleFixture();old=zero.View.battle.Unit(1).Tile;Check(zero.server.ReceiveBattlePacket(MovePacket(zero,1,new TutorialTile(0,0))).Accepted&&zero.View.battle.Unit(1).Tile==old,"MoveChar ignores both coordinates below1");
        var forged=LifecycleFixture();var raw=MovePacket(forged,1,NativeDestination(forged,1));var proof=forged.Notice();proof.nativeMovement=raw;proof.units.Single(u=>u.id==1).x++;
        Check(!forged.server.ReceiveBattleUnits(proof).Accepted,"movement proof cannot authorize an arbitrary snapshot");
        var unknown=LifecycleFixture();update=unknown.Notice();update.units.Single(u=>u.id==1).movementBudget=null;Check(unknown.server.ReceiveBattleUnits(update).Accepted,"unknown budget is retained as unknown");
        Check(!unknown.server.ReceiveBattlePacket(MovePacket(unknown,1,new TutorialTile(2,2))).Accepted,"unknown native path inputs are not treated as unreachable teleport");
        var menu=LifecycleFixture();update=menu.Notice();update.units.Single(u=>u.id==1).nativeMenuMask=0;Check(menu.server.ReceiveBattleUnits(update).Accepted,"known disabled action mask");
        Check(menu.server.ReceiveBattlePacket(MovePacket(menu,1,menu.View.battle.Unit(1).Tile,true)).Accepted&&menu.View.battle.Unit(1).nativeMenuMask==16,"MoveFail native10C OR1 restores raw menu movement bit16 without enabling unrelated actions");
        var palette=LifecycleFixture(true);update=palette.Notice();update.units.Single(u=>u.id==1).nativePalette=3;
        Check(palette.server.ReceiveBattleUnits(update).Accepted&&palette.View.battle.Unit(1).nativePalette==3,"explicit resolved client palette is accepted without guessing team relationships");
        var restored=new DarkSaver.Original.Entry.EntryAccountService(palette.store);new DarkSaver.Original.Entry.EntryClient(restored).OpenLocalProfile();
        Check(restored.BeginWorld(palette.definition).Accepted&&restored.WorldSnapshot().battle.Unit(1).nativePalette==3,"palette metadata survives scenario save/reconnect without replaying effects");
        foreach(int invalid in new[]{-1,4}){update=palette.Notice();update.units[0].nativePalette=invalid;Check(!palette.server.ReceiveBattleUnits(update).Accepted,"invalid palette does not enter a saved unit");}
        Check(NativeBodyPalette.Resolve(100,true,null,null,null,null)==null&&NativeBodyPalette.Resolve(100,true,0,null,null,null)==null,"unknown team/force affinity stays unknown");
        var flying=LifecycleFixture();var terrain=flying.definition.Map(flying.View.record.map).map;int attr=terrain.Attribute(flying.View.battle.Commander.Tile);terrain.terrain[attr*30+2]=99;terrain.terrain[attr*30+5]=1;
        update=flying.Notice();update.units[0].movementType=1;Check(flying.server.ReceiveBattleUnits(update).Accepted,"commander snapshot uses explicit movement kind rather than infantry terrain column");
        Check(flying.server.ReceiveBattlePacket(MovePacket(flying,0,flying.View.battle.Commander.Tile,true)).Accepted,"flying commander correction is not rejected by unrelated infantry water restrictions");
    }
    internal static void RunNativeMovementChecks(string path)
    {
        root=path;foreach(bool scenario in new[]{false,true})foreach(int source in new[]{0,1,400})CheckNativeMovementLifecycle(scenario,source);CheckNativeMovementGuards();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Native5C01/0E signed codec, original full backtrack/compression, atomic movement/correction with latest destination at next grid boundary, resource/replay/save/reconnect guards. Persistence retains approved final coordinates, not original server storage."}));
    }
}
