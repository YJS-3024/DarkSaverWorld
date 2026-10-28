using DarkSaver.Player;
using UnityEngine;
namespace DarkSaver.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BuildingDoor2D:MonoBehaviour
    {
        private string target,spawn;private float retryAfter;
        public void Configure(string destination,string spawnId)
        {target=destination;spawn=spawnId;var box=GetComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(DarkSaver.Network.LocalDoorGeometry.Width,DarkSaver.Network.LocalDoorGeometry.Height);box.offset=new Vector2(DarkSaver.Network.LocalDoorGeometry.OffsetX,0);}
        private void OnTriggerEnter2D(Collider2D other)=>TryEnter(other);
        private void OnTriggerStay2D(Collider2D other)=>TryEnter(other);
        private void TryEnter(Collider2D other)
        {
            if(Time.unscaledTime<retryAfter)return;
            var player=other.GetComponentInParent<TopDownPlayerController>();
            if(player==null || !player.isActiveAndEnabled || player.UiInputBlocked)return;
            var coordinator=FindAnyObjectByType<WorldSceneCoordinator>();
            if(coordinator==null || coordinator.IsTravelling)return;
            retryAfter=Time.unscaledTime+1f;coordinator.TryTravelTo(target,spawn);
        }
    }
}
