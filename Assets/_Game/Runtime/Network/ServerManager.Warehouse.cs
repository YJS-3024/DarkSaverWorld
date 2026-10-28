using System;
using DarkSaver.Combat;
using DarkSaver.Player;
using DarkSaver.World;
using DarkSaver.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.Network
{
    public sealed class Request_OpenWarehouse:ClientPacket
    {
        public string NpcId{get;}
        public Request_OpenWarehouse(string id){NpcId=id;}
    }
    public sealed class Response_OpenWarehouse:LocalCommandResponse
    {
        public Response_OpenWarehouse(bool accepted,string reason=null):base(accepted,reason){}
    }
    public sealed partial class ServerManager
    {
        private IDisposable warehouseBinding;
        public void BindLocalWarehouseService()
        {
            if(warehouseBinding!=null)return;
            warehouseBinding=RegisterRequestHandler<Request_OpenWarehouse>(request=>
            {
                var session=GetComponent<BattleTransitionController>();var player=FindFirstObjectByType<TopDownPlayerController>();
                ShopCounter2D npc=null;
                foreach(var row in FindObjectsByType<ShopCounter2D>(FindObjectsSortMode.None))
                    if(row.GetEntityId().ToString()==request.NpcId){npc=row;break;}
                bool accepted=session!=null && session.HasCreatedCharacter && !session.HasPendingTutorial && !session.IsTransitioning
                    && player!=null && player.isActiveAndEnabled && !player.UiInputBlocked
                    && SceneManager.GetSceneByName("KaosiaWarehouse_Map").isLoaded
                    && SceneManager.GetSceneByName("KaosiaWarehouse_Runtime").isLoaded
                    && FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling!=true
                    && !DarkSaver.Events.Scenario01Controller.BlocksWorldActions
                    && npc!=null && npc.isActiveAndEnabled && npc.gameObject.scene.name=="KaosiaWarehouse_Runtime"
                    && DarkSaver.Core.OriginalRestorationPolicy.AllowsStaffEvidence(npc.GetComponent<OriginalTownStaff>()?.Source?.evidence)
                    && npc.CanInteractFrom(player.transform.position);
                return new Response_OpenWarehouse(accepted,accepted?null:"창고 직원 앞에서 다시 이용해 주세요.");
            });
        }
        private void ClearWarehouseBinding(){warehouseBinding?.Dispose();warehouseBinding=null;}
    }
}
