using System;
using DarkSaver.Core;
using DarkSaver.Combat;
using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.World;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.UI
{
    [RequireComponent(typeof(BattleTransitionController))]
    public sealed class ScenarioRoomPanel:MonoBehaviour
    {
        [Serializable] private sealed class Control {public int id,x,y,width,height;}
        [Serializable] private sealed class Layout {public int width,height;public Control[] controls;}
        private BattleTransitionController session;
        private ServerManager server;
        private IDisposable listBinding,enterBinding;
        private ScenarioGuideNpc guide;
        private TopDownPlayerController player;
        private ScenarioRoomState listing;
        private InGameUi ui;
        private Layout layout;
        private int generation;
        private string message;
        private readonly System.Collections.Generic.Dictionary<int,NativeBattleWindows.Control> controls=new();
        public bool IsOpen {get;private set;}
        private void Bind()
        {
            if(server!=null)return;
            session=GetComponent<BattleTransitionController>();
            server=GetComponent<ServerManager>() ?? gameObject.AddComponent<ServerManager>();
            if(!OriginalRestorationPolicy.AllowSinglePlayerRules)return;
            listBinding=server.RegisterRequestHandler<ScenarioRoomListRequest>(r=>State(r.Key,r.Generation));
            enterBinding=server.RegisterRequestHandler<ScenarioRoomEnterRequest>(r=>
            {
                var state=State(r.Key,r.Generation);
                return state.Accepted && state.CanEnter?state:new ScenarioRoomState(false,r.Key,r.Generation,reason:state.Reason ?? "입장 조건을 확인해 주세요.");
            });
        }
        private ScenarioRoomState State(string key,int requestGeneration)
        {
            if(!IsOpen || guide==null || key!=guide.Key || requestGeneration!=generation || !guide.CanInteract(player)
                || FirstPlayTutorial.BlocksWorldActions || Events.Scenario01Controller.BlocksWorldActions
                || SceneManager.GetSceneByName("Battle").isLoaded || FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling==true
                || session==null || !session.HasCreatedCharacter || session.IsTransitioning)
                return new ScenarioRoomState(false,key,requestGeneration,reason:"안내인과의 대화를 다시 시작해 주세요.");
            var progress=key=="01"?session.CaptureScenario01():session.CaptureCampaign(key);
            bool allowed=session.CanEnterScenario(key);
            return new ScenarioRoomState(true,key,generation,ScenarioRoomRules.IsOngoing(progress),allowed,
                allowed?null:"이전 시나리오 완료 또는 현재 시나리오 기록을 확인해 주세요.",progress?.phase==Events.Scenario01Progress.Complete);
        }
        public bool TryOpen(ScenarioGuideNpc npc,TopDownPlayerController actor)
        {
            if(IsOpen || npc==null || !npc.CanInteract(actor) || actor.UiInputBlocked
                || GetComponent<TownServicesPanel>()?.IsOpen==true || GetComponent<BattleFormationPanel>()?.IsOpen==true)return false;
            Bind();guide=npc;player=actor;generation++;IsOpen=true;
            if(!server.TryRequest(new ScenarioRoomListRequest(npc.Key,generation),out var response) || response is not ScenarioRoomState state)
            {Close();return false;}
            var asset=Resources.Load<TextAsset>("UI/ScenarioRooms/Layout");
            layout=asset!=null?JsonUtility.FromJson<Layout>(asset.text):null;
            if(layout==null || layout.controls==null){Close();return false;}
            listing=state;message=state.Reason;actor.StopForSceneTravel();actor.SetUiInputBlocked(true);return true;
        }
        public bool TryEnter()
        {
            if(!IsOpen || guide==null)return false;
            if(!server.TryRequest(new ScenarioRoomEnterRequest(guide.Key,generation),out var response) || response is not ScenarioRoomState state)
            {message=response?.Reason;return false;}
            if(!state.Accepted || !state.CanEnter || state.Generation!=generation)return false;
            var coordinator=FindAnyObjectByType<WorldSceneCoordinator>();
            if(coordinator==null || !coordinator.TryTravelTo("Scenario"+state.Key,guide.TargetSpawn))
            {message="지금은 시나리오에 입장할 수 없습니다.";return false;}
            Close();return true;
        }
        public void Close()
        {
            IsOpen=false;generation++;ui?.Hide();if(player!=null)player.SetUiInputBlocked(false);
            player=null;guide=null;listing=null;
        }
        private NativeBattleWindows.Control Button(Control source)
        {
            if(controls.TryGetValue(source.id,out var control))return control;
            control=new NativeBattleWindows.Control{nativeId=source.id,rect=new Rect(source.x,source.y,source.width,source.height),frames=new Texture2D[3]};
            for(int i=0;i<3;i++)control.frames[i]=Resources.Load<Texture2D>("UI/ScenarioRooms/Button"+source.id+"_"+i);
            controls.Add(source.id,control);return control;
        }
        private void LateUpdate()
        {
            if(!IsOpen){ui?.Hide();return;}
            if(guide==null || !guide.CanInteract(player) || SceneManager.GetSceneByName("Battle").isLoaded){Close();return;}
            if(Input.GetKeyDown(KeyCode.Escape)){Close();return;}
            InGameUi.Ensure(ref ui,this,110);ui.Begin();ui.ModalBlocker("room-blocker");
            var origin=new Vector2((Rendering.GameScreen.Width-layout.width)*.5f,(Rendering.GameScreen.Height-layout.height)*.5f);
            ui.NativeTexturePanel("room-window",new Rect(origin,new Vector2(layout.width,layout.height)),Resources.Load<Texture2D>("UI/ScenarioRooms/Background"));
            ScenarioCatalog.TryGet(guide.Key,out var scenario);
            ui.NativeBitmapText("room-title",new Rect(origin.x+22,origin.y+11,450,18),"시나리오 "+guide.Key+" · "+scenario.Title,Color.white);
            ui.NativeBitmapText("room-description",new Rect(origin.x+102,origin.y+47,360,40),
                listing.HasOngoingRoom?"저장된 진행 중인 방이 있습니다.":listing.HasCompletedRecord?"완료된 기록입니다.":"진행 중인 방이 없습니다.",Color.white,2);
            ui.PortraitTexture("room-guide",new Rect(origin.x+28,origin.y+48,48,48),Resources.Load<Texture2D>("UI/ScenarioRooms/Guide"));
            ui.NativeBitmapText("room-entry",new Rect(origin.x+24,origin.y+147,98,94),
                (!listing.CanEnter?"입장 불가\n":listing.HasOngoingRoom?"진행 중\n":listing.HasCompletedRecord?"완료 기록\n":"새 방\n")+(session.PlayerName ?? ""),Color.white,5);
            if(!string.IsNullOrEmpty(message))ui.NativeBitmapText("room-message",new Rect(origin.x+28,origin.y+245,425,22),message,Color.white,2);
            foreach(var c in layout.controls)
                ui.NativeImageButton("room-control-"+c.id,origin+new Vector2(c.x,c.y),Button(c),
                    c.id==9?()=>TryEnter():c.id==53?Close:null,c.id==53 || c.id==9 && listing.CanEnter,
                    c.id==9?(listing.HasOngoingRoom?"진행 중인 방 참가":listing.HasCompletedRecord?"완료 기록 보기":"새 방 시작"):c.id==53?"취소":"다른 방 없음");
            ui.End();
        }
        private void OnDisable(){if(IsOpen)Close();}
        private void OnDestroy(){Close();listBinding?.Dispose();enterBinding?.Dispose();}
    }
}
