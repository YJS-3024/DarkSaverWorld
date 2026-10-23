using DarkSaver.Player;
using UnityEngine;

namespace DarkSaver.World
{
    public sealed class SceneSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId;

        public void Configure(string id) => spawnId = id;

        public bool Matches(string id) => spawnId == id;

        private void Start()
        {
            if (ScenePortal2D.RequestedSpawnId != spawnId) return;
            var player = FindFirstObjectByType<TopDownPlayerController>();
            if (player != null) player.transform.position = transform.position;
        }
    }
}
