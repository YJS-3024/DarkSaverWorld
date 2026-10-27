using DarkSaver.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using DarkSaver.Core;

namespace DarkSaver.UI
{
    public sealed partial class StartMenuController : MonoBehaviour
    {
        private ServerManager protocol;
        private string localHistoryPath;
        private string[] localSlotPaths;
        public bool ConfigureLocalLogin(string historyPath,string[] paths)
        {
            if(protocol!=null || paths==null || paths.Length!=NativeCharacterSlots.Count || string.IsNullOrWhiteSpace(historyPath))return false;
            foreach(string path in paths)if(string.IsNullOrWhiteSpace(path))return false;
            localHistoryPath=historyPath;localSlotPaths=(string[])paths.Clone();return true;
        }
        private string[] activeSlotPaths;
        private string authenticatedAccountId;
        public string CurrentAccountId=>authenticated?authenticatedAccountId:null;
        private string SlotPath(int index)=>activeSlotPaths!=null?activeSlotPaths[index]:localSlotPaths==null?NativeCharacterSlots.PathFor(index):localSlotPaths[index];
        private void ClearAccountSelection()
        {activeSlotPaths=null;authenticatedAccountId=null;selectedSlot=0;ResetServerSelection();System.Array.Clear(slots,0,slots.Length);System.Array.Clear(occupied,0,occupied.Length);}
        private readonly System.Collections.Generic.List<System.IDisposable> bindings=new();
        private void BindLocalServer()
        {
            if(protocol!=null)return;
            protocol=GetComponent<ServerManager>();if(protocol==null)protocol=gameObject.AddComponent<ServerManager>();
            var server=protocol.BindLocalAccountService(localHistoryPath,localSlotPaths);
            protocol.ResponseReceived+=ReceiveCharacterPacket;

        }
        private void ReceiveCharacterPacket(ServerPacket packet)
        {
            if(packet is not CharacterListResponse list || !list.Accepted || !authenticated
                || list.AccountId!=authenticatedAccountId || list.ServerId!=connectedServerId || connectedServerId==0)return;
            for(int i=0;i<slots.Length;i++)
            {
                occupied[i]=list.IsOccupied(i);var profile=list.CaptureProfile(i);
                slots[i]=profile==null?null:new NativeSaveData {hasCharacter=true,character=profile};
            }
        }
        private void OnDestroy()
        {foreach(var binding in bindings)binding.Dispose();if(protocol!=null)protocol.ResponseReceived-=ReceiveCharacterPacket;}
        private const string GameSceneName = "Kaosia_Play";
        private bool loading,loggingIn=true,authenticated;
        public bool IsAwaitingOriginalServerSelection {get;private set;}
        private bool firstLogin=true,loginFailed,loginCanRecover,registering,hasAccount;
        private string loginUserId="",loginPassword="",registrationId="",registrationPassword="",registrationConfirmation="";
        public bool IsRegistering=>registering;
        public bool IsLoggingIn=>loggingIn;
        private string loadMessage;
        private bool creating;
        private bool selecting;
        private int selectedSlot;
        private readonly NativeSaveData[] slots = new NativeSaveData[NativeCharacterSlots.Count];
        private readonly bool[] occupied = new bool[NativeCharacterSlots.Count];
        private CharacterPortraitCatalog portraits;
        public bool IsSelectingCharacter => selecting;
        private string characterName = string.Empty;
        public bool IsCreatingCharacter => creating;

        private void Start()
        {
            EnsureEntryCamera();
            StartGame();
        }
        public void RefreshContinue()
        {
            BindLocalServer();
            if(!authenticated)
            {
                protocol.TryRequest(new LoginStatusRequest(),out var status);
                if(status is LoginResponse login)
                {firstLogin=login.RequiresCharacterSelection;hasAccount=login.HasAccount;if(string.IsNullOrEmpty(loginUserId))loginUserId=login.AccountId ?? "";}
                return;
            }
            if(!protocol.TryRequest(new CharacterListRequest(),out var response))loadMessage=response?.Reason;

        }

        private InGameUi ui;
        private void LateUpdate()
        {
            InGameUi.Ensure(ref ui, this);
            ui.Begin();
            var width = Mathf.Min(420f, Rendering.GameScreen.Width - 40f);
            var height = creating || selecting || loggingIn || registering ? 440f : 350f;
            var panel = new Rect((Rendering.GameScreen.Width - width) * 0.5f, (Rendering.GameScreen.Height - height) * 0.5f, width, height);

            LoadEntryArtwork();
            ui.EntryArtwork("entry-background",new Rect(0,0,Rendering.GameScreen.Width,Rendering.GameScreen.Height),entryBackground,true);
            if(IsAwaitingOriginalServerSelection){DrawServerSelection();ui.End();return;}
            if(loggingIn){DrawLogin(panel);ui.End();return;}
            if(creating){DrawCharacterCreation(panel);ui.End();return;}
            if(registering || selecting)
            {
                ui.Panel("menu",panel);
                if(registering)DrawRegistration(panel);
                else DrawCharacterSelection(panel);
            }
            else DrawLogin(panel);
            ui.End();
        }

