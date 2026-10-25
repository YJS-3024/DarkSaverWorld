using DarkSaver.Player;
using DarkSaver.UI;
using UnityEngine;
namespace DarkSaver.World
{
    // Existing shop UI entry, attached to authored indoor staff.
    public sealed class ShopCounter2D : MonoBehaviour
    {
        private InGameUi ui;
        public bool TryOpen(TopDownPlayerController player)
        {
            if((ShopInteriorContext.Current==null && !DarkSaver.Combat.BattleTransitionController.IsTempleReady) || player==null || !player.isActiveAndEnabled || player.UiInputBlocked
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
        private void OnTriggerEnter2D(Collider2D other)=>TryOpen(other.GetComponent<TopDownPlayerController>());
        private void LateUpdate()
        {
            var player=FindAnyObjectByType<TopDownPlayerController>();
            if(player==null || player.UiInputBlocked || Vector2.Distance(player.transform.position,transform.position)>4 || Camera.main==null){ui?.Hide();return;}
            var screen=Camera.main.WorldToScreenPoint(transform.position);
            if(screen.z<=0 || !DarkSaver.Rendering.GameScreen.TryScreenToGui(screen,out var point)){ui?.Hide();return;}
            InGameUi.Ensure(ref ui,this,20);ui.Begin();
            ui.Button("counter",new Rect(point.x-90,point.y+8,180,28),DarkSaver.Combat.BattleTransitionController.IsTempleReady ? "신전 이용" : "상점 이용",()=>TryOpen(player));ui.End();
        }
        private void OnDisable()=>ui?.Hide();
    }
}
