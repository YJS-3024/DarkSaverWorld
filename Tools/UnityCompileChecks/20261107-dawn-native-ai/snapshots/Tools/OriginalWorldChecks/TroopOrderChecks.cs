using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class TroopOrderEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunTroopOrderChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    private static Fixture Orders(bool scenario=false,bool members=true)
    {
        var f=new Fixture(scenario:scenario);var n=f.Notice();n.units[0].canCommand=true;
        if(members&&!scenario)
        {
            int x=n.units[0].x,y=n.units[0].y;var fallen=f.Unit(9,100,x,y+1);fallen.hp=0;
            n.units=n.units.Concat(new[]{f.Unit(1,100,x-1,y),fallen,f.Unit(10,100,x,y-1)}).ToArray();
        }
        Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit commander permission, no generated hire roster");return f;
    }
    private static void QueueOrder(Fixture f,int choice)=>Check(f.server.RequestBattleOrder(choice,Id()).Accepted,"valid native menu command "+choice);
    private static TrainingUnitsNotice OrderResult(Fixture f,int? mode=null)
    {var n=f.Notice();n.request=f.View.battle.pending.id;n.units.Single(u=>u.id==0).troopOrder=mode??f.View.battle.pending.nativeOrder;return n;}
    internal static void RunTroopOrderChecks(string path)
    {
        root=path;Check(OriginalTroopOrders.NativeMode(1)==1&&OriginalTroopOrders.NativeMode(2)==2&&OriginalTroopOrders.NativeMode(3)==8,"native choice3 means internal8, never internal3");
        foreach(bool scenario in new[]{false,true})
        {
            var f=Orders(scenario);foreach(int choice in new[]{1,2,3,3,1})
            {
                string before=State(f.View.record);var units=f.View.battle.units;QueueOrder(f,choice);var r=f.View.battle.pending;
                Check(r.action==TrainingAction.TroopOrder&&r.orderChoice==choice&&r.nativeOrder==OriginalTroopOrders.NativeMode(choice)&&r.sourceLife==f.View.battle.Commander.life&&State(f.View.record)==before,"capture source life/order without predictive mode change");
                if(!scenario)Check(r.commandMembers.Select(m=>m.id).SequenceEqual(new[]{1,9}),"native member context1..9 includes9, excludes another group10/enemy400");
                if(r.commandMembers.Length>0){r.commandMembers[0].id=999;Check(f.View.battle.pending.commandMembers[0].id==1,"request member snapshots copied");}
                var n=OrderResult(f);Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.battle.Commander.troopOrder==OriginalTroopOrders.NativeMode(choice),"approve raw native order "+choice);
                Check(f.View.battle.units.Length==units.Length&&f.View.battle.units.All(u=>OriginalTroopOrders.SameExceptOrder(units.Single(b=>b.id==u.id),u)),"command does not spend resources/spawn/move/damage/change permissions");
                Check(f.View.record.inventory.Length==0&&f.View.record.hiredCount==0&&f.View.record.growth.experience==123,"no generated mercenaries, items or XP");
                before=State(f.View.record);Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==before,"duplicate command approval no second commit");
            }
        }
        var empty=Orders(members:false);QueueOrder(empty,2);Check(empty.View.battle.pending.commandMembers.Length==0&&empty.server.ReceiveBattleUnits(OrderResult(empty)).Accepted,"native mode flag can be set without inventing army members");
        var exhausted=Orders();var low=exhausted.Notice();low.units[0].mp=low.units[0].ap=0;low.units[0].canMove=low.units[0].canAttack=low.units[0].canRest=false;
        Check(exhausted.server.ReceiveBattleUnits(low).Accepted,"explicit exhausted commander");QueueOrder(exhausted,3);Check(exhausted.server.ReceiveBattleUnits(OrderResult(exhausted)).Accepted&&exhausted.View.record.mp==0&&exhausted.View.battle.Commander.ap==0,"order has no inferred AP/MP action cost");
        CheckOrderRejections();CheckOrderSave();CheckOrderPresentation();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,choices=3,menuChoices=new[]{1,2,3},nativeModes=new[]{1,2,8},unityPlayExecuted=false,scope="Three native troop order choices, original mode mapping, explicit permission/approval, known member context1..9, resource-preserving commit and scenario restoration. No invented AI/targeting/costs or actual Unity Play claim."}));
    }
    private static void CheckOrderRejections()
    {
        foreach(int choice in new[]{-1,0,4,8,int.MaxValue})
        {var f=Orders();string before=State(f.View.record);Check(f.server.RequestBattleOrder(choice,Id()).Failure==WorldFailure.InvalidInput&&State(f.View.record)==before&&f.View.battle.pending==null,"only three exposed menu choices "+choice);}
        foreach(bool? permission in new bool?[]{null,false})
        {var f=Orders();var n=f.Notice();n.units[0].canCommand=permission;Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit/unknown command permission");Check(f.server.RequestBattleOrder(1,Id()).Failure==WorldFailure.RulesUnavailable,"no invented permission default");}
        foreach(string bad in new[]{"mode","native3","peer-mode","mp","ap","hp","x","life","permission","member-position","member-remove","member-spawn","xp","item","magic","request","id","revision","session","decline"})
        {
            var f=Orders();QueueOrder(f,3);var n=OrderResult(f);string before=State(f.View.record);
            switch(bad)
            {
                case "mode":n.units[0].troopOrder=4;break;case "native3":n.units[0].troopOrder=3;break;case "peer-mode":n.units.Single(u=>u.id==1).troopOrder=8;break;
                case "mp":n.units[0].mp--;break;case "ap":n.units[0].ap--;break;case "hp":n.units[0].hp--;break;case "x":n.units[0].x--;break;
                case "life":n.units[0].life++;break;case "permission":n.units[0].canCommand=false;break;case "member-position":n.units.Single(u=>u.id==1).x--;break;
                case "member-remove":n.units=n.units.Where(u=>u.id!=1).ToArray();break;case "member-spawn":var c=n.units[0];n.units=n.units.Concat(new[]{f.Unit(2,100,c.x-2,c.y)}).ToArray();break;
                case "xp":n.experience=0;n.expectedGrowthRevision=f.View.record.growth.revision;break;case "item":n.itemConsumed=false;break;case "magic":n.magicTargets=Array.Empty<int>();break;
                case "request":n.request=Id();break;case "id":n.id="bad";break;case "revision":n.expectedRevision--;break;case "session":n.session=Id();break;case "decline":n.approved=false;break;
            }
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==before&&f.View.battle.magicEffects.Length==0,"malformed command has no side effects "+bad);
        }
        var unknown=Orders();var u=unknown.Notice();u.units[0].troopOrder=4;Check(unknown.server.ReceiveBattleUnits(u).Accepted&&unknown.View.battle.Commander.troopOrder==4,"known internal4 retained without inventing its meaning/menu option");
        QueueOrder(unknown,1);Check(unknown.server.ReceiveBattleUnits(OrderResult(unknown)).Accepted&&unknown.View.battle.Commander.troopOrder==1,"known menu mode replaces observed internal mode");
        var f2=Orders();string state=State(f2.View.record);Check(f2.server.RequestBattleAction(TrainingAction.Rest,-1,0,0,Id(),-1,null,1).Failure==WorldFailure.InvalidInput&&State(f2.View.record)==state,"order cannot attach to another action");
        Check(f2.server.RequestBattleAction(TrainingAction.TroopOrder,400,0,0,Id(),-1,null,1).Failure==WorldFailure.InvalidInput,"order cannot select enemy as commander");
        Check(f2.server.ExecuteBattleAction(TrainingAction.TroopOrder,-1,0,0,null,-1,null,1).Failure==WorldFailure.RulesUnavailable&&f2.View.battle.pending==null,"missing authority never applies local mode or locks input");
        QueueOrder(f2,1);Check(f2.server.RequestBattleOrder(2,Id()).Failure==WorldFailure.Busy,"one pending order/action");string id=f2.View.battle.pending.id;
        Check(f2.server.CancelBattleAction(id).Accepted&&f2.View.battle.pending==null&&State(f2.View.record)==state,"cancel order without cost or predictive state");
        Check(f2.server.ExecuteTrainingAction(TrainingAction.TroopOrder,-1,0,0,(r,v)=>OrderResult(f2),-1,null,2).Accepted&&f2.View.battle.Commander.troopOrder==2,"training adapter forwards native menu choice");
        var broken=Orders();Check(broken.server.ExecuteBattleAction(TrainingAction.TroopOrder,-1,0,0,(r,v)=>throw new Exception(),-1,null,1).Failure==WorldFailure.RulesUnavailable&&broken.View.battle.pending==null,"failed authority releases pending command");
    }
    private static void CheckOrderSave()
    {
        var f=Orders(scenario:true);QueueOrder(f,3);var n=OrderResult(f);string before=State(f.View.record);f.store.fail=true;
        Check(f.server.ReceiveBattleUnits(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle.Commander.troopOrder==null&&f.View.battle.pending!=null,"failed order save preserves mode/request");f.store.fail=false;
        Check(f.server.ReceiveBattleUnits(n).Accepted,"exact order approval retry");
        var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted&&restored.WorldSnapshot().battle.Commander.troopOrder==8&&restored.WorldSnapshot().battle.Commander.canCommand==true&&restored.WorldSnapshot().battle.pending==null,"scenario order/permission restored without pending replay");
        var training=Orders();QueueOrder(training,2);Check(training.server.ReceiveBattleUnits(OrderResult(training)).Accepted,"training order saved with normal interruption policy");restored=new EntryAccountService(training.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(training.definition).Accepted&&restored.WorldSnapshot().record.map==1000&&restored.WorldSnapshot().battle==null,"interrupted training still restores town, no invented resumable army");
    }
    private static void CheckOrderPresentation()
    {
        var f=Orders();f.Cast();var effects=f.View.battle.magicEffects;var resources=f.View.battle.Commander.Copy();QueueOrder(f,2);
        Check(f.server.ReceiveBattleUnits(OrderResult(f)).Accepted&&f.View.battle.magicEffects.Select(e=>e.id).SequenceEqual(effects.Select(e=>e.id))&&f.View.battle.magicEffects[0].age==effects[0].age,"command does not clear or replay existing spell effects");
        Check(f.View.battle.Commander.mp==resources.mp&&f.View.battle.Commander.ap==resources.ap,"existing cast resources unchanged by following command");
        Check(OriginalTroopOrders.MenuX(1)==32&&OriginalTroopOrders.MenuY(1)==0&&OriginalTroopOrders.MenuX(2)==8&&OriginalTroopOrders.MenuY(2)==64&&OriginalTroopOrders.MenuX(3)==56&&OriginalTroopOrders.MenuY(3)==64,"native direct top, attack bottom-left, defense bottom-right");
        Check(Enumerable.Range(1,3).Select(OriginalTroopOrders.IconX).SequenceEqual(new[]{0,32,64}),"native command icon row64 three cells");
    }
}
