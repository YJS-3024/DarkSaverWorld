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

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<TopDownPlayerController>();
            if (player != null) TryEnter(player);
        }
    }
}
