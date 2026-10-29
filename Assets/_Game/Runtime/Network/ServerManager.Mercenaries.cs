using System;
using DarkSaver.Combat;
using DarkSaver.Player;
using DarkSaver.UI;
using DarkSaver.World;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.Network
{
    public sealed partial class ServerManager
    {
        private IDisposable mercenaryOpen,mercenaryHire,mercenaryDismiss;
        private ShopCounter2D mercenaryCounter;
        public void BindLocalMercenaryService()
        {
            if(mercenaryOpen!=null)return;
            try
            {
                mercenaryOpen=RegisterRequestHandler<Request_OpenMercenary>(r=>
                {
                    var panel=GetComponent<TownServicesPanel>();var session=GetComponent<BattleTransitionController>();var player=FindAnyObjectByType<TopDownPlayerController>();ShopCounter2D npc=null;
                    foreach(var candidate in FindObjectsByType<ShopCounter2D>(FindObjectsSortMode.None))if(candidate.GetEntityId().ToString()==r.NpcId){npc=candidate;break;}
                    if(r.OwnerId!=100 || panel==null || panel.IsOpen || !panel.CanOpenService(TownService.Mercenary) || session==null || !session.HasCreatedCharacter || session.HasPendingTutorial || session.IsTransitioning || player==null || player.UiInputBlocked || !ValidCounter(npc,player))return new Response_OpenMercenary(false,r.NpcId,r.OwnerId,reason:"현재 위치에서는 용병 거래를 할 수 없습니다.");
                    mercenaryCounter=npc;var offers=panel.CaptureOriginalMercenaryOffers();PublishMercenaries();return new Response_OpenMercenary(true,r.NpcId,r.OwnerId,offers);
                });
                mercenaryHire=RegisterRequestHandler<Request_HireMercenary>(r=>
                {
                    if(!CanMercenaryTrade(r.OwnerId,out var session,out var panel))return new LocalCommandResponse(false,"용병의 집 거래창을 다시 열어 주세요.");
                    if(!MercenaryTradeRules.TryHire(r.OwnerId,r.JobId,r.Maximum,panel.CaptureOriginalMercenaryOffers(),session.CaptureOwnedRoster(),session.CaptureMercenaryInventory(),out var roster,out var money,out string reason))return new LocalCommandResponse(false,reason);
                    if(!session.CommitMercenaryTrade(roster,money,out reason))return new LocalCommandResponse(false,reason);
                    PublishMercenaries();return new LocalCommandResponse(true,"용병을 고용했습니다.");
                });
                mercenaryDismiss=RegisterRequestHandler<Request_DismissMercenary>(r=>
                {
                    if(!CanMercenaryTrade(r.OwnerId,out var session,out _))return new LocalCommandResponse(false,"용병의 집 거래창을 다시 열어 주세요.");
                    var money=session.CaptureMercenaryInventory();
                    if(!MercenaryTradeRules.TryDismiss(r.OwnerId,r.JobId,session.CaptureOwnedRoster(),money,out var roster,out string reason))return new LocalCommandResponse(false,reason);
                    if(!session.CommitMercenaryTrade(roster,money,out reason))return new LocalCommandResponse(false,reason);
                    PublishMercenaries();return new LocalCommandResponse(true,"용병을 해고했습니다.");
                });
            }
            catch{ClearMercenaryBindings();throw;}
        }
        private static bool ValidCounter(ShopCounter2D npc,TopDownPlayerController player)
            =>npc!=null && npc.isActiveAndEnabled && player!=null && player.isActiveAndEnabled && npc.gameObject.scene.name=="Interior2900_Runtime" && SceneManager.GetSceneByName("Interior2900_Map").isLoaded
                && SceneManager.GetSceneByName("Interior2900_Runtime").isLoaded && FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling!=true && npc.GetComponent<OriginalTownStaff>()?.Source?.nativeMap=="2900" && npc.CanInteractFrom(player.transform.position)
                && ((Vector2)player.transform.position-new Vector2(Mathf.Floor(player.transform.position.x)+.5f,Mathf.Floor(player.transform.position.y)+.5f)).sqrMagnitude<.0001f;
        private bool CanMercenaryTrade(ushort owner,out BattleTransitionController session,out TownServicesPanel panel)
        {
            session=GetComponent<BattleTransitionController>();panel=GetComponent<TownServicesPanel>();var player=FindAnyObjectByType<TopDownPlayerController>();
            return owner==100 && session!=null && session.HasCreatedCharacter && !session.HasPendingTutorial && !session.IsTransitioning && panel!=null && panel.IsOpen && panel.Service==TownService.Mercenary && panel.CanOpen && panel.HasMercenaryCounter(mercenaryCounter!=null?mercenaryCounter.GetEntityId().ToString():null) && ValidCounter(mercenaryCounter,player);
        }
        private void PublishMercenaries()
        {
            var session=GetComponent<BattleTransitionController>();int count=0,job=-1;
            foreach(var unit in session.CaptureOwnedRoster()?.units??Array.Empty<DarkSaver.Core.NativeOwnedUnitData>())if(unit.hireOwnerId==100){if(job!=-1 && job!=unit.jobId)job=-2;else if(job!=-2)job=unit.jobId;count++;}
            SendToClient(new Notify_Mercenaries(100,job,count,session.CurrentGold??0));
        }
        private void ClearMercenaryBindings(){mercenaryOpen?.Dispose();mercenaryHire?.Dispose();mercenaryDismiss?.Dispose();mercenaryOpen=mercenaryHire=mercenaryDismiss=null;mercenaryCounter=null;}
    }
}
