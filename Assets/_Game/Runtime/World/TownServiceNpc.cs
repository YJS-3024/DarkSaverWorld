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
        public void Configure(TownServiceNpcEntry value)
        {
            entry=value;ApplyOriginalPresentation();
        }
        private void OnEnable()=>ApplyOriginalPresentation();
        private void ApplyOriginalPresentation()
        {
            if(!OriginalRestorationPolicy.AllowSinglePlayerRules || entry!=null && KaosiaOriginalPresentation.IsTown(entry.townKey))gameObject.SetActive(false);
        }
        public bool TryInteract(TopDownPlayerController player,TownServicesPanel panel)
{return false;}        public static bool TryInteractAt(Vector2 world,TopDownPlayerController player,TownServicesPanel panel)
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