        public void StartGame()
        {
            if (loading) return;
            NativeSaveStore.ClearResume();
            NativeCharacterProfile.ClearPending();
            authenticated=false;IsAwaitingOriginalServerSelection=false;ClearAccountSelection();
            BindLocalServer();protocol.TryRequest(new LogoutRequest(),out _);
            RefreshContinue();selecting=false;creating=false;registering=false;loggingIn=true;loginFailed=false;loginCanRecover=false;loginPassword="";
            entryOptions=false;loadMessage=string.Empty;
            if(!hasAccount)OpenRegistration();
        }

        public bool Login(string userId=null,string password=null)
        {
            if(loading || !loggingIn)return false;
            if(userId!=null)loginUserId=userId;if(password!=null)loginPassword=password;
            BindLocalServer();authenticated=false;IsAwaitingOriginalServerSelection=false;ClearAccountSelection();
            bool accepted=protocol.TryRequest(new LoginRequest(loginUserId,loginPassword),out var raw);
            loginPassword="";
            var accountPaths=(raw as LoginResponse)?.CaptureSlotPaths();
            if((raw as LoginResponse)?.Authenticated==true && accountPaths?.Length==NativeCharacterSlots.Count)
            {activeSlotPaths=accountPaths;authenticatedAccountId=((LoginResponse)raw).AccountId;}
            if(!accepted)
            {loadMessage=raw?.Reason;loginFailed=true;loginCanRecover=(raw as LoginResponse)?.Authenticated==true;return false;}
            var response=raw as LoginResponse;
            if(response==null || activeSlotPaths==null){loadMessage="계정의 로그인 응답과 캐릭터 슬롯을 확인할 수 없습니다.";return false;}
            authenticated=true;loginCanRecover=true;loginFailed=false;loadMessage=string.Empty;
            IsAwaitingOriginalServerSelection=true;loggingIn=false;selecting=false;creating=false;
            // Authentication and listing are separate requests. A listing failure
            // stays on this screen with retry rather than undoing authentication.
            RefreshServerList();return true;
        }
        public bool OpenRegistration()
        {
            if(registering && !loading)return true;
            if(loading || !loggingIn)return false;
            BindLocalServer();protocol.TryRequest(new LogoutRequest(),out _);
            authenticated=false;IsAwaitingOriginalServerSelection=false;ClearAccountSelection();
            registrationId=loginUserId;registering=true;loggingIn=false;
            registrationPassword=registrationConfirmation="";
            loadMessage="아이디는 영문·숫자·밑줄 3~20자, 비밀번호는 8~64자로 입력하세요.";
            return true;
        }
        public void CancelRegistration()
        {registering=false;loggingIn=true;registrationPassword=registrationConfirmation="";loadMessage="";}
        public bool RegisterAccount(string userId,string password,string confirmation)
        {
            if(loading || !registering)return false;
            BindLocalServer();
            if(!protocol.TryRequest(new AccountRegisterRequest(userId,password,confirmation),out var raw))
            {loadMessage=raw?.Reason;return false;}
            var response=raw as AccountRegisterResponse;
            if(response==null || !response.Accepted){loadMessage="회원가입 응답을 확인할 수 없습니다.";return false;}
            hasAccount=true;authenticated=false;ClearAccountSelection();loginUserId=response?.UserId ?? userId;loginPassword="";
            registrationPassword=registrationConfirmation="";registering=false;loggingIn=true;
            loadMessage=response?.Reason ?? "회원가입이 완료되었습니다. 로그인해 주세요.";return true;
        }
        private void DrawRegistration(Rect panel)
        {
            // Local account input form; original web signup layout is not claimed.
            ui.Label("register-title",new Rect(panel.x+24,panel.y+24,panel.width-48,30),"회원가입",20);
            ui.Label("register-id-label",new Rect(panel.x+24,panel.y+76,110,22),"아이디",14);
            ui.TextInput("register-id",new Rect(panel.x+140,panel.y+76,panel.width-164,26),registrationId,20,v=>registrationId=v);
            ui.Label("register-password-label",new Rect(panel.x+24,panel.y+120,110,22),"비밀번호",14);
            ui.TextInput("register-password",new Rect(panel.x+140,panel.y+120,panel.width-164,26),registrationPassword,64,v=>registrationPassword=v,password:true);
            ui.Label("register-confirm-label",new Rect(panel.x+24,panel.y+164,110,22),"비밀번호 확인",14);
            ui.TextInput("register-confirm",new Rect(panel.x+140,panel.y+164,panel.width-164,26),registrationConfirmation,64,v=>registrationConfirmation=v,password:true);
            ui.Button("register-submit",new Rect(panel.x+24,panel.y+222,170,32),"가입",()=>RegisterAccount(registrationId,registrationPassword,registrationConfirmation),!loading);
            ui.Button("register-cancel",new Rect(panel.x+panel.width-194,panel.y+222,170,32),"취소",CancelRegistration,!loading);
            if(!string.IsNullOrEmpty(loadMessage))ui.Label("register-message",new Rect(panel.x+24,panel.y+278,panel.width-48,90),loadMessage,12);
        }
        public bool SelectSlot(int index)
        {
            if (loading || !selecting || index < 0 || index >= slots.Length) return false;
            selectedSlot = index; loadMessage = string.Empty; return true;
        }

