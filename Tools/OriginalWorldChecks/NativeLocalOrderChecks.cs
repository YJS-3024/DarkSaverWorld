using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
internal static class NativeLocalOrderEntry {public static void Main(string[] args)=>MagicProjectileChecks.RunNativeLocalOrderChecks(args[0]);}
internal static partial class MagicProjectileChecks
{
    internal static void RunNativeLocalOrderChecks(string path)
    {
        root=path;
        foreach(bool scenario in new[]{false,true})
        {
            var f=Orders(scenario);var manager=new LocalServerManager(f.server);int notified=0,called=0;
            manager.BattleAuthority=(q,v)=>{called++;throw new Exception("native setsolai has no server transaction");};manager.Notify_BattleChanged+=r=>notified++;
            foreach(int choice in new[]{1,2,3,1})
            {
                var before=f.View.battle.units;var reply=scenario?manager.Request_BattleOrder(choice):manager.Request_TrainingOrder(choice);
                Check(reply.Accepted&&f.View.battle.Commander.troopOrder==OriginalTroopOrders.NativeMode(choice)&&f.View.battle.pending==null,"native troop popup changes local commander mode and completes without external authority");
                Check(called==0&&f.View.battle.units.All(u=>OriginalTroopOrders.SameExceptOrder(before.Single(b=>b.id==u.id),u)),"local order has no wire call, resource cost, move, damage or unit creation");
            }
            Check(notified==4,"one local order notification after each durable mode change");
            var copy=State(f.View.record);f.store.fail=true;int previous=f.View.battle.Commander.troopOrder.Value;
            Check(manager.Request_BattleOrder(2).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==copy&&f.View.battle.Commander.troopOrder==previous&&f.View.battle.pending==null&&notified==4,"failed local mode save preserves state and unlocks popup");
            f.store.fail=false;Check(manager.Request_BattleOrder(2).Accepted,"local mode can retry after storage recovers");
            Check(!manager.Request_BattleOrder(4).Accepted,"unlabelled internal mode4 is not invented as a fourth menu choice");
            if(scenario)Check(manager.Request_TrainingOrder(3).Failure==WorldFailure.InvalidStage,"training-specific local order preserves map guard");
            var denied=f.Notice();denied.units[0].canCommand=false;Check(f.server.ReceiveBattleUnits(denied).Accepted&&!manager.Request_BattleOrder(3).Accepted,"local mode respects explicit command permission");
        }
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,unityPlayExecuted=false,scope="Real manager local setsolai path for three popup choices without a wire/authority dependency, existing permissions/training guard, no resource/unit mutations and durable save/retry."}));
    }
}
