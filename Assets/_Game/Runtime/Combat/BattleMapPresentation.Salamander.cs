using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class SalamanderVisual
        {
            public GameObject Root;
            public SpriteRenderer Caster;
            public SpriteRenderer[] Pieces;
            public Vector2Int Destination;
            public int MagicId,Lifetime;
            public float Age;
            public bool Playing;
        }
        readonly Dictionary<short,SalamanderVisual> salamanderCasters=new();
        NativeSalamanderEffects salamanderCatalog;
        void StartSalamander(int id,short casterId,Vector2Int destination)
        {
            salamanderCatalog??=Resources.Load<NativeSalamanderEffects>("Effects/NativeSalamanderEffects");
            if(!isActiveAndEnabled || !IsBound || salamanderCatalog==null || !salamanderCatalog.TryGetLifetime(id,out int lifetime))return;
            var caster=visuals.Find(v=>v.UnitId==casterId);if(caster.Body==null)return;
            if(thunderCasters.TryGetValue(casterId,out var charge))StopMagicVisual(charge);
            if(!salamanderCasters.TryGetValue(casterId,out var effect))
            {
                var root=new GameObject("Native Salamander Caster "+casterId);root.transform.SetParent(transform,false);
                effect=new SalamanderVisual{Root=root,Pieces=new SpriteRenderer[5]};salamanderCasters.Add(casterId,effect);
                for(int i=0;i<5;i++){var go=new GameObject(i==4?"Head":"Trail "+i);go.transform.SetParent(root.transform,false);effect.Pieces[i]=go.AddComponent<SpriteRenderer>();}
            }
            effect.Caster=caster.Body;effect.Destination=destination;effect.MagicId=id;
            effect.Lifetime=lifetime;effect.Age=0;effect.Playing=true;
            foreach(var body in effect.Pieces)
            {
                body.gameObject.layer=caster.Body.gameObject.layer;body.sortingLayerID=caster.Body.sortingLayerID;
                body.sortingOrder=caster.Body.sortingOrder+1;body.enabled=false;
            }
        }
        void StartLocalSalamander(int id,short casterId,short targetId)
        {
            var target=visuals.Find(v=>v.UnitId==targetId);if(target.Body==null)return;
            var cell=ground.WorldToCell(target.Body.transform.position);
            StartSalamander(id,casterId,new Vector2Int(cell.x,bounds.yMax-1-cell.y));
        }
        void AdvanceSalamanders(float seconds)
        {
            foreach(var pair in salamanderCasters)
            {
                var effect=pair.Value;if(!effect.Playing)continue;
                effect.Age+=seconds;int counter=NativeTargetEffects.CounterAt(effect.Age);
                if(!IsBound || battle.IsBattleFinished || effect.Caster==null || counter<0 || counter>=effect.Lifetime
                    || !battle.TryGetUnit(pair.Key,out var unit) || unit.LifeState==UnitLifeState.Removed)
                {StopSalamander(effect);continue;}
                var source=ground.WorldToCell(effect.Caster.transform.position);
                var delta=new Vector2Int((effect.Destination.x-source.x)*32,(effect.Destination.y-(bounds.yMax-1-source.y))*32);
                for(int i=0;i<5;i++)
                {
                    var body=effect.Pieces[i];body.enabled=salamanderCatalog.TryGetPiece(effect.MagicId,counter,delta,i,out var sprite,out var pixels);
                    if(!body.enabled)continue;
                    body.sprite=sprite;var cell=ground.layoutGrid.cellSize;
                    body.transform.position=effect.Caster.transform.position+ground.transform.TransformVector(new Vector3(pixels.x*cell.x/32,pixels.y*cell.y/32,0));
                }
            }
        }
        static void StopSalamander(SalamanderVisual effect){effect.Playing=false;foreach(var body in effect.Pieces)if(body!=null)body.enabled=false;}
        void StopSalamanderCaster(short id){if(salamanderCasters.TryGetValue(id,out var effect))StopSalamander(effect);}
        void SuspendSalamanders(){foreach(var effect in salamanderCasters.Values)StopSalamander(effect);}
        void ReleaseSalamanders(){SuspendSalamanders();foreach(var effect in salamanderCasters.Values)if(effect.Root!=null)ReleasePreview(effect.Root);salamanderCasters.Clear();}
    }
}
