using DarkSaver.Player;
using UnityEngine;

namespace DarkSaver.Combat
{
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class BattleEncounter2D : MonoBehaviour
    {
        private bool armed = true;
        private bool retireAfterDefeat;
        public void ConfigureFieldLifecycle() => retireAfterDefeat = true;
        private TopDownPlayerController encounteredPlayer;
        public bool IsArmed => armed;
        [SerializeField] private BattleObjectiveDefinition victoryObjective;
        [SerializeField] private BattleEnemyDefinition enemyDefinition;
        public void ConfigureEnemy(BattleEnemyDefinition enemy) => enemyDefinition = enemy;

        private void Awake()
        {
            if(Application.isPlaying && !DarkSaver.Core.OriginalRestorationPolicy.AllowSinglePlayerRules)
            {gameObject.SetActive(false);return;}
            GetComponent<BoxCollider2D>().isTrigger = true;
            GetComponent<SpriteRenderer>().sortingOrder = 20000 - Mathf.RoundToInt(transform.position.y * 10f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryEncounter(other.GetComponent<TopDownPlayerController>());
        }

        public bool TryEncounter(TopDownPlayerController player)
        {
            if (!DarkSaver.Core.OriginalRestorationPolicy.AllowSinglePlayerRules || TryRetireDefeatedFieldEncounter() || !isActiveAndEnabled || !armed || player == null || !player.isActiveAndEnabled || player.UiInputBlocked) return false;
            var transition = FindFirstObjectByType<BattleTransitionController>();
            if (transition == null || !transition.TryEnterBattle(victoryObjective, enemyDefinition, GetComponent<SpriteRenderer>())) return false;
            armed = false;
            encounteredPlayer = player;
            return true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<TopDownPlayerController>() == encounteredPlayer) TryRearm();
        }

        // Physics suspension can synthesize Trigger Exit during a battle. Rearm
        // only after world input/physics return and the player actually left.
        public bool TryRearm()
        {
            if (TryRetireDefeatedFieldEncounter() || armed || !isActiveAndEnabled || encounteredPlayer == null || !encounteredPlayer.isActiveAndEnabled) return false;
            var body = encounteredPlayer.GetComponent<Rigidbody2D>();
            var other = encounteredPlayer.GetComponent<Collider2D>();
            var trigger = GetComponent<BoxCollider2D>();
            if (body == null || !body.simulated || other == null || !other.enabled || !trigger.enabled) return false;
            var distance = trigger.Distance(other);
            if (!distance.isValid || distance.isOverlapped) return false;
            armed = true; return true;
        }

        private bool TryRetireDefeatedFieldEncounter()
        {
            if (!retireAfterDefeat || encounteredPlayer == null || GetComponent<SpriteRenderer>().enabled
                || UnityEngine.SceneManagement.SceneManager.GetSceneByName("Battle").isLoaded) return false;
            var session = FindFirstObjectByType<BattleTransitionController>();
            if (session?.IsTransitioning ?? false) return false;
            // A confirmed dead source body must not leave an invisible trigger.
            GetComponent<BoxCollider2D>().enabled = false; enabled = false; return true;
        }
        private void Update()
        {
            if (TryRetireDefeatedFieldEncounter()) return;
            if (!armed) TryRearm();
        }
    }
}
