using System;
namespace DarkSaver.Network
{
    [Serializable] public sealed class BuildingTravelRoute
    {
        public string fromKey,toKey,sourceMap,sourceRuntime,map,runtime,spawnId;
        public float doorX,doorY,x,y;public int width,height;
        public BuildingTravelRoute Clone()=>(BuildingTravelRoute)MemberwiseClone();
    }
    [Serializable] public sealed class BuildingTravelRoutes
    {
        public int version;public string provenance;public BuildingTravelRoute[] routes;
        public bool IsValid
        {
            get
            {
                if(version!=1 || routes==null || routes.Length<2 || routes.Length%2!=0)return false;
                var seen=new System.Collections.Generic.HashSet<string>();
                foreach(var r in routes)
                {
                    if(r==null || string.IsNullOrWhiteSpace(r.fromKey) || string.IsNullOrWhiteSpace(r.toKey)
                        || string.IsNullOrWhiteSpace(r.sourceMap) || string.IsNullOrWhiteSpace(r.sourceRuntime)
                        || string.IsNullOrWhiteSpace(r.map) || string.IsNullOrWhiteSpace(r.runtime) || string.IsNullOrWhiteSpace(r.spawnId)
                        || r.fromKey==r.toKey || (r.fromKey!="Kaosia" && r.toKey!="Kaosia")
                        || r.width<=0 || r.height<=0 || !Finite(r.doorX) || !Finite(r.doorY) || !Finite(r.x) || !Finite(r.y)
                        || r.doorX<0 || r.doorY<0 || r.x<0 || r.y<0 || r.x>=r.width || r.y>=r.height
                        || !seen.Add(r.fromKey+"/"+r.toKey+"/"+r.spawnId))return false;
                    var source=Region(r.fromKey);var target=Region(r.toKey);
                    if(source==null || target==null || r.sourceMap!=source.MapScene || r.sourceRuntime!=source.RuntimeScene
                        || r.map!=target.MapScene || r.runtime!=target.RuntimeScene || r.width!=target.Width || r.height!=target.Height
                        || r.doorX>=source.Width || r.doorY>=source.Height)return false;
                    int pairCount=0;bool reverse=false;
                    foreach(var other in routes)if(other!=null)
                    {
                        if(other.fromKey==r.fromKey && other.toKey==r.toKey)pairCount++;
                        if(other.fromKey==r.toKey && other.toKey==r.fromKey)reverse=true;
                    }
                    if(pairCount!=1 || !reverse)return false;
                }
                return true;
            }
        }
        private static DarkSaver.Core.TownDefinition Region(string key)
        {
            if(key=="Kaosia" && DarkSaver.Core.TownCatalog.TryGet(key,out var town))return town;
            if(DarkSaver.Core.ShopInteriorCatalog.TryGet(key,out var shop))return shop.Region;
            return DarkSaver.Core.KaosiaFacilityCatalog.TryGet(key,out var facility)?facility.Region:null;
        }
        internal static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);
    }
    public readonly struct BuildingTravelContext
    {
        public readonly string Region;public readonly float X,Y;public readonly bool Authenticated,Blocked;
        public BuildingTravelContext(string region,float x,float y,bool authenticated,bool blocked)
        {Region=region;X=x;Y=y;Authenticated=authenticated;Blocked=blocked;}
    }
    // Authority for this user-authorized local contract, not a recovered wire opcode.
    public sealed class BuildingTravelService
    {
        private readonly BuildingTravelRoute[] routes;private MapEnterNotification pending;private int sequence;
        public bool HasPending=>pending!=null;
        public BuildingTravelService(BuildingTravelRoutes source)
        {if(source==null || !source.IsValid)throw new ArgumentException("Invalid building routes.");routes=Array.ConvertAll(source.routes,r=>r.Clone());}
        public bool TryFind(BuildingTravelContext state,Request_EnterMap request,out BuildingTravelRoute route)
        {
            route=null;if(request==null)return false;
            var found=Array.Find(routes,r=>r.fromKey==state.Region && r.toKey==request.Target && r.spawnId==request.SpawnId);
            if(found==null)return false;route=found.Clone();return true;
        }
        public MapEnterNotification Enter(Request_EnterMap request,BuildingTravelContext state)
        {
            if(pending!=null || !state.Authenticated || state.Blocked || !BuildingTravelRoutes.Finite(state.X) || !BuildingTravelRoutes.Finite(state.Y)
                || !TryFind(state,request,out var route))return new MapEnterNotification(reason:"현재 건물 출입 요청을 처리할 수 없습니다.");
            if(!LocalDoorGeometry.Contains(state.X,state.Y,route.doorX,route.doorY) || sequence==int.MaxValue)return new MapEnterNotification(reason:"건물 출입구에 접근해 주세요.");
            pending=new MapEnterNotification(true,MapEnterStage.Started,route.map,route.runtime,route.spawnId,route.width,route.height,route.x,route.y,transferId:++sequence);
            return pending;
        }
        public WorldMoveNotification Move(WorldMoveRequest request,BuildingTravelContext state,float maximumStep,Func<float,float,bool> walkable)
        {
            if(request==null || pending!=null || !state.Authenticated || state.Blocked || walkable==null
                || !Array.Exists(routes,r=>r.fromKey==state.Region)
                || !BuildingTravelRoutes.Finite(state.X) || !BuildingTravelRoutes.Finite(state.Y)
                || !BuildingTravelRoutes.Finite(request.X) || !BuildingTravelRoutes.Finite(request.Y)
                || !BuildingTravelRoutes.Finite(maximumStep) || maximumStep<=0)
                return new WorldMoveNotification(reason:"현재 지역에서 이동할 수 없습니다.");
            float dx=request.X-state.X,dy=request.Y-state.Y;
            if(dx*dx+dy*dy>maximumStep*maximumStep || !walkable(request.X,request.Y))
                return new WorldMoveNotification(reason:"이동 위치를 확인해 주세요.");
            return new WorldMoveNotification(true,request.X,request.Y);
        }
        public MapEnterNotification Complete(MapEnterCompleteRequest request)
        {
            if(request==null || pending==null || request.TransferId!=pending.TransferId)
                return new MapEnterNotification(reason:"오래되거나 중복된 맵 전환 요청입니다.");
            var p=pending;pending=null;
            return request.Loaded?new MapEnterNotification(true,MapEnterStage.Ready,p.Map,p.Runtime,p.SpawnId,p.Width,p.Height,p.X,p.Y,transferId:p.TransferId)
                :new MapEnterNotification(reason:"목적지를 준비하지 못해 기존 지역을 유지합니다.",transferId:p.TransferId);
        }
    }
    public sealed class MapEnterCompleteRequest:ClientPacket
    {
        public int TransferId{get;}public bool Loaded{get;}
        public MapEnterCompleteRequest(int transferId,bool loaded){TransferId=transferId;Loaded=loaded;}
    }
}
