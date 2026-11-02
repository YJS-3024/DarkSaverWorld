using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DarkSaver.Original.World
{
    // Local service contracts; original resurrection wire opcodes/pricing are not inferred.
    public sealed class RevivalQuoteRequest
    {
        public string id,character,session,token,npc;public int map;public long revision;
        public RevivalQuoteRequest Copy()=>(RevivalQuoteRequest)MemberwiseClone();
    }
    public sealed class RevivalQuote
    {
        public string id,request,character,session,token,npc;public int map;public long revision;
        public bool approved;public ulong? fee;
        public RevivalQuote Copy()=>(RevivalQuote)MemberwiseClone();
    }
    public sealed class RevivalRequest
    {
        public string id,quote,character,session,token,npc;public int map;public long revision;
        public RevivalRequest Copy()=>(RevivalRequest)MemberwiseClone();
    }
    public sealed class RevivalNotice
    {
        public string id,request,quote,character,session,token,npc;public int map;public long revision;
        public bool approved;public CharacterGrowthHealth health;public ulong? gold;
    }
    [Serializable,DataContract] public sealed class RevivalReceipt
    {
        [DataMember] public string id,request,quote;
        [DataMember] public long revision;
        [DataMember] public ulong fee;
        public RevivalReceipt Copy()=>(RevivalReceipt)MemberwiseClone();
        public static bool Valid(RevivalReceipt r)=>r==null||r.revision>0&&Guid.TryParseExact(r.id,"N",out _)&&Guid.TryParseExact(r.request,"N",out _)&&Guid.TryParseExact(r.quote,"N",out _);
    }
    internal sealed class TempleRevivalOffer
    {
        public RevivalQuote Quote;
        public WorldRecord State;
        public bool Matches(WorldSession world)
        {
            var r=world.Record;var q=Quote;var s=State;
            return world.Kind==FacilityKind.Temple&&world.Token==q.token&&world.Npc==q.npc&&world.CharacterId==q.character&&world.CampaignSession==q.session
                &&r.map==q.map&&r.x==s.x&&r.y==s.y&&r.direction==s.direction&&r.ghost==s.ghost&&r.healthKnown==s.healthKnown
                &&r.hp==s.hp&&r.mp==s.mp&&r.maxHp==s.maxHp&&r.maxMp==s.maxMp&&r.goldKnown==s.goldKnown&&r.gold==s.gold
                &&(r.revival?.revision??0)==q.revision&&(r.growth?.revision??0)==(s.growth?.revision??0)&&(r.currency?.revision??0)==(s.currency?.revision??0)&&r.lastScenarioExit?.id==s.lastScenarioExit?.id;
        }
        // Compare only for quote/commit operations, never in the per-frame UI path.
        public static bool SameState(WorldRecord a,WorldRecord b)
        {
            if(a==null||b==null)return a==b;
            var serializer=new DataContractJsonSerializer(typeof(WorldRecord));
            using(var first=new MemoryStream())using(var second=new MemoryStream())
            {serializer.WriteObject(first,a);serializer.WriteObject(second,b);return first.ToArray().SequenceEqual(second.ToArray());}
        }
    }
}
