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
        {activeSlotPaths=null;authenticatedAccountId=null;selectedSlot=0;System.Array.Clear(slots,0,slots.Length);System.Array.Clear(occupied,0,occupied.Length);}
        private readonly System.Collections.Generic.List<System.IDisposable> bindings=new();
        private void BindLocalServer()
        {
            if(protocol!=null)return;
            protocol=GetComponent<ServerManager>();if(protocol==null)protocol=gameObject.AddComponent<ServerManager>();
            var server=new LocalCharacterServer(localHistoryPath,localSlotPaths);
            bindings.Add(protocol.RegisterRequestHandler<LoginRequest>(server.Login));
            bindings.Add(protocol.RegisterRequestHandler<LogoutRequest>(server.Logout));
            bindings.Add(protocol.RegisterRequestHandler<AccountRegisterRequest>(server.Register));
            bindings.Add(protocol.RegisterRequestHandler<LoginStatusRequest>(server.Status));
            bindings.Add(protocol.RegisterRequestHandler<CharacterListRequest>(server.List));
            bindings.Add(protocol.RegisterRequestHandler<CharacterCreateRequest>(server.Create));
            bindings.Add(protocol.RegisterRequestHandler<CharacterSelectRequest>(server.Select));
            protocol.ResponseReceived+=ReceiveCharacterPacket;

        }
        private void ReceiveCharacterPacket(ServerPacket packet)
        {
            if(packet is not CharacterListResponse list || !list.Accepted)return;
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
            authenticated=false;ClearAccountSelection();
            BindLocalServer();protocol.TryRequest(new LogoutRequest(),out _);
            RefreshContinue();selecting=false;creating=false;registering=false;loggingIn=true;loginFailed=false;loginCanRecover=false;loginPassword="";
            entryOptions=false;loadMessage=string.Empty;
        }

        public bool Login(string userId=null,string password=null)
        {
            if(loading || !loggingIn)return false;
            if(userId!=null)loginUserId=userId;if(password!=null)loginPassword=password;
            BindLocalServer();authenticated=false;ClearAccountSelection();
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
            if(!response.RequiresCharacterSelection)
            {
                if(ContinueGame(response.ResumePath))return true;
                loginFailed=true;return false;
            }
            RefreshContinue();loggingIn=false;selecting=true;creating=false;return true;
        }
        public bool OpenRegistration()
        {
            if(loading || !loggingIn)return false;
            RefreshContinue();
            registering=true;loggingIn=false;registrationPassword=registrationConfirmation="";
            loadMessage="새 아이디로 가입할 수 있습니다. 계정마다 캐릭터 10슬롯이 분리됩니다.";
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
            hasAccount=true;authenticated=false;ClearAccountSelection();loginUserId=response?.UserId ?? userId;loginPassword="";
            registrationPassword=registrationConfirmation="";registering=false;loggingIn=true;
            loadMessage=response?.Reason ?? "회원가입이 완료되었습니다. 로그인해 주세요.";return true;
        }
        private void DrawRegistration(Rect panel)
        {
            ui.Label("register-title",new Rect(panel.x+30,panel.y+105,panel.width-60,28),"회원가입",22);
            ui.Label("register-id-label",new Rect(panel.x+24,panel.y+148,104,28),"아이디",14);
            ui.TextInput("register-id",new Rect(panel.x+135,panel.y+145,panel.width-159,30),registrationId,20,v=>registrationId=v);
            ui.Label("register-pw-label",new Rect(panel.x+24,panel.y+191,104,28),"비밀번호",14);
            ui.TextInput("register-password",new Rect(panel.x+135,panel.y+188,panel.width-159,30),registrationPassword,64,v=>registrationPassword=v,password:true);
            ui.Label("register-confirm-label",new Rect(panel.x+24,panel.y+234,104,28),"비밀번호 확인",14);
            ui.TextInput("register-confirm",new Rect(panel.x+135,panel.y+231,panel.width-159,30),registrationConfirmation,64,v=>registrationConfirmation=v,password:true);
            ui.Label("register-guide",new Rect(panel.x+24,panel.y+269,panel.width-48,36),"아이디: 영문·숫자·_ 3~20자 / 비밀번호: 8~64자\n계정마다 캐릭터 10슬롯과 진행 상태를 따로 저장합니다.",12);
            ui.Button("register-submit",new Rect(panel.x+60,panel.y+311,panel.width-120,35),"가입하기",()=>RegisterAccount(registrationId,registrationPassword,registrationConfirmation),!loading);
            ui.Button("register-cancel",new Rect(panel.x+60,panel.y+352,panel.width-120,28),"로그인으로 돌아가기",CancelRegistration,!loading);
            if(!string.IsNullOrEmpty(loadMessage))ui.Label("register-message",new Rect(panel.x+20,panel.y+390,panel.width-40,38),loadMessage,12);
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
            ui.Label("select-title", new Rect(panel.x + 24, panel.y + 92, panel.width - 48, 28), string.IsNullOrEmpty(CurrentAccountId)?"모험할 캐릭터를 선택하세요":"캐릭터 선택 · "+CurrentAccountId, 18);
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                var profile = slots[i]?.character;
                string caption=profile!=null?profile.name:occupied[i]?"읽을 수 없는 저장":"빈 슬롯";
                float width=(panel.width-56)/2;
                ui.Button("slot-"+i,new Rect(panel.x+24+(i%2)*(width+8),panel.y+126+(i/2)*31,width,27),
                    (i+1)+". "+caption,()=>SelectSlot(index),!loading,selected:selectedSlot==i,
                    tooltip:profile!=null?profile.name+(profile.hasLevels?" · 레벨 "+profile.baseLevel:""):occupied[i]?"덮어쓰기 차단":"새 캐릭터 생성",singleLine:true,fontSize:12);
            }
            var chosen = slots[selectedSlot]?.character;
            ui.Label("selected-description",new Rect(panel.x+24,panel.y+282,panel.width-48,20),
                "슬롯 "+(selectedSlot+1)+" · "+(chosen!=null?chosen.name+(chosen.hasLevels?" · 레벨 "+chosen.baseLevel:""):occupied[selectedSlot]?"저장 읽기 실패 · 덮어쓰기 차단":"빈 슬롯"),12);
            if (chosen != null)
            {
                if (portraits == null) portraits = Resources.Load<CharacterPortraitCatalog>("UI/CharacterPortraits/CharacterPortraits");
                if (portraits != null && portraits.TryGet(chosen.jobId, out var face))
                    ui.PortraitTexture("selected-face", new Rect(panel.x + 24, panel.y + 306, 64, 64), face);
                ui.Button("enter", new Rect(panel.x + 104, panel.y + 306, panel.width - 128, 36), "선택한 캐릭터로 시작",
                    () => ContinueGame(SlotPath(selectedSlot)), !loading);
            }
            else ui.Button("new-character", new Rect(panel.x + 24, panel.y + 306, panel.width - 48, 36), "캐릭터 생성",
                () => BeginSelectedCharacterCreation(), !loading && !occupied[selectedSlot]);
            ui.Button("select-back", new Rect(panel.x + 104, panel.y + 348, panel.width - 128, 28), "다른 계정으로 로그인", StartGame, !loading);
            if (!string.IsNullOrEmpty(loadMessage)) ui.Label("select-message", new Rect(panel.x + 24, panel.y + 382, panel.width - 48, 50), loadMessage, 12);
        }

        public bool CreateCharacter(string name, int jobId = NativeCharacterProfile.SupportedStartingJob, string filePath = null)
        {
            if (loading || !creating || !authenticated) return false;
            filePath ??= SlotPath(selectedSlot);
            BindLocalServer();
            if(!protocol.TryRequest(new CharacterCreateRequest(name,jobId,filePath),out var response)){loadMessage=response?.Reason;return false;}
            loading = true;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            return true;
        }

        public bool ContinueGame(string filePath = null)
        {
            if (loading || !authenticated) return false;
            BindLocalServer();
            if(!protocol.TryRequest(new CharacterSelectRequest(filePath ?? SlotPath(selectedSlot)),out var response))
            {loadMessage=response?.Reason;return false;}
            loading = true;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            return true;
        }
    }
}
