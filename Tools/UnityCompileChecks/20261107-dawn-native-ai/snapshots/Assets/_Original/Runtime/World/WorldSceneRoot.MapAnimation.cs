using System.Collections.Generic;
using DarkSaver.Original.Maps;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private readonly NativeMapAnimationClock mapAnimationClock=new NativeMapAnimationClock();
        private readonly Dictionary<int,NativeMapTileAnimation> mapAnimationTiles=new Dictionary<int,NativeMapTileAnimation>();
        private readonly List<RawImage> mapAnimationImages=new List<RawImage>();
        private RectTransform mapAnimationLayer;
        private NativeMapPixels animatedMap;
        private bool mapTilesPainted;
        private int animationLeft,animationTop,animationRight,animationBottom;
        private byte paintedMapPhase;
        private NativeMapPixels LoadAnimatedWorldMap(int id)
        {
            var bytes=art.MapBytes(id);
            return bytes==null?null:NativeMapDecoder.RenderAnimatedLayers(bytes,g=>art.MapGraphic(g,id),MapRandomSeed());
        }
        private void BuildMapAnimation(NativeMapPixels pixels)
        {
            animatedMap=pixels;mapAnimationTiles.Clear();mapAnimationImages.Clear();mapTilesPainted=false;
            mapAnimationLayer=Group(mapRoot,"Native animated ground details",new Rect(0,0,0,0));
            if(pixels==null)return;int width=pixels.width/32;
            foreach(var tile in pixels.animations)mapAnimationTiles[tile.y*width+tile.x]=tile;
        }
        private void PaintMapAnimation(float cameraX,float cameraY)
        {
            if(animatedMap==null)return;
            if(mapAnimationClock.Advance(view.clock))animatedMap.animationPhase=unchecked((byte)(animatedMap.animationPhase+1));
            int width=animatedMap.width/32,height=animatedMap.height/32,index=0;
            int left=Mathf.Max(0,Mathf.FloorToInt(cameraX/32)),top=Mathf.Max(0,Mathf.FloorToInt(cameraY/32));
            int right=Mathf.Min(width,Mathf.CeilToInt((cameraX+640)/32)),bottom=Mathf.Min(height,Mathf.CeilToInt((cameraY+480)/32));
            if(mapTilesPainted&&animationLeft==left&&animationTop==top&&animationRight==right&&animationBottom==bottom&&paintedMapPhase==animatedMap.animationPhase)return;
            mapTilesPainted=true;animationLeft=left;animationTop=top;animationRight=right;animationBottom=bottom;paintedMapPhase=animatedMap.animationPhase;
            for(int y=top;y<bottom;y++)for(int x=left;x<right;x++)
            {
                if(!mapAnimationTiles.TryGetValue(y*width+x,out var tile))continue;
                var frame=tile.Frame(animatedMap.animationPhase);var texture=CampaignTexture(frame.rgba,32,32);
                if(index==mapAnimationImages.Count)mapAnimationImages.Add(Picture(mapAnimationLayer,"Native animated tile",new Rect(0,0,32,32),texture));
                var image=mapAnimationImages[index++];if(image.texture!=texture)image.texture=texture;
                if(!image.gameObject.activeSelf)image.gameObject.SetActive(true);
                Place((RectTransform)image.transform,new Rect(x*32,y*32,32,32));
            }
            for(int i=index;i<mapAnimationImages.Count;i++)mapAnimationImages[i].gameObject.SetActive(false);
        }
    }
}
