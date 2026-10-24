using DarkSaver.Player;
using UnityEngine;

namespace DarkSaver.Events
{
    public sealed class Scenario01Encounter : MonoBehaviour
    {
        private Scenario01Controller owner;
        private int id;
        private bool armed = true;
        private TopDownPlayerController player;
        public void Configure(Scenario01Controller controller, int unitId) { owner = controller; id = unitId; }
        private void OnTriggerEnter2D(Collider2D other)
        {
            var candidate = other.GetComponent<TopDownPlayerController>();
            if (!armed || candidate == null || !candidate.isActiveAndEnabled || candidate.UiInputBlocked) return;
            if (owner != null && owner.TryEncounter(id, GetComponent<SpriteRenderer>())) { armed = false; player = candidate; }
        }
        private void Update()
        {
            if (armed || player == null || !player.isActiveAndEnabled || player.UiInputBlocked) return;
            var other = player.GetComponent<Collider2D>(); var body = player.GetComponent<Rigidbody2D>();
            if (body == null || !body.simulated || other == null || !other.enabled) return;
            var distance = GetComponent<BoxCollider2D>().Distance(other);
            if (distance.isValid && !distance.isOverlapped) armed = true;
        }
    }
}
