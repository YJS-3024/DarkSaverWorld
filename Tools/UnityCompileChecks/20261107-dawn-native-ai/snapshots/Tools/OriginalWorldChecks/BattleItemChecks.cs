using System;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;

internal static class BattleItemEntry { public static void Main(string[] args)=>MagicProjectileChecks.RunBattleItemChecks(args[0]); }
internal static partial class MagicProjectileChecks
{
    private static Fixture ItemFixture(int item=300,bool scenario=false,int? remaining=null,bool sameTypeTwice=false)
    {
        var f=new Fixture(scenario:scenario,inventory:new[]{new WorldOwnedItem{id=Id(),item=item,slot=0,remainingUses=remaining},new WorldOwnedItem{id=Id(),item=sameTypeTwice?item:301,slot=1}});
        f.definition.battleItems=Read<BattleItemCatalog>("Data/Combat/BattleItems.json");
        var n=f.Notice();n.units[0].itemRules=new[]{new BattleItemRule{item=item,enabled=true,mpCost=0,apCost=3,menuSlot=0,targets=new[]{0,400}}};
        Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit item rule only, not learned magic");return f;
    }
    private static string Owned(Fixture f)=>f.View.record.inventory.First(i=>i.slot==0).id;
    private static TrainingUnitsNotice ItemResult(Fixture f,bool? consumed=true,int? remaining=null)
    {var n=MagicResult(f);n.itemConsumed=consumed;n.itemRemainingUses=remaining;return n;}
    private static void QueueItem(Fixture f,int target=400)=>Check(f.server.RequestBattleItem(Owned(f),target,Id()).Accepted,"owned battle item queued");
    internal static void RunBattleItemChecks(string path)
    {
        root=path;var catalog=Read<BattleItemCatalog>("Data/Combat/BattleItems.json");Check(catalog.entries.Length==33,"all native position7 links");
        foreach(var entry in catalog.entries)
        {
            var f=ItemFixture(entry.item,sameTypeTwice:true);string owned=Owned(f),other=f.View.record.inventory.Single(i=>i.id!=owned).id;string before=State(f.View.record);
            QueueItem(f);var r=f.View.battle.pending;
            Check(r.action==TrainingAction.UseItem&&r.ownedItem==owned&&r.item==entry.item&&r.itemSlot==0&&r.magic==entry.magic&&r.mpCost==0&&r.apCost==3&&State(f.View.record)==before,"literal item->magic binding and no predicted consumption "+entry.item);
            Check(OriginalNativeMagic.TryRequest(r.NativeItemPacket(32768),out var packet)&&packet.item==entry.item&&packet.itemToken==32768&&packet.magic==entry.magic&&r.NativePacket==null,"native5C91 needs explicit inventory token; local slot/GUID is not guessed "+entry.item);
            var n=ItemResult(f);Check(f.server.ReceiveBattleUnits(n).Accepted,"entire consumable approval "+entry.item);
            Check(f.View.record.inventory.Length==1&&f.View.record.inventory[0].id==other&&f.View.record.mp==45&&f.View.battle.Commander.ap==17,"exact object consumption, no duplicate spell MP charge "+entry.item);
            Check(f.View.record.growth.magic.SequenceEqual(new[]{101})&&f.View.record.growth.skills.SequenceEqual(new[]{10101}),"item use never grants/needs the linked learned magic");
            int steps=0;bool drew=false;while(f.View.battle.magicEffects.Length>0&&steps++<1024){f.server.AdvanceWorld(40);drew|=BattleMagicPresentation.Frames(f.View.battle.magicEffects).Length>0;}
            Check(steps<1024&&drew&&f.View.battle.magicEffects.Length==0,"linked general/special effect plays and ends "+entry.item);
            before=State(f.View.record);Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==before,"duplicate item result cannot consume another instance");
        }
        CheckItemRecovery();CheckItemFailures();CheckItemSaveAndRace();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions,consumableLinks=33,unityPlayExecuted=false,scope="Native item->magic33 links; exact owned object, native target assembly, explicit costs/consumption/remaining uses/health, atomic save/retry/stale store protection. No default heal/cost/durability formula or Unity Play claim."}));
    }
    private static void CheckItemRecovery()
    {
        var f=ItemFixture();var seed=f.Notice();seed.units[0].hp=17;Check(f.server.ReceiveBattleUnits(seed).Accepted,"injured caster");QueueItem(f,0);var n=ItemResult(f);n.units[0].hp=97;
        Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.record.hp==97&&f.View.record.mp==45,"explicit HP recovery on self, no implicit potion formula");
        f=ItemFixture(305);QueueItem(f,0);n=ItemResult(f);n.magicRestoreMp=15;n.units[0].mp=60;
        Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.record.mp==60&&f.View.battle.Commander.ap==17,"explicit MP recovery bound to item outcome");
        f=ItemFixture(307,remaining:10);QueueItem(f);n=ItemResult(f,false,9);n.units[1].nativeState=0;
        Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.record.inventory.Single(i=>i.slot==0).remainingUses==9,"boxed antidote explicit remaining count, object retained");
        Check(f.View.battle.Commander.itemRules[0].menuSlot==0,"approved nine-slot offer binding retained");
        QueueItem(f);n=ItemResult(f,false,0);Check(f.server.ReceiveBattleUnits(n).Accepted&&f.View.record.inventory.Single(i=>i.slot==0).remainingUses==0,"explicit zero remaining differs from unknown");
        Check(f.server.RequestBattleItem(Owned(f),400,Id()).Failure==WorldFailure.InsufficientResource,"exhausted retained item cannot be reused");
        f=ItemFixture(316);Check(!f.View.record.inventory[0].remainingUses.HasValue,"native raw durability does not initialize use count");QueueItem(f);n=ItemResult(f,false);
        Check(f.server.ReceiveBattleUnits(n).Accepted&&!f.View.record.inventory[0].remainingUses.HasValue,"explicit nonconsumption with unknown count stays unknown");
        f=ItemFixture(398);var source=f.View.battle.Unit(400);Check(f.server.RequestBattleItemAt(Owned(f),source.x,source.y,Id()).Accepted,"bomb uses original area magic");n=ItemResult(f);
        Check(f.server.ReceiveBattleUnits(n).Accepted,"explicit area item result");
    }
    private static void CheckItemFailures()
    {
        foreach(string bad in new[]{"owned","unsupported","missing-rule","missing-cost","disabled","mp","ap","target","bad-id","forged-magic","menu-slot","duplicate-menu"})
        {
            var f=ItemFixture(bad=="unsupported"?361:300);var n=f.Notice();
            switch(bad)
            {
                case "missing-rule":n.units[0].itemRules=null;break;case "missing-cost":n.units[0].itemRules[0].mpCost=null;break;
                case "disabled":n.units[0].itemRules[0].enabled=false;break;case "mp":n.units[0].itemRules[0].mpCost=46;break;case "ap":n.units[0].itemRules[0].apCost=21;break;
                case "target":n.units[0].itemRules[0].targets=Array.Empty<int>();break;
                case "menu-slot":n.units[0].itemRules[0].menuSlot=9;break;
                case "duplicate-menu":n.units[0].itemRules=n.units[0].itemRules.Concat(new[]{new BattleItemRule{item=301,menuSlot=0}}).ToArray();break;
            }
            string before=State(f.View.record);
            if(bad=="menu-slot"||bad=="duplicate-menu"){Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==before,"invalid item offer layout rejected");continue;}
            Check(f.server.ReceiveBattleUnits(n).Accepted,"item failure rule fixture");before=State(f.View.record);
            var reply=bad=="forged-magic"?f.server.RequestBattleAction(TrainingAction.UseItem,400,0,0,Id(),301,Owned(f)):f.server.RequestBattleItem(bad=="owned"?Id():bad=="bad-id"?"bad":Owned(f),400,Id());
            Check(!reply.Accepted&&State(f.View.record)==before&&f.View.battle.pending==null,"invalid item request atomic "+bad);
        }
        foreach(string bad in new[]{"consumption","negative-count","positive-count-consumed","targets","outside","duplicate","mp","ap","life","link","unapproved"})
        {
            var f=ItemFixture();QueueItem(f);var n=ItemResult(f);string before=State(f.View.record);
            switch(bad){case "consumption":n.itemConsumed=null;break;case "negative-count":n.itemConsumed=false;n.itemRemainingUses=-1;break;case "positive-count-consumed":n.itemRemainingUses=9;break;
                case "targets":n.magicTargets=null;break;case "outside":n.magicTargets=new[]{999};break;case "duplicate":n.magicTargets=new[]{400,400};break;case "mp":n.units[0].mp--;break;case "ap":n.units[0].ap++;break;
                case "life":n.units[1].life++;break;case "link":f.definition.battleItems.Find(300).magic=301;break;case "unapproved":n.approved=false;break;}
            Check(!f.server.ReceiveBattleUnits(n).Accepted&&State(f.View.record)==before&&f.View.battle.magicEffects.Length==0,"invalid item outcome atomic "+bad);
        }
        var unrelated=ItemFixture();var unsolicited=unrelated.Notice();unsolicited.itemConsumed=false;Check(!unrelated.server.ReceiveBattleUnits(unsolicited).Accepted,"consumption cannot attach to an unsolicited roster");
        var cancelled=ItemFixture();string state=State(cancelled.View.record);QueueItem(cancelled);Check(cancelled.server.CancelBattleAction(cancelled.View.battle.pending.id).Accepted&&State(cancelled.View.record)==state,"cancel before approval preserves item/resources");
        var noAuthority=ItemFixture();Check(noAuthority.server.ExecuteBattleAction(TrainingAction.UseItem,400,0,0,null,-1,Owned(noAuthority)).Failure==WorldFailure.RulesUnavailable&&noAuthority.View.battle.pending==null,"missing authority does not consume/lock input");
        var training=ItemFixture();Check(training.server.ExecuteTrainingAction(TrainingAction.UseItem,400,0,0,(r,v)=>ItemResult(training),-1,Owned(training)).Accepted,"training compatibility adapter forwards exact owned ID");
    }
    private static void CheckItemSaveAndRace()
    {
        foreach(bool scenario in new[]{false,true})
        {
            var f=ItemFixture(307,scenario,10);string owned=Owned(f);QueueItem(f);var n=ItemResult(f,false,9);string before=State(f.View.record);f.store.fail=true;
            Check(f.server.ReceiveBattleUnits(n).Failure==WorldFailure.StorageUnavailable&&State(f.View.record)==before&&f.View.battle.magicEffects.Length==0,"save failure leaves item/count/HP/AP untouched");f.store.fail=false;Check(f.server.ReceiveBattleUnits(n).Accepted,"retry same item approval once");
            var restored=new EntryAccountService(f.store);new EntryClient(restored).OpenLocalProfile();Check(restored.BeginWorld(f.definition).Accepted,"reload item result");var view=restored.WorldSnapshot();
            Check(view.record.inventory.Single(i=>i.id==owned).remainingUses==9&&view.record.mp==45&&(view.battle?.magicEffects?.Length??0)==0,"persist remaining count without item animation replay");
            Check(scenario?view.record.map==26:view.record.map==1000,"interrupted training still restores town entrance");
        }
        foreach(bool scenario in new[]{false,true})
        {
            var f=ItemFixture(scenario:scenario);QueueItem(f);var n=ItemResult(f);var before=State(f.View.record);f.store.data.accounts[0].character.world.inventory[1].remainingUses=7;
            Check(f.server.ReceiveBattleUnits(n).Failure==WorldFailure.StaleRequest&&State(f.View.record)==before&&f.store.data.accounts[0].character.world.inventory[1].remainingUses==7,"stale client cannot overwrite external item changes");
        }
        var invalid=new WorldRecord{inventory=new[]{new WorldOwnedItem{id=Id(),item=300,slot=0,remainingUses=-1}}};Check(!WorldRecord.Valid(invalid),"negative persisted count invalid");
    }
}
