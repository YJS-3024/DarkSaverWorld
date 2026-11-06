using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.Entry;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform battleFeedbackLayer;
        private readonly List<RawImage> battleFeedbackImages=new List<RawImage>();
        private void BuildBattleFeedback()
        {
            battleFeedbackImages.Clear();
            battleFeedbackLayer=Group(mapRoot,"Native numeric pass after magic",new Rect(0,0,0,0));
        }
        private int FeedbackDirection(int unit,long life,int fallback)
            =>view.battle?.attackEffects?.FirstOrDefault(e=>e.unit==unit&&e.life==life&&!e.Expired)?.direction??fallback;
        private Texture2D CombatBodyPicture(TrainingUnit unit)
        {
            int key=OriginalBattleLifecycle.AppearanceKey(unit);
            return art.PalettePicture(definition.combatCatalog?.Job(key)?.picture??-1,unit.nativePalette??(unit.id>=0&&unit.id<10?art.ownPalette:0))??campaign?.Picture(key);
        }
        private bool LiveBattleBodyVisible(TrainingUnit actor)
        {
            if(actor==null)return true;
            if(actor.nativeActive==false||actor.nativeEnabled==false)return false;
            if(((actor.nativeState??0)&0x80)!=0)
                return view.battle.deaths.Any(e=>e.unit==actor.id&&e.life==actor.life&&e.held&&!e.Expired);
            return actor.nativeActive==true&&actor.nativeEnabled==true||actor.hp>0||actor.ghost
                ||view.battle.numberEffects.Any(e=>e.unit==actor.id&&e.life==actor.life&&!e.Expired);
        }
        private Texture2D CorpsePicture(TrainingUnit unit)
        {
            if(unit==null)return null;
            return art.PalettePicture(definition.combatCatalog?.Job(unit.job)?.picture??-1,unit.nativePalette??(unit.id>=0&&unit.id<10?art.ownPalette:0))??campaign?.Picture(unit.job);
        }
        private void DrawNativeUnitFrames(RectTransform layer,List<RawImage> images,ref int index,IEnumerable<BattleFeedbackFrame> frames,Texture body)
        {
            foreach(var frame in frames)
            {
                var texture=frame.surface==BattleFeedbackSurface.Body?body:frame.surface==BattleFeedbackSurface.Board?art.battle.board:frame.surface==BattleFeedbackSurface.Contact?art.battle.attackEffects:frame.surface==BattleFeedbackSurface.Effects?art.battle.effects:art.attackMarker;
                // DirectDraw rejects invalid source rectangles; clamped Unity UVs smear them.
                if(texture==null||!NativeBodyFrames.Fits(frame,texture.width,texture.height))continue;
                if(index==images.Count)images.Add(Picture(layer,"Native unit frame",new Rect(0,0,0,0),texture));
                var image=images[index++];image.texture=texture;image.raycastTarget=false;image.gameObject.SetActive(true);
                Place((RectTransform)image.transform,new Rect((float)frame.x,(float)frame.y,frame.width,frame.height));
                image.uvRect=NativeEntryLayout.Uv(texture,new Rect(frame.atlasX,frame.atlasY,frame.width,frame.height));
            }
        }
    }
}
