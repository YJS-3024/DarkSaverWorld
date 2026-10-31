using System;
using System.Linq;
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
            if (map == null || !map.map.Contains(new TutorialTile(record.x, record.y)) || map.npcs.Any(n=>n.x==record.x&&n.y==record.y)) return WorldResult(WorldFailure.InvalidStage);
            world = new WorldSession(definition, record, owner.character.job); worldCharacter = owner.character.id;
            if (map.training) { var exit = definition.Map(1000).portals.First(g => g.targetMap == map.id); record.map = 1000; record.x = exit.returnX; record.y = exit.returnY; world.Relocate(record); }
            failure = CommitWorld(world.Record);
            if (failure != WorldFailure.None) { ResetWorld(); return WorldResult(failure); }
            return WorldResult(WorldFailure.None);
        }
        public WorldView WorldSnapshot() => world?.Snapshot();
        private WorldFailure ReadyWorld()
        { var f = WorldOwner(out _); return f != WorldFailure.None ? f : world == null ? WorldFailure.InvalidStage : WorldFailure.None; }
        private WorldFailure CommitWorld(WorldRecord candidate)
        {
            var f = WorldOwner(out var owner); if (f != WorldFailure.None) return f;
            if (!WorldRecord.Valid(candidate)) return WorldFailure.InvalidInput;
            if (!store.TryRead(out var fresh) || !LocalEntryPolicy.IsDocument(fresh)) return WorldFailure.StorageUnavailable;
            var freshOwner = fresh.accounts.FirstOrDefault(a => a.id == accountId);
            if (freshOwner?.character?.id != worldCharacter || freshOwner.salt != owner.salt || freshOwner.passwordHash != owner.passwordHash)
                return WorldFailure.Unauthenticated;
            var saved = candidate.Copy();
            // User-specified interrupted-training policy: persist the town entrance,
            // not a resumable battle or an invented death/recovery reward.
            if (world.Definition.Map(saved.map).training)
            { var gate = world.Definition.Map(1000).portals.First(g => g.targetMap == saved.map); saved.map = 1000; saved.x = gate.returnX; saved.y = gate.returnY; }
            var documentCandidate = fresh.Copy(); documentCandidate.accounts.First(a => a.id == accountId).character.world = saved;
            if (!store.TryWrite(documentCandidate)) return WorldFailure.StorageUnavailable;
            document = documentCandidate; return WorldFailure.None;
        }
        public WorldReply MoveWorld(int x, int y)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            var before = world.Record.Copy(); f = world.Move(x, y);
            if (f == WorldFailure.None) worldBeforeMove = before;
            return WorldResult(f);
        }
        public void AdvanceWorld(double ms)
        {
            if (ReadyWorld() != WorldFailure.None) return;
            world.Presentation?.Advance(ms);
            if(world.Presentation?.Snapshot().completed == true && world.Record.campaignPhase != "explore" && world.Record.campaignPhase != "complete")
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
            if (world.Moving || world.Kind != FacilityKind.None) return WorldResult(WorldFailure.Busy);
            var map = world.Definition.Map(world.Record.map); var gate = map.portals.FirstOrDefault(g => g.id == id);
            if (gate == null || world.PendingPortal != id || !gate.cells.Any(c => c.x == world.Record.x && c.y == world.Record.y)
                || world.Definition.Map(gate.targetMap).training != training) return WorldResult(WorldFailure.InvalidInput);
            var next = world.Record.Copy(); next.map = gate.targetMap; next.x = gate.spawnX; next.y = gate.spawnY; next.direction = 1;
            f = CommitWorld(next); world.PendingPortal = null;
            if (f == WorldFailure.None) world.Relocate(next);
            return WorldResult(f);
        }
        public WorldReply EscapeTraining()
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            if (!world.Definition.Map(world.Record.map).training || world.Moving) return WorldResult(WorldFailure.InvalidStage);
            var gate = world.Definition.Map(1000).portals.First(g => g.targetMap == world.Record.map);
            var next = world.Record.Copy(); next.map = 1000; next.x = gate.returnX; next.y = gate.returnY;
            f = CommitWorld(next); if (f == WorldFailure.None) world.Relocate(next); return WorldResult(f);
        }
        public WorldReply OpenWorldFacility(string npcId)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            if (world.PresentationLocked) return WorldResult(WorldFailure.Busy);
            var npc = world.Definition.Map(world.Record.map).npcs.FirstOrDefault(n => n.id == npcId);
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
            var npc = world.Definition.Map(world.Record.map).npcs.FirstOrDefault(n => n.id == world.Npc);
            return world.CanInteract(npc) ? WorldFailure.None : WorldFailure.TooFar;
        }
        private WorldReply ApplyWorld(WorldRecord next, string request)
        {
            var f = CommitWorld(next);
            if (f == WorldFailure.None) { world.Record = next.Copy(); world.Applied.Add(request); }
            return WorldResult(f);
        }
        public WorldReply BuyWorldItem(int item, string token, string request)
        {
            var f = Facility(FacilityKind.Shop, token, request); if (f != WorldFailure.None) return WorldResult(f);
            var shop = world.Definition.shops.FirstOrDefault(s => s.map == world.Record.map); var product = world.Definition.Item(item);
            if (product == null || shop == null || !shop.items.Contains(item) || product.price < 0) return WorldResult(WorldFailure.InvalidInput);
            if (!world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            if (world.Record.gold < (ulong)product.price) return WorldResult(WorldFailure.InsufficientFunds);
            int slot = Enumerable.Range(0, 28).Where(i => world.Record.inventory.All(v => v.slot != i)).DefaultIfEmpty(-1).First();
            if (slot < 0) return WorldResult(WorldFailure.InventoryFull);
            var next = world.Record.Copy(); next.gold -= (ulong)product.price;
            next.inventory = next.inventory.Concat(new[] { new WorldOwnedItem { id = Guid.NewGuid().ToString("N"), item = item, slot = slot } }).ToArray();
            return ApplyWorld(next, request);
        }
        public WorldReply SellWorldItem(string owned, string token, string request)
        {
            var f = Facility(FacilityKind.Shop, token, request); if (f != WorldFailure.None) return WorldResult(f);
            var item = world.Record.inventory.FirstOrDefault(i => i.id == owned);
            if (item == null) return WorldResult(WorldFailure.NotOwned);
            var product = world.Definition.Item(item.item);
            if (!world.Record.goldKnown || product?.sellKnown != true || product.sellPrice < 0) return WorldResult(WorldFailure.RulesUnavailable);
            var next = world.Record.Copy();
            try { next.gold = checked(next.gold + (ulong)product.sellPrice); } catch (OverflowException) { return WorldResult(WorldFailure.InvalidInput); }
            next.inventory = next.inventory.Where(i => i.id != owned).ToArray(); return ApplyWorld(next, request);
        }
        public WorldReply BankWorld(bool deposit, ulong amount, string token, string request)
        {
            var f = Facility(FacilityKind.Bank, token, request); if (f != WorldFailure.None) return WorldResult(f);
            if (amount == 0) return WorldResult(WorldFailure.InvalidInput);
            var r = world.Definition.rules;
            if (!world.Record.goldKnown || !world.Record.bankKnown || !r.bankFeeKnown) return WorldResult(WorldFailure.RulesUnavailable);
            var next = world.Record.Copy();
            try
            {
                if (deposit) { ulong cost = checked(amount + r.bankFee); if (cost > next.gold) return WorldResult(WorldFailure.InsufficientFunds); next.gold -= cost; next.bank = checked(next.bank + amount); }
                else { if (amount > next.bank || amount < r.bankFee) return WorldResult(WorldFailure.InsufficientFunds); next.bank -= amount; next.gold = checked(next.gold + (amount - r.bankFee)); }
            }
            catch (OverflowException) { return WorldResult(WorldFailure.InvalidInput); }
            return ApplyWorld(next, request);
        }
        public WorldReply MoveWorldStorage(bool deposit, string owned, string token, string request)
        {
            var f = Facility(FacilityKind.Warehouse, token, request); if (f != WorldFailure.None) return WorldResult(f);
            var r = world.Definition.rules;
            if (!r.storageRuleKnown || r.storageSlots <= 0) return WorldResult(WorldFailure.RulesUnavailable);
            var candidate = world.Draft.Copy(); var source = deposit ? candidate.inventory : candidate.storage; var target = deposit ? candidate.storage : candidate.inventory;
            var item = source.FirstOrDefault(i => i.id == owned); if (item == null) return WorldResult(WorldFailure.NotOwned);
            int capacity = deposit ? r.storageSlots : 28;
            int slot = Enumerable.Range(0, capacity).Where(i => target.All(v => v.slot != i)).DefaultIfEmpty(-1).First();
            if (slot < 0) return WorldResult(WorldFailure.InventoryFull);
            var moved = item.Copy(); moved.slot = slot; target = target.Concat(new[] { moved }).ToArray(); source = source.Where(i => i.id != owned).ToArray();
            candidate.inventory = deposit ? source : target; candidate.storage = deposit ? target : source;
            world.Draft = candidate; world.PendingStorageFee = r.storageFee; world.Applied.Add(request); return WorldResult(WorldFailure.None);
        }
        public WorldReply ConfirmWorldStorage(string token, string request)
        {
            var f = Facility(FacilityKind.Warehouse, token, request); if (f != WorldFailure.None) return WorldResult(f);
            var r = world.Definition.rules;
            if (!r.storageRuleKnown || !world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            var candidate = world.Draft.Copy(); if (candidate.gold < world.PendingStorageFee) return WorldResult(WorldFailure.InsufficientFunds);
            candidate.gold -= world.PendingStorageFee; var reply = ApplyWorld(candidate, request);
            if (reply.Accepted) { world.Close(); return WorldResult(WorldFailure.None); } return reply;
        }
        public WorldReply HireWorldMercenary(int job, string token, string request)
        {
            var f = Facility(FacilityKind.Mercenary, token, request); if (f != WorldFailure.None) return WorldResult(f);
            var r = world.Definition.rules; var offer = world.Definition.hires.FirstOrDefault(h => h.commander == world.CommanderJob && h.job == job);
            if (offer == null) return WorldResult(WorldFailure.InvalidInput);
            if (!r.hireRuleKnown || !world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            if ((world.Record.hiredCount > 0 && world.Record.hiredJob != job) || world.Record.hiredCount >= offer.limit) return WorldResult(WorldFailure.InvalidInput);
            if (offer.price < 0 || world.Record.gold < (ulong)offer.price) return WorldResult(WorldFailure.InsufficientFunds);
            var candidate = world.Record.Copy(); candidate.gold -= (ulong)offer.price; candidate.hiredJob = job; candidate.hiredCount++;
            return ApplyWorld(candidate, request);
        }
        public WorldReply DismissWorldMercenary(string token, string request)
        {
            var f = Facility(FacilityKind.Mercenary, token, request); if (f != WorldFailure.None) return WorldResult(f);
            if (!world.Definition.rules.dismissalRuleKnown || !world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            if (world.Record.hiredCount == 0) return WorldResult(WorldFailure.NotOwned);
            var candidate = world.Record.Copy(); candidate.hiredCount--; if (candidate.hiredCount == 0) candidate.hiredJob = -1;
            try { candidate.gold = checked(candidate.gold + world.Definition.rules.dismissalRefund); } catch (OverflowException) { return WorldResult(WorldFailure.InvalidInput); }
            return ApplyWorld(candidate, request);
        }
        public WorldReply ReviveWorld(string token, string request)
        {
            var f = Facility(FacilityKind.Temple, token, request); if (f != WorldFailure.None) return WorldResult(f);
            var r = world.Definition.rules;
            if (!r.reviveRuleKnown || !world.Record.healthKnown || !world.Record.goldKnown) return WorldResult(WorldFailure.RulesUnavailable);
            if (!world.Record.ghost || world.Record.hp != 0 || r.reviveHp <= 0 || r.reviveHp > world.Record.maxHp || r.reviveMp < 0 || r.reviveMp > world.Record.maxMp)
                return WorldResult(WorldFailure.InvalidStage);
            if (world.Record.gold < r.reviveFee) return WorldResult(WorldFailure.InsufficientFunds);
            var candidate = world.Record.Copy(); candidate.gold -= r.reviveFee; candidate.ghost = false; candidate.hp = r.reviveHp; candidate.mp = r.reviveMp;
            return ApplyWorld(candidate, request);
        }
    }
}
