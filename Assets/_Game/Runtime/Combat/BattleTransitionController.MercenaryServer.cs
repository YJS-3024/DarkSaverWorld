using DarkSaver.Core;
using DarkSaver.Network;
namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        private NativeOwnedRoster mercenaryRosterCandidate;
        private NativeInventory mercenaryInventoryCandidate;
        public NativeInventory CaptureMercenaryInventory()=>inventory?.Clone();
        public bool CommitMercenaryTrade(NativeOwnedRoster roster,NativeInventory stock,out string reason)
        {
            reason="용병 거래를 저장할 수 없습니다.";
            if(!HasCreatedCharacter || HasPendingTutorial || transitioning || saveStore==null || roster?.IsValid!=true || stock?.IsValid!=true || mercenaryRosterCandidate!=null || worldCharacterSaveCandidate!=null)return false;
            var characterData=WorldCharacterData.Copy(worldCharacters);
            if(characterData!=null)
            {
                int count=0,job=-1;bool mixed=false;
                foreach(var unit in roster.units)if(unit.hireOwnerId==100){if(job!=-1 && job!=unit.jobId)mixed=true;job=unit.jobId;count++;}
                var record=System.Array.Find(characterData,r=>r.RecordId==100);
                if(!mixed && record?.Info!=null)
                {
                    record.Info[0x96]=(byte)job;record.Info[0x97]=(byte)(job>>8);
                    record.Info[0x98]=(byte)count;record.Info[0x99]=(byte)(count>>8);
                }
            }
            worldCharacterSaveCandidate=characterData;
            mercenaryRosterCandidate=roster.Clone();mercenaryInventoryCandidate=stock.Clone();bool previous=processingSessionRequest;processingSessionRequest=true;
            try
            {
                if(!SaveWorld()){reason=lastSaveMessage;return false;}
                ownedRoster=mercenaryRosterCandidate;inventory=mercenaryInventoryCandidate;worldCharacters=characterData;OwnedRosterRevision++;
                GetComponent<DarkSaver.UI.BattleFormationPanel>()?.UseSessionRoster();reason="";return true;
            }
            finally{worldCharacterSaveCandidate=null;mercenaryRosterCandidate=null;mercenaryInventoryCandidate=null;processingSessionRequest=previous;}
        }
    }
}
