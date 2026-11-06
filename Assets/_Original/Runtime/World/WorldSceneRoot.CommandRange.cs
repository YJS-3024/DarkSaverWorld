using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DarkSaver.Original.Entry;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform commandRangeLayer;
        private readonly List<RawImage> commandRangeLines=new List<RawImage>();
        private readonly List<RaycastResult> commandRangeHits=new List<RaycastResult>();
        private void BuildCommandRange()
        {
            commandRangeLines.Clear();
            commandRangeLayer=Group(mapRoot,"Native hovered command range outline",new Rect(0,0,0,0));
        }
        private void PaintCommandRange()
        {
            foreach(var line in commandRangeLines)line.gameObject.SetActive(false);
            if(!BattleMap()||view.battle==null||view.facility!=null||notice!=null||CampaignCutscene||EventSystem.current==null)return;
            commandRangeHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Input.mousePosition},commandRangeHits);
            if(commandRangeHits.Count==0||commandRangeHits[0].gameObject==null||!commandRangeHits[0].gameObject.transform.IsChildOf(mapRoot))return;
            var hit=commandRangeHits[0].gameObject.transform;int hovered=-1;
            if(hit==playerHit||hit.IsChildOf(playerHit))hovered=0;
            else foreach(var pair in trainingImages)if(hit==pair.Value.hit||hit.IsChildOf(pair.Value.hit)){hovered=pair.Key;break;}
            var leader=OriginalCommandRange.DisplayCommander(view.battle.units,hovered);if(leader==null)return;
            var map=DisplayMap(mapId).map;int index=0;
            // Restrict work to the native640x480 viewport; RectMask2D clips partial edges.
            int left=Mathf.Max(0,Mathf.FloorToInt(-mapRoot.anchoredPosition.x/32));
            int top=Mathf.Max(0,Mathf.FloorToInt(mapRoot.anchoredPosition.y/32));
            int right=Mathf.Min(map.width-1,left+20),bottom=Mathf.Min(map.height-1,top+15);
            for(int ty=top;ty<=bottom;ty++)for(int tx=left;tx<=right;tx++)
            {
                var edges=OriginalCommandRange.Edges(leader.x,leader.y,leader.commandRange.Value,tx,ty,map.width,map.height);
                if((edges&CommandRangeEdges.Top)!=0)PaintCommandRangeLine(ref index,new Rect(tx*32+1,ty*32-1,32,3),true);
                if((edges&CommandRangeEdges.Bottom)!=0)PaintCommandRangeLine(ref index,new Rect(tx*32+1,ty*32+31,32,3),true);
                if((edges&CommandRangeEdges.Left)!=0)PaintCommandRangeLine(ref index,new Rect(tx*32-1,ty*32+1,3,32),false);
                if((edges&CommandRangeEdges.Right)!=0)PaintCommandRangeLine(ref index,new Rect(tx*32+31,ty*32+1,3,32),false);
            }
        }
        private void PaintCommandRangeLine(ref int index,Rect rect,bool horizontal)
        {
            if(index==commandRangeLines.Count)commandRangeLines.Add(Picture(commandRangeLayer,"Native command boundary",rect,art.battle.board));
            var image=commandRangeLines[index++];image.gameObject.SetActive(true);Place((RectTransform)image.transform,rect);
            image.uvRect=NativeEntryLayout.Uv(art.battle.board,horizontal?new Rect(303,300,32,3):new Rect(300,303,3,32));
        }
    }
}
