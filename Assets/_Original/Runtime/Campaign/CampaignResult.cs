using System;
using System.Linq;
using System.Runtime.Serialization;

namespace DarkSaver.Original.Campaign
{
    // Display records, not reward instructions or original wire opcodes.
    [Serializable,DataContract] public sealed class CampaignResultUnit
    {
        [DataMember] public int unit,job;
        [DataMember] public string name;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? level,classJob;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public ulong? experience;
        public CampaignResultUnit Copy()=>(CampaignResultUnit)MemberwiseClone();
        public bool Valid=>unit>=0&&job>=0&&!string.IsNullOrWhiteSpace(name)&&name.Length<=128&&(!level.HasValue||level>=0)&&(!classJob.HasValue||classJob>=0);
    }
    [Serializable,DataContract] public sealed class CampaignResultRecord
    {
        [DataMember] public string id,visit;
        [DataMember] public int map;
        [DataMember] public bool victory;
        [DataMember(EmitDefaultValue=false)] public string outcome;
        public bool IsDefeat=>!victory&&outcome=="defeat";
        public bool IsRetreat=>!victory&&outcome=="retreat";
        [NonSerialized,DataMember(EmitDefaultValue=false)] public ulong? experience,spoils;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? kills,eventCompleted,eventTotal;
        [DataMember] public CampaignResultUnit[] allies=Array.Empty<CampaignResultUnit>(),enemies=Array.Empty<CampaignResultUnit>();
        public CampaignResultRecord Copy()
        {var r=(CampaignResultRecord)MemberwiseClone();r.allies=allies?.Select(u=>u?.Copy()).ToArray();r.enemies=enemies?.Select(u=>u?.Copy()).ToArray();return r;}
        public static bool Valid(CampaignResultRecord r)
        {
            if(r==null)return true;
            if(!Guid.TryParseExact(r.id,"N",out _)||!Guid.TryParseExact(r.visit,"N",out _)||r.map<0
                ||!(r.victory&&(r.outcome==null||r.outcome=="victory")||r.IsDefeat||r.IsRetreat)
                ||r.kills<0||r.eventCompleted.HasValue!=r.eventTotal.HasValue||r.eventCompleted<0||r.eventTotal<r.eventCompleted
                ||r.allies==null||r.enemies==null||r.allies.Length+r.enemies.Length>1000)return false;
            var all=r.allies.Concat(r.enemies).ToArray();return all.All(u=>u?.Valid==true)&&all.Select(u=>u.unit).Distinct().Count()==all.Length;
        }
    }
    [Serializable,DataContract] public sealed class CampaignVisitRecord
    {
        [DataMember] public string id;
        [DataMember] public int map,sourceMap,x,y,direction;
        [DataMember(EmitDefaultValue=false)] public CampaignResultRecord result;
        public CampaignVisitRecord Copy(){var r=(CampaignVisitRecord)MemberwiseClone();r.result=result?.Copy();return r;}
        public static bool Valid(CampaignVisitRecord r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&r.map>=0&&r.sourceMap>=0&&r.sourceMap!=r.map&&r.x>=0&&r.y>=0&&r.direction>=0&&r.direction<4
            &&CampaignResultRecord.Valid(r.result)&&(r.result==null||r.result.visit==r.id&&r.result.map==r.map);
    }
    public sealed class CampaignResultNotice
    {
        public string character,session;
        public bool approved;
        public CampaignResultRecord result;
    }
}
