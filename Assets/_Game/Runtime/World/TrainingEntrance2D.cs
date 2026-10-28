using DarkSaver.Jobs;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEngine;

namespace DarkSaver.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TrainingEntrance2D : MonoBehaviour
    {
        [SerializeField] private JobCatalog playerJobs;
        private void Awake(){var box=GetComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(DarkSaver.Network.LocalDoorGeometry.Width,DarkSaver.Network.LocalDoorGeometry.Height);box.offset=new Vector2(DarkSaver.Network.LocalDoorGeometry.OffsetX,0);}
        public void Configure(JobCatalog catalog) => playerJobs = catalog;

        public static bool TryResolveTrainingMap(JobCatalog catalog, int jobId, out int mapId)
        {
            // Compatibility signature for serialized callers. Job tiers alone do
            // not establish a native server's destination map assignment.
            mapId = 0;
            return false;
        }

        public bool TryEnter(TopDownPlayerController player)
        {
            if (!isActiveAndEnabled || gameObject.scene.name != "Town_Runtime" || player == null
                || !player.isActiveAndEnabled || player.UiInputBlocked) return false;
            var coordinator=FindAnyObjectByType<WorldSceneCoordinator>();
            return coordinator!=null && coordinator.TryEnterTraining();
        }

        private float retryAfter;
        private void OnTriggerEnter2D(Collider2D other)=>TryTrigger(other);
        private void OnTriggerStay2D(Collider2D other)=>TryTrigger(other);
        private void TryTrigger(Collider2D other)
        {
            if(Time.unscaledTime<retryAfter)return;
            var player=other.GetComponentInParent<TopDownPlayerController>();
            if(player==null || player.UiInputBlocked || FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling==true)return;
            retryAfter=Time.unscaledTime+1;TryEnter(player);
        }
    }
}
