using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class LevelUpCircleVisual { public GameObject Root; public SpriteRenderer Caster; public SpriteRenderer[] Pieces; public float Age; public bool Playing; }
        readonly Dictionary<short,LevelUpCircleVisual> levelUpCircles=new();
        NativeLevelUpCircleEffects levelUpCircleCatalog;
        void StartLevelUpCircle(int magicId,short casterId)
        {
            levelUpCircleCatalog??=Resources.Load<NativeLevelUpCircleEffects>("Effects/NativeLevelUpCircleEffects");
            if(!isActiveAndEnabled || !IsBound || levelUpCircleCatalog==null || !levelUpCircleCatalog.Supports(magicId))return;
            var caster=visuals.Find(v=>v.UnitId==casterId);if(caster.Body==null)return;
            if(thunderCasters.TryGetValue(casterId,out var charge))StopMagicVisual(charge);
            if(!levelUpCircles.TryGetValue(casterId,out var effect))
            {
                var root=new GameObject("Native Level Up Circle "+casterId);root.transform.SetParent(transform,false);
                effect=new LevelUpCircleVisual{Root=root,Pieces=new SpriteRenderer[12]};levelUpCircles.Add(casterId,effect);
                for(int i=0;i<12;i++){var go=new GameObject("Piece "+i);go.transform.SetParent(root.transform,false);effect.Pieces[i]=go.AddComponent<SpriteRenderer>();}
            }
            effect.Caster=caster.Body;effect.Age=0;effect.Playing=true;
            for(int i=0;i<12;i++)
            {
                var body=effect.Pieces[i];body.sprite=levelUpCircleCatalog.Piece(i);body.enabled=false;
                body.gameObject.layer=caster.Body.gameObject.layer;body.sortingLayerID=caster.Body.sortingLayerID;
                body.sortingOrder=caster.Body.sortingOrder-1; // Native redraws the caster after the ground circle.
            }
        }
        void AdvanceLevelUpCircles(float seconds)
        {
            foreach(var pair in levelUpCircles)
            {
                var effect=pair.Value;if(!effect.Playing)continue;
                effect.Age+=seconds;int counter=NativeTargetEffects.CounterAt(effect.Age);
                if(!IsBound || battle.IsBattleFinished || effect.Caster==null || counter<0 || counter>22
                    || !battle.TryGetUnit(pair.Key,out var unit) || unit.LifeState==UnitLifeState.Removed)
                {StopLevelUpCircle(effect);continue;}
                int visible=NativeLevelUpCircleEffects.VisibleCount(counter);
                for(int i=0;i<12;i++)
                {
                    var body=effect.Pieces[i];body.enabled=i<visible;if(!body.enabled)continue;
                    var pixels=levelUpCircleCatalog.Offset(i);var cell=ground.layoutGrid.cellSize;
                    body.transform.position=effect.Caster.transform.position+ground.transform.TransformVector(new Vector3(pixels.x*cell.x/32,pixels.y*cell.y/32,0));
                }
            }
            UpdateLevelUpFlash();
        }
        static void StopLevelUpCircle(LevelUpCircleVisual effect){effect.Playing=false;foreach(var body in effect.Pieces)if(body!=null)body.enabled=false;}
        void StopLevelUpCircleCaster(short id){if(levelUpCircles.TryGetValue(id,out var effect))StopLevelUpCircle(effect);UpdateLevelUpFlash();}
        void SuspendLevelUpCircles(){foreach(var effect in levelUpCircles.Values)StopLevelUpCircle(effect);HideLevelUpFlash();}
        void ReleaseLevelUpCircles(){SuspendLevelUpCircles();foreach(var effect in levelUpCircles.Values)if(effect.Root!=null)ReleasePreview(effect.Root);levelUpCircles.Clear();ReleaseLevelUpFlash();}
    }
}
