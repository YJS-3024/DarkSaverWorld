using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.Campaign;

namespace DarkSaver.Original.World
{
    public enum FacilityKind { None, Shop, Bank, Warehouse, Mercenary, Temple, ScenarioGateway }
    public enum WorldFailure { None, Unauthenticated, InvalidStage, InvalidInput, Moving, TooFar, Busy, StaleRequest, RulesUnavailable, InsufficientFunds, InventoryFull, NotOwned, StorageUnavailable, InsufficientResource, ShopCategoryMismatch }
    [Serializable] public sealed class WorldPoint { public int x, y; public TutorialTile Tile => new TutorialTile(x, y); }
    [Serializable] public sealed class WorldPortal
    {
        public string id, gateway; public int targetMap, spawnX, spawnY, spawnWidth, spawnHeight, returnX, returnY; public WorldPoint[] cells;
        public WorldPoint ArrivalAt(int x,int y)
        {
            // Local authoring policy: ordered source cells map proportionally to ordered destination cells.
            // Old records have a one-cell destination. A single source cell uses its first destination cell.
            int width=spawnWidth==0?1:spawnWidth,height=spawnHeight==0?1:spawnHeight;
            int at=Array.FindIndex(cells,c=>c.x==x&&c.y==y),count=checked(width*height);
            if(at<0||count<=0)throw new InvalidOperationException("Invalid portal arrival.");
            int slot=cells.Length<=1?0:(int)(((long)at*(count-1)+(cells.Length-1)/2)/(cells.Length-1));
            return new WorldPoint{x=spawnX+slot%width,y=spawnY+slot/width};
        }
    }
    [Serializable] public sealed class WorldNpc
    { public string id, name, evidence, gateway; public int picture, x, y, kind; public WorldPoint[] counter; }
    [Serializable] public sealed class WorldDecoration { public int picture, x, y, width, height; }
    [Serializable] public sealed class WorldMapDefinition
    {
        public int id, ground; public string title, sourceVersion, nativeKey; public bool training;
        public TutorialMap map; public WorldPortal[] portals; public WorldNpc[] npcs; public WorldDecoration[] decorations;
        public WorldMapDefinition CopyForPlacement(){var copy=(WorldMapDefinition)MemberwiseClone();copy.portals=(WorldPortal[])portals.Clone();copy.npcs=(WorldNpc[])npcs.Clone();return copy;}
    }
    [Serializable] public sealed class WorldItemDefinition
    { public int id, picture, equipType, requiredLevel; public int[] jobs,jobKinds; public long price, sellPrice; public string name, description; public bool sellKnown; }
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
        public BattleItemCatalog battleItems;
        public WorldMapDefinition[] maps; public WorldShop[] shops; public WorldItemDefinition[] items;
        public WorldHireOffer[] hires; public WorldRules rules; public WorldWindowDefinition[] windows;
        public CampaignDefinition campaign;
        public ScenarioGatewaySettings gateways;
        public CharacterGrowthCatalog growthCatalog;
        public NativeMercenaryCatalog mercenaryCatalog;
        public MagicMenuCatalog magicMenu;
        public MagicProjectileCatalog magicProjectiles;
        public OriginalCombatCatalog combatCatalog;
        internal WorldMapDefinition[] gatewayBaseMaps;
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
        // Optional local authoritative count, never seeded from native table durability.
        // Nullable DataMembers use the account DataContract serializer. NonSerialized
        // excludes only Unity's unsupported field serializer, preserving null vs zero.
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? remainingUses;
        public WorldOwnedItem Copy() => (WorldOwnedItem)MemberwiseClone();
    }
    [Serializable, DataContract] public sealed class WorldRecord
    {
        [DataMember] public int version = 1, map = 1000, x = 21, y = 21, direction = 1;
        [DataMember] public bool goldKnown, bankKnown, healthKnown, ghost;
        [DataMember] public ulong gold, bank;
        [DataMember] public int hp, maxHp, mp, maxMp, hiredJob = -1, hiredCount;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? level;
        [DataMember(EmitDefaultValue=false)] public string campaignPhase;
        [DataMember(EmitDefaultValue=false)] public string trainingEntrance;
        [DataMember(EmitDefaultValue=false)] public CampaignProgressRecord campaignProgress;
        [DataMember(EmitDefaultValue=false)] public CampaignDeliveryRecord campaignDelivery;
        [DataMember(EmitDefaultValue=false)] public int[] clearedScenarios;
        [DataMember(EmitDefaultValue=false)] public CharacterGrowthRecord growth;
        [DataMember(EmitDefaultValue=false)] public PlayerIdentityRecord identity;
        [DataMember(EmitDefaultValue=false)] public CommanderRoster roster;
        [DataMember(EmitDefaultValue=false)] public CampaignVisitRecord scenarioVisit;
        [DataMember(EmitDefaultValue=false)] public CampaignResultRecord lastScenarioResult;
        [DataMember(EmitDefaultValue=false)] public ScenarioBattleRecord scenarioBattle;
        [DataMember(EmitDefaultValue=false)] public CampaignExitRecord lastScenarioExit;
        [DataMember(EmitDefaultValue=false)] public RevivalReceipt revival;
        [DataMember(EmitDefaultValue=false)] public CurrencyReceipt currency;
        [DataMember(EmitDefaultValue=false)] public BankTransferReceipt lastBankTransfer;
        [DataMember(EmitDefaultValue=false)] public WarehouseReceipt warehouse;
        [DataMember(EmitDefaultValue=false)] public MercenaryReceipt mercenary;
        [DataMember(EmitDefaultValue=false)] public ShopReceipt shop;
        [DataMember(EmitDefaultValue=false)] public ItemUseReceipt itemUse;
        [DataMember(EmitDefaultValue=false)] public ClassChangeReceipt classChange;
        [DataMember(EmitDefaultValue=false)] public StatAllocationRecord statAllocation;
        [DataMember(EmitDefaultValue=false)] public MagicLoadoutRecord magicLoadout;
        [DataMember] public WorldOwnedItem[] inventory = Array.Empty<WorldOwnedItem>(), storage = Array.Empty<WorldOwnedItem>();
        [DataMember(EmitDefaultValue=false)] public WorldOwnedItem[] equipment;
        public WorldOwnedItem[] Equipment => equipment ?? Array.Empty<WorldOwnedItem>();
        public WorldRecord Copy()
        { var r = (WorldRecord)MemberwiseClone(); r.inventory = inventory.Select(i => i.Copy()).ToArray(); r.storage = storage.Select(i => i.Copy()).ToArray(); r.equipment = equipment?.Select(i=>i.Copy()).ToArray();r.growth=growth?.Copy();r.identity=identity?.Copy();r.roster=roster?.Copy();r.scenarioVisit=scenarioVisit?.Copy();r.lastScenarioResult=lastScenarioResult?.Copy();r.scenarioBattle=scenarioBattle?.Copy();r.lastScenarioExit=lastScenarioExit?.Copy();r.revival=revival?.Copy();r.currency=currency?.Copy();r.lastBankTransfer=lastBankTransfer?.Copy();r.warehouse=warehouse?.Copy();r.mercenary=mercenary?.Copy();r.shop=shop?.Copy();r.itemUse=itemUse?.Copy();r.classChange=classChange?.Copy();r.statAllocation=statAllocation?.Copy();r.magicLoadout=magicLoadout?.Copy();r.campaignProgress=campaignProgress?.Copy();r.campaignDelivery=campaignDelivery?.Copy();r.clearedScenarios=clearedScenarios==null?null:(int[])clearedScenarios.Clone(); return r; }
        public static bool Valid(WorldRecord r)
        {
            if (r == null) return true; // Compatible with pre-world accounts.
            if(!CommanderRoster.Valid(r.roster))return false;
            if(!PlayerIdentityRecord.Valid(r.identity))return false;
            if(!CampaignDeliveryRecord.Valid(r.campaignDelivery))return false;
            if(!CampaignProgressRecord.Valid(r.campaignProgress)||r.campaignProgress!=null&&(r.campaignPhase==null||r.campaignProgress.visit!=(r.scenarioVisit?.id??"legacy:"+r.map))
                ||r.clearedScenarios!=null&&(r.clearedScenarios.Any(i=>i<0||i>999)||r.clearedScenarios.Distinct().Count()!=r.clearedScenarios.Length))return false;
            if(!CharacterGrowthRecord.Valid(r.growth)||r.growth!=null&&r.level!=r.growth.totalLevel)return false;
            if(!CampaignVisitRecord.Valid(r.scenarioVisit)||!CampaignResultRecord.Valid(r.lastScenarioResult)||r.scenarioVisit!=null&&(r.scenarioVisit.map!=r.map||r.campaignPhase==null))return false;
            if(r.scenarioVisit?.result!=null&&(r.campaignPhase!="outro"&&r.campaignPhase!="complete"||!r.scenarioVisit.result.victory&&r.campaignPhase!="complete"))return false;
            if(!ScenarioBattleRecord.Valid(r.scenarioBattle))return false;
            if(!CampaignExitRecord.Valid(r.lastScenarioExit))return false;
            if(!RevivalReceipt.Valid(r.revival))return false;
            if(!CurrencyReceipt.Valid(r.currency))return false;
            if(!BankTransferReceipt.Valid(r.lastBankTransfer)||r.lastBankTransfer!=null&&r.lastBankTransfer.currencyRevision>(r.currency?.revision??0))return false;
            if(!MagicLoadoutRecord.Valid(r.magicLoadout)||r.magicLoadout!=null&&r.magicLoadout.growthRevision>(r.growth?.revision??0))return false;
            if(!StatAllocationRecord.Valid(r.statAllocation)||r.statAllocation!=null&&r.statAllocation.growthRevision>(r.growth?.revision??0))return false;
            if(!ClassChangeReceipt.Valid(r.classChange)||r.classChange!=null&&r.classChange.growthRevision>(r.growth?.revision??0))return false;
            if(!WarehouseReceipt.Valid(r.warehouse)||!MercenaryReceipt.Valid(r.mercenary)||!ShopReceipt.Valid(r.shop)||!ItemUseReceipt.Valid(r.itemUse))return false;
            if(r.scenarioBattle!=null)
            {
                var commander=r.scenarioBattle.units.First(u=>u.id==0);
                if(r.scenarioBattle.map!=r.map||r.campaignPhase!="explore"||!r.healthKnown||commander.x!=r.x||commander.y!=r.y
                    ||commander.hp!=r.hp||commander.maxHp!=r.maxHp||commander.mp!=r.mp||commander.maxMp!=r.maxMp)return false;
            }
            if(r.campaignPhase!=null&&r.campaignPhase!="intro"&&r.campaignPhase!="explore"&&r.campaignPhase!="outro"&&r.campaignPhase!="complete")return false;
            if (r.version != 1 || r.map < 0 || r.x < 0 || r.y < 0 || r.direction < 0 || r.direction > 3 || r.level<0
                || r.inventory == null || r.storage == null || r.hiredCount < 0 || (r.hiredCount == 0 && r.hiredJob != -1) || (r.hiredCount > 0 && r.hiredJob < 0)
                || (!r.goldKnown && r.gold != 0) || (!r.bankKnown && r.bank != 0)
                || r.hp < 0 || r.mp < 0 || r.maxHp < r.hp || r.maxMp < r.mp) return false;
            var secondary=r.roster?.rows.Skip(1).SelectMany(v=>(v.profile?.inventory??Array.Empty<WorldOwnedItem>()).Concat(v.profile?.equipment??Array.Empty<WorldOwnedItem>()))??Enumerable.Empty<WorldOwnedItem>();
            var all = r.inventory.Concat(r.storage).Concat(r.Equipment).Concat(secondary).ToArray();
            return all.All(i => i != null && Guid.TryParseExact(i.id, "N", out _) && i.item >= 0 && i.slot >= 0 && (!i.remainingUses.HasValue || i.remainingUses>=0))
                && all.Select(i => i.id).Distinct().Count() == all.Length
                && r.inventory.Select(i => i.slot).Distinct().Count() == r.inventory.Length
                && r.storage.Select(i => i.slot).Distinct().Count() == r.storage.Length
                && r.Equipment.All(i=>i.slot<WaitingEquipmentLayout.SlotCount)
                && r.Equipment.Select(i=>i.slot).Distinct().Count()==r.Equipment.Length;
        }
    }
    public sealed class WorldFacilityView
    {
        public string token, npc, room; public int scenario; public FacilityKind kind;
        public WorldOwnedItem[] inventory, storage; public int[] offers; public ulong storageFee;
        public RevivalQuote revivalQuote;
        public ShopQuote shopQuote;
        public WarehouseTerms warehouseTerms;public bool storageFeeKnown;public long storageDraftRevision;
    }
    public sealed class WorldView
    {
        public WorldRecord record; public WorldFacilityView facility;
        public string pendingPortal; public double clock, renderX, renderY; public bool moving;
        public WorldFailure failure;
        public CampaignUnit[] units;
        public CampaignPresentationView presentation;
        public string campaignSession;
        public CommanderRoster commanders;public int inspectedCommander;public CommanderRequest commanderRequest;public string commanderSession;
        public string growthSession,characterId;
        public TrainingView training;
        public TrainingView battle;
        public CampaignRetreatRequest pendingRetreat;
        public RevivalQuoteRequest revivalQuoteRequest;
        public RevivalRequest revivalRequest;
        public string currencySession;
        public CurrencyRequest currencyRequest;
        public BankTransferRequest bankTransferRequest;
        public WarehouseRequest warehouseRequest;
        public MercenaryRequest mercenaryRequest;
        public ShopRequest shopQuoteRequest,shopTradeRequest;
        public ItemUseRequest itemUseRequest;
        public MagicLoadoutRequest magicLoadoutRequest;public MagicLoadoutView magicLoadout;public int[] battleMagic;
        public CommanderAdjustmentTarget adjustmentTarget;public CommanderProfile adjustmentProfile;
        public bool MagicLoadoutNeedsConfirm
        {get{var state=adjustmentTarget==null?record.magicLoadout:adjustmentProfile?.magicLoadout;var growth=adjustmentTarget==null?record.growth:adjustmentProfile?.growth;return magicLoadout!=null&&(magicLoadout.Dirty||state?.Matches(growth)!=true||!state.selected.SequenceEqual(magicLoadout.selected));}}
        public StatAdjustmentRequest statAdjustmentRequest;public StatAdjustmentView statAdjustment;
        public ClassOptionsRequest classOptionsRequest;public ClassChangeRequest classChangeRequest;public ClassOptionsNotice classOptions;
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
    internal sealed partial class WorldSession
    {
        public string CommanderSession=Guid.NewGuid().ToString("N");
        public CommanderRequest PendingCommander;
        public int InspectedCommander;
        public CommanderRoster CommanderView()
        {
            var r=Record.roster?.Copy();if(r==null)return null;
            r.rows[0].profile=CommanderProfile.Capture(Record);r.rows[0].job=CommanderJob;return r;
        }
        public void ResetCommanderContexts()
        {
            Close();AdjustmentTarget=null;GrowthSession=Guid.NewGuid().ToString("N");CurrencySession=Guid.NewGuid().ToString("N");CampaignSession=Guid.NewGuid().ToString("N");
            PendingCurrency=null;CurrencyNotices.Clear();CampaignNotices.Clear();Effects.Clear();PendingCommander=null;
        }
        public readonly WorldDefinition Definition;
        private readonly int startingJob;
        public int CommanderJob=>Record.growth?.job??Record.roster?.rows[0].job??startingJob;
        internal WorldHireOffer[] HireOffers()=>Definition.mercenaryCatalog?.Offers(Record)??Definition.hires.Where(h=>h.commander==CommanderJob).ToArray();
        public string GrowthSession=Guid.NewGuid().ToString("N");
        public string CurrencySession=Guid.NewGuid().ToString("N");
        public readonly HashSet<string> CurrencyNotices=new HashSet<string>();
        public CurrencyRequest PendingCurrency;
        public BankTransferRequest PendingBankTransfer;
        public WorldRecord BankTransferState;
        public MercenaryRequest PendingMercenary;
        public WorldRecord MercenaryState;
        public ShopRequest PendingShopQuote,PendingShopTrade;
        public WorldRecord ShopQuoteState;
        public ShopOffer ShopOffer;
        public ClassOptionsRequest PendingClassOptions;public ClassChangeRequest PendingClassChange;
        public MagicLoadoutView MagicDraft;public MagicLoadoutRequest PendingMagicLoadout;public WorldRecord MagicBaseline,MagicRequestState;
        public readonly HashSet<string> MagicNotices=new HashSet<string>();
        public StatAdjustmentView StatDraft;public StatAdjustmentRequest PendingStatAdjustment;public WorldRecord StatBaseline,StatRequestState;
        public readonly HashSet<string> StatNotices=new HashSet<string>();
        public WorldRecord ClassOptionsState;public ClassOffer ClassOffer;
        public readonly HashSet<string> ClassNotices=new HashSet<string>();
        public ItemUseRequest PendingItemUse;
        public WorldRecord ItemUseState;
        public readonly HashSet<string> ItemUseNotices=new HashSet<string>();
        public readonly HashSet<string> ShopNotices=new HashSet<string>();
        public readonly HashSet<string> MercenaryNotices=new HashSet<string>();
        public WarehouseTerms WarehouseTerms;
        public WarehouseRequest PendingWarehouse;
        public WorldRecord WarehouseRequestState;
        public long StorageDraftRevision;
        public readonly HashSet<string> WarehouseNotices=new HashSet<string>();
        public string CharacterId;
        public TrainingCombatSession Combat;
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
        public CampaignRetreatRequest PendingRetreat;
        public TempleRevivalOffer RevivalOffer;
        public RevivalQuoteRequest PendingRevivalQuote;
        public RevivalRequest PendingRevival;
        public WorldRecord RevivalQuoteState;
        public bool SceneLocked => PhaseSavePending || Record.campaignProgress?.events.Any(e=>!e.completed)==true || (Presentation != null && !Presentation.Snapshot().completed)
            || Record.scenarioVisit!=null && Record.campaignPhase=="complete";
        public bool PresentationLocked=>SceneLocked||PendingRetreat!=null;
        public readonly HashSet<string> Applied = new HashSet<string>();
        private readonly Queue<TutorialTile> path = new Queue<TutorialTile>();
        private TutorialTile from;
        private double movement;
        private bool stopping;
        private readonly HashSet<string> arrivalPortals = new HashSet<string>();
        public bool Moving => path.Count > 0 || Combat?.Moving==true;
        public WorldSession(WorldDefinition definition, WorldRecord record, int job)
        { Definition = definition; Record = record.Copy(); startingJob = job; from = new TutorialTile(record.x, record.y); MarkArrivalPortals();BeginPresentation(); }
        private void MarkArrivalPortals()
        {arrivalPortals.Clear();foreach(var p in Map(Record.map).portals.Where(p=>p.cells.Any(c=>c.x==Record.x&&c.y==Record.y)))arrivalPortals.Add(p.id);}
        private readonly Dictionary<int,(long revision,WorldMapDefinition map)> changedMaps=new Dictionary<int,(long,WorldMapDefinition)>();
        public WorldMapDefinition Map(int id)
        {
            var original=Definition.Map(id);var progress=Record.campaignProgress;
            if(original==null||Definition.campaign?.HasNativeMaps!=true||progress==null||!progress.steps.Any(s=>s.scene==id&&(s.command.StartsWith("CHANGESPRITE ",StringComparison.Ordinal)||s.command.StartsWith("CHANGETILE2 ",StringComparison.Ordinal))))return original;
            if(changedMaps.TryGetValue(id,out var cached)&&cached.revision==progress.revision)return cached.map;
            var native=Definition.campaign.ApplyChanges(id,progress.steps);var copy=original.CopyForPlacement();var map=original.map;
            copy.map=new TutorialMap{width=map.width,height=map.height,attributes=native.attributes,renderAttributes=native.renderAttributes,terrain=map.terrain,abilities=map.abilities};
            changedMaps[id]=(progress.revision,copy);return copy;
        }
        public Func<CampaignCommand,int?> ScriptHandler;
        public string ActiveCampaignEvent;
        public void RestartPresentation()=>BeginPresentation();
        private void BeginPresentation()
        {
            var map = Definition.campaign?.Map(Record.map); Presentation = null; ActiveCampaignEvent=null; changedMaps.Clear(); Effects.Clear(); PhaseSavePending=false;
            if (map?.kind == "scenario" && Record.campaignPhase != "explore" && Record.campaignPhase != "complete")
                SetPresentation(Record.campaignPhase == "outro" ? map.outro : map.intro);
        }
        public void SetPresentation(string script,string context=null,CampaignActor[] actors=null)
        {
            ActiveCampaignEvent=null;
            Presentation=new CampaignPresentation(script,CommanderJob,Record.map,id=>Map(id)!=null,ScriptHandler,
                context??Record.map+":"+Record.campaignPhase+":"+OriginalCampaignScript.Fingerprint(script),actors);
            Presentation.Effect+=command=>Effects.Add(command);Presentation.Advance(0);
        }
        public void ClearRevival(){RevivalOffer=null;PendingRevivalQuote=null;PendingRevival=null;RevivalQuoteState=null;}
        public void ClearMagicRequest(){PendingMagicLoadout=null;MagicRequestState=null;}
        public void ClearMagicLoadout(){ClearMagicRequest();MagicDraft=null;MagicBaseline=null;}
        public void ClearStatRequest(){PendingStatAdjustment=null;StatRequestState=null;}
        public void ClearStatAdjustment(){ClearStatRequest();StatDraft=null;StatBaseline=null;}
        public void ClearClassChange(){PendingClassOptions=null;PendingClassChange=null;ClassOptionsState=null;ClassOffer=null;}
        public void ClearItemUse(){PendingItemUse=null;ItemUseState=null;}
        public void ClearShop(){PendingShopQuote=PendingShopTrade=null;ShopQuoteState=null;ShopOffer=null;}
        public void ClearMercenary(){PendingMercenary=null;MercenaryState=null;}
        public void ClearBankTransfer(){PendingBankTransfer=null;BankTransferState=null;}
        public void ClearWarehouseRequest(){PendingWarehouse=null;WarehouseRequestState=null;}
        public void Close() { Kind = FacilityKind.None; Token = Npc = null; Draft = null; PendingStorageFee = 0;ClearRevival();ClearBankTransfer();ClearMercenary();ClearShop();ClearItemUse();ClearClassChange();ClearStatAdjustment();ClearMagicLoadout();ClearWarehouseRequest();WarehouseTerms=null;StorageDraftRevision=0; }
        public bool CanInteract(WorldNpc npc)
        {
            var p = new TutorialTile(Record.x, Record.y);
            return !Moving && npc != null && (TutorialGeometry.Distance(p, new TutorialTile(npc.x, npc.y)) <= 1
                || npc.counter.Any(c => c.Tile == p));
        }
        public WorldFailure Move(int x, int y)
        {
            if (Kind != FacilityKind.None || PresentationLocked || PendingPortal!=null || Combat!=null) return WorldFailure.Busy;
            var m = Map(Record.map); var target = new TutorialTile(x, y);
            var occupied = new HashSet<TutorialTile>(m.npcs.Select(n => new TutorialTile(n.x, n.y)));
            foreach(var unit in Definition.campaign?.Map(Record.map)?.units ?? Array.Empty<CampaignUnit>()) occupied.Add(new TutorialTile(unit.x,unit.y));
            // Local routes must not cross another facility's entry on the way to a target.
            foreach (var portal in m.portals.Where(p=>!arrivalPortals.Contains(p.id)))
                foreach (var cell in portal.cells)
                    if (cell.Tile != target) occupied.Add(cell.Tile);
            bool continuing=Moving;var start=continuing?path.Peek():new TutorialTile(Record.x,Record.y);
            var route = start==target?new List<TutorialTile>():m.map.Path(start, target, 100000, occupied);
            if (route == null || (!continuing&&route.Count==0)) return WorldFailure.InvalidInput;
            // Preserve the in-flight grid step, replacing only its remaining path.
            path.Clear();if(continuing)path.Enqueue(start);
            foreach (var point in route) path.Enqueue(point);
            PendingPortal = PendingNpc = null; stopping = false;
            if(!continuing){movement=0;Face();} return WorldFailure.None;
        }
        public void StopAtNextTile() { stopping = true; }
        private double Duration => path.Count > 0 && path.Peek().X != from.X && path.Peek().Y != from.Y ? 240 : 160;
        private void Face() { from = new TutorialTile(Record.x, Record.y); Record.direction = TutorialGeometry.FacingRow(path.Peek().X - from.X, path.Peek().Y - from.Y); }
        // Returns true on a completed route or requested grid-aligned stop.
        public bool Advance(double ms)
        {
            if (double.IsNaN(ms) || double.IsInfinity(ms) || ms < 0) return false;
            Clock += ms; Combat?.Advance(ms); if (path.Count==0) return false; movement += ms;
            while (path.Count>0 && movement >= Duration)
            {
                movement -= Duration; var p = path.Dequeue(); Record.x = p.X; Record.y = p.Y;
                var portals=Map(Record.map).portals;
                arrivalPortals.RemoveWhere(id=>!portals.Any(g=>g.id==id&&g.cells.Any(c=>c.Tile==p)));
                var portal = portals.FirstOrDefault(g => !arrivalPortals.Contains(g.id)&&g.cells.Any(c => c.Tile == p));
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
        { bool changed = Record.map != record.map; Record = record.Copy(); path.Clear(); movement = 0; from = new TutorialTile(record.x, record.y); PendingPortal = PendingNpc = null;PendingRetreat=null; Close(); if(changed){Combat=null;MarkArrivalPortals();CampaignSession=Guid.NewGuid().ToString("N");CampaignNotices.Clear();BeginPresentation();} }
        public WorldView Snapshot()
        {
            double phase = path.Count==0 ? 0 : Math.Floor(movement / 80) * (Duration == 240 ? 10.0 / 32 : .5);
            var record=Record.Copy();if(Combat?.Moving==true)record.direction=Combat.Motion.Direction;
            return new WorldView { record = record, moving = Moving, clock = Clock, pendingPortal = PendingPortal, failure = Failure,
                renderX = Combat?.Moving==true?Combat.Motion.X:path.Count==0 ? Record.x : from.X + (path.Peek().X - from.X) * phase,
                renderY = Combat?.Moving==true?Combat.Motion.Y:path.Count==0 ? Record.y : from.Y + (path.Peek().Y - from.Y) * phase,
                units = (Definition.campaign?.Map(Record.map)?.units ?? Array.Empty<CampaignUnit>()).Select(u => u.Copy()).ToArray(),
                presentation = Presentation?.Snapshot(),
                campaignSession = CampaignSession,commanders=CommanderView(),inspectedCommander=InspectedCommander,commanderRequest=PendingCommander?.Copy(),commanderSession=CommanderSession,
                growthSession=GrowthSession,characterId=CharacterId,adjustmentTarget=AdjustmentTarget?.Copy(),adjustmentProfile=AdjustmentProfile?.Copy(),
                training=Map(Record.map).training?Combat?.Snapshot():null,battle=Combat?.Snapshot(),
                pendingRetreat=PendingRetreat?.Copy(),
                revivalQuoteRequest=PendingRevivalQuote?.Copy(),revivalRequest=PendingRevival?.Copy(),
                currencySession=CurrencySession,currencyRequest=PendingCurrency?.Copy(),
                bankTransferRequest=PendingBankTransfer?.Copy(),
                warehouseRequest=PendingWarehouse?.Copy(),mercenaryRequest=PendingMercenary?.Copy(),
                shopQuoteRequest=PendingShopQuote?.Copy(),shopTradeRequest=PendingShopTrade?.Copy(),itemUseRequest=PendingItemUse?.Copy(),
                magicLoadoutRequest=PendingMagicLoadout?.Copy(),magicLoadout=MagicDraft?.Copy(),battleMagic=Record.growth?.magic==null?null:(int[])Record.growth.magic.Clone(),
                statAdjustmentRequest=PendingStatAdjustment?.Copy(),statAdjustment=StatDraft?.Copy(),
                classOptionsRequest=PendingClassOptions?.Copy(),classChangeRequest=PendingClassChange?.Copy(),classOptions=ClassOffer?.Matches(this)==true?ClassOffer.Options.Copy():null,
                facility = Kind == FacilityKind.None ? null : new WorldFacilityView { token = Token, npc = Npc, kind = Kind,
                    revivalQuote=RevivalOffer?.Matches(this)==true?RevivalOffer.Quote.Copy():null,
                    shopQuote=ShopOffer?.Matches(this)==true?ShopOffer.Quote.Copy():null,
                    warehouseTerms=WarehouseTerms?.Copy(),storageDraftRevision=StorageDraftRevision,
                    storageFeeKnown=WarehouseTerms!=null?WarehouseTerms.fee.HasValue&&WarehouseTerms.feeSource.HasValue:Definition.rules.storageRuleKnown,
                    room = Kind == FacilityKind.ScenarioGateway ? Token : null, scenario = Kind == FacilityKind.ScenarioGateway ? Definition.gateways.gates.First(g=>g.id==Map(Record.map).npcs.First(n=>n.id==Npc).gateway).targetMap : 0,
                    inventory = (Draft ?? Record).inventory.Select(i => i.Copy()).ToArray(), storage = (Draft ?? Record).storage.Select(i => i.Copy()).ToArray(),
                    storageFee = PendingStorageFee, offers = (int[])(Definition.shops.FirstOrDefault(s => s.map == Record.map)?.items.Clone() ?? Array.Empty<int>()) } };
        }
    }
}
