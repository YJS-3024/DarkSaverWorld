using System;
using System.Linq;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private WorldSession world;
        private WorldRecord worldBeforeMove;
        private string worldCharacter;
        private void ResetWorld() { world = null; worldBeforeMove = null; worldCharacter = null; }
        private WorldFailure WorldOwner(out LocalAccountRecord owner)
        {
            var failure = TutorialOwner(out owner);
            if (failure != TutorialFailure.None) return failure == TutorialFailure.StorageUnavailable ? WorldFailure.StorageUnavailable : WorldFailure.Unauthenticated;
            if (!owner.character.tutorialCompleted) return WorldFailure.InvalidStage;
            if (world != null && owner.character.id != worldCharacter) return WorldFailure.Unauthenticated;
            return WorldFailure.None;
        }
        private WorldReply WorldResult(WorldFailure result)
        { if (world != null) world.Failure = result; return new WorldReply(result, world?.Snapshot()); }
        public WorldReply BeginWorld(WorldDefinition definition)
        {
            var failure = WorldOwner(out var owner); if (failure != WorldFailure.None) return WorldResult(failure);
            if (world != null || definition?.IsValid != true) return WorldResult(WorldFailure.InvalidStage);
            var record = owner.character.world?.Copy() ?? new WorldRecord {
                goldKnown = definition.rules.initialGoldKnown, gold = definition.rules.initialGoldKnown ? definition.rules.initialGold : 0,
                bankKnown = definition.rules.initialBankKnown, bank = definition.rules.initialBankKnown ? definition.rules.initialBank : 0 };
            var map = definition.Map(record.map);
            if(record.growth!=null&&definition.growthCatalog?.Accepts(record.growth)!=true)return WorldResult(WorldFailure.InvalidStage);
            if (map == null || !map.map.Contains(new TutorialTile(record.x, record.y)) || map.npcs.Any(n=>n.x==record.x&&n.y==record.y)) return WorldResult(WorldFailure.InvalidStage);
            world = new WorldSession(definition, record, owner.character.job); worldCharacter = owner.character.id;world.CharacterId=worldCharacter;
            failure=RestoreScenarioBattle();if(failure!=WorldFailure.None){ResetWorld();return WorldResult(failure);}
            if (map.training) { var exit = TrainingEntrance(record); if(exit==null){ResetWorld();return WorldResult(WorldFailure.InvalidInput);}record.map = 1000; record.x = exit.returnX; record.y = exit.returnY; record.trainingEntrance=null;world.Relocate(record); }
            failure = CommitWorld(world.Record);
            if (failure != WorldFailure.None) { ResetWorld(); return WorldResult(failure); }
            world.ScriptHandler=ExecuteCampaignScript;world.RestartPresentation();
            return WorldResult(WorldFailure.None);
        }
        public WorldView WorldSnapshot() => world?.Snapshot();
        public bool TryWorldClientSession(out string character,out string session)
        {character=session=null;if(ReadyWorld()!=WorldFailure.None)return false;character=worldCharacter;session=world.CommanderSession;return true;}
        public bool IsWorldClientSession(string character,string session)=>TryWorldClientSession(out var current,out var connection)&&current==character&&connection==session;
        private WorldFailure ReadyWorld()
        { var f = WorldOwner(out _); return f != WorldFailure.None ? f : world == null ? WorldFailure.InvalidStage : WorldFailure.None; }
        public bool TryWorldItemIds(out int[] items)
        {items=null;if(ReadyWorld()!=WorldFailure.None)return false;items=world.Definition.items.Select(item=>item.id).ToArray();return true;}
        private WorldPortal TrainingEntrance(WorldRecord record) => world.Map(1000).portals.FirstOrDefault(g=>(g.targetMap==record.map||record.map>=101&&record.map<=107&&g.targetMap>=101&&g.targetMap<=107)&&(string.IsNullOrEmpty(record.trainingEntrance)||g.id==record.trainingEntrance));
        private WorldFailure CommitWorld(WorldRecord candidate,WorldRecord expectedRevivalState=null,WorldRecord expectedCurrencyState=null)
        {
            var f = WorldOwner(out var owner); if (f != WorldFailure.None) return f;
            if (!WorldRecord.Valid(candidate)) return WorldFailure.InvalidInput;
            if (!store.TryRead(out var fresh) || !LocalEntryPolicy.IsDocument(fresh)) return WorldFailure.StorageUnavailable;
            var freshOwner = fresh.accounts.FirstOrDefault(a => a.id == accountId);
            if (freshOwner?.character?.id != worldCharacter || freshOwner.salt != owner.salt || freshOwner.passwordHash != owner.passwordHash)
                return WorldFailure.Unauthenticated;
            if((freshOwner.character.world?.campaignDelivery?.revision??0)!=(world.Record.campaignDelivery?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.campaignProgress?.revision??0)!=(world.Record.campaignProgress?.revision??0))return WorldFailure.StaleRequest;
            if(!(freshOwner.character.world?.clearedScenarios??Array.Empty<int>()).SequenceEqual(world.Record.clearedScenarios??Array.Empty<int>()))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.roster?.revision??0)!=(world.Record.roster?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.identity?.revision??0)!=(world.Record.identity?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.growth?.revision??0)!=(world.Record.growth?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.scenarioBattle?.revision??0)!=(world.Record.scenarioBattle?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.revival?.revision??0)!=(world.Record.revival?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.currency?.revision??0)!=(world.Record.currency?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.warehouse?.revision??0)!=(world.Record.warehouse?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.mercenary?.revision??0)!=(world.Record.mercenary?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.shop?.revision??0)!=(world.Record.shop?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.itemUse?.revision??0)!=(world.Record.itemUse?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.statAllocation?.revision??0)!=(world.Record.statAllocation?.revision??0))return WorldFailure.StaleRequest;
            if((freshOwner.character.world?.magicLoadout?.revision??0)!=(world.Record.magicLoadout?.revision??0))return WorldFailure.StaleRequest;
            if(expectedRevivalState!=null&&!TempleRevivalOffer.SameState(freshOwner.character.world,expectedRevivalState))return WorldFailure.StaleRequest;
            if(expectedCurrencyState!=null&&!CurrencyStoredStateMatches(freshOwner.character.world,expectedCurrencyState))return WorldFailure.StaleRequest;
            var freshWorld=freshOwner.character.world;
            if((freshWorld?.scenarioVisit?.nativeEnd?.revision??0)!=(world.Record.scenarioVisit?.nativeEnd?.revision??0))return WorldFailure.StaleRequest;
            if(freshWorld?.scenarioVisit?.id!=world.Record.scenarioVisit?.id||freshWorld?.scenarioVisit?.result?.id!=world.Record.scenarioVisit?.result?.id
                ||(freshWorld?.scenarioVisit?.result?.nativeKills?.revision??0)!=(world.Record.scenarioVisit?.result?.nativeKills?.revision??0)
                ||freshWorld?.lastScenarioResult?.id!=world.Record.lastScenarioResult?.id
                ||freshWorld?.lastScenarioExit?.id!=world.Record.lastScenarioExit?.id
                ||(world.Record.scenarioVisit!=null||world.Record.scenarioBattle!=null)&&freshWorld?.campaignPhase!=world.Record.campaignPhase)return WorldFailure.StaleRequest;
            f=PrepareCurrencyCommit(candidate);if(f!=WorldFailure.None)return f;
            bool currencyChanged=(candidate.currency?.revision??0)!=(world.Record.currency?.revision??0);
            bool growthChanged=(candidate.growth?.revision??0)!=(world.Record.growth?.revision??0);
            bool statContextChanged=(world.StatDraft!=null||world.PendingStatAdjustment!=null)&&!TempleRevivalOffer.SameState(candidate,world.Record);
            bool magicContextChanged=(world.MagicDraft!=null||world.PendingMagicLoadout!=null)&&!TempleRevivalOffer.SameState(candidate,world.Record);
            var saved = candidate.Copy();
            // User-specified interrupted-training policy: persist the town entrance,
            // not a resumable battle or an invented death/recovery reward.
            if (world.Map(saved.map).training)
            { var gate = TrainingEntrance(saved);if(gate==null)return WorldFailure.InvalidInput;saved.map = 1000; saved.x = gate.returnX; saved.y = gate.returnY;saved.trainingEntrance=null; }
            var documentCandidate = fresh.Copy(); documentCandidate.accounts.First(a => a.id == accountId).character.world = saved;
            if (!store.TryWrite(documentCandidate)) return WorldFailure.StorageUnavailable;
            if(currencyChanged){world.PendingCurrency=null;world.ClearBankTransfer();world.ClearWarehouseRequest();world.ClearMercenary();world.ClearShop();world.ClearItemUse();world.ClearClassChange();world.ClearStatAdjustment();world.ClearMagicLoadout();}
            if(growthChanged){world.ClearClassChange();world.ClearStatAdjustment();world.ClearMagicLoadout();}
            if(statContextChanged)world.ClearStatAdjustment();
            if(magicContextChanged)world.ClearMagicLoadout();
            document = documentCandidate; return WorldFailure.None;
        }
        public WorldReply MoveWorld(int x, int y)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            if(world.Map(world.Record.map).training)return WorldResult(WorldFailure.InvalidStage);
            bool continuing=world.Moving;var before = world.Record.Copy(); f = world.Move(x, y);
            if(f==WorldFailure.None){world.ClearItemUse();world.ClearClassChange();world.ClearStatAdjustment();world.ClearMagicLoadout();if(!continuing)worldBeforeMove=before;}
            return WorldResult(f);
        }
        public WorldReply EquipWorldItem(string owned,int slot,string request,int inventorySlot=-1)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(world.PendingCommander!=null||world.PendingItemUse!=null||world.PendingClassChange!=null||world.PendingClassOptions!=null||world.PendingStatAdjustment!=null||world.StatDraft?.Dirty==true||world.PendingMagicLoadout!=null||world.MagicDraft?.Dirty==true)return WorldResult(WorldFailure.Busy);
            if(world.Moving||world.Kind!=FacilityKind.None||world.PresentationLocked||world.PendingPortal!=null||world.Combat?.Pending!=null)return WorldResult(WorldFailure.Busy);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            f=BuildEquippedRecord(world.Record,world.CommanderJob,owned,slot,inventorySlot,out var next);
            return f==WorldFailure.None?ApplyWorld(next,request):WorldResult(f);
        }
        private WorldFailure BuildEquippedRecord(WorldRecord current,int commanderJob,string owned,int slot,int inventorySlot,out WorldRecord result)
        {
            var next=current.Copy();result=next;
            var equipped=next.Equipment.FirstOrDefault(i=>i.id==owned);
            if(equipped!=null)
            {
                if(slot!=-1)return WorldFailure.InvalidInput;
                if(inventorySlot>=28||inventorySlot < -1||inventorySlot>=0&&next.inventory.Any(i=>i.slot==inventorySlot))return WorldFailure.InvalidInput;
                int free=inventorySlot>=0?inventorySlot:Enumerable.Range(0,28).Where(i=>next.inventory.All(v=>v.slot!=i)).DefaultIfEmpty(-1).First();
                if(free<0)return WorldFailure.InventoryFull;
                var returned=equipped.Copy();returned.slot=free;
                next.equipment=next.Equipment.Where(i=>i.id!=owned).ToArray();next.inventory=next.inventory.Concat(new[]{returned}).ToArray();
            }
            else
            {
                var item=next.inventory.FirstOrDefault(i=>i.id==owned);if(item==null)return WorldFailure.NotOwned;
                var product=world.Definition.Item(item.item);if(product==null)return WorldFailure.InvalidInput;
                var growthJob=world.Definition.growthCatalog?.Job(commanderJob);
                if(product.jobs==null&&(product.jobKinds==null||growthJob==null)||product.requiredLevel>0&&!next.level.HasValue)return WorldFailure.RulesUnavailable;
                bool allowed=product.jobs?.Contains(commanderJob)==true||growthJob!=null&&product.jobKinds?.Contains(growthJob.kind)==true;
                if(!allowed||product.requiredLevel>(next.level??0))return WorldFailure.InvalidInput;
                int type=product.equipType;
                bool fits=(type==1||type==2||type==8)&&(slot==2||slot==3)||type==3&&slot==1||type==4&&slot==5
                    ||type==5&&slot==0||type==6&&(slot==4||slot==6);
                if(!fits)return WorldFailure.InvalidInput;
                var opposite=(slot==2||slot==3)?next.Equipment.FirstOrDefault(i=>i.slot==(slot==2?3:2)):null;
                int oppositeType=opposite==null?-1:world.Definition.Item(opposite.item)?.equipType??-1;
                if((type==2||type==8)&&oppositeType==type)return WorldFailure.InvalidInput;
                var replaced=next.Equipment.Where(i=>i.slot==slot||(i==opposite&&(type==1||oppositeType==1))).ToArray();
                var moved=item.Copy();moved.slot=slot;
                next.inventory=next.inventory.Where(i=>i.id!=owned).ToArray();
                foreach(var old in replaced)
                {
                    int free=Enumerable.Range(0,28).Where(i=>next.inventory.All(v=>v.slot!=i)).DefaultIfEmpty(-1).First();
                    if(free<0)return WorldFailure.InventoryFull;
                    var returned=old.Copy();returned.slot=free;next.inventory=next.inventory.Concat(new[]{returned}).ToArray();
                }
                next.equipment=next.Equipment.Except(replaced).Concat(new[]{moved}).ToArray();
            }
            return WorldFailure.None;
        }
        public WorldReply UseWorldItem(string owned,string request)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            if(world.Moving||world.Kind!=FacilityKind.None||world.PresentationLocked||world.PendingPortal!=null||world.Combat?.Pending!=null)return WorldResult(WorldFailure.Busy);
            if(!Guid.TryParseExact(request,"N",out _)||world.Applied.Contains(request))return WorldResult(WorldFailure.StaleRequest);
            var item=world.Record.inventory.FirstOrDefault(i=>i.id==owned);if(item==null)return WorldResult(WorldFailure.NotOwned);
            int type=world.Definition.Item(item.item)?.equipType??-1;
            if(type!=10)return WorldResult(WorldFailure.InvalidInput);
            // The native head-drop request is connected; unconfirmed server effects must not consume an item.
            return WorldResult(WorldFailure.RulesUnavailable);
        }
        public void AdvanceWorld(double ms)
        {
            if (ReadyWorld() != WorldFailure.None) return;
            if(double.IsNaN(ms)||double.IsInfinity(ms)||ms<0)return;
            if(world.NativeEndWaiting)
            {
                world.Advance(ms);world.NativeEndElapsed+=ms;
                if(world.Record.scenarioVisit.nativeEnd.stage==NativeScenarioEndRecord.Notice&&world.NativeEndElapsed>10000)
                {var closed=CloseNativeScenarioEndNotice();world.Failure=closed.Failure;}
                return;
            }
            world.NativeDialogue.Advance(ms);
            if(!AdvanceCampaignEvents(ms))return;
            if(world.Record.scenarioVisit?.nativeEnd?.stage==NativeScenarioEndRecord.Outro&&world.Presentation?.Snapshot().completed==true)
            {var ended=FinishNativeScenarioEnd();world.Failure=ended.Failure;return;}
            if(world.ActiveCampaignEvent==null&&world.Presentation?.Snapshot().completed == true && world.Record.campaignPhase != "explore" && world.Record.campaignPhase != "complete")
            {
                var phase=world.Record.Copy();phase.campaignPhase=phase.campaignPhase=="outro"?"complete":"explore";
                var saved=CommitWorld(phase);world.PhaseSavePending=saved!=WorldFailure.None;if(saved==WorldFailure.None){world.Record=phase;world.Failure=WorldFailure.None;}else{world.Failure=saved;return;}
            }
            if (!world.Advance(ms)) return;
            var pending = world.PendingNpc; world.PendingNpc = null;
            var f = CommitWorld(world.Record);
            if (f != WorldFailure.None) { world.Relocate(worldBeforeMove); world.Failure = f; return; }
            if (pending != null) OpenWorldFacility(pending); else world.Failure = WorldFailure.None;
        }
        public WorldReply EnterWorldPortal(string id, bool training)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            if (world.Moving || world.Kind != FacilityKind.None || world.PresentationLocked) return WorldResult(WorldFailure.Busy);
            var map = world.Map(world.Record.map); var gate = map.portals.FirstOrDefault(g => g.id == id);
            if (gate == null || world.PendingPortal != id || !gate.cells.Any(c => c.x == world.Record.x && c.y == world.Record.y)
                || world.Map(gate.targetMap).training != training) return WorldResult(WorldFailure.InvalidInput);
            int target=gate.targetMap;
            if(training&&target>=101&&target<=107)
            {
                int job=world.CommanderJob;
                int tier=world.Definition.growthCatalog?.Job(job)?.stage??0;
                // Legacy definitions may lack stage metadata; only the four original first-stage jobs are unambiguous.
                if(tier==0&&job>=100&&job<=103)tier=1;
                if(tier<1||tier>7||world.Definition.Map(100+tier)?.training!=true)return WorldResult(WorldFailure.RulesUnavailable);
                target=100+tier;
            }
            var arrival=gate.ArrivalAt(world.Record.x,world.Record.y);
            var next = world.Record.Copy(); next.map = target; next.x = arrival.x; next.y = arrival.y; next.direction = 1;
            next.campaignPhase=world.Definition.campaign?.Map(next.map)?.kind=="scenario"?"intro":null;
            next.trainingEntrance=training?gate.id:null;
            f=PrepareScenarioVisit(next);if(f!=WorldFailure.None)return WorldResult(f);
            f = CommitWorld(next);
            if (f == WorldFailure.None) world.Relocate(next);
            return WorldResult(f);
        }
        public WorldReply EscapeTraining()
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            if (!world.Map(world.Record.map).training || world.Moving) return WorldResult(WorldFailure.InvalidStage);
            if(world.Combat?.Pending!=null)return WorldResult(WorldFailure.Busy);
            var gate = TrainingEntrance(world.Record);if(gate==null)return WorldResult(WorldFailure.InvalidInput);
            var next = world.Record.Copy(); next.map = 1000; next.x = gate.returnX; next.y = gate.returnY;
            next.trainingEntrance=null;
            f = CommitWorld(next); if (f == WorldFailure.None) world.Relocate(next); return WorldResult(f);
        }
        public WorldReply OpenWorldFacility(string npcId)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            if (world.PresentationLocked||world.Combat!=null) return WorldResult(WorldFailure.Busy);
            var npc = world.Map(world.Record.map).npcs.FirstOrDefault(n => n.id == npcId);
            if (npc == null) return WorldResult(WorldFailure.InvalidInput);
            if (world.Moving) { world.PendingNpc = npcId; world.StopAtNextTile(); return WorldResult(WorldFailure.None); }
            if (!world.CanInteract(npc)) return WorldResult(WorldFailure.TooFar);
            if (world.Kind != FacilityKind.None) return WorldResult(WorldFailure.Busy);
            world.Kind = (FacilityKind)npc.kind; world.Npc = npc.id; world.Token = Guid.NewGuid().ToString("N");
            if (world.Kind == FacilityKind.Warehouse) world.Draft = world.Record.Copy();
            return WorldResult(WorldFailure.None);
        }
        public WorldReply CloseWorldFacility()
        { var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f); world.Close(); return WorldResult(WorldFailure.None); }
        private WorldFailure Facility(FacilityKind kind, string token, string request)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return f;
            if (world.Moving) return WorldFailure.Moving;
            if (world.Kind != kind || world.Token != token || string.IsNullOrEmpty(token) || !Guid.TryParseExact(request, "N", out _) || world.Applied.Contains(request))
                return WorldFailure.StaleRequest;
            var npc = world.Map(world.Record.map).npcs.FirstOrDefault(n => n.id == world.Npc);
            return world.CanInteract(npc) ? WorldFailure.None : WorldFailure.TooFar;
        }
        private WorldReply ApplyWorld(WorldRecord next, string request,WorldRecord expectedRevivalState=null)
        {
            var f = CommitWorld(next,expectedRevivalState);
            if (f == WorldFailure.None) { world.Record = next.Copy(); world.Applied.Add(request); }
            return WorldResult(f);
        }
        public WorldReply BuyWorldItem(int item, string token, string request)
        {
            var f = Facility(FacilityKind.Shop, token, request); if (f != WorldFailure.None) return WorldResult(f);
            if(world.PendingShopQuote!=null||world.PendingShopTrade!=null||world.ShopOffer!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.shop?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var shop = world.Definition.shops.FirstOrDefault(s => s.map == world.Record.map); var product = world.Definition.Item(item);
            if (product == null || shop == null || !shop.items.Contains(item) || product.price < 0) return WorldResult(WorldFailure.InvalidInput);
            if (!world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            if (world.Record.gold < (ulong)product.price) return WorldResult(WorldFailure.InsufficientFunds);
            int slot = Enumerable.Range(0, 28).Where(i => world.Record.inventory.All(v => v.slot != i)).DefaultIfEmpty(-1).First();
            if (slot < 0) return WorldResult(WorldFailure.InventoryFull);
            var next = world.Record.Copy(); next.gold -= (ulong)product.price;
            next.inventory = next.inventory.Concat(new[] { new WorldOwnedItem { id = Guid.NewGuid().ToString("N"), item = item, slot = slot } }).ToArray();
            var instance=next.inventory.Last();next.shop=new ShopReceipt{id=request,request=request,revision=revision+1,buy=true,item=item,owned=instance.id,slot=slot,price=(ulong)product.price};
            return ApplyWorld(next, request,world.Record);
        }
        public WorldReply SellWorldItem(string owned, string token, string request)
        {
            var f = Facility(FacilityKind.Shop, token, request); if (f != WorldFailure.None) return WorldResult(f);
            if(world.PendingShopQuote!=null||world.PendingShopTrade!=null||world.ShopOffer!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.shop?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var item = world.Record.inventory.FirstOrDefault(i => i.id == owned);
            if (item == null) return WorldResult(WorldFailure.NotOwned);
            var product = world.Definition.Item(item.item);
            if (!world.Record.goldKnown || product?.sellKnown != true || product.sellPrice < 0) return WorldResult(WorldFailure.RulesUnavailable);
            var next = world.Record.Copy();
            try { next.gold = checked(next.gold + (ulong)product.sellPrice); } catch (OverflowException) { return WorldResult(WorldFailure.InvalidInput); }
            next.inventory = next.inventory.Where(i => i.id != owned).ToArray();
            next.shop=new ShopReceipt{id=request,request=request,revision=revision+1,buy=false,item=item.item,owned=owned,slot=item.slot,price=(ulong)product.sellPrice};
            return ApplyWorld(next, request,world.Record);
        }
        public WorldReply BankWorld(bool deposit, ulong amount, string token, string request)
        {
            var f = Facility(FacilityKind.Bank, token, request); if (f != WorldFailure.None) return WorldResult(f);
            if (amount == 0) return WorldResult(WorldFailure.InvalidInput);
            if(world.PendingBankTransfer!=null)return WorldResult(WorldFailure.Busy);
            var r = world.Definition.rules;
            if (!world.Record.goldKnown || !world.Record.bankKnown || !r.bankFeeKnown) return WorldResult(WorldFailure.RulesUnavailable);
            var next = world.Record.Copy();
            try
            {
                if (deposit) { ulong cost = checked(amount + r.bankFee); if (cost > next.gold) return WorldResult(WorldFailure.InsufficientFunds); next.gold -= cost; next.bank = checked(next.bank + amount); }
                else { if (amount > next.bank || amount < r.bankFee) return WorldResult(WorldFailure.InsufficientFunds); next.bank -= amount; next.gold = checked(next.gold + (amount - r.bankFee)); }
            }
            catch (OverflowException) { return WorldResult(WorldFailure.InvalidInput); }
            long revision=world.Record.currency?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            next.currency=new CurrencyReceipt{id=request,revision=revision+1};
            next.lastBankTransfer=new BankTransferReceipt{id=request,request=request,currencyRevision=revision+1,deposit=deposit,amount=amount,fee=r.bankFee,feeSource=BankFeeSource.Wallet};
            return ApplyWorld(next, request);
        }
        public WorldReply MoveWorldStorage(bool deposit, string owned, string token, string request)
        {
            var f = Facility(FacilityKind.Warehouse, token, request); if (f != WorldFailure.None) return WorldResult(f);
            if(world.PendingWarehouse!=null||world.WarehouseTerms!=null)return WorldResult(WorldFailure.Busy);
            var r = world.Definition.rules;
            if (!r.storageRuleKnown || r.storageSlots <= 0) return WorldResult(WorldFailure.RulesUnavailable);
            if(world.StorageDraftRevision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var candidate = world.Draft.Copy(); var source = deposit ? candidate.inventory : candidate.storage; var target = deposit ? candidate.storage : candidate.inventory;
            var item = source.FirstOrDefault(i => i.id == owned); if (item == null) return WorldResult(WorldFailure.NotOwned);
            int capacity = deposit ? r.storageSlots : 28;
            int slot = Enumerable.Range(0, capacity).Where(i => target.All(v => v.slot != i)).DefaultIfEmpty(-1).First();
            if (slot < 0) return WorldResult(WorldFailure.InventoryFull);
            var moved = item.Copy(); moved.slot = slot; target = target.Concat(new[] { moved }).ToArray(); source = source.Where(i => i.id != owned).ToArray();
            candidate.inventory = deposit ? source : target; candidate.storage = deposit ? target : source;
            world.Draft = candidate; world.PendingStorageFee = r.storageFee;world.StorageDraftRevision++; world.Applied.Add(request); return WorldResult(WorldFailure.None);
        }
        public WorldReply ConfirmWorldStorage(string token, string request)
        {
            var f = Facility(FacilityKind.Warehouse, token, request); if (f != WorldFailure.None) return WorldResult(f);
            if(world.PendingWarehouse!=null||world.WarehouseTerms!=null)return WorldResult(WorldFailure.Busy);
            var r = world.Definition.rules;
            if (!r.storageRuleKnown || !world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            long revision=world.Record.warehouse?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var candidate = world.Record.Copy();candidate.inventory=world.Draft.inventory.Select(i=>i.Copy()).ToArray();candidate.storage=world.Draft.storage.Select(i=>i.Copy()).ToArray();
            if (candidate.gold < world.PendingStorageFee) return WorldResult(WorldFailure.InsufficientFunds);
            candidate.gold -= world.PendingStorageFee;
            candidate.warehouse=new WarehouseReceipt{id=request,request=request,revision=revision+1,draftRevision=world.StorageDraftRevision,capacity=r.storageSlots,fee=world.PendingStorageFee,feeSource=BankFeeSource.Wallet};
            var reply = ApplyWorld(candidate, request,world.Record);
            if (reply.Accepted) { world.Close(); return WorldResult(WorldFailure.None); } return reply;
        }
        public WorldReply HireWorldMercenary(int job, string token, string request)
        {
            var f = Facility(FacilityKind.Mercenary, token, request); if (f != WorldFailure.None) return WorldResult(f);
            if(world.PendingMercenary!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.mercenary?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var r = world.Definition.rules; var offer = world.HireOffers().FirstOrDefault(h => h.job == job);
            if (offer == null) return WorldResult(WorldFailure.InvalidInput);
            if (!r.hireRuleKnown || !world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            if ((world.Record.hiredCount > 0 && world.Record.hiredJob != job) || world.Record.hiredCount >= offer.limit) return WorldResult(WorldFailure.InvalidInput);
            if (offer.price < 0 || world.Record.gold < (ulong)offer.price) return WorldResult(WorldFailure.InsufficientFunds);
            var candidate = world.Record.Copy(); candidate.gold -= (ulong)offer.price; candidate.hiredJob = job; candidate.hiredCount++;
            candidate.mercenary=new MercenaryReceipt{id=request,request=request,revision=revision+1,hire=true,job=job,count=candidate.hiredCount,amount=(ulong)offer.price};
            return ApplyWorld(candidate, request,world.Record);
        }
        public WorldReply DismissWorldMercenary(string token, string request)
        {
            var f = Facility(FacilityKind.Mercenary, token, request); if (f != WorldFailure.None) return WorldResult(f);
            if(world.PendingMercenary!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.mercenary?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            if (!world.Definition.rules.dismissalRuleKnown || !world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            if (world.Record.hiredCount == 0) return WorldResult(WorldFailure.NotOwned);
            var candidate = world.Record.Copy(); candidate.hiredCount--; if (candidate.hiredCount == 0) candidate.hiredJob = -1;
            try { candidate.gold = checked(candidate.gold + world.Definition.rules.dismissalRefund); } catch (OverflowException) { return WorldResult(WorldFailure.InvalidInput); }
            candidate.mercenary=new MercenaryReceipt{id=request,request=request,revision=revision+1,hire=false,job=world.Record.hiredJob,count=candidate.hiredCount,amount=world.Definition.rules.dismissalRefund};
            return ApplyWorld(candidate, request,world.Record);
        }
        public WorldReply ReviveWorld(string token, string request)
        {
            var f = Facility(FacilityKind.Temple, token, request); if (f != WorldFailure.None) return WorldResult(f);
            var r = world.Definition.rules;
            if (!r.reviveRuleKnown || !world.Record.healthKnown || !world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            if (!world.Record.ghost || world.Record.hp != 0 || r.reviveHp <= 0 || r.reviveHp > world.Record.maxHp || r.reviveMp < 0 || r.reviveMp > world.Record.maxMp)
                return WorldResult(WorldFailure.InvalidStage);
            if (world.Record.gold < r.reviveFee) return WorldResult(WorldFailure.InsufficientFunds);
            if(world.PendingRevival!=null||world.PendingRevivalQuote!=null)return WorldResult(WorldFailure.Busy);
            long revision=world.Record.revival?.revision??0;if(revision==long.MaxValue)return WorldResult(WorldFailure.StaleRequest);
            var candidate = world.Record.Copy(); candidate.gold -= r.reviveFee; candidate.ghost = false; candidate.hp = r.reviveHp; candidate.mp = r.reviveMp;
            candidate.revival=new RevivalReceipt{id=request,request=request,quote=request,revision=revision+1,fee=r.reviveFee};
            var reply=ApplyWorld(candidate,request,world.Record);if(reply.Accepted)world.ClearRevival();return reply;
        }
    }
}
