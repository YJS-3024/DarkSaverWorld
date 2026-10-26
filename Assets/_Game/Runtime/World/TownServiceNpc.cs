using System;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    [Serializable] public sealed class TownServiceNpcEntry
    { public string townKey,name,action,texture;public int questId,appearanceId;public float x,y; }
    [Serializable] public sealed class TownServiceNpcData
    { public int version;public TownServiceNpcEntry[] entries; }

    // Local service roles/positions are authorized by the user; artwork is original.
    public sealed class TownServiceNpc : MonoBehaviour
    {
        private TownServiceNpcEntry entry;
        public TownServiceNpcEntry Definition=>entry;
        public void Configure(TownServiceNpcEntry value)=>entry=value;
        public bool TryInteract(TopDownPlayerController player,TownServicesPanel panel)
        {
            if(entry==null || player==null || panel==null || !isActiveAndEnabled || !player.isActiveAndEnabled || player.UiInputBlocked
                || !TownCatalog.TryGet(entry.townKey,out var town) || gameObject.scene.name!=town.RuntimeScene
                || !SceneManager.GetSceneByName(town.MapScene).isLoaded || !panel.CanOpen
                || FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling==true
                || Vector2.Distance(player.transform.position,transform.position)>GameConstants.TownNpcInteractionDistance)return false;
            player.StopForSceneTravel();
            var session=panel.GetComponent<BattleTransitionController>();
            if(entry.action=="Formation")return session!=null && session.OpenFormationPreparation();
            if(entry.action=="Save")
            {
                if(session==null)return false;
                bool saved=session.SaveWorld();
                return panel.OpenDialogue(entry.name,new[]{saved?"진행 상황을 기록했습니다.":session.LastSaveMessage ?? "기록하지 못했습니다."},player);
            }
            if(!Enum.TryParse<TownService>(entry.action,out var service) || service==TownService.Overview || service==TownService.Dialogue || service==TownService.Shop || service==TownService.Temple)return false;
            return panel.OpenFromNpc(service,player,entry.name,entry.questId);
        }
        public static bool TryInteractAt(Vector2 world,TopDownPlayerController player,TownServicesPanel panel)
        {
            TownServiceNpc selected=null;int order=int.MinValue;
            foreach(var hit in Physics2D.OverlapPointAll(world))
            {
                var npc=hit.GetComponentInParent<TownServiceNpc>();if(npc==null || !npc.isActiveAndEnabled)continue;
                int candidate=npc.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
                if(selected==null || candidate>order){selected=npc;order=candidate;}
            }
            return selected!=null && selected.TryInteract(player,panel);
        }
    }
}
