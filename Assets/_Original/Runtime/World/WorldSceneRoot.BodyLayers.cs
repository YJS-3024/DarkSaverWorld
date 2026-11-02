using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private sealed class BodySource
        {
            public RawImage image;
            public NativeBodyOccupant occupant;
            public int direction;
            public bool moving;
            public uint? state;
        }
        private RectTransform nativeBodyBack,nativeBodyFront;
        private byte[] nativeBodyAttributes;
        private readonly List<BodySource> nativeBodies=new List<BodySource>();
        private readonly List<RawImage> nativeBackImages=new List<RawImage>(),nativeFrontImages=new List<RawImage>();
        private void ClearNativeBodies()
        {nativeBodies.Clear();nativeBackImages.Clear();nativeFrontImages.Clear();nativeBodyAttributes=null;}
        private void PaintNativeBody(RawImage image,int direction,bool moving,int appearance=-1,int? movementType=null,int tileX=-1,int tileY=-1,uint? state=null,int unit=-1)
        {
            // The interactive root stays on its tile; visuals are drawn in two map passes.
            image.enabled=false;
            nativeBodies.Add(new BodySource{image=image,direction=direction,moving=moving,state=state,
                occupant=new NativeBodyOccupant{id=unit<0?10000+nativeBodies.Count:unit,x=tileX,y=tileY,appearance=appearance,movementType=movementType}});
        }
        private int BodyRenderAttribute(int x,int y)
        {
            var map=DisplayMap(mapId).map;var tile=new TutorialTile(x,y);
            if(!map.Contains(tile))return -1;
            return nativeBodyAttributes!=null&&nativeBodyAttributes.Length==map.width*map.height?nativeBodyAttributes[y*map.width+x]:map.RenderAttribute(tile);
        }
        private TrainingUnit NativeSourceUnit(BodySource source)
            =>BattleMap()&&view.presentation?.completed!=false?view.battle?.Unit(source.occupant.id):null;
        private bool VisibleNativeSource(BodySource source)
        {
            if(source.image==null)return false;
            var unit=NativeSourceUnit(source);
            if(unit!=null&&(unit.nativeActive==false||unit.nativeEnabled==false))return false;
            if(source.image.gameObject.activeInHierarchy)return true;
            // Corpse rendering has its own branch even though the live hit root is hidden.
            return unit!=null&&view.battle.deaths.Any(e=>e.unit==unit.id&&e.life==unit.life&&e.Visible);
        }
        private NativeUnitPresentation NativeSourcePresentation(BodySource source,NativeViewport viewport)
        {
            float x=0,y=0;var parent=source.image.transform.parent as RectTransform;
            while(parent!=null&&parent!=mapRoot){x+=parent.anchoredPosition.x;y-=parent.anchoredPosition.y;parent=parent.parent as RectTransform;}
            var actor=source.occupant;var unit=NativeSourceUnit(source);var texture=source.image.texture;
            return new NativeUnitPresentation{unit=actor.id,life=unit?.life??0,state=source.state,viewport=viewport,
                tileX=actor.x*32,tileY=actor.y*32,renderX=x,renderY=y,clock=view.clock,
                textureWidth=texture?.width??0,textureHeight=texture?.height??0,direction=source.direction,
                appearance=actor.appearance,attribute=BodyRenderAttribute(actor.x,actor.y),movementType=actor.movementType,moving=source.moving,
                active=unit?.nativeActive!=false,corpseAvailable=CorpsePicture(unit)!=null,
                attack=unit==null?CampaignAttack(actor.id):view.battle.attackEffects.FirstOrDefault(e=>e.unit==unit.id&&e.life==unit.life),
                number=unit==null?null:view.battle.numberEffects.FirstOrDefault(e=>e.unit==unit.id&&e.life==unit.life),
                death=unit==null?null:view.battle.deaths.FirstOrDefault(e=>e.unit==unit.id&&e.life==unit.life)};
        }
        private void PaintNativeBodyLayers()
        {
            foreach(var image in nativeBackImages)image.gameObject.SetActive(false);
            foreach(var image in nativeFrontImages)image.gameObject.SetActive(false);
            foreach(var image in battleFeedbackImages)image.gameObject.SetActive(false);
            var sources=nativeBodies.Where(VisibleNativeSource).ToDictionary(s=>s.occupant.id);
            var viewport=NativeViewport.FromPixels(-mapRoot.anchoredPosition.x,mapRoot.anchoredPosition.y,640,480);
            var poses=sources.ToDictionary(p=>p.Key,p=>NativeSourcePresentation(p.Value,viewport));
            var map=DisplayMap(mapId).map;
            var plan=NativeBodyOrder.Plan(map.width,map.height,BodyRenderAttribute,sources.Values.Select(s=>s.occupant),viewport.left,viewport.top,viewport.columns,viewport.rows,includePostPasses:true);
            int back=0,front=0,numbers=0;
            foreach(var draw in plan)
            {
                if(draw.stage==3)continue; // Magic has its own layer after the second unit pass.
                var source=sources[draw.id];var pose=poses[draw.id];
                if(draw.stage==4)
                {DrawNativeUnitFrames(battleFeedbackLayer,battleFeedbackImages,ref numbers,NativeUnitFrames.Numbers(pose),null);continue;}
                var frames=NativeUnitFrames.Compose(pose,draw.part);
                var texture=NativeUnitFrames.Corpse(pose)?CorpsePicture(NativeSourceUnit(source)):source.image.texture;
                if(draw.stage==1)DrawNativeUnitFrames(nativeBodyBack,nativeBackImages,ref back,frames,texture);
                else DrawNativeUnitFrames(nativeBodyFront,nativeFrontImages,ref front,frames,texture);
            }
            // Native stage2 does body redraws -> magic -> a final Y/X numeric scan.
            battleFeedbackLayer.SetAsLastSibling();
        }
    }
}
