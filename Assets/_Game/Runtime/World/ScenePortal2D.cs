using DarkSaver.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ScenePortal2D : MonoBehaviour
    {
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawnId;

        public static string RequestedSpawnId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSpawnRequest() => RequestedSpawnId = null;

        public static void SetRequestedSpawn(string spawnId) => RequestedSpawnId = spawnId;

        public void Configure(string sceneName, string spawnId)
        {
            targetScene = sceneName;
            targetSpawnId = spawnId;
            var trigger = GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<TopDownPlayerController>();
            if (player == null || !player.isActiveAndEnabled) return;
            var coordinator = FindFirstObjectByType<WorldSceneCoordinator>();
            if (coordinator != null) coordinator.TravelTo(targetScene, targetSpawnId);
            else { RequestedSpawnId = targetSpawnId; SceneManager.LoadScene(targetScene); }
        }
    }
}
