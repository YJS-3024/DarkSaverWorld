using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    [Serializable] public sealed class OriginalCombatJob
    {
        public int job,picture,baseExperience,rawAttackRange,rawMoveTick,rawAttackTick,rawRestTick,rawCommandRange,rawMovementType;
        public string name,category;
    }
    [Serializable] public sealed class OriginalCombatCatalog
    {
        public int version; public OriginalCombatJob[] jobs;
        public OriginalCommandRangeItem[] commandRangeItems;
        public OriginalCombatJob Job(int id)=>jobs?.FirstOrDefault(j=>j.job==id);
    }
    public enum TrainingAction { Move, Attack, Rest, CastMagic, UseSkill, UseItem, TroopOrder, Regroup, SoldierMove }
    // Local adapter contracts, not invented original wire opcodes. Costs/ranges are supplied by authority.
    [Serializable,DataContract] public sealed class TrainingUnit
    {
        [DataMember] public int id,job,x,y,direction=1,hp,maxHp,mp,maxMp,ap,maxAp;
        [DataMember] public long life;
        [DataMember(EmitDefaultValue=false)] public int? movementBudget,attackRange;
        [DataMember] public bool canMove,canAttack,canRest;
        [DataMember(EmitDefaultValue=false)] public BattleMagicRule[] magicRules;
        [DataMember(EmitDefaultValue=false)] public BattleItemRule[] itemRules;
        [DataMember(EmitDefaultValue=false)] public uint? nativeState;
        [DataMember(EmitDefaultValue=false)] public bool? canCommand;
        [DataMember(EmitDefaultValue=false)] public int? troopOrder;
        // Final received NativeCharacter+0x4c, not an inferred sum of job and equipment tables.
        [DataMember(EmitDefaultValue=false)] public int? commandRange;
        [DataMember(EmitDefaultValue=false)] public int? movementType;
        // Explicit local authority result of enemy eligibility; not an invented native wire field.
        [DataMember(EmitDefaultValue=false)] public int[] attackTargets;
        [DataMember(EmitDefaultValue=false)] public bool? nativeActive,nativeEnabled;
        [DataMember(EmitDefaultValue=false)] public int? nativeMenuMask;
        // Optional resolved runtime+68 appearance key; class/job and movement kind remain separate.
        [DataMember(EmitDefaultValue=false)] public int? nativeAppearanceJob;
        // Resolved client palette0..3. Unknown team/ally context is not guessed from ID.
        [DataMember(EmitDefaultValue=false)] public int? nativePalette;
        [DataMember(EmitDefaultValue=false)] public CharacterGrowthStats effectiveStats;
        [DataMember] public bool ghost;
        public TrainingUnit Copy(){var r=(TrainingUnit)MemberwiseClone();r.attackTargets=attackTargets?.ToArray();r.magicRules=magicRules?.Select(v=>v?.Copy()).ToArray();r.itemRules=itemRules?.Select(v=>v?.Copy()).ToArray();r.effectiveStats=effectiveStats?.Copy();return r;}
        public TutorialTile Tile=>new TutorialTile(x,y);
    }
    [Serializable,DataContract] public sealed class BattleLifeRecord
    {
        [DataMember] public int id;
        [DataMember] public long life;
        public BattleLifeRecord Copy()=>(BattleLifeRecord)MemberwiseClone();
    }
    // Local server persistence for approved scenario states; pending actions and presentation clocks are transient.
    [Serializable,DataContract] public sealed class ScenarioBattleRecord
    {
        [DataMember] public int map;
        [DataMember] public long revision;
        [DataMember] public string lastNotice;
        [DataMember] public TrainingUnit[] units;
        [DataMember] public BattleLifeRecord[] lives;
        public ScenarioBattleRecord Copy()
        {var r=(ScenarioBattleRecord)MemberwiseClone();r.units=units?.Select(u=>u?.Copy()).ToArray();r.lives=lives?.Select(l=>l?.Copy()).ToArray();return r;}
        public static bool Valid(ScenarioBattleRecord r)
        {
            if(r==null)return true;
            if(r.map<0||r.revision<1||!Guid.TryParseExact(r.lastNotice,"N",out _)||r.units==null||r.units.Length<1||r.units.Length>1000
                ||r.lives==null||r.lives.Length>1000||r.units.Any(u=>u==null)||r.lives.Any(l=>l==null))return false;
            if(r.units.Select(u=>u.id).Distinct().Count()!=r.units.Length||r.lives.Select(l=>l.id).Distinct().Count()!=r.lives.Length
                ||r.units.Count(u=>u.id==0)!=1||r.lives.Any(l=>l.id<0||l.id>999||l.life<1))return false;
            return r.units.All(u=>u.id>=0&&u.id<=999&&u.job>=0&&u.life>0&&u.x>=0&&u.y>=0&&u.direction>=0&&u.direction<4
                &&u.maxHp>0&&u.hp>=0&&u.hp<=u.maxHp&&u.maxMp>=0&&u.mp>=0&&u.mp<=u.maxMp&&u.maxAp>0&&u.ap>=0&&u.ap<=u.maxAp
                &&(!u.movementBudget.HasValue||u.movementBudget>=0&&u.movementBudget<=100000)&&(!u.attackRange.HasValue||u.attackRange>0)
                &&(!u.ghost||u.hp==0)&&(!u.nativeAppearanceJob.HasValue||u.nativeAppearanceJob>=0&&u.nativeAppearanceJob<=999)&&(!u.nativePalette.HasValue||u.nativePalette>=0&&u.nativePalette<4)&&(u.effectiveStats==null||u.effectiveStats.Valid)&&BattleMagicRule.ValidRules(u.magicRules)&&BattleItemRule.ValidRules(u.itemRules)&&OriginalTroopOrders.ValidMode(u.troopOrder)&&OriginalRegroupMovement.ValidMovementType(u.movementType)&&OriginalMemberCommands.ValidTargets(u.attackTargets)&&OriginalBattleTicks.ValidMask(u.nativeMenuMask)&&r.lives.Any(l=>l.id==u.id&&l.life==u.life));
        }
    }
    public sealed class TrainingActionRequest
    {
        public string character,session,id; public long revision,sourceLife,targetLife;
        public TrainingAction action; public int source,target=-1,x,y,magic=-1,mpCost,apCost,pattern,magicRange,magicDistance,magicUnit;
        public TutorialTile[] movePath;
        public TutorialTile[] nativeMovePoints;
        public int nativeMoveAxisBefore,nativeMoveAxisAfter;
        public int sourceTerrain=-1,targetTerrain=-1;
        public uint? nativeMagicSignature;
        public bool automatic;
        public BattleLifeRecord[] attackCandidates;
        public int aiHintBefore,aiHintAfter;
        public uint aiStateAfter;
        public string ownedItem;public int item=-1,itemSlot=-1;
        public int orderChoice,nativeOrder;public BattleLifeRecord[] commandMembers;
        public BattleCommandRange[] commandRanges;
        public BattleMagicTarget[] magicTargets;
        public bool IsCast=>action==TrainingAction.CastMagic||action==TrainingAction.UseSkill||action==TrainingAction.UseItem;
        // Verified outgoing Attack(10 bytes)/Rest(4 bytes)/move hint(24 bytes). A fresh array on each read;
        // other actions retain their local contract until their complete wire format is verified.
        public byte[] NativePacket=>OriginalBattleRequests.Packet(this);
        // Explicit native mode1/2: the caller must know its original command context.
        public byte[] NativeMagicPacket(int mode)=>(action==TrainingAction.CastMagic||action==TrainingAction.UseSkill)&&nativeMagicSignature.HasValue&&magicTargets!=null
            ?OriginalNativeMagic.Request(new NativeMagicPacket{magic=magic,source=source,x=x,y=y,mode=mode,signature=nativeMagicSignature.Value,targets=magicTargets.Select(t=>t.id).ToArray()}):null;
        // The original second item WORD is an inventory token; local layout slot/GUID is not it.
        public byte[] NativeItemPacket(int token)=>action==TrainingAction.UseItem&&nativeMagicSignature.HasValue&&magicTargets!=null
            ?OriginalNativeMagic.Request(new NativeMagicPacket{item=item,itemToken=token,magic=magic,source=source,x=x,y=y,signature=nativeMagicSignature.Value,targets=magicTargets.Select(t=>t.id).ToArray()}):null;
        public TrainingActionRequest Copy(){var r=(TrainingActionRequest)MemberwiseClone();r.movePath=movePath?.ToArray();r.nativeMovePoints=nativeMovePoints?.ToArray();r.attackCandidates=attackCandidates?.Select(t=>t.Copy()).ToArray();r.commandMembers=commandMembers?.Select(m=>m.Copy()).ToArray();r.commandRanges=commandRanges?.Select(m=>m.Copy()).ToArray();r.magicTargets=magicTargets?.Select(t=>t.Copy()).ToArray();return r;}
    }
    public sealed class TrainingUnitsNotice
    {
        public string character,session,id,request;
        public bool approved;
        public long expectedRevision;
        public TrainingUnit[] units;
        // Explicit affected IDs for an area cast; null is not an empty approval.
        public int[] magicTargets;
        public int[] revivedTargets;
        // Explicit MP restored by the accepted outcome (e.g. MP drain); never inferred from damage.
        public int? magicRestoreMp;
        public bool? itemConsumed;public int? itemRemainingUses;
        public BattleMagicEvent[] magicEvents;
        public BattleFeedbackEvent[] feedbackEvents;
        public BattleLifecycleEvent[] lifecycleEvents;
        public NativeBattleResponse nativeResponse;
        public NativeBattleNotice nativeMovement;
        public NativeBattleNotice nativeMagic;
        // Absolute owner XP, never a locally inferred per-kill delta or share formula.
        public ulong? experience; public long expectedGrowthRevision;
    }
    public sealed class TrainingView
    {
        public long revision;
        public TrainingUnit[] units;
        public TrainingActionRequest pending;
        public TrainingUnit Commander=>units?.FirstOrDefault(u=>u.id==0);
        public TrainingUnit Unit(int id)=>units?.FirstOrDefault(u=>u.id==id);
        public BattleCommandRange[] CommandRanges=>OriginalCommandRange.Evaluate(units);
        public BattleUnitMotionView[] motions;
        public BattleUnitMotionView commanderMotion;
        public BattleMagicEffect[] magicEffects;
        public BattleNumberEffect[] numberEffects;
        public BattleAttackEffect[] attackEffects;
        public BattleDeathEffect[] deaths;
        public string impact; public double impactAge; public int impactX,impactY;
    }
    public sealed class BattleUnitMotionView
    {
        public int unit,direction,tileX,tileY;public long life;public double x,y;
    }
    internal sealed class TrainingMotion
    {
        private readonly Queue<TutorialTile> route;
        private TutorialTile from;
        private double elapsed;
        private bool started;
        private TutorialTile? redirect;
        private Func<TutorialTile,TutorialTile[]> replan;
        public TutorialTile CurrentTile=>from;
        public TutorialTile NextTile=>Moving?route.Peek():from;
        public bool Started=>started;
        public int Direction { get; private set; }
        public bool Moving=>route.Count>0;
        private double Duration=>route.Count>0&&route.Peek().X!=from.X&&route.Peek().Y!=from.Y?240:160;
        public TrainingMotion(TutorialTile start,IEnumerable<TutorialTile> path)
        {from=start;route=new Queue<TutorialTile>(path);Face();}
        private void Face(){if(Moving)Direction=TutorialGeometry.FacingRow(route.Peek().X-from.X,route.Peek().Y-from.Y);}
        public void Advance(double ms)
        {
            if(ms>0)started=true;
            elapsed+=ms;
            while(Moving&&elapsed>=Duration)
            {
                elapsed-=Duration;from=route.Dequeue();
                if(redirect.HasValue)
                {
                    var target=redirect.Value;var points=replan(from);redirect=null;replan=null;route.Clear();
                    if(points==null)from=target;else foreach(var point in points)route.Enqueue(point);
                }
                Face();
            }
        }
        // MoveChar replaces only the latest queued destination and preserves the current leg.
        public void Redirect(TutorialTile target,Func<TutorialTile,TutorialTile[]> calculate)
        {
            if(Moving&&started){var next=route.Peek();route.Clear();route.Enqueue(next);redirect=target;replan=calculate;return;}
            route.Clear();redirect=null;replan=null;elapsed=0;started=false;
            var points=calculate(from);if(points==null)from=target;else foreach(var point in points)route.Enqueue(point);Face();
        }
        public double X=>!Moving?from.X:from.X+(route.Peek().X-from.X)*Phase;
        public double Y=>!Moving?from.Y:from.Y+(route.Peek().Y-from.Y)*Phase;
        private double Phase=>Math.Floor(elapsed/80)*(Duration==240?10.0/32:.5);
    }
    internal sealed class TrainingCombatSession
    {
        public long Revision;
        public TrainingUnit[] Units=Array.Empty<TrainingUnit>();
        public TrainingActionRequest Pending;
        public WorldRecord ItemBaseline;
        public readonly HashSet<string> Notices=new HashSet<string>(),Requests=new HashSet<string>();
        public readonly HashSet<string> PreviewedCasts=new HashSet<string>();
        public readonly Dictionary<int,long> Lives=new Dictionary<int,long>();
        public TrainingMotion Motion;
        public readonly Dictionary<int,(long life,TutorialTile target,TrainingMotion motion)> MemberMotions=new Dictionary<int,(long,TutorialTile,TrainingMotion)>();
        public readonly HashSet<int> RegroupAttempts=new HashSet<int>();
        public readonly HashSet<int> AutomationAttempts=new HashSet<int>();
        public readonly Dictionary<int,(long life,int hint)> AiHints=new Dictionary<int,(long,int)>();
        public readonly Random AiRandom=new Random();
        public readonly List<BattleMagicEffect> MagicEffects=new List<BattleMagicEffect>();
        public readonly Dictionary<int,BattleNumberEffect> NumberEffects=new Dictionary<int,BattleNumberEffect>();
        public readonly Dictionary<int,BattleAttackEffect> AttackEffects=new Dictionary<int,BattleAttackEffect>();
        public readonly Dictionary<int,(long life,int flag)> AttackFlags=new Dictionary<int,(long,int)>();
        public readonly Dictionary<int,BattleDeathEffect> Deaths=new Dictionary<int,BattleDeathEffect>();
        public string Impact; public double ImpactAge; public int ImpactX,ImpactY;
        public bool Moving=>Motion?.Moving==true;
        public void Advance(double ms)
        {
            foreach(var death in Deaths.Values)
            {
                death.Advance(ms,NumberEffects.TryGetValue(death.unit,out var n)&&n.life==death.life?Math.Max(0,27*OriginalBattleFeedback.CounterMilliseconds-n.age):0);
                if(death.Expired)
                {
                    var unit=Units.FirstOrDefault(u=>u.id==death.unit&&u.life==death.life);
                    // Verified client corpse retirement, not a server HP/XP/respawn decision.
                    if(unit!=null){unit.nativeState=0;unit.nativeEnabled=false;}
                }
            }
            Motion?.Advance(ms);
            foreach(var p in MemberMotions.Values)p.motion.Advance(ms);
            foreach(var id in MemberMotions.Where(p=>!p.Value.motion.Moving).Select(p=>p.Key).ToArray())MemberMotions.Remove(id);
            foreach(var effect in MagicEffects)effect.Advance(ms);
            MagicEffects.RemoveAll(e=>e.Expired);
            foreach(var effect in NumberEffects.Values)effect.age+=ms;
            foreach(var id in NumberEffects.Where(p=>p.Value.Expired).Select(p=>p.Key).ToArray())NumberEffects.Remove(id);
            foreach(var effect in AttackEffects.Values)effect.age+=ms;
            foreach(var id in AttackEffects.Where(p=>p.Value.Expired).Select(p=>p.Key).ToArray())AttackEffects.Remove(id);
            if(Impact!=null){ImpactAge+=ms;if(ImpactAge>=160)Impact=null;}
        }

        public TrainingView Snapshot()=>new TrainingView{revision=Revision,units=Units.Select(u=>u.Copy()).ToArray(),pending=Pending?.Copy(),commanderMotion=Motion?.Moving==true?new BattleUnitMotionView{unit=0,life=Units.First(u=>u.id==0).life,x=Motion.X,y=Motion.Y,direction=Motion.Direction,tileX=Motion.CurrentTile.X,tileY=Motion.CurrentTile.Y}:null,motions=MemberMotions.Select(p=>new BattleUnitMotionView{unit=p.Key,life=p.Value.life,x=p.Value.motion.X,y=p.Value.motion.Y,direction=p.Value.motion.Direction,tileX=p.Value.motion.CurrentTile.X,tileY=p.Value.motion.CurrentTile.Y}).ToArray(),magicEffects=MagicEffects.Select(e=>e.Copy()).ToArray(),deaths=Deaths.Values.Select(e=>e.Copy()).ToArray(),numberEffects=NumberEffects.Values.Select(e=>e.Copy()).ToArray(),attackEffects=AttackEffects.Values.Select(e=>e.Copy()).ToArray(),impact=Impact,impactAge=ImpactAge,impactX=ImpactX,impactY=ImpactY};
    }
}
