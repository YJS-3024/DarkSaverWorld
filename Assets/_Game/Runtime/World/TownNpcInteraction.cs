using DarkSaver.Player;
using DarkSaver.UI;
using UnityEngine;

namespace DarkSaver.World
{
    // Attach only to confirmed NPC placements; no fabricated NPCs or dialogue.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TownNpcInteraction : MonoBehaviour
    {
        [SerializeField] private string speaker;
        [SerializeField] private string[] dialogue = System.Array.Empty<string>();
        [SerializeField] private TownService destination = TownService.Overview;
        public void Configure(string name, string[] lines)
        { speaker = name; dialogue = lines == null ? null : (string[])lines.Clone(); destination = TownService.Overview; }
        public bool ConfigureFacility(string name, string[] lines, TownService service)
        {
            if (string.IsNullOrWhiteSpace(name) || lines == null || lines.Length == 0
                || !System.Enum.IsDefined(typeof(TownService), service) || service == TownService.Dialogue) return false;
            foreach (var line in lines) if (string.IsNullOrWhiteSpace(line)) return false;
            Configure(name, lines); destination = service; return true;
        }

        public bool TryInteract(TopDownPlayerController player, TownServicesPanel panel)
        {
            if (!isActiveAndEnabled || gameObject.scene.name != "Town_Runtime" || player == null || panel == null
                || player.UiInputBlocked || Vector2.Distance(player.transform.position, transform.position) > GameConstants.TownNpcInteractionDistance) return false;
            return panel.OpenDialogue(speaker, dialogue, player, destination);
        }

        public static bool TryInteractAt(Vector2 world, TopDownPlayerController player, TownServicesPanel panel)
        {
            TownNpcInteraction selected = null; int order = int.MinValue;
            foreach (var hit in Physics2D.OverlapPointAll(world))
            {
                var npc = hit.GetComponentInParent<TownNpcInteraction>();
                if (npc == null || !npc.isActiveAndEnabled || npc.gameObject.scene.name != "Town_Runtime") continue;
                int candidate = npc.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
                if (selected == null || candidate > order) { selected = npc; order = candidate; }
            }
            return selected != null && selected.TryInteract(player, panel);
        }
    }
}
