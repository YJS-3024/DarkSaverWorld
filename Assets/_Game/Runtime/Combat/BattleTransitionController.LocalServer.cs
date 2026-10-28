using System;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Items;
using DarkSaver.Jobs;
using DarkSaver.Magic;
using DarkSaver.Network;
using DarkSaver.Player;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        private ServerManager sessionProtocol;
        private readonly List<IDisposable> sessionBindings=new();
        private bool processingSessionRequest;
        private ItemCatalog serverShopCatalog;
        private NativeShopOffer[] serverShopOffers=Array.Empty<NativeShopOffer>();
        private ExperienceProgressionTable serverProgression;
        private MercenaryHireCatalog serverHireSkills;
        private JobCatalog serverHireJobs;
        private MagicCatalog serverMagic;
        private TopDownPlayerController serverPlayer;
        // Scene-owned encounter configuration is not serialized as GameObject pointers.
        // The local transition command consumes this configured encounter once.
        private BattleObjectiveDefinition serverEncounterObjective;
        private BattleEnemyDefinition serverEncounterEnemy;
        private UnityEngine.SpriteRenderer serverEncounterVisual;
        public void ConfigureLocalShop(ItemCatalog catalog,NativeShopOffer[] offers)
        {
            serverShopCatalog=catalog;
            serverShopOffers=Array.ConvertAll(offers,o=>new NativeShopOffer {itemId=o.itemId,buyPrice=o.buyPrice,sellPrice=o.sellPrice});
        }
        private ServerManager EnsureSessionServer()
        {
            if(sessionProtocol!=null)return sessionProtocol;
            sessionProtocol=GetComponent<ServerManager>();if(sessionProtocol==null)sessionProtocol=gameObject.AddComponent<ServerManager>();
            LocalCommandResponse Handle(Func<LocalCommandResponse> execute,bool storageOnly=false)
            {
                if(!isActiveAndEnabled)return new LocalCommandResponse(false,"로컬 세션 서버가 비활성 상태입니다.");
                if(!storageOnly && !OriginalRestorationPolicy.AllowSinglePlayerRules)
                    return new LocalCommandResponse(false,OriginalRestorationPolicy.PendingServerRule);
                processingSessionRequest=true;
                try
                {
                    var result=execute();
                    return new SessionStateNotification(result.Accepted,result.Reason,character,inventory,ownedRoster);
                }
                finally{processingSessionRequest=false;}
            }
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalFormationRequest>(p=>Handle(()=>new LocalCommandResponse(TrySetOwnedRoster(p.CaptureRoster(),out var reason),reason))));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalBattleTransitionRequest>(p=>Handle(()=>
            {
                if(p.Enter)return new LocalCommandResponse(TryEnterBattle(serverEncounterObjective,serverEncounterEnemy,serverEncounterVisual));
                if(transitioning || !UnityEngine.SceneManagement.SceneManager.GetSceneByName(BattleSceneName).isLoaded)return new LocalCommandResponse(false,"복귀할 전투가 없습니다.");
                ExitBattle();return new LocalCommandResponse(true);
            })));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalBattleCheckpointRequest>(p=>Handle(()=>new LocalCommandResponse(SaveBattleCheckpoint(out var reason),reason),storageOnly:true)));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<Request_SaveTrainingReturn>(p=>Handle(()=>new LocalCommandResponse(SaveTrainingReturnCheckpoint(out var reason),reason),storageOnly:true)));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<SaveWorldRequest>(p=>Handle(()=>new LocalCommandResponse(SaveWorld(),lastSaveMessage),storageOnly:true)));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<TownQuestRequest>(p=>Handle(()=>new LocalCommandResponse(TryCommitTownQuest(p.QuestId,p.Claim,out var reason),reason))));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalEquipmentChangeRequest>(p=>Handle(()=>new LocalCommandResponse(TryChangeEquipment(p.Slot,p.Item,p.CaptureBaseline(),out var reason),reason))));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalBattleItemUseRequest>(p=>Handle(()=>new LocalCommandResponse(TryUseBattleRecoveryItem(activeBattle,p.Item,p.Target,p.CaptureBaseline(),out var reason),reason))));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalHireRequest>(p=>Handle(()=>new LocalCommandResponse(TryHireMercenary(p.Skill,p.Job,serverProgression,serverHireSkills,serverHireJobs,out var reason),reason))));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalClassChangeRequest>(p=>Handle(()=>new LocalCommandResponse(TryChangeClass(serverProgression,serverMagic,serverPlayer,out var reason),reason))));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalStatAllocationRequest>(p=>Handle(()=>new LocalCommandResponse(TryAllocateCharacterStats(p.CaptureBaseline(),p.Attack,p.Defense,p.MagicAttack,p.MagicDefense,out var reason,p.CaptureMagic()),reason))));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalTradeRequest>(p=>Handle(()=>
            {
                var quote=Array.Find(serverShopOffers,o=>o.itemId==p.Item);
                if(quote==null)return new LocalCommandResponse(false,"로컬 서버의 판매 목록에 없는 품목입니다.");
                return new LocalCommandResponse(TryTrade(quote,p.Quantity,p.Buying,serverShopCatalog,out var reason),reason);
            })));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalTempleRecoveryRequest>(p=>Handle(()=>new LocalCommandResponse(TryTempleRecovery(out var reason),reason))));
            RegisterEmoteHandler();
            return sessionProtocol;
        }
        private bool SubmitSession(ClientPacket request,out string reason)
        {
            var accepted=EnsureSessionServer().TryRequest(request,out var response);
            reason=response?.Reason ?? "로컬 세션 응답이 없습니다.";return accepted;
        }
        private void OnApplicationQuit()=>SaveExitCheckpoint();
        private void OnApplicationPause(bool paused){if(paused)SaveExitCheckpoint();}
        private void SaveExitCheckpoint()
        {
            if (!HasCreatedCharacter || transitioning) return;
            var world=UnityEngine.Object.FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>();
            if(world!=null && world.IsLocalTraining){SaveTrainingReturnCheckpoint(out _);return;}
            if (UnityEngine.SceneManagement.SceneManager.GetSceneByName(BattleSceneName).isLoaded)
                SaveBattleCheckpoint(out _);
            else SaveWorld();
        }
        private void SaveCombatResultCheckpoint(CombatResult result) => SaveBattleCheckpoint(out _);

        public bool SaveTrainingReturnCheckpoint(out string reason)
        {
            if(!processingSessionRequest)return SubmitSession(new Request_SaveTrainingReturn(),out reason);
            reason="훈련장 복귀 위치를 저장할 수 없습니다.";
            var world=UnityEngine.Object.FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>();
            if(!HasCreatedCharacter || transitioning || world==null || !world.TryCaptureTrainingReturn(this,out var snapshot)){lastSaveMessage=reason;return false;}
            if(saveStore==null){lastSaveMessage=reason="선택한 캐릭터의 저장 경로가 없습니다.";return false;}
            if(!saveStore.TrySave(snapshot,out reason)){lastSaveMessage=reason;return false;}
            lastSaveMessage=reason=string.Empty;return true;
        }

        public bool SaveBattleCheckpoint(out string reason)
        {
            if (!processingSessionRequest) return SubmitSession(new LocalBattleCheckpointRequest(), out reason);
            reason = "전투 중단 상태를 저장할 수 없습니다.";
            if (!HasCreatedCharacter || transitioning || activeBattle == null || saveStore == null
                || !UnityEngine.SceneManagement.SceneManager.GetSceneByName(BattleSceneName).isLoaded
                || !saveStore.TryLoad(out var previous, out reason)) return false;
            // Reuse the world location saved before entry, never the battle grid position.
            var candidate = CaptureSaveState(previous.mapScene, previous.runtimeScene,
                new UnityEngine.Vector2(previous.worldX, previous.worldY));
            if (!TryCaptureBattleState(activeBattle, activeFormation, candidate, out reason)) return false;
            if (!saveStore.TrySave(candidate, out reason)) { lastSaveMessage = reason; return false; }
            return true;
        }
        private void OnDestroy()
        {if(activeBattle!=null)activeBattle.CombatResultApplied-=SaveCombatResultCheckpoint;foreach(var binding in sessionBindings)binding.Dispose();sessionBindings.Clear();}
    }
}
