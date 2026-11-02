using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class CommandRangeEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunCommandRangeChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    internal static void RunCommandRangeChecks(string path)
    {
        root=path;
        CheckCommandGeometry();CheckCommandSnapshots();CheckCommandSave();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Received command radius, inclusive Manhattan bounds, native outline geometry, troop parent groups, outside regroup context, mode-only atomicity and scenario restoration. No invented final equipment formula or autonomous AI movement."}));
    }
    private static void CheckCommandGeometry()
    {
        var leader=new TrainingUnit{id=20,life=7,hp=100,commandRange=3,troopOrder=2,x=10,y=10};
        var member=new TrainingUnit{id=29,life=2,hp=10,x=12,y=11};
        var unrelated=new TrainingUnit{id=30,life=1,hp=10,commandRange=8};var units=new[]{leader,member,unrelated};
        Check(OriginalCommandRange.DisplayCommander(units,29)==leader,"ninth mercenary belongs to commander20, not neighboring group30");
        var state=OriginalCommandRange.Evaluate(units).Single();
        Check(state.commander==20&&state.commanderLife==7&&state.memberLife==2&&state.distance==3&&state.inside==true&&state.regroup==false,"inclusive diagonal boundary and life identity");
        member.x++;state=OriginalCommandRange.Evaluate(units).Single();
        Check(state.inside==false&&state.regroup==true&&state.regroupSearchRadius==3,"one tile outside automatic attack radius requests regroup search");
        foreach(int mode in new[]{2,4,8})foreach(int range in new[]{-1,0,1,2,3,4,7})
        {
            leader.commandRange=range;leader.troopOrder=mode;member.x=30;
            state=OriginalCommandRange.Evaluate(units).Single();
            Check(state.regroup==true&&state.regroupSearchRadius==(range<3?2:range),"native automatic mode regroup search minimum2");
        }
        leader.troopOrder=1;state=OriginalCommandRange.Evaluate(units).Single();
        Check(state.inside==false&&state.regroup==false&&!state.regroupSearchRadius.HasValue,"direct mode has no invented automatic follow");
        leader.troopOrder=null;Check(OriginalCommandRange.Evaluate(units).Single().regroup==null,"unknown mode remains unknown");
        leader.commandRange=null;state=OriginalCommandRange.Evaluate(units).Single();
        Check(state.inside==null&&state.distance==null&&OriginalCommandRange.DisplayCommander(units,29)==null,"missing final radius is not filled from job data");
        leader.commandRange=3;leader.ghost=true;Check(OriginalCommandRange.Evaluate(units).Single().inside==null&&OriginalCommandRange.DisplayCommander(units,29)==null,"ghost commander cannot supply a range");
        leader.ghost=false;leader.nativeState=0x80;Check(OriginalCommandRange.DisplayCommander(units,29)==null,"native ghost flag suppresses outline");leader.nativeState=null;
        leader.hp=0;Check(OriginalCommandRange.Evaluate(units).Single().inside==null,"dead commander has no active range");leader.hp=100;
        member.hp=0;Check(OriginalCommandRange.Evaluate(units).Single().inside==null,"fallen member does not request regroup");member.ghost=true;
        Check(OriginalCommandRange.DisplayCommander(units,29)==leader,"native hover checks parent status, not hovered member ghost bit");member.ghost=false;member.hp=10;
        Check(OriginalCommandRange.Evaluate(new[]{member}).Single().inside==null,"missing parent remains unavailable");
        foreach(int radius in new[]{0,-1}){leader.commandRange=radius;Check(OriginalCommandRange.DisplayCommander(units,20)==null,"native less than1 hides outline");}
        Check(OriginalCommandRange.Distance(int.MinValue,int.MinValue,int.MaxValue,int.MaxValue)==8589934590L,"distance arithmetic does not overflow");
        foreach(int r in new[]{1,2,3,7})
        {
            int count=0,edgeCount=0;
            for(int y=0;y<25;y++)for(int x=0;x<25;x++)
            {
                if(OriginalCommandRange.Contains(12,12,r,x,y))count++;
                var edges=OriginalCommandRange.Edges(12,12,r,x,y,25,25);
                if((edges&CommandRangeEdges.Top)!=0)edgeCount++;
                if((edges&CommandRangeEdges.Bottom)!=0)edgeCount++;
                if((edges&CommandRangeEdges.Left)!=0)edgeCount++;
                if((edges&CommandRangeEdges.Right)!=0)edgeCount++;
            }
            Check(count==1+2*r*(r+1)&&edgeCount==8*r+4,"closed diamond area and perimeter "+r);
        }
        Check(OriginalCommandRange.Edges(0,0,3,0,0,10,10)==(CommandRangeEdges.Top|CommandRangeEdges.Left),"map border closes clipped outline");
        Check(OriginalCommandRange.Edges(5,5,1,5,5,10,10)==CommandRangeEdges.None,"center is not filled or outlined");
        Check(OriginalCommandRange.Edges(5,5,1,6,6,10,10)==CommandRangeEdges.None,"range1 excludes diagonal");
        Check(OriginalCommandRange.Edges(0,0,int.MaxValue,9,9,10,10)==(CommandRangeEdges.Bottom|CommandRangeEdges.Right),"large received radius clips to map edges without radius allocation");
    }
    private static void CheckCommandSnapshots()
    {
        var f=new Fixture();Check(f.View.battle.Commander.commandRange==null&&f.definition.combatCatalog.Job(100).rawCommandRange==3,"old snapshot final radius unknown despite known Fighter raw3");
        Check(f.definition.combatCatalog.commandRangeItems.Length==9&&f.definition.combatCatalog.commandRangeItems.Single(i=>i.item==148).rawCommandRange==2,"nine original item bonuses preserved separately");
        var n=f.Notice();var leader=n.units[0];leader.commandRange=1;leader.canCommand=true;leader.troopOrder=2;
        n.units=n.units.Concat(new[]{f.Unit(1,100,leader.x-2,leader.y)}).ToArray();
        Check(f.server.ReceiveBattleUnits(n).Accepted,"received final radius and known member");
        var snapshot=f.View.battle;Check(snapshot.CommandRanges.Single().inside==false&&snapshot.CommandRanges.Single().regroup==true,"live view derives outside state");
        snapshot.Commander.commandRange=99;Check(f.View.battle.Commander.commandRange==1,"returned unit snapshot cannot alter authoritative range");
        Check(f.server.RequestBattleOrder(3,Id()).Accepted,"outside mercenary does not block changing command mode");
        var request=f.View.battle.pending;Check(request.commandRanges.Single().range==1&&request.commandRanges.Single().distance==2,"local authority receives range and actual distance context");
        request.commandRanges[0].range=99;Check(f.View.battle.pending.commandRanges[0].range==1,"pending range context deeply copied");
        var bad=f.Notice();bad.request=request.id;bad.units[0].troopOrder=8;bad.units[0].commandRange=3;string before=State(f.View.record);
        Check(!f.server.ReceiveBattleUnits(bad).Accepted&&State(f.View.record)==before&&f.View.battle.Commander.commandRange==1,"mode-only reply cannot silently change final equipment/radius stat");
        var good=f.Notice();good.request=request.id;good.units[0].troopOrder=8;
        Check(f.server.ReceiveBattleUnits(good).Accepted,"exact command reply after rejection");
        n=f.Notice();n.units[0].commandRange=4;Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.battle.CommandRanges.Single().inside==true,"explicit updated final range recomputes status");
        n=f.Notice();n.units[0].commandRange=null;Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.battle.CommandRanges.Single().inside==null,"loss of final stat clears stale range");
    }
    private static void CheckCommandSave()
    {
        var f=new Fixture(scenario:true);var n=f.Notice();n.units[0].commandRange=7;
        string before=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattleUnits(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle.Commander.commandRange==null,"failed range save preserves prior snapshot");f.store.fail=false;
        Check(f.server.ReceiveBattleUnits(n).Accepted,"retry received final stat");
        var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();
        Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Commander.commandRange==7,"scenario reconnect restores received final radius");
        n=f.Notice();n.units[0].commandRange=null;Check(f.server.ReceiveBattleUnits(n).Accepted,"legacy-compatible absent radius");
        restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();
        Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Commander.commandRange==null,"legacy save does not gain invented default range");
    }
}
