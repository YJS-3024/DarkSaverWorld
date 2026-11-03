using System;
using System.Linq;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    public enum MagicLoadoutOperation { State,Add,Remove,Confirm }
    [Serializable] public sealed class MagicMenuEntry { public int id,iconX,iconY,mp,distance,range,team,pattern,unit;public string name; }
    [Serializable] public sealed class MagicMenuCatalog
    {public MagicMenuEntry[] entries;public MagicMenuEntry Find(int id)=>entries?.FirstOrDefault(e=>e.id==id);}
    public sealed class MagicLoadoutView
    {
        public string terms;public int[] learned,initial,selected;public long draftRevision;
        public bool Dirty=>!initial.SequenceEqual(selected);
        public MagicLoadoutView Copy()=>new MagicLoadoutView{terms=terms,learned=(int[])learned.Clone(),initial=(int[])initial.Clone(),selected=(int[])selected.Clone(),draftRevision=draftRevision};
        public bool CanAdd(int index)=>index>=0&&index<learned.Length&&selected.Length<8&&!selected.Contains(learned[index]);
        public bool CanRemove(int index)=>index>=0&&index<selected.Length;
    }
    // Legacy adapter selection contract, retained for old saved records. It is not the original learning window or native83/84/85 wire protocol.
    public sealed class MagicLoadoutRequest
    {
        public CommanderAdjustmentTarget target;
        public int? nativeRecord;
        public string id,character,session,terms;public int map,index,magic;public long revision,growthRevision,draftRevision;
        public MagicLoadoutOperation operation;public int[] selected;
        public string statTerms;public long statRevision,statDraftRevision;public AttributeTriplet[] attributes;public int? statPoints;
        public MagicLoadoutRequest Copy(){var r=(MagicLoadoutRequest)MemberwiseClone();r.target=target?.Copy();r.selected=selected==null?null:(int[])selected.Clone();r.attributes=attributes?.Select(v=>v.Copy()).ToArray();return r;}
        public static bool Same(MagicLoadoutRequest a,MagicLoadoutRequest b)=>a!=null&&b!=null&&CommanderAdjustmentTarget.Same(a.target,b.target)&&a.nativeRecord==b.nativeRecord&&a.id==b.id&&a.character==b.character&&a.session==b.session&&a.terms==b.terms
            &&a.map==b.map&&a.index==b.index&&a.magic==b.magic&&a.revision==b.revision&&a.growthRevision==b.growthRevision&&a.draftRevision==b.draftRevision&&a.operation==b.operation
            &&(a.selected==null?b.selected==null:b.selected!=null&&a.selected.SequenceEqual(b.selected))&&a.statTerms==b.statTerms&&a.statRevision==b.statRevision&&a.statDraftRevision==b.statDraftRevision&&a.statPoints==b.statPoints
            &&(a.attributes==null?b.attributes==null:AttributeTriplet.Same(a.attributes,b.attributes));
    }
    public sealed class MagicLoadoutNotice
    {
        public string id;public MagicLoadoutRequest request;public bool approved;public int[] learned,selected;
        public AttributeTriplet[] attributes;public int? statPoints;public CharacterGrowthStats effectiveStats;public CharacterGrowthHealth health;
    }
    [Serializable,DataContract] public sealed class MagicLoadoutRecord
    {
        [DataMember] public string id,request,terms;
        [DataMember] public long revision,growthRevision;
        [DataMember] public int job;
        [DataMember] public int[] selected;
        public MagicLoadoutRecord Copy(){var r=(MagicLoadoutRecord)MemberwiseClone();r.selected=selected==null?null:(int[])selected.Clone();return r;}
        public static bool Valid(MagicLoadoutRecord r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.request,"N",out _)&&Guid.TryParseExact(r.terms,"N",out _)
            &&r.revision>0&&r.growthRevision>0&&r.job>=0&&r.selected!=null&&r.selected.Length<=8&&r.selected.All(id=>id>0)&&r.selected.Distinct().Count()==r.selected.Length;
        public bool Matches(CharacterGrowthRecord growth)=>growth!=null&&job==growth.job&&selected.All(id=>growth.magic.Contains(id));
    }
}
