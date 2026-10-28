using DarkSaver.Combat;
using DarkSaver.Network;
using DarkSaver.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.World
{
    public sealed partial class WorldSceneCoordinator
    {
        private BattleMapPresentation trainingView;
        private int trainingSession;
        private BattleTransitionController trainingOwner;
        public bool IsLocalTraining => trainingView!=null && trainingSession>0;
        public bool OwnsTrainingView(BattleMapPresentation view) => IsLocalTraining && trainingView==view;
        private TrainingTravelContext CaptureTrainingContext()
        {
            var session=FindAnyObjectByType<BattleTransitionController>();
            var player=FindFirstObjectByType<TopDownPlayerController>();
            string region=IsLocalTraining?"Training"+trainingView.GetComponent<BattleSceneRoot>().TrainingMapId:
                SceneManager.GetSceneByName("Kaosia_Map").isLoaded && SceneManager.GetSceneByName("Town_Runtime").isLoaded?"Kaosia":null;
            bool blocked=travelling || session==null || session.IsTransitioning || session.HasPendingTutorial
                || player==null || (!IsLocalTraining && !player.isActiveAndEnabled) || player.UiInputBlocked
                || DarkSaver.UI.FirstPlayTutorial.BlocksWorldActions || DarkSaver.Events.Scenario01Controller.BlocksWorldActions
                || SceneManager.GetSceneByName("Battle").isLoaded;
            return new TrainingTravelContext(region,player==null?0:player.transform.position.x,player==null?0:player.transform.position.y,
                session!=null && session.HasCreatedCharacter,blocked);
        }
        public bool TryEnterTraining()
        {
            BindServer();return TryTravelTo("Training"+protocol.LocalTrainingMapId,"TrainingEntry");
        }
        public bool TryRetreatTraining()
        {
            if(!IsLocalTraining || travelling)return false;
            BindServer();return protocol.TryRequest(new Request_RetreatTraining(trainingSession),out _);
        }
        private bool ReceiveTrainingPacket(ServerPacket packet)
        {
            if(packet is Response_EnterTraining entered)
            {
                if(!entered.Accepted){LastTravelMessage=entered.Reason;return true;}
                if(travelling || IsLocalTraining || entered.SessionId!=protocol.LocalTrainingSessionId)return true;
                var player=FindFirstObjectByType<TopDownPlayerController>();
                if(player==null){protocol.TryRequest(new Request_ReadyTraining(entered.SessionId,false),out _);return true;}
                trainingSession=entered.SessionId;activeTransfer=entered.SessionId;trainingTransfer=true;travelling=true;
                var approval=new MapEnterNotification(true,MapEnterStage.Started,"Map_"+entered.MapId+"_;","Training"+entered.MapId+"_Runtime",
                    width:20,height:15,x:entered.X+.5f,y:15-1-entered.Y+.5f,transferId:entered.SessionId);
                StartCoroutine(TravelRoutine(player,approval,
                    loaded=>protocol.TryRequest(new Request_ReadyTraining(entered.SessionId,loaded),out _),
                    ()=>PrepareTrainingView(player,entered.MapId),
                    finish:committed=>{if(!committed){ReleaseTrainingView();trainingSession=0;}},save:false,
                    commitCheck:()=>
                    {
                        if(trainingOwner!=null && trainingOwner.SaveTrainingReturnCheckpoint(out var reason))return true;
                        LastTravelMessage="훈련장 복귀 위치를 저장하지 못해 입장을 취소했습니다. "+(trainingOwner?.LastSaveMessage ?? "저장 경로를 확인해 주세요.");
                        protocol.AbortUncommittedTrainingEntry(entered.SessionId);return false;
                    }));
                return true;
            }
            if(packet is Response_RetreatTraining escaped)
            {
                if(!escaped.Accepted){LastTravelMessage=escaped.Reason;return true;}
                if(!IsLocalTraining || escaped.SessionId!=trainingSession || travelling)return true;
                var player=FindFirstObjectByType<TopDownPlayerController>();
                if(player==null){protocol.TryRequest(new Request_ReadyTraining(escaped.SessionId,false),out _);return true;}
                activeTransfer=escaped.SessionId;trainingTransfer=true;travelling=true;
                var approval=new MapEnterNotification(true,MapEnterStage.Started,escaped.MapScene,escaped.RuntimeScene,
                    "FromTraining",43,43,escaped.X+.5f,43-1-escaped.Y+.5f,transferId:escaped.SessionId);
                StartCoroutine(TravelRoutine(player,approval,
                    loaded=>protocol.TryRequest(new Request_ReadyTraining(escaped.SessionId,loaded),out _),
                    commitAction:()=>{ReleaseTrainingView();trainingSession=0;}));
                return true;
            }
            return false;
        }
        private bool PrepareTrainingView(TopDownPlayerController player,int mapId)
        {
            Tilemap ground=null,terrain=null;
            var map=SceneManager.GetSceneByName("Map_"+mapId+"_;");
            foreach(var root in map.GetRootGameObjects())foreach(var tiles in root.GetComponentsInChildren<Tilemap>())
            {if(tiles.name=="Ground")ground=tiles;else if(tiles.name=="TerrainAttributes")terrain=tiles;}
            var session=FindAnyObjectByType<BattleTransitionController>();
            if(ground==null || terrain==null || ground.cellBounds.size.x!=20 || ground.cellBounds.size.y!=15
                || session==null || !session.HasPlayerVitals || Camera.main==null)return false;
            var go=new GameObject("Local training visit");
            SceneManager.MoveGameObjectToScene(go,SceneManager.GetSceneByName("Training"+mapId+"_Runtime"));
            var battle=go.AddComponent<BattleSceneRoot>();
            var profile=session.CaptureCharacterProfile();
            if(profile==null || !battle.ConfigureTrainingMap(mapId) || !battle.InitializeTrainingVisitor(profile.jobId,session.PlayerName,
                session.PlayerHp,session.PlayerMaxHp,session.PlayerMp,session.PlayerMaxMp,session.PlayerExperience))
            {go.SetActive(false);Destroy(go);return false;}
            var body=player.GetComponent<SpriteRenderer>();
            player.transform.position=ground.GetCellCenterWorld(new Vector3Int(10,7,0));
            var rigidbody=player.GetComponent<Rigidbody2D>();if(rigidbody!=null)rigidbody.position=player.transform.position;
            trainingView=go.AddComponent<BattleMapPresentation>();
            if(!trainingView.TryBind(ground,terrain,Camera.main,new[]{new BattleMapVisual(100,body)}))
            {ReleaseTrainingView();return false;}
            trainingOwner=session;return true;
        }
        public bool TryCaptureTrainingReturn(BattleTransitionController owner,out DarkSaver.Core.NativeSaveData snapshot)
        {
            snapshot=null;
            if(!IsLocalTraining || owner==null || owner!=trainingOwner || protocol==null
                || !trainingView.GetComponent<BattleSceneRoot>().TryGetUnit(100,out var unit))return false;
            int map=trainingView.GetComponent<BattleSceneRoot>().TrainingMapId;
            var candidate=owner.CaptureSaveState("Map_"+map+"_;","Training"+map+"_Runtime",new Vector2(10.5f,7.5f));
            candidate.hasVitals=true;candidate.hp=unit.Hp;candidate.maxHp=unit.MaxHp;candidate.mp=unit.Mp;candidate.maxMp=unit.MaxMp;
            candidate.lifeState=(int)unit.LifeState;candidate.experience=unit.Experience;
            return protocol.TryPrepareTrainingResume(candidate,out snapshot);
        }
        private void ReleaseTrainingView()
        {
            trainingOwner=null;
            if(trainingView==null)return;
            trainingView.SuspendPresentation();var go=trainingView.gameObject;trainingView=null;
            go.SetActive(false);Destroy(go);
        }
    }
}
