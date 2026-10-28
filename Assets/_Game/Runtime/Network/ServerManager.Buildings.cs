using System;
using UnityEngine;
namespace DarkSaver.Network
{
    public sealed partial class ServerManager
    {
        private BuildingTravelService buildingService;private Func<BuildingTravelContext> buildingContext;
        private IDisposable buildingEntryBinding,buildingCompleteBinding,buildingMoveBinding;
        private Func<float,float,bool> buildingWalkable;
        public void BindLocalBuildingTravelService(Func<BuildingTravelContext> context,Func<float,float,bool> walkable=null)
        {
            if(context==null)throw new ArgumentNullException(nameof(context));
            if(buildingService!=null){buildingContext=context;buildingWalkable=walkable;return;}
            if(LocalServer.HasHandler(new WorldMoveRequest()) || LocalServer.HasHandler(new Request_EnterMap()) || LocalServer.HasHandler(new MapEnterCompleteRequest(0,false)))
                throw new InvalidOperationException("Map travel handlers already have an owner.");
            var asset=Resources.Load<TextAsset>("World/BuildingRoutes");
            var service=new BuildingTravelService(asset==null?null:JsonUtility.FromJson<BuildingTravelRoutes>(asset.text));
            buildingContext=context;buildingWalkable=walkable;buildingService=service;
            try
            {
                buildingEntryBinding=RegisterRequestHandler<Request_EnterMap>(request=>{
                    if(trainingTravel!=null && request.Target=="Training"+trainingTravel.MapId && request.SpawnId=="TrainingEntry")
                    {
                        if(!Application.CanStreamedLevelBeLoaded("Map_"+trainingTravel.MapId+"_;") || !Application.CanStreamedLevelBeLoaded("Training"+trainingTravel.MapId+"_Runtime"))
                            return new Response_EnterTraining("훈련장 맵을 불러올 수 없습니다.");
                        return trainingTravel.Enter(new Request_EnterTraining());
                    }
                    var state=buildingContext();
                    if(!service.TryFind(state,request,out var route) || !Application.CanStreamedLevelBeLoaded(route.map) || !Application.CanStreamedLevelBeLoaded(route.runtime))
                        return new MapEnterNotification(reason:"연결된 건물 맵을 불러올 수 없습니다.");
                    return service.Enter(request,state);
                });
                buildingCompleteBinding=RegisterRequestHandler<MapEnterCompleteRequest>(service.Complete);
                buildingMoveBinding=RegisterRequestHandler<WorldMoveRequest>(request=>service.Move(request,buildingContext(),
                    GameConstants.PlayerMoveSpeed*Time.fixedDeltaTime+.001f,buildingWalkable));
            }
            catch{ClearBuildingBindings();throw;}
        }
        public void UnbindLocalBuildingTravelService(Func<BuildingTravelContext> owner)
        {if(buildingContext==owner)ClearBuildingBindings();}
        private void ClearBuildingBindings()
        {buildingEntryBinding?.Dispose();buildingCompleteBinding?.Dispose();buildingMoveBinding?.Dispose();buildingEntryBinding=buildingCompleteBinding=buildingMoveBinding=null;buildingWalkable=null;buildingService=null;buildingContext=null;}
    }
}
