using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        NativeMagicPresentationCatalog presentationCatalog;
        void StopSharedCaster(short id){if(thunderCasters.TryGetValue(id,out var old))StopMagicVisual(old);}
        void StartSharedCaster(int id,short casterId)
        {
            presentationCatalog??=Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog");
            if(!isActiveAndEnabled || !IsBound || presentationCatalog==null || !presentationCatalog.TryGet(id,out var entry) || !entry.SharedCaster)return;
            var caster=visuals.Find(v=>v.UnitId==casterId);if(caster.Body==null)return;
            thunderCatalog??=Resources.Load<NativeThunderEffects>("Effects/NativeThunderEffectsV2");if(thunderCatalog==null)return;
            if(!thunderCasters.TryGetValue(casterId,out var charge))
            {
                var go=new GameObject("Native Thunder Caster "+casterId);go.transform.SetParent(transform,false);
                charge=new FireballVisual{TargetId=casterId,Body=go.AddComponent<SpriteRenderer>()};thunderCasters.Add(casterId,charge);
            }
            charge.Body.gameObject.layer=caster.Body.gameObject.layer;charge.Body.sortingLayerID=caster.Body.sortingLayerID;
            charge.Body.sortingOrder=caster.Body.sortingOrder+1;charge.TargetBody=caster.Body;charge.Thunder=thunderCatalog;
            charge.Age=0;charge.Playing=true;charge.Body.enabled=false;
        }
    }
}
