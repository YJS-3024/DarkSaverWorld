using System;
using System.Collections.Generic;
using DarkSaver.Original.Entry;
using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        // Original learning/owned lists. The old selected-spell adapter is no longer used by this UI.
        private readonly List<(Button hit,RawImage image,RawImage outline,int list,int index)> magicCells=new List<(Button,RawImage,RawImage,int,int)>();
        private int selectedMagicList=-1,selectedMagicIndex=-1;private string classMagicToken;private long classMagicRevision;
        private Text magicName,magicKind,magicDetail,magicPoints;
        private RawImage classMagicGhost,classMagicDenied;private Texture2D classMagicDeniedTexture;private string classMagicDragToken;private long classMagicDragRevision;private int classMagicDragList,classMagicDragIndex;
        private void BuildMagicEditor()
        {
            ClearClassMagicDrag();magicCells.Clear();selectedMagicList=selectedMagicIndex=-1;classMagicToken=null;
            for(int list=0;list<2;list++)for(int index=0;index<(list==0?16:8);index++)
            {
                int l=list,n=index;var hit=Hit(classFull,"Native magic cell"+list+"-"+index,new Rect(208+index%8*33,(list==0?47:121)+index/8*33,32,32),()=>{selectedMagicList=l;selectedMagicIndex=n;PaintClassWindow();});
                hit.GetComponent<Image>().color=Color.clear;var outline=NativeSelectionBorder((RectTransform)hit.transform);var image=Picture((RectTransform)hit.transform,"Original magic icon",new Rect(0,0,32,32),art.battle.icons);magicCells.Add((hit,image,outline,list,index));
                hit.gameObject.AddComponent<NativeClassMagicPointer>().Configure(classFull,()=>BeginClassMagicDrag(l,n,hit),ClassMagicDragCurrent,(p,end)=>MoveClassMagicDrag(p,end),ClearClassMagicDrag);
            }
            magicName=ClassText("Original selected magic name",new Rect(179,163,170,14),"");magicKind=ClassText("Original selected magic kind",new Rect(349,163,173,14),"");magicDetail=ClassText("Original selected magic detail",new Rect(179,180,343,14),"");
            magicPoints=ClassText("Original magic points",new Rect(500,92,40,16),"—");
        }
        private void PaintMagicEditor(bool pending)
        {
            var source=server.ClassClientView;var state=source?.magic;
            if(classMagicGhost!=null&&!ClassMagicDragCurrent())ClearClassMagicDrag();
            if(classMagicToken!=source?.token){classMagicToken=source?.token;selectedMagicList=selectedMagicIndex=-1;}classMagicRevision=source?.magicRevision??0;
            int count=selectedMagicList==0?(state?.available.Length??0):(state?.owned.Length??0);if(selectedMagicIndex>=count)selectedMagicIndex=count-1;
            int chosen=-1;
            foreach(var cell in magicCells)
            {
                var ids=cell.list==0?state?.available:state?.owned;bool exists=ids!=null&&cell.index<ids.Length;cell.hit.interactable=exists&&!pending;
                cell.outline.enabled=exists&&cell.list==selectedMagicList&&cell.index==selectedMagicIndex;
                var entry=exists?definition.magicMenu?.Find(ids[cell.index]):null;
                cell.image.enabled=entry!=null&&entry.iconX>=0&&entry.iconX<640&&entry.iconY>=32&&entry.iconY<160&&entry.iconX%32==0&&entry.iconY%32==0
                    &&!(classMagicGhost!=null&&cell.list==classMagicDragList&&cell.index==classMagicDragIndex);
                if(cell.image.enabled){var icon=NativeClassPresentation.MagicIconOrigin(entry);cell.image.uvRect=NativeEntryLayout.Uv(art.battle.icons,new Rect(icon.X,icon.Y,32,32));}
                if(exists&&cell.list==selectedMagicList&&cell.index==selectedMagicIndex)chosen=ids[cell.index];
            }
            var selected=definition.magicMenu?.Find(chosen);magicName.text=selected?.name??"";magicKind.text=NativeClassPresentation.MagicKind(selected);magicDetail.text=NativeClassPresentation.MagicDetails(selected);magicPoints.text=state?.points?.ToString(System.Globalization.CultureInfo.InvariantCulture)??"—";
        }
        private bool CanEditMagic(int id)
        {var source=server.ClassClientView;return (id==77||id==79)&&source?.token==classMagicToken&&source?.magicRevision==classMagicRevision&&source?.magic?.CanButton(selectedMagicList,selectedMagicIndex)==true;}
        private void EditMagic(int id)
        {if(!CanEditMagic(id))return;Handle(server.Request_ClassMagicButton(id==77?1:2,selectedMagicList,selectedMagicIndex,classMagicToken,classMagicRevision,RequestId()));PaintClassWindow();}
        private bool BeginClassMagicDrag(int list,int index,Button hit)
        {
            if(!hit.IsInteractable()||classPanel==null||classMinimized||!ClassTargetCurrent||notice!=null)return false;
            var source=server.ClassClientView;var state=source?.magic;if(state==null||source.token!=classMagicToken||source.magicRevision!=classMagicRevision||state.Spell(list,index)<0)return false;
            var entry=definition.magicMenu?.Find(state.Spell(list,index));if(entry==null)return false;
            ClearClassMagicDrag();classMagicDragToken=source.token;classMagicDragRevision=source.magicRevision;classMagicDragList=list;classMagicDragIndex=index;selectedMagicList=list;selectedMagicIndex=index;
            classMagicGhost=Picture(classFull,"Native cursor-centered magic",new Rect(0,0,32,32),art.battle.icons,new Rect(entry.iconX,entry.iconY,32,32));
            if(classMagicDeniedTexture==null)classMagicDeniedTexture=Resources.Load<Texture2D>("OriginalClassDropDenied");
            if(classMagicDeniedTexture!=null)classMagicDenied=Picture(classFull,"Native bitmap154 forbidden drop",new Rect(0,0,32,32),classMagicDeniedTexture);
            PaintClassWindow();return true;
        }
        private bool ClassMagicDragCurrent()
        {var source=server.ClassClientView;return classMagicGhost!=null&&classPanel!=null&&!classMinimized&&classFull.gameObject.activeInHierarchy&&ClassTargetCurrent&&!view.moving&&notice==null&&source?.token==classMagicDragToken&&source.magicRevision==classMagicDragRevision;}
        private void MoveClassMagicDrag(Vector2 point,bool end)
        {
            if(!ClassMagicDragCurrent()){ClearClassMagicDrag();return;}
            if(!end)
            {
                var bounds=new Rect(Mathf.FloorToInt(point.x)-16,Mathf.FloorToInt(point.y)-16,32,32);Place((RectTransform)classMagicGhost.transform,bounds);classMagicGhost.transform.SetAsLastSibling();
                if(classMagicDenied!=null){bool allowed=NativeClassMagicDrop.Allowed(server.ClassClientView?.magic,classMagicDragList,classMagicDragIndex,Mathf.FloorToInt(point.x),Mathf.FloorToInt(point.y));classMagicDenied.gameObject.SetActive(!allowed);Place((RectTransform)classMagicDenied.transform,bounds);classMagicDenied.transform.SetAsLastSibling();}
                return;
            }
            bool drop=NativeClassMagicDrop.Allowed(server.ClassClientView?.magic,classMagicDragList,classMagicDragIndex,Mathf.FloorToInt(point.x),Mathf.FloorToInt(point.y));int list=classMagicDragList,index=classMagicDragIndex;string token=classMagicDragToken;long revision=classMagicDragRevision;ClearClassMagicDrag();
            if(drop){var reply=server.Request_ClassMagicDrag(list,index,token,revision,RequestId());Handle(reply);if(reply.Accepted)selectedMagicList=selectedMagicIndex=-1;}PaintClassWindow();
        }
        private void ClearClassMagicDrag()
        {if(classMagicGhost!=null){classMagicGhost.gameObject.SetActive(false);Destroy(classMagicGhost.gameObject);}classMagicGhost=null;if(classMagicDenied!=null){classMagicDenied.gameObject.SetActive(false);Destroy(classMagicDenied.gameObject);}classMagicDenied=null;classMagicDragToken=null;}
    }
}
