using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using DarkSaver.Original.Tutorial;

internal static class MemberCommandEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunMemberCommandChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static Fixture Members(bool scenario=false,bool near=false)
    {
        var f=new Fixture(scenario:scenario);var n=f.Notice();n.units[0].canCommand=true;n.units[0].troopOrder=1;n.units[0].commandRange=0;
        var map=f.definition.Map(f.View.record.map);var blocked=OriginalMemberCommands.StaticBlocked(map);TrainingUnit member=null;
        for(int x=1;x<map.map.width-2&&member==null;x++)for(int y=1;y<map.map.height-1&&member==null;y++)
        {
            var a=new TutorialTile(x,y);var b=new TutorialTile(x+1,y);
            if(near&&OriginalCommandRange.Distance(n.units[0].x,n.units[0].y,x,y)>3)continue;
            if(blocked.Contains(a)||blocked.Contains(b)||a==n.units[0].Tile||b==n.units[0].Tile||map.map.Terrain(a,2)==99||map.map.Terrain(b,2)==99)continue;
            var enemy=n.units.Single(u=>u.id==400);enemy.x=x+1;enemy.y=y;
            member=f.Unit(1,100,x,y);member.hp=10;member.nativeState=0x10;member.attackTargets=new[]{400};
        }
        Check(member!=null,"actual decoded map has an adjacent source/target pair");n.units=n.units.Concat(new[]{member}).ToArray();Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit owned soldier permission and target eligibility");return f;
    }
    private static TrainingUnitsNotice MemberResult(Fixture f)
    {
        var r=f.View.battle.pending;var n=f.Notice();n.request=r.id;var actor=n.units.Single(u=>u.id==r.source);actor.ap=13;actor.nativeState&=~0x30u;
        if(r.action==TrainingAction.Move){actor.x=r.x;actor.y=r.y;}
        if(r.action==TrainingAction.Attack)n.units.Single(u=>u.id==r.target).hp-=10;
        if(r.action==TrainingAction.Rest){actor.hp=actor.maxHp;actor.mp=actor.maxMp;}
        return n;
    }
    private static TutorialTile MemberDestination(Fixture f)
    {
        var map=f.definition.Map(f.View.record.map);
        for(int x=1;x<map.map.width;x++)for(int y=1;y<map.map.height;y++)
            if(OriginalMemberCommands.Route(map,f.definition.combatCatalog,f.View.battle.units,1,x,y)?.Count>0)return new TutorialTile(x,y);
        throw new Exception("decoded map member route missing");
    }
    private static void CheckMemberArea()
    {
        var terrain=new int[810];for(int i=2;i<810;i+=3)terrain[i]=1;terrain[32]=99;
        var map=new TutorialMap{width=11,height=11,attributes=new int[121],terrain=terrain};var catalog=new OriginalCombatCatalog{version=3,jobs=new[]{new OriginalCombatJob{job=100,rawMovementType=0}}};
        var actor=new TrainingUnit{job=100,x=5,y=5,attackRange=1};var area=OriginalBasicAttackArea.Weights(map,catalog,actor);
        Check(area.Count(v=>v>0)==4&&area[5*11+5]==-1&&area[6*11+6]<1,"range1 is four cardinal cells, never self/diagonal");
        actor.attackRange=2;area=OriginalBasicAttackArea.Weights(map,catalog,actor);Check(area.Count(v=>v>0)==12,"unblocked non-bow radius2 diamond");
        map.attributes[5*11+6]=1;area=OriginalBasicAttackArea.Weights(map,catalog,actor);Check(area[5*11+6]<1&&area[5*11+7]<1,"SetAreaB blocked terrain stops propagation");
        actor.movementType=5;area=OriginalBasicAttackArea.Weights(map,catalog,actor);Check(area[5*11+7]>0&&area.Count(v=>v>0)==12,"bow uses SetArea and ignores movement terrain blocking");
        actor.movementType=1;area=OriginalBasicAttackArea.Weights(map,catalog,actor);Check(area[5*11+7]>0,"non-bow movement class selects its own terrain column");
        actor.attackRange=null;Check(OriginalBasicAttackArea.Weights(map,catalog,actor)==null,"unknown final attack radius remains unknown");
    }
    private static void CheckMemberLifecycle(bool scenario)
    {
        var f=Members(scenario);string state=State(f.View.record);var source=f.View.battle.Unit(1).Copy();
        Check(f.server.ExecuteBattleMemberAction(1,TrainingAction.Attack,400,0,0,null).Failure==WorldFailure.RulesUnavailable&&State(f.View.record)==state,"no authority means no hit or AP consumption");
        Check(f.server.RequestBattleMemberAction(1,TrainingAction.Attack,400,0,0,Id()).Accepted,"manual attack works without a blanket command-radius restriction");var r=f.View.battle.pending;
        Check(r.source==1&&r.target==400&&r.sourceLife==source.life&&r.targetLife==1&&r.sourceTerrain>=0&&r.targetTerrain>=0&&!r.automatic,"source/target identity and terrain captured");
        Check(State(f.View.record)==state,"attack has no predictive side effects");Check(f.server.ReceiveBattleUnits(MemberResult(f)).Accepted,"approved member attack");
        Check(f.View.battle.Unit(1).ap==13&&f.View.battle.Unit(400).hp==40&&f.View.battle.Commander.ap==20&&f.View.battle.Commander.hp==200,"explicit member AP and target damage, commander unchanged");
        Check(f.View.battle.impactX==r.x&&f.View.battle.impactY==r.y,"member impact remains at requested enemy contact");
        Check(f.server.ExecuteBattleMemberAction(1,TrainingAction.Rest,-1,0,0,(q,v)=>MemberResult(f)).Accepted&&f.View.battle.Unit(1).hp==50,"explicit rest outcome restores source only");
        var dest=MemberDestination(f);source=f.View.battle.Unit(1).Copy();Check(f.server.RequestBattleMemberAction(1,TrainingAction.Move,-1,dest.X,dest.Y,Id()).Accepted,"member movement queued");r=f.View.battle.pending;var original=r.movePath.ToArray();r.movePath[0]=new TutorialTile(999,999);Check(f.View.battle.pending.movePath.SequenceEqual(original),"member path snapshot isolated");
        Check(f.server.ReceiveBattleUnits(MemberResult(f)).Accepted&&f.View.battle.Unit(1).Tile==dest&&f.View.battle.motions.Single().unit==1,"approved move starts member animation");f.server.AdvanceWorld(80);
        var motion=f.View.battle.motions.Single();Check(motion.x!=source.x||motion.y!=source.y,"member visibly advances on path");Check(!f.View.moving,"member animation does not lock commander input");f.server.AdvanceWorld(100000);
        Check(f.View.battle.motions.Length==0,"movement finishes without stale motion");
        var snapshot=f.View.battle;snapshot.Unit(1).attackTargets[0]=999;Check(f.View.battle.Unit(1).attackTargets[0]==400,"target eligibility deep copy");
        if(scenario)Check(f.server.ExecuteTrainingMemberAction(1,TrainingAction.Rest,-1,0,0,(q,v)=>MemberResult(f)).Failure==WorldFailure.InvalidStage,"training-only entry point guard");
    }
    private static void CheckMemberRejections()
    {
        foreach(int member in new[]{-1,0,10,11,400,999}){var f=Members();Check(!f.server.RequestBattleMemberAction(member,TrainingAction.Rest,-1,0,0,Id()).Accepted,"manual control is only own soldier slots "+member);}
        foreach(var action in new[]{TrainingAction.CastMagic,TrainingAction.UseSkill,TrainingAction.UseItem,TrainingAction.TroopOrder,TrainingAction.Regroup}){var f=Members();Check(!f.server.RequestBattleMemberAction(1,action,-1,0,0,Id()).Accepted,"soldier menu exposes only move/attack/rest "+action);}
        foreach(int[] targets in new int[][]{null,Array.Empty<int>()}){var f=Members();var n=f.Notice();n.units.Single(u=>u.id==1).attackTargets=targets;Check(f.server.ReceiveBattleUnits(n).Accepted,"unknown/empty enemy permission snapshot");Check(!f.server.RequestBattleMemberAction(1,TrainingAction.Attack,400,0,0,Id()).Accepted,"no invented enemy eligibility");}
        foreach(int[] targets in new[]{new[]{400,400},new[]{-1},new[]{1000}}){var f=Members();var n=f.Notice();n.units.Single(u=>u.id==1).attackTargets=targets;Check(!f.server.ReceiveBattleUnits(n).Accepted,"malformed enemy list rejected");}
        foreach(string bad in new[]{"commander-hp","other-mode","source-hp","source-position","source-permission","source-life","target-max","target-ap","extra-unit","experience-revision","session","revision","request"})
        {
            var f=Members();string state=State(f.View.record);Check(f.server.RequestBattleMemberAction(1,TrainingAction.Attack,400,0,0,Id()).Accepted,"queue invalid member reply");var n=MemberResult(f);var actor=n.units.Single(u=>u.id==1);var enemy=n.units.Single(u=>u.id==400);
            switch(bad){case "commander-hp":n.units[0].hp--;break;case "other-mode":n.units[0].troopOrder=2;break;case "source-hp":actor.hp++;break;case "source-position":actor.y++;break;case "source-permission":actor.canMove=false;break;case "source-life":actor.life++;break;case "target-max":enemy.maxHp++;break;case "target-ap":enemy.ap--;break;case "extra-unit":n.units=n.units.Where(u=>u.id!=400).ToArray();break;case "experience-revision":n.experience=200;n.expectedGrowthRevision=99;break;case "session":n.session=Id();break;case "revision":n.expectedRevision--;break;case "request":n.request=Id();break;}
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==state&&f.View.battle.Unit(1).ap==20,"reject mixed/stale member action "+bad);
        }
        var blocked=Members();var unit=blocked.View.battle.Unit(1);Check(!blocked.server.RequestBattleMemberAction(1,TrainingAction.Move,-1,unit.x,unit.y,Id()).Accepted,"own tile not a movement target");var target=blocked.View.battle.Unit(400);Check(!blocked.server.RequestBattleMemberAction(1,TrainingAction.Move,-1,target.x,target.y,Id()).Accepted,"occupied destination rejected");
    }
    private static void CheckMemberPersistence()
    {
        var f=Members(true);Check(f.server.RequestBattleMemberAction(1,TrainingAction.Rest,-1,0,0,Id()).Accepted,"scenario rest queued");var n=MemberResult(f);string state=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattleUnits(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==state&&f.View.battle.Unit(1).hp==10,"failed rest save preserves injured soldier");f.store.fail=false;Check(f.server.ReceiveBattleUnits(n).Accepted,"exact rest approval save retry");
        var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Unit(1).hp==50&&restored.WorldSnapshot().battle.Unit(1).ap==13&&restored.WorldSnapshot().battle.Unit(1).attackTargets.SequenceEqual(new[]{400}),"reconnect restores soldier health/AP/enemy permission");
        var invalid=Members();Check(invalid.server.ExecuteBattleMemberAction(1,TrainingAction.Rest,-1,0,0,(r,v)=>throw new Exception()).Failure==WorldFailure.RulesUnavailable&&invalid.View.battle.pending==null,"failed member authority releases input");
    }
    internal static void RunMemberCommandChecks(string path)
    {
        root=path;CheckMemberArea();CheckMemberLifecycle(false);CheckMemberLifecycle(true);CheckMemberRejections();CheckMemberPersistence();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Owned mercenary move/attack/rest requests and explicit authority results, native SetArea/SetAreaB, terrain context, atomic source-aware saves and rendered motion. No invented enemy lists/damage/rest formula."}));
    }
}
