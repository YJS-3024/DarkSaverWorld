using System;
using System.Linq;
using System.Runtime.Serialization;
namespace DarkSaver.Original.World
{
    // Shared info-manager header used by F3, distinct from the selected owned general record.
    [Serializable,DataContract] public sealed class PlayerIdentityRecord
    {
        [DataMember] public string id;[DataMember] public long revision;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? age,gender;
        [DataMember(EmitDefaultValue=false)] public string spouse;
        public PlayerIdentityRecord Copy()=>(PlayerIdentityRecord)MemberwiseClone();
        public static bool Valid(PlayerIdentityRecord r)=>r==null||r.revision>0&&Guid.TryParseExact(r.id,"N",out _)
            &&(!r.gender.HasValue||r.gender>=0&&r.gender<=2)&&(r.spouse==null||!r.spouse.Any(char.IsControl));
    }
    public sealed class PlayerIdentityNotice
    {
        public string id,character,session,spouse;public long expectedRevision;public bool approved;public int? age,gender;
    }
}
