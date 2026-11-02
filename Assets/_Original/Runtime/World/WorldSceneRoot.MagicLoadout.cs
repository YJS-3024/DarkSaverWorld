using System;
using System.Collections.Generic;
using DarkSaver.Original.Entry;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private readonly List<(Button hit,RawImage image,int list,int index)> magicCells=new List<(Button,RawImage,int,int)>();
        private int selectedMagicList=-1,selectedMagicIndex=-1;private string magicTerms;
        private Text magicDetail;
        private void BuildMagicEditor()
        {
            magicCells.Clear();selectedMagicList=selectedMagicIndex=-1;magicTerms=null;
            for(int list=0;list<2;list++)for(int index=0;index<(list==0?16:8);index++)
            {
                int l=list,n=index;var hit=Hit(classFull,"Received magic cell"+list+"-"+index,new Rect(208+index%8*33,(list==0?47:121)+index/8*33,32,32),()=>{selectedMagicList=l;selectedMagicIndex=n;PaintClassWindow();});
                hit.GetComponent<Image>().color=Color.clear;var image=Picture((RectTransform)hit.transform,"Original magic icon",new Rect(0,0,32,32),art.battle.icons);magicCells.Add((hit,image,list,index));
            }
            magicDetail=ClassText("Selected magic detail",new Rect(177,160,345,37),"");magicDetail.fontSize=10;
        }
        private void PaintMagicEditor(bool pending)
        {
            var draft=view.magicLoadout;
            if(magicTerms!=draft?.terms){magicTerms=draft?.terms;selectedMagicList=selectedMagicIndex=-1;}
            if(selectedMagicList==1&&selectedMagicIndex>=(draft?.selected.Length??0))selectedMagicIndex=(draft?.selected.Length??0)-1;
            int chosen=-1;
            foreach(var cell in magicCells)
            {
                var ids=cell.list==0?draft?.learned:draft?.selected;bool exists=ids!=null&&cell.index<ids.Length;cell.hit.interactable=exists&&!pending;
                var entry=exists?definition.magicMenu?.Find(ids[cell.index]):null;
                cell.image.enabled=entry!=null&&entry.iconX>=0&&entry.iconX<640&&entry.iconY>=32&&entry.iconY<160&&entry.iconX%32==0&&entry.iconY%32==0;
                if(cell.image.enabled)cell.image.uvRect=NativeEntryLayout.Uv(art.battle.icons,new Rect(entry.iconX,entry.iconY+(int)(view.clock/300)%2*160,32,32));
                if(exists&&cell.list==selectedMagicList&&cell.index==selectedMagicIndex)chosen=ids[cell.index];
            }
            var selected=definition.magicMenu?.Find(chosen);
            magicDetail.text=selected==null?(chosen<0?"":chosen.ToString()):selected.name+"\nMP : "+selected.mp+"   거리 : "+selected.distance+"   범위 : "+selected.range;
        }
        private bool CanEditMagic(int id)=>view.magicLoadout!=null&&(id==79&&selectedMagicList==0&&view.magicLoadout.CanAdd(selectedMagicIndex)||id==77&&selectedMagicList==1&&view.magicLoadout.CanRemove(selectedMagicIndex));
        private void EditMagic(int id)
        {
            if(!CanEditMagic(id))return;
            Handle(id==79?server.Request_AddBattleMagic(selectedMagicIndex,RequestId()):server.Request_RemoveBattleMagic(selectedMagicIndex,RequestId()));PaintClassWindow();
        }
    }
}
