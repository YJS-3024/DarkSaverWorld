using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class ExpandingCaster
        {
            public SpriteRenderer Body,Caster;public NativeExpandingCasterEffects.Entry Entry;public float Age;public bool Playing;
        }
        readonly Dictionary<short,ExpandingCaster> expandingCasters=new();
        NativeExpandingCasterEffects expandingCasterCatalog;
        void StartExpandingCaster(int id,short casterId)
        {
            expandingCasterCatalog??=Resources.Load<NativeExpandingCasterEffects>("Effects/NativeExpandingCasterEffects");
            if(!isActiveAndEnabled || !IsBound || expandingCasterCatalog==null || !expandingCasterCatalog.TryGet(id,out var entry))return;
            var caster=visuals.Find(v=>v.UnitId==casterId);if(caster.Body==null)return;
            if(thunderCasters.TryGetValue(casterId,out var charge))StopMagicVisual(charge);
            if(!expandingCasters.TryGetValue(casterId,out var effect))
            {
                var go=new GameObject("Native Expanding Caster "+casterId);go.transform.SetParent(transform,false);
                effect=new ExpandingCaster{Body=go.AddComponent<SpriteRenderer>()};expandingCasters.Add(casterId,effect);
            }
            effect.Entry=entry;effect.Caster=caster.Body;effect.Age=0;effect.Playing=true;effect.Body.enabled=false;
            effect.Body.gameObject.layer=caster.Body.gameObject.layer;effect.Body.sortingLayerID=caster.Body.sortingLayerID;
            effect.Body.sortingOrder=caster.Body.sortingOrder+1;
        }
        void AdvanceExpandingCasters(float seconds)
        {
            foreach(var pair in expandingCasters)
            {
                var effect=pair.Value;if(!effect.Playing)continue;
                effect.Age+=seconds;int counter=NativeTargetEffects.CounterAt(effect.Age);
                if(!IsBound || battle.IsBattleFinished || effect.Caster==null || counter<0 || counter>=effect.Entry.Lifetime
                    || !battle.TryGetUnit(pair.Key,out var unit) || unit.LifeState==UnitLifeState.Removed)
                {StopExpandingCaster(effect);continue;}
                var sprite=effect.Entry.Frame(counter);var pixels=effect.Entry.PixelSize(counter);
                effect.Body.sprite=sprite;effect.Body.transform.localScale=new Vector3(pixels.x/sprite.rect.width,pixels.y/sprite.rect.height,1);
                effect.Body.transform.position=effect.Caster.transform.position; // Original stretch rectangle is centered on the native cell, not on the sprite body.
                effect.Body.enabled=pixels.x>0 && pixels.y>0;
            }
        }
        static void StopExpandingCaster(ExpandingCaster effect){effect.Playing=false;if(effect.Body!=null)effect.Body.enabled=false;}
        void StopExpandingCasterUnit(short id){if(expandingCasters.TryGetValue(id,out var effect))StopExpandingCaster(effect);}
        void SuspendExpandingCasters(){foreach(var effect in expandingCasters.Values)StopExpandingCaster(effect);}
        void ReleaseExpandingCasters(){SuspendExpandingCasters();foreach(var effect in expandingCasters.Values)if(effect.Body!=null)ReleasePreview(effect.Body.gameObject);expandingCasters.Clear();}
    }
}
