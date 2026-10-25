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
            LocalCommandResponse Handle(Func<LocalCommandResponse> execute)
            {
                if(!isActiveAndEnabled)return new LocalCommandResponse(false,"로컬 세션 서버가 비활성 상태입니다.");
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
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<SaveWorldRequest>(p=>Handle(()=>new LocalCommandResponse(SaveWorld(),lastSaveMessage))));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<TownQuestRequest>(p=>Handle(()=>new LocalCommandResponse(TryCommitTownQuest(p.QuestId,p.Claim,out var reason),reason))));
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<TutorialProgressRequest>(p=>Handle(()=>new LocalCommandResponse(TrySaveTutorial(p.Step,p.Moved,p.Skip,p.Checkpoint,out var reason),reason))));
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
            if(HasCreatedCharacter && !transitioning && !UnityEngine.SceneManagement.SceneManager.GetSceneByName(BattleSceneName).isLoaded)
                SaveWorld();
        }
        private void OnDestroy()
        {foreach(var binding in sessionBindings)binding.Dispose();sessionBindings.Clear();}
    }
}
