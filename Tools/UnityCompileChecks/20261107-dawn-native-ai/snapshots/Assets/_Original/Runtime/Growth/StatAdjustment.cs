using System;
using System.Linq;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    public enum StatAdjustmentOperation { State,Step,Confirm }
    [Serializable,DataContract] public sealed class AttributeTriplet
    {
        [DataMember] public int before,after,limit;
        public AttributeTriplet Copy()=>(AttributeTriplet)MemberwiseClone();
        public bool Valid=>before>=short.MinValue&&before<=short.MaxValue&&after>=short.MinValue&&after<=short.MaxValue&&limit>=short.MinValue&&limit<=short.MaxValue;
        public static bool Same(AttributeTriplet[] a,AttributeTriplet[] b)=>a!=null&&b!=null&&a.Length==b.Length&&a.Select((v,i)=>v!=null&&b[i]!=null&&v.before==b[i].before&&v.after==b[i].after&&v.limit==b[i].limit).All(v=>v);
    }
    // Local contract; received raw attributes/costs are distinct from growth points and effective combat stats.
    public sealed class StatAdjustmentRequest
    {
        public CommanderAdjustmentTarget target;
        public int? nativeRecord;public byte[] NativePacket=>operation==StatAdjustmentOperation.State?NativeClassAdjustmentPackets.Costs(nativeRecord):operation==StatAdjustmentOperation.Step?NativeClassAdjustmentPackets.Stat(nativeRecord,stat,direction):operation==StatAdjustmentOperation.Confirm?NativeClassAdjustmentPackets.Close(nativeRecord,true):null;
        public string id,character,session,terms;public int map,stat,direction;public long revision,growthRevision,draftRevision;
        public StatAdjustmentOperation operation;public AttributeTriplet[] values;public int points;
        public StatAdjustmentRequest Copy(){var r=(StatAdjustmentRequest)MemberwiseClone();r.target=target?.Copy();r.values=values?.Select(v=>v.Copy()).ToArray();return r;}
        public static bool Same(StatAdjustmentRequest a,StatAdjustmentRequest b)=>a!=null&&b!=null&&CommanderAdjustmentTarget.Same(a.target,b.target)&&a.nativeRecord==b.nativeRecord&&a.id==b.id&&a.character==b.character&&a.session==b.session&&a.terms==b.terms
            &&a.map==b.map&&a.stat==b.stat&&a.direction==b.direction&&a.revision==b.revision&&a.growthRevision==b.growthRevision&&a.draftRevision==b.draftRevision
            &&a.operation==b.operation&&a.points==b.points&&(a.values==null?b.values==null:AttributeTriplet.Same(a.values,b.values));
    }
    public sealed class StatAdjustmentNotice
    {
        public string id;public StatAdjustmentRequest request;public bool approved;public AttributeTriplet[] values;public int? points;public int[] costs;
        public CharacterGrowthStats effectiveStats;public CharacterGrowthHealth health;
    }
    public sealed class StatAdjustmentView
    {
        public string terms;public AttributeTriplet[] initial,values;public int[] costs;public int initialPoints,points;public long draftRevision;
        public bool Dirty=>points!=initialPoints||!AttributeTriplet.Same(initial,values);
        public StatAdjustmentView Copy()=>new StatAdjustmentView{terms=terms,initial=initial.Select(v=>v.Copy()).ToArray(),values=values.Select(v=>v.Copy()).ToArray(),costs=(int[])costs.Clone(),initialPoints=initialPoints,points=points,draftRevision=draftRevision};
        public bool TryStep(int stat,int direction,out AttributeTriplet[] next,out int remaining)
        {
            next=null;remaining=points;if(stat<0||stat>=6||(direction!=1&&direction!=2))return false;
            int value=values[stat].after,cost=costs[stat];
            if(direction==1){if(points<cost||value==short.MaxValue)return false;value++;remaining-=cost;}
            else{if(value<=initial[stat].after||points>short.MaxValue-cost)return false;value--;remaining+=cost;}
            next=values.Select(v=>v.Copy()).ToArray();next[stat].after=value;return true;
        }
    }
    [Serializable,DataContract] public sealed class StatAllocationRecord
    {
        [DataMember] public string id,request,terms;
        [DataMember] public long revision,growthRevision;
        [DataMember] public int job,points;
        [DataMember] public AttributeTriplet[] values;
        public StatAllocationRecord Copy(){var r=(StatAllocationRecord)MemberwiseClone();r.values=values?.Select(v=>v.Copy()).ToArray();return r;}
        public static bool Valid(StatAllocationRecord r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.request,"N",out _)&&Guid.TryParseExact(r.terms,"N",out _)
            &&r.revision>0&&r.growthRevision>0&&r.job>=0&&r.points>=0&&r.points<=short.MaxValue&&r.values?.Length==6&&r.values.All(v=>v?.Valid==true);
    }
}
