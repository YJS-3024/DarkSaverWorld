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
        public float InteractionDistance { get; private set; }=GameConstants.TownNpcInteractionDistance;
        internal void ConfigureWideCounter()=>InteractionDistance=3f;
        public bool TryOpen(TopDownPlayerController player)
        {
            if(!isActiveAndEnabled || (ShopInteriorContext.Current==null && !DarkSaver.Combat.BattleTransitionController.IsTempleReady) || player==null || !player.isActiveAndEnabled || player.UiInputBlocked || Vector2.Distance(player.transform.position,transform.position)>InteractionDistance
                || FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling==true)return false;
            return FindAnyObjectByType<TownServicesPanel>()?.Open(DarkSaver.Combat.BattleTransitionController.IsTempleReady ? TownService.Temple : TownService.Shop,player) ?? false;
        }
        public static bool TryInteractAt(Vector2 world,TopDownPlayerController player)
        {
            ShopCounter2D selected=null;int order=int.MinValue;
            foreach(var hit in Physics2D.OverlapPointAll(world))
            {
                var npc=hit.GetComponentInParent<ShopCounter2D>();
                if(npc==null || !npc.isActiveAndEnabled || player==null || Vector2.Distance(player.transform.position,npc.transform.position)>4)continue;
                int candidate=npc.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
                if(selected==null || candidate>order){selected=npc;order=candidate;}
            }
            if(selected==null || !selected.TryOpen(player))return false;
            player.StopForSceneTravel();return true;
        }
    }
}
