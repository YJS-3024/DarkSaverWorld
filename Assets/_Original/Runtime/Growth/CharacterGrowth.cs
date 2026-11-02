using System;
using System.Linq;
using System.Runtime.Serialization;

namespace DarkSaver.Original.World
{
    [Serializable] public sealed class CharacterGrowthRow
    {
        public int job,classLevel,nextJob,tablePoints,magicLevelUp;
        public long requiredExperience;
        public int[] magic,skills,sourceLines;
        public string rawMagic,rawSkills;
        public bool abilitiesResolved;
    }
    [Serializable] public sealed class CharacterGrowthJob
    {public int id,picture,kind;public string name;}
    [Serializable] public sealed class CharacterGrowthCatalog
    {
        public int version;public string sourceHash;
        public CharacterGrowthJob[] jobs;
        public CharacterGrowthRow[] rows;
        public int[] magicIds,skillIds;
        public CharacterGrowthJob Job(int id)=>jobs?.FirstOrDefault(j=>j.id==id);
        public CharacterGrowthRow Row(int job,int level)=>rows?.FirstOrDefault(r=>r.job==job&&r.classLevel==level);
        public bool Accepts(CharacterGrowthRecord record)=>CharacterGrowthRecord.Valid(record)&&record!=null&&Job(record.job)!=null&&Row(record.job,record.classLevel)!=null
            &&record.magic.All(id=>magicIds?.Contains(id)==true)&&record.skills.All(id=>skillIds?.Contains(id)==true);
    }
    [Serializable,DataContract] public sealed class CharacterGrowthStats
    {
        [DataMember] public int attack,defense,magicAttack,magicDefense;
        [DataMember(EmitDefaultValue=false)] public short[] nativeValues;
        public CharacterGrowthStats Copy(){var r=(CharacterGrowthStats)MemberwiseClone();r.nativeValues=nativeValues==null?null:(short[])nativeValues.Clone();return r;}
        public bool Valid=>attack>=0&&defense>=0&&magicAttack>=0&&magicDefense>=0&&(nativeValues==null||nativeValues.Length==16);
    }
    [Serializable,DataContract] public sealed class CharacterGrowthRecord
    {
        [DataMember(EmitDefaultValue=false)] public uint? nextExperience;
        [DataMember(EmitDefaultValue=false)] public short[] nativeLevels;
        [DataMember] public int version=1,job,classLevel,totalLevel;
        [DataMember] public ulong experience;
        [DataMember] public long revision;
        [DataMember] public string lastNotice;
        [DataMember(EmitDefaultValue=false)] public int? points,magicLevel;
        [DataMember] public int[] magic=Array.Empty<int>(),skills=Array.Empty<int>();
        [DataMember(EmitDefaultValue=false)] public CharacterGrowthStats stats;
        public CharacterGrowthRecord Copy()
        {var r=(CharacterGrowthRecord)MemberwiseClone();r.magic=magic==null?null:(int[])magic.Clone();r.skills=skills==null?null:(int[])skills.Clone();r.stats=stats?.Copy();r.nativeLevels=nativeLevels==null?null:(short[])nativeLevels.Clone();return r;}
        public static bool Valid(CharacterGrowthRecord r)=>r==null||r.version==1&&r.job>=0&&r.classLevel>0&&r.totalLevel>=r.classLevel&&r.revision>0
            &&Guid.TryParseExact(r.lastNotice,"N",out _)&&(!r.points.HasValue||r.points.Value>=0)&&(!r.magicLevel.HasValue||r.magicLevel.Value>=0)
            &&r.magic!=null&&r.skills!=null&&r.magic.All(id=>id>0)&&r.skills.All(id=>id>0)
            &&r.magic.Distinct().Count()==r.magic.Length&&r.skills.Distinct().Count()==r.skills.Length&&(r.stats==null||r.stats.Valid)&&(r.nativeLevels==null||r.nativeLevels.Length==2);
    }
    public enum CharacterGrowthOperation { Snapshot,LevelChanged,ClassChanged }
    // In-process authoritative results. This does not define new original wire opcodes or server formulas.
    public sealed class CharacterGrowthHealth
    {public int hp,maxHp,mp,maxMp;public bool Valid=>hp>=0&&maxHp>0&&hp<=maxHp&&mp>=0&&maxMp>=0&&mp<=maxMp;}
    public sealed class CharacterGrowthNotice
    {
        public string character,session,id;public bool approved;public long expectedRevision;
        public CharacterGrowthOperation operation;
        public CharacterGrowthRecord state;
        public CharacterGrowthHealth health;
    }
    public sealed class CharacterExperienceNotice
    {
        public string character,session,id;public bool approved;public long expectedRevision;
        public ulong experience;
    }
}
