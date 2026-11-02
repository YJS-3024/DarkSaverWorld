using System;
using System.Linq;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    // Received candidates/points, never inferred from nextJob or local XP thresholds.
    public sealed class ClassCandidate
    {
        public int job;public string description;
        public ClassCandidate Copy()=>(ClassCandidate)MemberwiseClone();
    }
    public sealed class ClassOptionsRequest
    {
        public CommanderAdjustmentTarget target;
        public string id,character,session;public int map;public long revision;
        public ClassOptionsRequest Copy(){var r=(ClassOptionsRequest)MemberwiseClone();r.target=target?.Copy();return r;}
        public static bool Same(ClassOptionsRequest a,ClassOptionsRequest b)=>a!=null&&b!=null&&CommanderAdjustmentTarget.Same(a.target,b.target)&&a.id==b.id&&a.character==b.character&&a.session==b.session&&a.map==b.map&&a.revision==b.revision;
    }
    public sealed class ClassOptionsNotice
    {
        public string id;public ClassOptionsRequest request;public bool approved;public int? classPoints;
        public ClassCandidate left,right;public ClassCandidate[] options;
        public ClassOptionsNotice Copy()=>new ClassOptionsNotice{id=id,request=request?.Copy(),approved=approved,classPoints=classPoints,left=left?.Copy(),right=right?.Copy(),options=options?.Select(c=>c?.Copy()).ToArray()};
        public ClassCandidate Candidate(int slot,int index)=>slot==0&&index==0?left:slot==1&&index==0?right:slot==2&&index>=0&&index<(options?.Length??0)?options[index]:null;
    }
    public sealed class ClassChangeRequest
    {
        public CommanderAdjustmentTarget target;
        public string id,options,character,session;public int map,slot,index,job;public long revision;
        public ClassChangeRequest Copy(){var r=(ClassChangeRequest)MemberwiseClone();r.target=target?.Copy();return r;}
        public static bool Same(ClassChangeRequest a,ClassChangeRequest b)=>a!=null&&b!=null&&CommanderAdjustmentTarget.Same(a.target,b.target)&&a.id==b.id&&a.options==b.options&&a.character==b.character&&a.session==b.session
            &&a.map==b.map&&a.slot==b.slot&&a.index==b.index&&a.job==b.job&&a.revision==b.revision;
    }
    public sealed class ClassChangeNotice
    {
        public string id;public ClassChangeRequest request;public bool approved;
        public CharacterGrowthRecord state;public CharacterGrowthHealth health;
    }
    [Serializable,DataContract] public sealed class ClassChangeReceipt
    {
        [DataMember] public string id,request,options;
        [DataMember] public long growthRevision;
        [DataMember] public int previousJob,job,slot,index;
        public ClassChangeReceipt Copy()=>(ClassChangeReceipt)MemberwiseClone();
        public static bool Valid(ClassChangeReceipt r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.request,"N",out _)&&Guid.TryParseExact(r.options,"N",out _)
            &&r.growthRevision>0&&r.previousJob>=0&&r.job>=0&&r.job!=r.previousJob&&r.slot>=0&&r.slot<3&&r.index>=0&&(r.slot==2||r.index==0);
    }
    internal sealed class ClassOffer
    {
        public ClassOptionsNotice Options;public WorldRecord State;
        public bool Matches(WorldSession s)=>Options?.request!=null&&s.Kind==FacilityKind.None&&Options.request.character==s.CharacterId&&Options.request.session==s.GrowthSession
            &&Options.request.map==s.Record.map&&s.AdjustmentTargetMatches&&CommanderAdjustmentTarget.Same(Options.request.target,s.AdjustmentTarget)&&Options.request.revision==(s.AdjustmentRecord?.growth?.revision??0);
    }
}
