using DarkSaver.Combat;
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
            mapId = 0;
            if (catalog == null || !catalog.TryGet(jobId, out var job) || job.CategoryLabel != "player_job") return false;
            // Player progression groups: five starting jobs, then ten jobs per tier.
            // 165..169 are excluded by the user's confirmed original rule.
            if (jobId >= 100 && jobId <= 104) mapId = 101;
            else if (jobId >= 105 && jobId <= 164) mapId = 102 + (jobId - 105) / 10;
            return mapId != 0;
        }

        public bool TryEnter(TopDownPlayerController player)
        {
            if (!isActiveAndEnabled || gameObject.scene.name != "Town_Runtime" || player == null
                || !player.isActiveAndEnabled || player.UiInputBlocked) return false;
            var session = FindAnyObjectByType<BattleTransitionController>();
            var coordinator = FindAnyObjectByType<WorldSceneCoordinator>();
            var hud = FindAnyObjectByType<WorldSessionHud>();
            if (session == null || !session.HasCreatedCharacter || coordinator == null || coordinator.IsTravelling) return false;
            if (!TryResolveTrainingMap(playerJobs, session.PlayerJobId, out var mapId))
            { hud?.SetTrainingEntryMessage("해당 클래스는 훈련장에 입장할 수 없습니다."); return false; }
            if (!coordinator.TryTravelTo("Training" + mapId, "TrainingEntry"))
            { hud?.SetTrainingEntryMessage("훈련장 이동 준비가 되지 않았습니다."); return false; }
            hud?.SetTrainingEntryMessage(null); return true;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<TopDownPlayerController>();
            if (player != null) TryEnter(player);
        }
    }
}
