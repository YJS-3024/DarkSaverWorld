using System;
using System.Runtime.Serialization;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Campaign
{
    public enum CampaignExitKind { Retreat, Defeat, Terminated }
    public sealed class CampaignRetreatRequest
    {
        public string character,session,visit,id;
        public long battleRevision;
        public CampaignRetreatRequest Copy()=>(CampaignRetreatRequest)MemberwiseClone();
    }
    public sealed class CampaignExitNotice
    {
        public string character,session,visit,id,request;
        public bool approved;
        // Explicit local authority contract, not a field of54: end first, then
        // await its display report. Default keeps existing immediate-return behavior.
        public bool awaitResult;
        public CampaignExitKind kind;
        public long expectedBattleRevision;
        public CharacterGrowthHealth health;
        public CampaignResultRecord result;
    }
    [Serializable,DataContract] public sealed class CampaignExitRecord
    {
        [DataMember] public string id,visit;
        [DataMember] public int map;
        [DataMember] public CampaignExitKind kind;
        public CampaignExitRecord Copy()=>(CampaignExitRecord)MemberwiseClone();
        public static bool Valid(CampaignExitRecord r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.visit,"N",out _)&&r.map>=0&&Enum.IsDefined(typeof(CampaignExitKind),r.kind);
    }
}
