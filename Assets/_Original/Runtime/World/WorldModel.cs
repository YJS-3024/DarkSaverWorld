using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.Campaign;

namespace DarkSaver.Original.World
{
    public enum FacilityKind { None, Shop, Bank, Warehouse, Mercenary, Temple }
    public enum WorldFailure { None, Unauthenticated, InvalidStage, InvalidInput, Moving, TooFar, Busy, StaleRequest, RulesUnavailable, InsufficientFunds, InventoryFull, NotOwned, StorageUnavailable }
    [Serializable] public sealed class WorldPoint { public int x, y; public TutorialTile Tile => new TutorialTile(x, y); }
    [Serializable] public sealed class WorldPortal
    { public string id; public int targetMap, spawnX, spawnY, returnX, returnY; public WorldPoint[] cells; }
    [Serializable] public sealed class WorldNpc
    { public string id, name, evidence; public int picture, x, y, kind; public WorldPoint[] counter; }
    [Serializable] public sealed class WorldDecoration { public int picture, x, y, width, height; }
    [Serializable] public sealed class WorldMapDefinition
    {
        public int id, ground; public string title, sourceVersion, nativeKey; public bool training;
        public TutorialMap map; public WorldPortal[] portals; public WorldNpc[] npcs; public WorldDecoration[] decorations;
    }
    [Serializable] public sealed class WorldItemDefinition
    { public int id, picture; public long price, sellPrice; public string name, description; public bool sellKnown; }
    [Serializable] public sealed class WorldShop { public int map; public int[] items; public string evidence; }
    [Serializable] public sealed class WorldHireOffer { public int commander, job, picture, limit; public long price; public string name; }
    [Serializable] public sealed class WorldRules
    {
        // Defaults deliberately withhold unknown original server policies.
        public bool initialGoldKnown, initialBankKnown, bankFeeKnown, storageRuleKnown, reviveRuleKnown, hireRuleKnown, dismissalRuleKnown;
        public ulong initialGold, initialBank, bankFee, storageFee, reviveFee, dismissalRefund;
        public int storageSlots, reviveHp, reviveMp;
    }
    [Serializable] public sealed class WorldWindowDefinition
    { public int scene, width, height; public WaitingButton[] buttons; }
    [Serializable] public sealed class WorldDefinition
    {
        public WorldMapDefinition[] maps; public WorldShop[] shops; public WorldItemDefinition[] items;
        public WorldHireOffer[] hires; public WorldRules rules; public WorldWindowDefinition[] windows;
        public CampaignDefinition campaign;
        public WorldMapDefinition Map(int id) => maps?.FirstOrDefault(m => m.id == id);
        public WorldItemDefinition Item(int id) => items?.FirstOrDefault(i => i.id == id);
        public bool IsValid => rules != null && Map(1000)?.map?.IsValid == true && shops != null && items != null && hires != null
            && maps.All(m => m.map?.IsValid == true && m.portals != null && m.npcs != null && m.decorations != null)
            && maps.Select(m => m.id).Distinct().Count() == maps.Length;
    }
    [Serializable, DataContract] public sealed class WorldOwnedItem
    {
        [DataMember] public string id;
        [DataMember] public int item, slot;
        public WorldOwnedItem Copy() => (WorldOwnedItem)MemberwiseClone();
    }
    [Serializable, DataContract] public sealed class WorldRecord
    {
        [DataMember] public int version = 1, map = 1000, x = 21, y = 21, direction = 1;
        [DataMember] public bool goldKnown, bankKnown, healthKnown, ghost;
        [DataMember] public ulong gold, bank;
        [DataMember] public int hp, maxHp, mp, maxMp, hiredJob = -1, hiredCount;
        [DataMember(EmitDefaultValue=false)] public string campaignPhase;
        [DataMember] public WorldOwnedItem[] inventory = Array.Empty<WorldOwnedItem>(), storage = Array.Empty<WorldOwnedItem>();
        public WorldRecord Copy()
        { var r = (WorldRecord)MemberwiseClone(); r.inventory = inventory.Select(i => i.Copy()).ToArray(); r.storage = storage.Select(i => i.Copy()).ToArray(); return r; }
        public static bool Valid(WorldRecord r)
        {
            if (r == null) return true; // Compatible with pre-world accounts.
            if(r.campaignPhase!=null&&r.campaignPhase!="intro"&&r.campaignPhase!="explore"&&r.campaignPhase!="outro"&&r.campaignPhase!="complete")return false;
            if (r.version != 1 || r.map < 0 || r.x < 0 || r.y < 0 || r.direction < 0 || r.direction > 3
                || r.inventory == null || r.storage == null || r.hiredCount < 0 || (r.hiredCount == 0 && r.hiredJob != -1) || (r.hiredCount > 0 && r.hiredJob < 0)
                || (!r.goldKnown && r.gold != 0) || (!r.bankKnown && r.bank != 0)
                || r.hp < 0 || r.mp < 0 || r.maxHp < r.hp || r.maxMp < r.mp) return false;
            var all = r.inventory.Concat(r.storage).ToArray();
            return all.All(i => i != null && Guid.TryParseExact(i.id, "N", out _) && i.item >= 0 && i.slot >= 0)
                && all.Select(i => i.id).Distinct().Count() == all.Length
                && r.inventory.Select(i => i.slot).Distinct().Count() == r.inventory.Length
                && r.storage.Select(i => i.slot).Distinct().Count() == r.storage.Length;
        }
    }
    public sealed class WorldFacilityView
    {
        public string token, npc; public FacilityKind kind;
        public WorldOwnedItem[] inventory, storage; public int[] offers; public ulong storageFee;
    }
    public sealed class WorldView
    {
        public WorldRecord record; public WorldFacilityView facility;
        public string pendingPortal; public double clock, renderX, renderY; public bool moving;
        public WorldFailure failure;
        public CampaignUnit[] units;
        public CampaignPresentationView presentation;
        public string campaignSession;
    }
    public sealed class WorldReply
    {
        public WorldFailure Failure { get; }
        public bool Accepted => Failure == WorldFailure.None;
        public WorldView View { get; }
        public WorldReply(WorldFailure failure, WorldView view = null) { Failure = failure; View = view; }
    }
    // In-process server state. Map routes/NPC placements are explicit configuration,
    // never inferred original network packets or restored single-player gameplay.
    internal sealed class WorldSession
    {
        public readonly WorldDefinition Definition;
        public readonly int CommanderJob;
        public WorldRecord Record, Draft;
        public string Token, Npc, PendingPortal, PendingNpc;
        public FacilityKind Kind;
        public ulong PendingStorageFee;
        public WorldFailure Failure;
        public double Clock;
        public CampaignPresentation Presentation;
        public string CampaignSession = Guid.NewGuid().ToString("N");
        public readonly HashSet<string> CampaignNotices = new HashSet<string>();
        public readonly List<CampaignCommand> Effects = new List<CampaignCommand>();
        public bool PhaseSavePending;
        public bool PresentationLocked => PhaseSavePending || (Presentation != null && !Presentation.Snapshot().completed);
        public readonly HashSet<string> Applied = new HashSet<string>();
        private readonly Queue<TutorialTile> path = new Queue<TutorialTile>();
        private TutorialTile from;
        private double movement;
        private bool stopping;
        public bool Moving => path.Count > 0;
        public WorldSession(WorldDefinition definition, WorldRecord record, int job)
        { Definition = definition; Record = record.Copy(); CommanderJob = job; from = new TutorialTile(record.x, record.y); BeginPresentation(); }
        private void BeginPresentation()
        {
            var map = Definition.campaign?.Map(Record.map); Presentation = null; Effects.Clear(); PhaseSavePending=false;
            if (map?.kind == "scenario" && Record.campaignPhase != "explore" && Record.campaignPhase != "complete")
                SetPresentation(Record.campaignPhase == "outro" ? map.outro : map.intro);
        }
        public void SetPresentation(string script)
        { Presentation = new CampaignPresentation(script, CommanderJob, Record.map, id => Definition.Map(id) != null); Presentation.Effect += command => Effects.Add(command); Presentation.Advance(0); }
        public void Close() { Kind = FacilityKind.None; Token = Npc = null; Draft = null; PendingStorageFee = 0; }
        public bool CanInteract(WorldNpc npc)
        {
            var p = new TutorialTile(Record.x, Record.y);
            return !Moving && npc != null && (TutorialGeometry.Distance(p, new TutorialTile(npc.x, npc.y)) <= 1
                || npc.counter.Any(c => c.Tile == p));
        }
        public WorldFailure Move(int x, int y)
        {
            if (Moving) return WorldFailure.Moving;
            if (Kind != FacilityKind.None || PresentationLocked) return WorldFailure.Busy;
            var m = Definition.Map(Record.map); var target = new TutorialTile(x, y);
            var occupied = new HashSet<TutorialTile>(m.npcs.Select(n => new TutorialTile(n.x, n.y)));
            foreach(var unit in Definition.campaign?.Map(Record.map)?.units ?? Array.Empty<CampaignUnit>()) occupied.Add(new TutorialTile(unit.x,unit.y));
            // Local routes must not cross another facility's entry on the way to a target.
            foreach (var portal in m.portals)
                foreach (var cell in portal.cells)
                    if (cell.Tile != target) occupied.Add(cell.Tile);
            var route = m.map.Path(new TutorialTile(Record.x, Record.y), target, 100000, occupied);
            if (route == null || route.Count == 0) return WorldFailure.InvalidInput;
            foreach (var point in route) path.Enqueue(point);
            PendingPortal = null; movement = 0; stopping = false; Face(); return WorldFailure.None;
        }
        public void StopAtNextTile() { stopping = true; }
        private double Duration => path.Count > 0 && path.Peek().X != from.X && path.Peek().Y != from.Y ? 240 : 160;
        private void Face() { from = new TutorialTile(Record.x, Record.y); Record.direction = TutorialGeometry.FacingRow(path.Peek().X - from.X, path.Peek().Y - from.Y); }
        // Returns true on a completed route or requested grid-aligned stop.
        public bool Advance(double ms)
        {
            if (double.IsNaN(ms) || double.IsInfinity(ms) || ms < 0) return false;
            Clock += ms; if (!Moving) return false; movement += ms;
            while (Moving && movement >= Duration)
            {
                movement -= Duration; var p = path.Dequeue(); Record.x = p.X; Record.y = p.Y;
                var portal = Definition.Map(Record.map).portals.FirstOrDefault(g => g.cells.Any(c => c.Tile == p));
                if (stopping || portal != null) path.Clear();
                if (Moving) Face();
                else
                {
                    from = p;
                    PendingPortal = portal?.id;
                    return true;
                }
            }
            return false;
        }
        public void Relocate(WorldRecord record)
        { bool changed = Record.map != record.map; Record = record.Copy(); path.Clear(); movement = 0; from = new TutorialTile(record.x, record.y); PendingPortal = PendingNpc = null; Close(); if(changed){CampaignSession=Guid.NewGuid().ToString("N");CampaignNotices.Clear();BeginPresentation();} }
        public WorldView Snapshot()
        {
            double phase = !Moving ? 0 : Math.Floor(movement / 80) * (Duration == 240 ? 10.0 / 32 : .5);
            return new WorldView { record = Record.Copy(), moving = Moving, clock = Clock, pendingPortal = PendingPortal, failure = Failure,
                renderX = !Moving ? Record.x : from.X + (path.Peek().X - from.X) * phase,
                renderY = !Moving ? Record.y : from.Y + (path.Peek().Y - from.Y) * phase,
                units = (Definition.campaign?.Map(Record.map)?.units ?? Array.Empty<CampaignUnit>()).Select(u => u.Copy()).ToArray(),
                presentation = Presentation?.Snapshot(),
                campaignSession = CampaignSession,
                facility = Kind == FacilityKind.None ? null : new WorldFacilityView { token = Token, npc = Npc, kind = Kind,
                    inventory = (Draft ?? Record).inventory.Select(i => i.Copy()).ToArray(), storage = (Draft ?? Record).storage.Select(i => i.Copy()).ToArray(),
                    storageFee = PendingStorageFee, offers = (int[])(Definition.shops.FirstOrDefault(s => s.map == Record.map)?.items.Clone() ?? Array.Empty<int>()) } };
        }
    }
}
