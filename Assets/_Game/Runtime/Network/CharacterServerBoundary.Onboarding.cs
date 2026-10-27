using System;
using DarkSaver.Core;
namespace DarkSaver.Network
{
    public sealed partial class CharacterServerBoundary
    {
        private bool entryPrepared;
        private bool OwnsSlot(string path)=>!string.IsNullOrWhiteSpace(path)
            && Array.Exists(accounts.SlotPaths(accountId,legacySlots),p=>NativeAccountDirectory.SamePath(p,path));
        public LocalCommandResponse Create(CharacterCreateRequest request)
        {
            if(!authenticated || selectedServerId!=LocalServerId || entryPrepared || request==null || !OwnsSlot(request.LocalSavePath))
                return new CharacterCreateResponse(false,"로그인과 서버·캐릭터 슬롯 선택을 확인해 주세요.");
            if(!NativeCharacterProfile.IsStartingJob(request.Job))
                return new CharacterCreateResponse(false,"파이터·나이트·매지션 중 선택해 주세요.");
            string name=request.Name?.Trim();
            if(!NativeCharacterProfile.IsValidName(name))return new CharacterCreateResponse(false,"올바른 캐릭터 이름을 입력해 주세요.");
            var data=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,
                mapScene="Kaosia_Map",runtimeScene="Town_Runtime",worldX=21,worldY=21,clearedScenarios=Array.Empty<string>(),
                hasCharacter=true,character=new NativeCharacterProfile {name=name,jobId=request.Job,tutorial=new NativeTutorialProgress()}};
            // This flow grants no guessed gold, equipment, levels or combat values.
            return new CharacterCreateResponse(new NativeSaveStore(request.LocalSavePath).TryCreateNew(data,out var reason),reason);
        }
        public LocalCommandResponse Select(CharacterSelectRequest request)
        {
            if(!authenticated || selectedServerId!=LocalServerId || entryPrepared || request==null || !OwnsSlot(request.LocalSavePath))
                return new CharacterEntryResponse(false,reason:"로그인과 서버·캐릭터 선택을 확인해 주세요.");
            if(!new NativeSaveStore(request.LocalSavePath).TryLoad(out var data,out var reason) || !data.hasCharacter)
                return new CharacterEntryResponse(false,reason:reason ?? "캐릭터 정보가 없습니다.");
            // A new authenticated entry restarts an unfinished lesson. Checkpoints
            // remain usable within this session, but never resume a partial lesson.
            if(data.character.tutorial!=null && !data.character.tutorial.IsComplete)
            {
                data.character.tutorial=new NativeTutorialProgress();
                if(!new NativeSaveStore(request.LocalSavePath).TrySave(data,out reason))
                    return new CharacterEntryResponse(false,reason:reason);
            }
            var result=new CharacterEntryResponse(true,accountId,selectedServerId,request.LocalSavePath,data);
            if(result.CaptureState()==null)return new CharacterEntryResponse(false,reason:"캐릭터 진입 정보를 확인할 수 없습니다.");
            entryPrepared=true;return result;
        }
    }
    public sealed class CharacterEntryResponse:LocalCommandResponse
    {
        private readonly NativeSaveData state;
        public string AccountId {get;}
        public int ServerId {get;}
        public string SavePath {get;}
        public bool RequiresTutorial=>state?.character?.tutorial!=null && !state.character.tutorial.IsComplete;
        public NativeSaveData CaptureState()=>NativeSaveStore.CopyForTransfer(state);
        public CharacterEntryResponse(bool accepted,string accountId=null,int serverId=0,string savePath=null,NativeSaveData state=null,string reason=null):base(accepted,reason)
        {AccountId=accountId;ServerId=serverId;SavePath=savePath;this.state=NativeSaveStore.CopyForTransfer(state);}
    }
}
