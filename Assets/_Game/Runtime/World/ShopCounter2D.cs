using DarkSaver.Player;
using DarkSaver.UI;
using UnityEngine;
namespace DarkSaver.World
{
    // Existing shop UI entry, attached to authored indoor staff.
    public sealed class ShopCounter2D : MonoBehaviour
    {
        private void OnEnable()
        {
            if(Application.isPlaying && !DarkSaver.Core.OriginalRestorationPolicy.AllowSinglePlayerRules
                && !DarkSaver.Core.OriginalRestorationPolicy.AllowsStaffEvidence(GetComponent<OriginalTownStaff>()?.Source?.evidence))
                gameObject.SetActive(false);
        }
        // Compatibility/display value only; CanInteractFrom owns the cell-based permission.
        public float InteractionDistance { get; private set; }=GameConstants.TownNpcInteractionDistance;
        private Vector2Int[] counterFront = System.Array.Empty<Vector2Int>();
        internal void ConfigureCounterFront(Vector2Int[] cells)
            => counterFront = cells == null ? System.Array.Empty<Vector2Int>() : (Vector2Int[])cells.Clone();
        public bool CanInteractFrom(Vector2 position)
        {
            if (NpcInteractionRules.IsAdjacent(position, transform.position)) return true;
            foreach (var cell in counterFront)
                if (NpcInteractionRules.IsCounterFront(position, cell)) return true;
            return false;
        }
        public bool TryOpen(TopDownPlayerController player)
        {
            bool mercenary=gameObject.scene.name=="Interior2900_Runtime";
            bool warehouse=gameObject.scene.name=="KaosiaWarehouse_Runtime";
            if(!isActiveAndEnabled || (ShopInteriorContext.Current==null && !DarkSaver.Combat.BattleTransitionController.IsTempleReady && !warehouse && !mercenary) || player==null || !player.isActiveAndEnabled || player.UiInputBlocked || !CanInteractFrom(player.transform.position)
                || FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling==true)return false;
            var point=new Vector2(Mathf.Floor(player.transform.position.x)+.5f,Mathf.Floor(player.transform.position.y)+.5f);
            if(((Vector2)player.transform.position-point).sqrMagnitude>.0001f)
                return player.QueueNpcInteraction(()=>{if(this!=null)TryOpen(player);});
            if(warehouse)return NativeWarehousePanel.RequestOpen(this,player);
            if(mercenary)return FindAnyObjectByType<TownServicesPanel>()?.RequestOpenMercenary(this,player)==true;
            var panel = FindAnyObjectByType<TownServicesPanel>();if(panel==null)return false;
            return DarkSaver.Combat.BattleTransitionController.IsTempleReady
                ? panel.Open(TownService.Temple, player) : panel.RequestOpenShop(this, player);
        }
        public static bool TryInteractAt(Vector2 world,TopDownPlayerController player)
        {
            ShopCounter2D selected=null;int order=int.MinValue;
            foreach(var hit in Physics2D.OverlapPointAll(world))
            {
                var npc=hit.GetComponentInParent<ShopCounter2D>();
                if(npc==null || !npc.isActiveAndEnabled || player==null || !npc.CanInteractFrom(player.transform.position))continue;
                int candidate=npc.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
                if(selected==null || candidate>order){selected=npc;order=candidate;}
            }
            if(selected==null || !selected.TryOpen(player))return false;
            return true;
        }
    }
}
