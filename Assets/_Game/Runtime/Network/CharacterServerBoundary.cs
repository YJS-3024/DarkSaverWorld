using System;
using System.IO;
using DarkSaver.Core;
namespace DarkSaver.Network
{
    // User-authorized local authentication service. Original online wire opcodes
    // remain unconfirmed; character/world initialization is a separate boundary.
    public sealed partial class CharacterServerBoundary
    {
        private readonly NativeAccountDirectory accounts;
        private readonly string[] legacySlots;
        private readonly string configuredHistory;
        private readonly Func<NativeSaveData,NativeSaveData> prepareEntry;
        private bool authenticated;
        private string accountId;
        public const int LocalServerId=1;
        private int selectedServerId;
        public int SelectedServerId=>authenticated?selectedServerId:0;
        public CharacterServerBoundary(string loginHistoryPath=null,string[] paths=null,Func<NativeSaveData,NativeSaveData> prepareEntry=null)
        {
            string history=Path.GetFullPath(loginHistoryPath ?? NativeLoginHistory.DefaultPath);
            configuredHistory=history;this.prepareEntry=prepareEntry;
            legacySlots=paths==null?NativeCharacterSlots.AllPaths():(string[])paths.Clone();
            if(legacySlots.Length!=NativeCharacterSlots.Count || Array.Exists(legacySlots,string.IsNullOrWhiteSpace))
                throw new ArgumentException("Exactly ten valid local slot paths are required.",nameof(paths));
            for(int i=0;i<legacySlots.Length;i++)legacySlots[i]=Path.GetFullPath(legacySlots[i]);
            accounts=new NativeAccountDirectory(Path.Combine(Path.GetDirectoryName(history),"native-account.json"));
        }
        internal bool MatchesConfiguration(string history,string[] paths)
        {
            if(history!=null && !NativeAccountDirectory.SamePath(configuredHistory,history))return false;
            if(paths==null)return true;
            if(paths.Length!=legacySlots.Length)return false;
            for(int i=0;i<paths.Length;i++)if(!NativeAccountDirectory.SamePath(paths[i],legacySlots[i]))return false;
            return true;
        }
        private void ResetAuthentication(){authenticated=false;accountId=null;selectedServerId=0;entryPrepared=false;}
        private LoginResponse State()=>new LoginResponse(true,requiresCharacterSelection:true,
            hasAccount:accounts.HasAccounts,accountId:accountId,authenticated:authenticated,
            slotPaths:authenticated?accounts.SlotPaths(accountId,legacySlots):null);
        public LocalCommandResponse Logout(LogoutRequest request)
        {
            ResetAuthentication();NativeSaveStore.ClearResume();NativeCharacterProfile.ClearPending();
            return new LocalCommandResponse(true);
        }
        public LocalCommandResponse Status(LoginStatusRequest request)=>State();
        public LocalCommandResponse Login(LoginRequest request)
        {
            ResetAuthentication();
            if(request==null)return new LoginResponse(false,"로그인 요청이 없습니다.");
            if(!accounts.TryAuthenticate(request.UserId,request.Password,out var reason))
                return new LoginResponse(false,reason,
                    hasAccount:accounts.HasAccounts);
            accountId=NativeAccountDirectory.NormalizeId(request.UserId);authenticated=true;
            return State(); // Never resume a save, create a character or enter a map here.
        }
        public LocalCommandResponse Register(AccountRegisterRequest request)
        {
            if(request==null)return new AccountRegisterResponse(false,"회원가입 요청이 없습니다.");
            if(!accounts.TryRegister(request.UserId,request.Password,request.Confirmation,out var reason))
                return new AccountRegisterResponse(false,reason);
            ResetAuthentication();
            return new AccountRegisterResponse(true,"회원가입이 완료되었습니다. 로그인해 주세요.",
                NativeAccountDirectory.NormalizeId(request.UserId));
        }
        public LocalCommandResponse ListServers(ServerListRequest request)
        {
            if(!authenticated || request==null)return new ServerListResponse(false,reason:"먼저 로그인해 주세요.");
            return new ServerListResponse(true,accountId,new[]{new LocalServerInfo(LocalServerId,"로컬 서버",true)});
        }
        public LocalCommandResponse SelectServer(ServerSelectRequest request)
        {
            if(!authenticated)return new ServerSelectResponse(false,reason:"먼저 로그인해 주세요.");
            if(request==null || request.ServerId!=LocalServerId)
                return new ServerSelectResponse(false,reason:"선택할 수 없는 서버입니다.");
            selectedServerId=LocalServerId;
            return new ServerSelectResponse(true,selectedServerId,accountId);
        }
        public LocalCommandResponse List(CharacterListRequest request)
        {
            if(!authenticated || selectedServerId!=LocalServerId || request==null)
                return new LocalCommandResponse(false,"로그인 후 서버를 선택해 주세요.");
            var paths=accounts.SlotPaths(accountId,legacySlots);
            var occupied=new bool[paths.Length];var profiles=new NativeCharacterProfile[paths.Length];
            for(int i=0;i<paths.Length;i++)
            {
                occupied[i]=NativeCharacterSlots.IsOccupied(paths[i]);
                if(new NativeSaveStore(paths[i]).TryLoad(out var data,out _) && data.hasCharacter)
                    profiles[i]=data.character.Clone();
            }
            int preferred=-1;string entryReason=null;
            if(NativeLoginHistory.TryRead(accounts.HistoryPath(accountId,configuredHistory),out var last,out var historyReason,accountId))
            {
                preferred=Array.FindIndex(paths,p=>NativeAccountDirectory.SamePath(p,last));
                if(preferred<0 || profiles[preferred]==null)entryReason="마지막 캐릭터 저장을 확인해 주세요.";
            }
            else if(historyReason!=null)entryReason=historyReason;
            return new CharacterListResponse(occupied,profiles,accountId,selectedServerId,preferred,entryReason);
        }
    }
}
