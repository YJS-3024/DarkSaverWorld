using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class ThrustBeamVisual
        {
            public GameObject Root;public SpriteRenderer Caster;public SpriteRenderer[] Parts;
            public int MagicId,Lifetime,Direction;public float Age;public bool Playing;
            public NativeThrustBeamEffects Catalog;
        }
        readonly Dictionary<short,ThrustBeamVisual> thrustBeams=new();
        NativeThrustBeamEffects thrustBeamCatalog;
        NativeThrustBeamEffects doubleThrustBeamCatalog;
        NativeThrustBeamEffects plasmaBeamCatalog;
        void StartThrustBeam(int id,short casterId,int direction)
        {
            thrustBeamCatalog??=Resources.Load<NativeThrustBeamEffects>("Effects/NativeThrustBeamEffects");
            doubleThrustBeamCatalog??=Resources.Load<NativeThrustBeamEffects>("Effects/NativeDoubleThrustBeamEffects");
            plasmaBeamCatalog??=Resources.Load<NativeThrustBeamEffects>("Effects/NativePlasmaBeamEffects");
            if(!isActiveAndEnabled || !IsBound)return;
            var catalog=thrustBeamCatalog;int life=0;
            if(catalog==null || !catalog.TryGetLifetime(id,out life))
            {catalog=doubleThrustBeamCatalog;if(catalog==null || !catalog.TryGetLifetime(id,out life))
                {catalog=plasmaBeamCatalog;if(catalog==null || !catalog.TryGetLifetime(id,out life))return;}}
            var caster=visuals.Find(v=>v.UnitId==casterId);if(caster.Body==null)return;
            if(thunderCasters.TryGetValue(casterId,out var charge))StopMagicVisual(charge);
            if(!thrustBeams.TryGetValue(casterId,out var effect))
            {
                var root=new GameObject("Native Thrust Beam "+casterId);root.transform.SetParent(transform,false);
                effect=new ThrustBeamVisual{Root=root,Parts=new SpriteRenderer[9]};thrustBeams.Add(casterId,effect);
                for(int i=0;i<9;i++)
                {
                    var go=new GameObject((i>=6?"Third ":i>=3?"Second ":"")+(i%3==0?"Start":i%3==1?"Body":"End"));go.transform.SetParent(root.transform,false);
                    effect.Parts[i]=go.AddComponent<SpriteRenderer>();
                    // Both native strip columns/rows are pixel-identical. A borderless slice is
                    // equivalent to repetition, without fractional-screen-pixel tile seams.
                    if(i%3==1)effect.Parts[i].drawMode=SpriteDrawMode.Sliced;
                }
            }
            effect.Caster=caster.Body;effect.MagicId=id;effect.Lifetime=life;effect.Direction=direction;effect.Age=0;effect.Playing=true;
            effect.Catalog=catalog;
            foreach(var body in effect.Parts)
            {
                body.gameObject.layer=caster.Body.gameObject.layer;body.sortingLayerID=caster.Body.sortingLayerID;
                body.sortingOrder=caster.Body.sortingOrder+1;body.enabled=false;
            }
        }
        void AdvanceThrustBeams(float seconds)
        {
            foreach(var pair in thrustBeams)
            {
                var effect=pair.Value;if(!effect.Playing)continue;
                effect.Age+=seconds;int counter=NativeTargetEffects.CounterAt(effect.Age);
                if(!IsBound || battle.IsBattleFinished || effect.Caster==null || counter<0 || counter>=effect.Lifetime
                    || !battle.TryGetUnit(pair.Key,out var unit) || unit.LifeState==UnitLifeState.Removed)
                {StopThrustBeam(effect);continue;}
                for(int i=0;i<9;i++)
                {
                    var body=effect.Parts[i];body.enabled=false;if(i>=effect.Catalog.PartCount)continue;
                    body.enabled=effect.Catalog.TryGetPart(effect.MagicId,counter,effect.Direction,i,out var sprite,out var pixels,out var size);
                    if(!body.enabled)continue;
                    body.sprite=sprite;if(i%3==1)body.size=size/32;
                    var cell=ground.layoutGrid.cellSize;
                    body.transform.position=effect.Caster.transform.position+ground.transform.TransformVector(new Vector3(pixels.x*cell.x/32,pixels.y*cell.y/32,0));
                }
            }
        }
        static void StopThrustBeam(ThrustBeamVisual effect){effect.Playing=false;foreach(var body in effect.Parts)if(body!=null)body.enabled=false;}
        void StopThrustBeamCaster(short id){if(thrustBeams.TryGetValue(id,out var effect))StopThrustBeam(effect);}
        void SuspendThrustBeams(){foreach(var effect in thrustBeams.Values)StopThrustBeam(effect);}
        void ReleaseThrustBeams(){SuspendThrustBeams();foreach(var effect in thrustBeams.Values)if(effect.Root!=null)ReleasePreview(effect.Root);thrustBeams.Clear();}
    }
}