        public bool BeginSelectedCharacterCreation()
        {
            if (loading || !selecting || occupied[selectedSlot]) return false;
            creating = true; selectedStartingJob=NativeCharacterProfile.SupportedStartingJob; characterName = string.Empty; loadMessage = string.Empty; return true;
        }

        private void DrawCharacterSelection(Rect panel)
        {
            ui.Label("characters-title",new Rect(panel.x+24,panel.y+18,panel.width-48,26),"캐릭터 선택",20);
            for(int i=0;i<slots.Length;i++)
            {
                int slot=i;var profile=slots[i]?.character;
                string title=(i+1)+". "+(profile?.name ?? (occupied[i]?"저장 정보 확인 필요":"빈 슬롯"));
                float x=panel.x+24+(i%2)*188,y=panel.y+64+(i/2)*48;
                ui.Button("character-slot-"+i,new Rect(x,y,180,38),title,()=>SelectSlot(slot),!loading,selected:selectedSlot==i);
            }
            bool occupiedSlot=occupied[selectedSlot];
            ui.Button("characters-enter",new Rect(panel.x+24,panel.y+318,panel.width-48,32),occupiedSlot?"선택한 캐릭터로 시작":"캐릭터 생성",
                ()=>{if(occupiedSlot)ContinueGame();else BeginSelectedCharacterCreation();},!loading && (!occupiedSlot || slots[selectedSlot]?.character!=null));
            if(!string.IsNullOrEmpty(loadMessage))ui.Label("characters-message",new Rect(panel.x+24,panel.y+352,panel.width-48,24),loadMessage,12);
            ui.Button("characters-logout",new Rect(panel.x+24,panel.y+376,panel.width-48,32),"로그인 화면으로",StartGame,!loading);
        }

        public void CancelCharacterCreation(){if(loading)return;creating=false;RefreshContinue();}
        public bool CreateCharacter(string name, int jobId = NativeCharacterProfile.SupportedStartingJob, string filePath = null)
        {
            if (loading || !creating || !authenticated) return false;
            filePath ??= SlotPath(selectedSlot);
            BindLocalServer();
            if(!protocol.TryRequest(new CharacterCreateRequest(name,jobId,filePath),out var response)){loadMessage=response?.Reason;return false;}
            creating=false;RefreshContinue();
            return ContinueGame(filePath);
        }

        public bool TryPrepareCharacterEntry(string filePath,out string reason)
        {
            reason="캐릭터 진입을 준비할 수 없습니다.";
            if(loading || !authenticated || !selecting || IsAwaitingOriginalServerSelection || connectedServerId==0)return false;
            BindLocalServer();
            if(!protocol.TryRequest(new CharacterSelectRequest(filePath ?? SlotPath(selectedSlot)),out var packet)
                || packet is not CharacterEntryResponse response || response.AccountId!=authenticatedAccountId
                || response.ServerId!=connectedServerId)
            {reason=packet?.Reason ?? reason;return false;}
            var data=response.CaptureState();
            if(data==null || !NativeSaveStore.QueueResume(data,response.SavePath))return false;
            reason=string.Empty;return true;
        }

        public bool ContinueGame(string filePath = null)
        {
            if(!TryPrepareCharacterEntry(filePath,out var reason)){loadMessage=reason;return false;}
            loading = true;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            return true;
        }
    }
}
