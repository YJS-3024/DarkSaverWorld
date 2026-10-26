using DarkSaver.Core;

namespace DarkSaver.Network
{
    // Native local account/character service. No external online account connection.
    public sealed class LocalCharacterServer
    {
        private bool connected,prepared;
        private readonly string legacyHistoryPath;
        private readonly string[] legacySlotPaths;
        private string historyPath,accountId;
        private readonly NativeAccountDirectory accounts;
        private string[] slotPaths;
        public LocalCharacterServer(string loginHistoryPath=null,string[] paths=null)
        {
            legacyHistoryPath=System.IO.Path.GetFullPath(loginHistoryPath ?? NativeLoginHistory.DefaultPath);
            legacySlotPaths=paths==null?NativeCharacterSlots.AllPaths():(string[])paths.Clone();
            if(legacySlotPaths.Length==0 || legacySlotPaths.Length>NativeCharacterSlots.Count)throw new System.ArgumentException("Invalid character slot count.");
            accounts=new NativeAccountDirectory(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(legacyHistoryPath),"native-account.json"));
            historyPath=legacyHistoryPath;slotPaths=(string[])legacySlotPaths.Clone();
        }
        private void ResetConnection()
        {
            connected=false;prepared=false;accountId=null;historyPath=legacyHistoryPath;slotPaths=(string[])legacySlotPaths.Clone();
            NativeSaveStore.ClearResume();NativeCharacterProfile.ClearPending();
        }
        public LocalCommandResponse Logout(LogoutRequest request){ResetConnection();return new LocalCommandResponse(true);}
        public LocalCommandResponse Status(LoginStatusRequest request)=>InspectLogin();
        private LoginResponse InspectLogin(bool authenticated=false)
        {
            if(!connected)return new LoginResponse(true,hasAccount:accounts.HasAccounts,accountId:accounts.LegacyAccountId);
            bool ready=NativeLoginHistory.TryResolve(historyPath,slotPaths,out var resume,out var selection,out var reason,accountId);
            return new LoginResponse(ready,reason,selection,resume,accounts.HasAccounts,accountId,authenticated,slotPaths);
        }
        public LocalCommandResponse Register(AccountRegisterRequest request)
        {
            if(prepared)return new AccountRegisterResponse(false,"캐릭터 진입 중에는 가입할 수 없습니다.");
            if(!accounts.TryRegister(request.UserId,request.Password,request.Confirmation,out var reason))return new AccountRegisterResponse(false,reason);
            ResetConnection();
            return new AccountRegisterResponse(true,"회원가입이 완료되었습니다. 로그인해 주세요.",NativeAccountDirectory.NormalizeId(request.UserId));
        }
        public LocalCommandResponse Login(LoginRequest request)
        {
            ResetConnection();
            if(accounts.HasAccounts)
            {
                if(!accounts.TryAuthenticate(request.UserId,request.Password,out var reason))
                    return new LoginResponse(false,reason,hasAccount:true);
            }
            else if(!string.IsNullOrEmpty(request.UserId) || !string.IsNullOrEmpty(request.Password))
                return new LoginResponse(false,"등록된 계정이 없습니다. 회원가입해 주세요.");
            accountId=string.IsNullOrEmpty(request.UserId)?null:NativeAccountDirectory.NormalizeId(request.UserId);
            historyPath=accounts.HistoryPath(accountId,legacyHistoryPath);slotPaths=accounts.SlotPaths(accountId,legacySlotPaths);
            connected=true;return InspectLogin(true);
        }
        public LocalCommandResponse List(CharacterListRequest request)
        {
            if(!connected)return new LocalCommandResponse(false,"로컬 연결을 먼저 시작하세요.");
            var occupied=new bool[slotPaths.Length];var profiles=new NativeCharacterProfile[slotPaths.Length];
            for(int i=0;i<profiles.Length;i++)
            {
                string path=slotPaths[i];occupied[i]=NativeCharacterSlots.IsOccupied(path);
                if(new NativeSaveStore(path).TryLoad(out var data,out _) && data.hasCharacter)profiles[i]=data.character.Clone();
            }
            return new CharacterListResponse(occupied,profiles);
        }
        private bool OwnsSlot(string path)=>!string.IsNullOrWhiteSpace(path) && System.Array.Exists(slotPaths,slot=>NativeAccountDirectory.SamePath(slot,path));
        public LocalCommandResponse Create(CharacterCreateRequest request)
        {
            if(!connected || prepared || !OwnsSlot(request.LocalSavePath))return new CharacterCreateResponse(false,"캐릭터 생성 연결 상태를 확인하세요.");
            if(NativeCharacterSlots.IsOccupied(request.LocalSavePath))return new CharacterCreateResponse(false,"이미 저장된 슬롯입니다. 다른 빈 슬롯을 선택해 주세요.");
            if(!NativeCharacterProfile.TryQueue(request.Name,request.Job,request.LocalSavePath,out var reason))return new CharacterCreateResponse(false,reason);
            if(!NativeLoginHistory.TryRemember(historyPath,request.LocalSavePath,out reason,accountId))
            {NativeCharacterProfile.ClearPending();return new CharacterCreateResponse(false,reason);}
            prepared=true;return new CharacterCreateResponse(true);
        }
        public LocalCommandResponse Select(CharacterSelectRequest request)
        {
            if(!connected || prepared || !OwnsSlot(request.LocalSavePath))return new LocalCommandResponse(false,"캐릭터 선택 연결 상태를 확인하세요.");
            var store=new NativeSaveStore(request.LocalSavePath);
            if(!store.TryLoad(out var data,out var reason))return new LocalCommandResponse(false,reason);
            if(!data.hasCharacter)return new LocalCommandResponse(false,"이 저장에는 선택할 캐릭터가 없습니다.");
            if(!NativeLoginHistory.TryRemember(historyPath,store.FilePath,out reason,accountId))return new LocalCommandResponse(false,reason);
            if(!NativeSaveStore.QueueResume(data,store.FilePath))return new LocalCommandResponse(false,"캐릭터 복원 준비에 실패했습니다.");
            prepared=true;return new LocalCommandResponse(true);
        }
    }
}
