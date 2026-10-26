using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.World
{
    public sealed class ScenarioGuideNpc:MonoBehaviour
    {
        public string Key { get; private set; }
        public string FieldKey { get; private set; }
        public string TargetSpawn { get; private set; }
        public void Configure(WorldRoute route)
        {Key=ScenarioCatalog.TryGet(route.toKey,out var scenario)?scenario.Key:null;FieldKey=route.fromKey;TargetSpawn=route.targetSpawnId;}
        public bool CanInteract(TopDownPlayerController player)=>isActiveAndEnabled && player!=null && player.isActiveAndEnabled
            && Key!=null && FieldCatalog.TryGet(FieldKey,out var field) && gameObject.scene.name==field.RuntimeScene
            && SceneManager.GetSceneByName(field.MapScene).isLoaded
            && Vector2.Distance(player.transform.position,transform.position)<=GameConstants.TownNpcInteractionDistance;
        public static bool TryInteractAt(Vector2 point,TopDownPlayerController player)
        {
            ScenarioGuideNpc selected=null;int order=int.MinValue;
            foreach(var hit in Physics2D.OverlapPointAll(point))
            {
                var guide=hit.GetComponentInParent<ScenarioGuideNpc>();if(guide==null || !guide.CanInteract(player))continue;
                int candidate=guide.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
                if(selected==null || candidate>order){selected=guide;order=candidate;}
            }
            return selected!=null && FindAnyObjectByType<ScenarioRoomPanel>()?.TryOpen(selected,player)==true;
        }
    }
}
