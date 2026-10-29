using System;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Network;
namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        private WorldCharacterData[] worldCharacters,worldCharacterSaveCandidate;
        public WorldCharacterData[] CaptureWorldCharacterData()=>WorldCharacterData.Copy(worldCharacters);
        public WorldRosterEntry[] CaptureWorldCharacterRoster()
        {
            if(!HasCreatedCharacter)return Array.Empty<WorldRosterEntry>();
            string JobName(int id){foreach(var c in formationCatalogs)if(c!=null && c.TryGet(id,out var entry))return entry.Name;return NativeCharacterProfile.StartingJobName(id);}
            var rows=new List<WorldRosterEntry>{new WorldRosterEntry{RecordId=100,JobId=character.jobId,Name=character.name,JobName=JobName(character.jobId),Profile=character.Clone(),Experience=playerExperience,
                MaxHp=hasPlayerVitals?(int?)playerMaxHp:null,MaxMp=hasPlayerVitals?(int?)playerMaxMp:null}};
            if(ownedRoster?.units!=null)foreach(var u in ownedRoster.units)
            {
                if(!DarkSaver.Jobs.NativePlayerJobs.Contains(u.jobId) || u.instanceId<0 || u.instanceId>ushort.MaxValue || rows.Exists(r=>r.RecordId==u.instanceId))continue;
                rows.Add(new WorldRosterEntry{RecordId=(ushort)u.instanceId,JobId=u.jobId,Name=u.name,JobName=JobName(u.jobId),MaxHp=u.maxHp,MaxMp=u.maxMp,Attack=u.attack,Defense=u.defense,Experience=u.experience});
                if(rows.Count==10)break;
            }
            return rows.ToArray();
        }
        public bool TrySaveWorldCharacterData(WorldCharacterData[] data,out string reason)
        {
            reason="캐릭터 설정을 저장할 준비가 되지 않았습니다.";
            if(!HasCreatedCharacter || transitioning || saveStore==null || worldCharacterSaveCandidate!=null || !WorldCharacterData.Validate(data) || data==null)return false;
            var owned=CaptureWorldCharacterRoster();
            foreach(var row in data)if(!Array.Exists(owned,r=>r.RecordId==row.RecordId)){reason="보유하지 않은 캐릭터의 설정입니다.";return false;}
            var candidate=WorldCharacterData.Copy(data);worldCharacterSaveCandidate=candidate;
            bool previousRequest=processingSessionRequest;processingSessionRequest=true;
            try
            {
                if(!SaveWorld()){reason=lastSaveMessage;return false;}
                worldCharacters=candidate;reason=string.Empty;return true;
            }
            finally{worldCharacterSaveCandidate=null;processingSessionRequest=previousRequest;}
        }
        public DarkSaver.Jobs.JobCatalog WorldCharacterJobCatalog
        {get{foreach(var catalog in formationCatalogs)if(catalog!=null && catalog.TryGet(PlayerJobId,out _))return catalog;return null;}}
    }
}
