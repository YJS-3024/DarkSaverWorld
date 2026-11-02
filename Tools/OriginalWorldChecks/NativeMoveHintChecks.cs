using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;
internal static class NativeMoveHintEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunNativeMoveHintChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static void CheckMoveHintCodec()
    {
        foreach(int source in new[]{0,1,999,2999})foreach(int count in new[]{1,2,3,4,5})
        {
            var points=Enumerable.Range(0,5).Select(i=>i<count?new TutorialTile(10-i,20):new TutorialTile(-1,-1)).ToArray();var raw=OriginalNativeMoveHints.Packet(source,points);
            Check(raw.Length==24&&raw[0]==0x5c&&raw[1]==0x43&&OriginalNativeMoveHints.TryDecode(raw,out int decoded,out var p)&&decoded==source&&p.SequenceEqual(points),"fixed24-byte native543, exactly five signed pairs including sentinel padding");
            Check(OriginalBattleRequests.TryDecode(raw,out var generic)&&generic.action==TrainingAction.Move&&generic.source==source&&generic.x==10&&generic.y==20&&generic.movePoints.SequenceEqual(points),"common outgoing decoder preserves movement hints, not a path count");
        }
        foreach(var points in new[]{new[]{new TutorialTile(-1,-1),new TutorialTile(-1,-1),new TutorialTile(-1,-1),new TutorialTile(-1,-1),new TutorialTile(-1,-1)},new[]{new TutorialTile(5,5),new TutorialTile(-1,-1),new TutorialTile(4,5),new TutorialTile(-1,-1),new TutorialTile(-1,-1)},new[]{new TutorialTile(5,5),new TutorialTile(-1,5),new TutorialTile(-1,-1),new TutorialTile(-1,-1),new TutorialTile(-1,-1)}})
            Check(OriginalNativeMoveHints.Packet(1,points)==null,"invalid/mixed/trailing-after-sentinel pairs are not encoded");
        Check(!OriginalNativeMoveHints.TryDecode(new byte[23],out _,out _)&&!OriginalNativeMoveHints.TryDecode(new byte[25],out _,out _),"strict movement application length");
    }
    private static void CheckMoveHintIntegration(bool scenario)
    {
        var f=Members(scenario);var map=f.definition.Map(f.View.record.map);var dest=MemberDestination(f);string before=State(f.View.record);QueueNativeMove(f,1,dest);
        var r=f.View.battle.pending;Check(r.nativeMovePoints?.Length==5&&r.nativeMovePoints[0]==dest&&r.NativePacket?.Length==24&&State(f.View.record)==before,"source-aware movement queues native hints without predicting AP/HP/position or storing phase");
        var expected=OriginalNativeMoveHints.Calculate(map.map,f.definition.combatCatalog,f.View.battle.units,1,dest.X,dest.Y,r.nativeMoveAxisBefore);
        Check(expected!=null&&expected.points.SequenceEqual(r.nativeMovePoints)&&expected.axisAfter==r.nativeMoveAxisAfter,"prepared request matches original score/backtrack and captured phase");
        r.nativeMovePoints[0]=new TutorialTile(999,999);Check(f.View.battle.pending.nativeMovePoints[0]==dest,"native point snapshots are isolated");
        int after=f.View.battle.pending.nativeMoveAxisAfter;Check(f.server.CancelBattleAction(f.View.battle.pending.id).Accepted,"cancel pending move without starting animation");QueueNativeMove(f,1,dest);
        Check(f.View.battle.pending.nativeMoveAxisBefore==after,"DLL-local axis advances on preparation, not after a server approval");
        Check(f.server.ReceiveBattleUnits(MemberResult(f)).Accepted&&f.View.battle.Unit(1).Tile==dest,"existing approved move contract and animation preserved");
        var auto=RegroupFixture(scenario);before=State(auto.View.record);
        Check(auto.server.RequestBattleRegroup(1,Id()).Accepted,"actual decoded terrain has an approved regroup candidate");
        Check(auto.View.battle.pending.action==TrainingAction.Regroup&&auto.View.battle.pending.NativePacket?.Length==24&&State(auto.View.record)==before,"automatic regroup exposes native movement bytes without changing saved state");
        Check(auto.server.ReceiveBattleUnits(RegroupResult(auto)).Accepted,"regroup hint does not replace existing approval");
        var soldier=RegroupFixture(scenario);before=State(soldier.View.record);
        Check(soldier.server.RequestBattleSoldierAction(1,Id(),n=>0,0).Accepted,"full soldier AI queues native movement on actual terrain");
        Check(soldier.View.battle.pending.action==TrainingAction.SoldierMove&&soldier.View.battle.pending.NativePacket?.Length==24&&State(soldier.View.record)==before,"full AI move exposes the same opcode and leaves authoritative values untouched");
        Check(soldier.server.ReceiveBattleUnits(SoldierResult(soldier)).Accepted,"soldier movement hint preserves existing final state validation");
        var commander=Members(scenario);var definition=commander.definition.Map(commander.View.record.map);TutorialTile? destination=null;
        for(int y=1;y<definition.map.height-1&&!destination.HasValue;y++)for(int x=1;x<definition.map.width-1&&!destination.HasValue;x++)
            if(OriginalMemberCommands.Route(definition,commander.definition.combatCatalog,commander.View.battle.units,0,x,y)?.Count>0)destination=new TutorialTile(x,y);
        Check(destination.HasValue,"find commander destination on decoded map without overriding terrain");before=State(commander.View.record);
        Check(commander.server.RequestBattleAction(TrainingAction.Move,-1,destination.Value.X,destination.Value.Y,Id()).Accepted,"commander movement queues metadata through its own entry point");
        var queued=commander.View.battle.pending;
        Check(queued.source==0&&OriginalBattleRequests.TryDecode(queued.NativePacket,out var decoded)&&decoded.source==0&&decoded.movePoints[0]==destination.Value&&State(commander.View.record)==before,"commander native request starts at destination and changes no saved position/resources");
    }
    private static void QueueNativeMove(Fixture f,int source,TutorialTile dest)=>Check(f.server.RequestBattleMemberAction(source,TrainingAction.Move,-1,dest.X,dest.Y,Id()).Accepted,"queue member move with native metadata");
    internal static void RunNativeMoveHintChecks(string path)
    {
        root=path;CheckMoveHintCodec();CheckMoveHintIntegration(false);CheckMoveHintIntegration(true);
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Native543 move hint codec and queue metadata, signed padding, cloned snapshots, retained client-local tie axis and existing movement approval. Not a replacement of full client MoveChar path/requeue/correction execution."}));
    }
}
