using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Network;
using DarkSaver.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarkSaver.UI
{
    // Original Saver world window bindings, distinct from GameCtl battle function keys.
    [DisallowMultipleComponent]
    public sealed partial class NativeWorldWindows : MonoBehaviour
    {
        private readonly List<int> open=new();
        private readonly HashSet<int> minimized=new();
        private readonly Dictionary<int,Vector2> positions=new();
        private readonly List<string> messages=new();
        private NativeBattleWindows catalog,helpCatalog;
        private readonly Dictionary<int,Sprite> summaryPictures=new();
        private InGameUi ui;
        private ServerManager server;
        private bool whisperSettingsPending;
        private TextAsset helpText;
        private NativeOwnedUnitData[] summaryRoster;
        private int summaryRevision=-1;
        private static readonly int[] FunctionKeys={1,2,3,4,5,8,9,12};
        private bool expandedChat;
        private string draft="";
        private bool exitModal;
        private DarkSaver.Player.TopDownPlayerController modalOwner;
        public string LastMessage {get;private set;}
        public bool ExitConfirmationOpen=>exitModal;
        public string ChatDraft=>draft;
        public bool IsWindowOpen(int scene)=>open.Contains(scene);
        public bool IsWindowMinimized(int scene)=>minimized.Contains(scene);
        public bool IsAvailable=>isActiveAndEnabled && (GetComponent<WorldSessionHud>()?.IsVisible ?? false)
            && GetComponent<BattleTransitionController>()?.HasCreatedCharacter==true
            && !GetComponent<BattleTransitionController>().IsTransitioning
            && FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling!=true
            && (TownSceneContext.IsReady || FieldSceneContext.Current!=null && UnityEngine.SceneManagement.SceneManager.GetSceneByName(FieldSceneContext.Current.MapScene).isLoaded);
        private void OnEnable(){BindServer();}
        private void BindServer()
        {
            var next=GetComponent<ServerManager>();if(next==server){BindWorldCharacterServer();SyncWhisperSettings();return;}
            if(server!=null)server.ResponseReceived-=Receive;
            server=next;if(server!=null){server.ResponseReceived+=Receive;server.BindLocalWorldTextService(CaptureWorldTextContext);whisperSettingsPending=true;BindWorldCharacterServer();SyncWhisperSettings();}
        }
        private void SyncWhisperSettings()
        {
            if(!whisperSettingsPending || server==null || !IsAvailable || exitModal || HasForeignModal)return;
            whisperSettingsPending=false;
            if(server.LocalServer.HasHandler(new Request_WorldWhisperSetting(true)))server.TryRequest(new Request_WorldWhisperSetting(NativeWorldPreferences.Get(3)),out _);
        }
        private void OnDisable(){CloseAll();if(server!=null){server.ResponseReceived-=Receive;server.UnbindLocalWorldTextService(CaptureWorldTextContext);server.UnbindLocalWorldCharacters(this);}server=null;ui?.Hide();}
        public static bool IsTyping
        {
            get
            {
                var selected=EventSystem.current?.currentSelectedGameObject;
                var field=selected!=null?selected.GetComponent<InputField>():null;
                return field!=null && field.isFocused && field.GetComponentInParent<NativeWorldWindows>()!=null;
            }
        }
        private bool HasPersonalPanel=>GetComponent<TownServicesPanel>()?.IsOpen==true && (GetComponent<TownServicesPanel>().Service==TownService.Inventory || GetComponent<TownServicesPanel>().Service==TownService.CharacterInfo);
        private bool HasForeignModal=>GetComponent<TownServicesPanel>()?.IsOpen==true && !HasPersonalPanel || GetComponent<BattleFormationPanel>()?.IsOpen==true;
        public bool HandleFunctionKey(int key)
        {
            if(!IsAvailable || exitModal)return false;
            if(key==3 || key==4)return TogglePersonal(key==3?TownService.CharacterInfo:TownService.Inventory);
            if(HasForeignModal || FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>()?.UiInputBlocked==true && !exitModal && !HasPersonalPanel)return false;
            int scene=key==1?19:key==2?2:key==5?3:key==8?15:key==9?27:-1;
            if(scene>=0)return ToggleWindow(scene);
            return key==12 && SubmitText("/공간이동");
        }
        private WorldTextContext CaptureWorldTextContext()=>new WorldTextContext(GetComponent<BattleTransitionController>()?.PlayerName,IsAvailable && !exitModal && !HasForeignModal);
        private NativeBattleWindows.Window WindowFor(int id)
        {
            if(id==19){helpCatalog??=Resources.Load<NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows");return helpCatalog?.Find(7);}
            return catalog?.Find(id==3 && expandedChat?303:id);
        }
        private bool TogglePersonal(TownService service)
        {
            if(exitModal)return false;
            var panel=GetComponent<TownServicesPanel>();if(panel==null || panel.IsOpen && panel.Service!=TownService.Inventory && panel.Service!=TownService.CharacterInfo)return false;
            if(panel.IsOpen && panel.Service==service){panel.Close();return true;}
            RefreshWorldCharactersFromServer();
            bool opened=GetComponent<WorldSessionHud>().OpenPersonalWindowFromShortcut(service);
            return opened;
        }
        public bool ToggleWindow(int scene)
        {
            if(!IsAvailable || (scene!=2 && scene!=3 && scene!=15 && scene!=19 && scene!=27))return false;
            if(exitModal && scene!=27 || HasForeignModal)return false;
            catalog??=Resources.Load<NativeBattleWindows>("UI/WorldWindows/NativeWorldWindows");
            if(WindowFor(scene)==null)return false;
            if(open.Contains(scene))
            {
                if(minimized.Remove(scene))return true;
                CloseWindow(scene);return true;
            }
            if(scene==27)
            {
                if(IsTyping)EventSystem.current.SetSelectedGameObject(null);
                var player=FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>();
                if(player==null || player.UiInputBlocked && !HasPersonalPanel)return false;
                if(!player.UiInputBlocked){player.SetUiInputBlocked(true);modalOwner=player;}exitModal=true;
            }
            if(scene==2)RefreshWorldCharactersFromServer();
            open.Add(scene);return true;
        }
        public void CloseWindow(int scene)
        {open.Remove(scene);minimized.Remove(scene);if(scene==27)ReleaseModal();}
        private void ReleaseModal(){if(modalOwner!=null)modalOwner.SetUiInputBlocked(false);modalOwner=null;exitModal=false;}
        private void CloseAll(){open.Clear();minimized.Clear();ReleaseModal();}
        public bool Minimize(int scene)
        {
            if(!IsAvailable || !open.Contains(scene) || scene!=2 && scene!=3)return false;
            minimized.Add(scene);return true;
        }
        public bool SubmitText(string text)=>SubmitText(text,out _);
        private bool SubmitText(string text,out bool delivered)
        {
            delivered=false;
            if(!IsAvailable || exitModal || HasForeignModal || string.IsNullOrWhiteSpace(text) || text.Length>255)return false;
            foreach(char c in text)if(char.IsControl(c))return false;
            var packet=new Request_WorldText(text);
            if(!OriginalWorldTextCodec.TryEncode(packet,out _)){ReportCommandFailure("입력한 글자를 확인해 주세요. 최대 255바이트까지 보낼 수 있습니다.");return false;}
            BindServer();
            if(server!=null && server.LocalServer.HasHandler(packet))
            {
                delivered=true;bool accepted=server.TryRequest(packet,out var reply);
                if(!accepted)ReportCommandFailure(reply?.Reason ?? "요청을 처리할 수 없습니다.");
                return accepted;
            }
            if(server!=null && server.SendToServer(packet)){delivered=true;return true;}
            ReportCommandFailure("요청을 처리할 서버가 연결되지 않았습니다.");return false;
        }
        private void Receive(ServerPacket packet)
        {
            if(packet is Notify_Mercenaries mercenaries){GetComponent<TownServicesPanel>()?.BindMercenaryState(mercenaries);return;}
            if(packet is CharacterAdjustmentOptionsNotification options){var panel=GetComponent<TownServicesPanel>();var jobs=GetComponent<BattleTransitionController>()?.WorldCharacterJobCatalog;if(jobs!=null)panel?.BindCharacterAdjustmentOptions(options,jobs);return;}
            if(packet is CharacterAdjustmentValuesNotification costs){GetComponent<TownServicesPanel>()?.BindWorldStatCosts(costs);return;}
            if(packet is Notify_WorldRoster roster){BindWorldRoster(roster);return;}
            if(packet is Notify_WorldInventory inventory){nativeInventory=inventory.Capture();RefreshOpenPersonalWindow();return;}
            if(!(packet is Notify_WorldText text) || !Enum.IsDefined(typeof(WorldTextChannel),text.Channel))return;
            if(text.Channel==WorldTextChannel.General && !NativeWorldPreferences.Get(2)
                || text.Channel==WorldTextChannel.Whisper && !NativeWorldPreferences.Get(3))return;
            if(!string.IsNullOrWhiteSpace(text.Text))AppendMessage(text.Text);
        }
        public void ReportOriginalUiError(string message){if(!string.IsNullOrWhiteSpace(message))ReportCommandFailure(message);}
        private void ReportCommandFailure(string message)
        {
            AppendMessage(message);
            if(!open.Contains(3))ToggleWindow(3);
            minimized.Remove(3);
        }
        private void AppendMessage(string text)
        {
            LastMessage=text.Length>512?text.Substring(0,512):text;
            messages.Add(LastMessage);if(messages.Count>100)messages.RemoveAt(0);
        }
        private void LateUpdate()
        {
            if(!IsAvailable){CloseAll();if(GetComponent<BattleTransitionController>()?.HasCreatedCharacter!=true){messages.Clear();draft="";LastMessage=null;summaryRoster=null;summaryRevision=-1;ResetWorldRecords();}ui?.Hide();return;}
            BindServer();
            var selected=EventSystem.current?.currentSelectedGameObject;
            bool typing=selected!=null && selected.GetComponent<InputField>()!=null;
            foreach(int key in FunctionKeys)if(Input.GetKeyDown((KeyCode)((int)KeyCode.F1+key-1)))HandleFunctionKey(key);
            if(!typing)
            {
                if(Input.GetKeyDown(KeyCode.I))HandleFunctionKey(4);
                if(Input.GetKeyDown(KeyCode.C))HandleFunctionKey(3);
            }
            if(open.Contains(3) && typing && Input.GetKeyDown(KeyCode.Return))SendDraft();
            if(exitModal && Input.GetKeyDown(KeyCode.Return))ConfirmQuit();
            else if(exitModal && Input.GetKeyDown(KeyCode.Escape))CloseWindow(27);
            InGameUi.Ensure(ref ui,this,25);ui.Begin();
            catalog??=Resources.Load<NativeBattleWindows>("UI/WorldWindows/NativeWorldWindows");
            // Draw the modal last; its full-screen hit shield prevents click-through.
            if(!HasForeignModal)foreach(int id in open.ToArray())if(id!=27)DrawWindow(id);
            if(exitModal){ui.NativeRosterSelection("modal-shield",new Rect(0,0,Rendering.GameScreen.Width,Rendering.GameScreen.Height),()=>{},true,"");DrawWindow(27);}
            ui.End();
        }
        private Vector2 Position(int id,Vector2 size)
        {
            if(!positions.TryGetValue(id,out var p))p=id==2?Vector2.zero:id==3?new Vector2(23,371):id==15?new Vector2(485,20):new Vector2((640-size.x)/2,(480-size.y)/2);
            p=new Vector2(Mathf.Clamp(p.x,0,640-size.x),Mathf.Clamp(p.y,0,480-size.y));positions[id]=p;return p;
        }
        public bool BlocksPointer(Vector2 screen)
        {
            if(!IsAvailable || HasForeignModal)return false;if(exitModal)return true;
            if(!Rendering.GameScreen.TryScreenToGui(screen,out var point))return false;
            if(open.Contains(3) && !minimized.Contains(3) && new Rect(10,458,489,16).Contains(point))return true;
            foreach(int id in open)
            {
                var w=WindowFor(id);if(w==null)continue;
                Vector2 size=minimized.Contains(id)?w.minimizedSize:w.size;
                if(new Rect(Position(id,size),size).Contains(point))return true;
            }
            return false;
        }
        private void DrawWindow(int id)
        {
            var w=WindowFor(id);if(w==null)return;
            bool mini=minimized.Contains(id);Vector2 size=mini?w.minimizedSize:w.size;Vector2 p=Position(id,size);
            string prefix="world-"+id;
            if(mini){ui.NativeMinimizedWindow(prefix+"-compact",new Rect(p,size),w.minimizedBackground,()=>minimized.Remove(id));return;}
            ui.NativeTexturePanel(prefix+"-background",new Rect(p,size),w.background);
            if(id==2)DrawSummary(p);
            if(id==15)ui.NativeBitmapText("world-current-font",new Rect(p.x+55,p.y+165,42,15),NativeWorldPreferences.FontName,Color.black);
            else if(id==19)
            {
                helpText??=Resources.Load<TextAsset>("UI/WorldWindows/NativeWorldHelp");
                ui.NativeBitmapScrollText(prefix+"-help",ScrollArea(p,15,w.controls[0],w.controls[1]),helpText?.text ?? "",w.controls[0],w.controls[1]);
            }
            else if(id==3)DrawChat(p,w);
            for(int i=0;i<w.controls.Length;i++)
            {
                if(id==19 && i<2 || id==3 && i>=3)continue;
                var c=w.controls[i];int index=i;
                if(id==15 && i>=1 && i<=6)
                {int setting=i-1;ui.NativeCheckbox(prefix+"-control-"+i,p+c.rect.position,c,NativeWorldPreferences.Get(setting),()=>ToggleSetting(setting),true,SettingNames[setting]);continue;}
                bool enabled=id!=2 || i!=3 && i<9; // index3 is native no-op; roster toggles require server unit flags.
                if(id==2 && i>=9)
                {int slot=i-8;RefreshRoster();bool known=slot<worldRoster.Length;
                    ui.NativeCheckbox(prefix+"-control-"+i,p+c.rect.position,c,known && worldRoster[slot].OriginalFlag==1,()=>SetWorldRoleFlag(slot),known,"시나리오 출전 설정");continue;}
                ui.NativeImageButton(prefix+"-control-"+i,p+c.rect.position,c,()=>Control(id,index),enabled,ControlTip(id,i));
            }
            if(id!=27)
            {
                var handle=new Rect(p.x+8,p.y+3,size.x-45,id==3?4:15);
                ui.WindowDragHandle(prefix+"-drag",handle,delta=>positions[id]=Position(id,size)+delta);
            }
        }
        private void ToggleSetting(int setting)
        {
            bool value=!NativeWorldPreferences.Get(setting);NativeWorldPreferences.Set(setting,value);
            if(setting==3)SubmitWorldPacket(new Request_WorldWhisperSetting(value));

        }
        private static readonly string[] SettingNames={"배경음악","효과음","전체대화수신","귓속말 수신","메뉴보기","전체화면 (다음 실행부터 적용)"};
        private static string ControlTip(int id,int index)
        {
            if(id==2)return index==0?"최소화":index==1?"닫기":index==2?"대화창":index==4?"환경설정":index==5?"도움말":index==6?"게임 종료":index==7?"장비 장착":index==8?"자기 정보":"부대 선택 설정";
            if(id==3)return index==0?"최소화":index==1?"확대/축소":"닫기";
            if(id==27)return index==0?"게임 종료":"취소";
            return id==15 && index==7?"현재 글꼴 변경":"닫기";
        }
        private void Control(int id,int index)
        {
            if(id==2)
            {
                if(index==0)Minimize(2);else if(index==1)CloseWindow(2);
                else if(index==2)ToggleWindow(3);else if(index==4)ToggleWindow(15);else if(index==5)ToggleWindow(19);
                else if(index==6)ToggleWindow(27);else if(index==7)HandleFunctionKey(4);else if(index==8)HandleFunctionKey(3);
            }
            else if(id==3){if(index==0)Minimize(3);else if(index==1)ToggleChatExpansion();else if(index==2)CloseWindow(3);}
            else if(id==27){if(index==0)ConfirmQuit();else CloseWindow(27);}
            else if(id==15 && index==7)NativeWorldPreferences.CycleFont();
            else CloseWindow(id);
        }
        public void ToggleChatExpansion()
        {
            var prior=catalog?.Find(expandedChat?303:3);var next=catalog?.Find(expandedChat?3:303);
            if(prior==null || next==null || !IsAvailable || !open.Contains(3))return;
            var position=Position(3,prior.size);positions[3]=new Vector2(position.x,position.y+prior.size.y-next.size.y);
            expandedChat=!expandedChat;
        }
        private void DrawSummary(Vector2 p)
        {
            var session=GetComponent<BattleTransitionController>();if(session==null || !session.HasCreatedCharacter)return;int job=session.PlayerJobId;
            ui.NativeBitmapText("summary-name",new Rect(p.x+115,p.y+74,111,18),SelectedWorldUnit?.Name ?? session.PlayerName,Color.black);
            ui.NativeBitmapText("summary-job",new Rect(p.x+115,p.y+93,111,18),SelectedWorldUnit?.JobName ?? NativeCharacterProfile.StartingJobName(SelectedWorldUnit?.JobId ?? job) ?? "",Color.black);
            if(!string.IsNullOrEmpty(SelectedWorldUnit?.Family))ui.NativeBitmapText("summary-family",new Rect(p.x+115,p.y+112,111,18),SelectedWorldUnit.Family,Color.black);
            DrawRoleCells(p);
            string gold=session.CurrentGold?.ToString() ?? "";
            ui.NativeStatusNumber("summary-gold",p+new Vector2(141-gold.Length*5,301),gold);
        }
        private Sprite SummaryPicture(int job)
        {
            // Saver1.2 0x41C7E0 uses GetCharSurface with source(32,32)-(64,64), not a face portrait.
            if(job==GetComponent<BattleTransitionController>()?.PlayerJobId)
            {
                var body=FindAnyObjectByType<DarkSaver.Player.TopDownPlayerController>()?.GetComponent<DarkSaver.Player.DirectionalSpriteAnimator>()?.StatusPicture;
                if(body!=null)return body;
            }
            if(!summaryPictures.TryGetValue(job,out var sprite))
            {
                sprite=Resources.Load<Sprite>("Scenarios/Shared/Characters/"+job+"_Down_1") ?? Resources.Load<Sprite>("Scenarios/01/Characters/"+job+"_Down_1");
                summaryPictures[job]=sprite;
            }
            return sprite;
        }
        private static Rect ScrollArea(Vector2 origin,float left,NativeBattleWindows.Control up,NativeBattleWindows.Control down)
            =>new Rect(origin.x+left,origin.y+up.rect.y,up.rect.x+up.rect.width-left,down.rect.y+down.rect.height-up.rect.y);
        private void DrawChat(Vector2 p,NativeBattleWindows.Window w)
        {
            string text=string.Join("\n",messages);
            ui.NativeBitmapScrollText("world-chat-log",ScrollArea(p,10,w.controls[3],w.controls[4]),text,w.controls[3],w.controls[4],textColor:Color.white,followTail:true);
            ui.TextInput("world-chat-input",new Rect(10,458,489,16),draft,255,value=>draft=value);
            var field=ui.Find<InputField>("world-chat-input");
            ui.Find<RawImage>("world-chat-input").color=Color.black;
            field.textComponent.color=new Color(.9f,.88f,.75f);field.textComponent.fontSize=14;
            field.lineType=InputField.LineType.SingleLine;
        }
        public bool SendDraft()
        {bool accepted=SubmitText(draft,out bool delivered);if(delivered)draft="";return accepted;}
        private void ConfirmQuit()
        {
            if(!exitModal || !IsAvailable)return;
            CloseWindow(27);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
