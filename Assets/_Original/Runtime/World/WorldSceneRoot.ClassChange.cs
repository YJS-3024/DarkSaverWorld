using System;
using System.Collections.Generic;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform classPanel,classFull,classMini;
        private int classMap,classSlot=-1,classIndex;
        private bool classMinimized;
        private string classOptionsId,classCommanderIdentity;
        private Text className,classJob,classLevel,classExperience,classDetail;
        private RawImage classCurrent,classFace;
        private readonly List<(Button hit,RawImage image)> classCandidates=new List<(Button,RawImage)>();
        private readonly List<(Button hit,int id,int stat)> classControls=new List<(Button,int,int)>();
        private string InspectedIdentity=>InspectedCommander?.id??view?.characterId;
        private bool ClassTargetCurrent=>view!=null&&classCommanderIdentity==InspectedIdentity
            &&(view.adjustmentTarget==null?view.inspectedCommander==0:view.adjustmentTarget.index==view.inspectedCommander
                &&view.adjustmentTarget.identity==InspectedIdentity&&view.adjustmentTarget.rosterRevision==view.commanders?.revision);
        private void OpenClassWindow()
        {
            if(!PersonalAvailable||view.moving||BattleMap())return;
            if(classPanel!=null&&classCommanderIdentity==InspectedIdentity)
            {classMinimized=false;classPanel.SetAsLastSibling();QueryAdjustmentData();PaintClassWindow();return;}
            if(classPanel!=null)CloseClassWindow();
            var spec=art.ClassLayout;
            if(spec==null||art.classWindow==null||art.classMinimized==null){ShowFailure(WorldFailure.RulesUnavailable);return;}
            var binding=server.Request_SelectAdjustmentCommander(view.inspectedCommander,InspectedIdentity,view.commanders?.revision??0);Handle(binding);if(!binding.Accepted)return;
            classMap=view.record.map;classCommanderIdentity=InspectedIdentity;classPanel=Group(worldWindows,"Original scene23",new Rect(50,20,spec.width,spec.height));
            classFull=Group(classPanel,"Full adjustment",new Rect(0,0,spec.width,spec.height));classFull.gameObject.AddComponent<Image>().color=Color.clear;
            BlackCell(classFull,new Rect(11,31,518,393));
            Picture(classFull,"Native scene23 frame",new Rect(0,0,spec.width,spec.height),art.classWindow);
            className=ClassText("Name",new Rect(50,128,140,16),InspectedName);
            classJob=ClassText("Job",new Rect(50,145,140,16),"");classLevel=ClassText("Received level",new Rect(50,163,140,16),"");
            classExperience=ClassText("Received experience",new Rect(50,181,140,16),"");classDetail=ClassText("Received candidate description",new Rect(30,349,171,39),"");
            classFace=Picture(classFull,"Commander face64",new Rect(20,47,64,64),InspectedFace);
            classCurrent=Picture(classFull,"Current class32",new Rect(99,228,32,32),CommanderIcon(InspectedJob),new Rect(32,32,32,32));
            var cells=new[]{new Vector2(32,228),new Vector2(167,228),new Vector2(100,296)};
            for(int slot=0;slot<3;slot++)
            {
                int selected=slot;var hit=Hit(classFull,"Received class slot"+slot,new Rect(cells[slot].x,cells[slot].y,32,32),()=>{classSlot=selected;PaintClassWindow();});
                hit.GetComponent<Image>().color=Color.clear;var image=Picture((RectTransform)hit.transform,"Class icon",new Rect(0,0,32,32),null);classCandidates.Add((hit,image));
            }
            BuildStatEditor();BuildMagicEditor();
            foreach(var b in spec.buttons)
            {
                int stat=(b.id==78||b.id==80)?(b.y-224)/22:-1;
                int id=b.id;var hit=Hit(classFull,"Native class control"+id,new Rect(b.x,b.y,b.width,b.height),()=>ClassControl(id,stat));
                var image=Picture((RectTransform)hit.transform,"Native button states",new Rect(0,0,b.width,b.height),art.buttons);
                hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,art.buttons,new Rect(b.sx,b.sy,b.width,b.height),new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));
                classControls.Add((hit,id,stat));
            }
            var drag=Hit(classFull,"Drag class header",new Rect(200,0,160,25),()=>{});Vector2? offset=null;
            drag.gameObject.AddComponent<NativeTutorialDrag>().Configure(screen,(point,end)=>
            {if(!offset.HasValue)offset=point-new Vector2(classPanel.anchoredPosition.x,-classPanel.anchoredPosition.y);var at=point-offset.Value;Place(classPanel,new Rect(Mathf.Clamp(at.x,0,640-spec.width),Mathf.Clamp(at.y,0,480-spec.height),spec.width,spec.height));if(end)offset=null;});
            classMini=Group(classPanel,"Native minimized",new Rect(0,0,31,32));var restore=Hit(classMini,"Restore class window",new Rect(0,0,31,32),()=>{classMinimized=false;PaintClassWindow();});
            Picture((RectTransform)restore.transform,"Native scene23m",new Rect(0,0,31,32),art.classMinimized);
            QueryAdjustmentData();PaintClassWindow();
        }
        private Text ClassText(string name,Rect rect,string text)
        {var bounds=Group(classFull,name+" bounds",rect);bounds.gameObject.AddComponent<RectMask2D>();var label=Label(bounds,name,new Rect(0,0,rect.width,rect.height),text);label.color=Color.black;label.alignment=TextAnchor.UpperLeft;return label;}
        private void ForgetClassWindow()
        {
            if(classPanel!=null){classPanel.gameObject.SetActive(false);Destroy(classPanel.gameObject);}
            classPanel=classFull=classMini=null;classCandidates.Clear();classControls.Clear();classOptionsId=classCommanderIdentity=null;classSlot=-1;classIndex=0;classMinimized=false;magicCells.Clear();
        }
        private void CloseClassWindow()
        {
            ForgetClassWindow();
            if(server!=null){Handle(server.Request_CloseClassOptions());Handle(server.Request_CancelStatAllocation());Handle(server.Request_CancelMagicLoadout());}
        }
        private void ClassControl(int id,int stat)
        {
            if(!ClassTargetCurrent){CloseClassWindow();return;}
            if(id==37){classMinimized=true;PaintClassWindow();return;}
            if(id==38||id==54){CloseClassWindow();return;}
            if(id==19)
            {
                if(view.MagicLoadoutNeedsConfirm||view.statAdjustment?.Dirty==true)
                {var reply=view.MagicLoadoutNeedsConfirm?server.Request_ConfirmMagicLoadout(RequestId()):server.Request_ConfirmStatAllocation(RequestId());Handle(reply);if(!reply.Accepted){PaintClassWindow();return;}}
                CloseClassWindow();return;
            }
            if(id==77||id==79){EditMagic(id);return;}
            if(id==78||id==80){Handle(server.Request_AdjustStat(stat,id==80?1:2,RequestId()));PaintClassWindow();return;}
            if(id==56||id==57){classIndex+=id==56?-1:1;PaintClassWindow();return;}
            if(id==81&&view.classOptions!=null)
            {var options=view.classOptions.id;var reply=server.Request_ChangeClass(classSlot,classSlot==2?classIndex:0,options,RequestId());Handle(reply);if(reply.Accepted)QueryAdjustmentData();PaintClassWindow();}
        }
        private void PaintClassWindow()
        {
            if(classPanel==null)return;
            if(!ClassTargetCurrent||view.moving||view.facility!=null||view.pendingPortal!=null||view.record.map!=classMap||BattleMap()||view.presentation?.completed==false){CloseClassWindow();return;}
            classFull.gameObject.SetActive(!classMinimized);classMini.gameObject.SetActive(classMinimized);
            var options=view.classOptions;bool pending=view.classOptionsRequest!=null||view.classChangeRequest!=null||view.statAdjustmentRequest!=null||view.magicLoadoutRequest!=null;
            if(classOptionsId!=options?.id)
            {
                classOptionsId=options?.id;classIndex=0;
                classSlot=options?.right!=null?1:options?.options?.Length>0?2:options?.left!=null?0:-1;
            }
            className.text=InspectedName;classFace.texture=InspectedFace;classFace.enabled=classFace.texture!=null;
            var profile=view.adjustmentTarget==null?CommanderProfile.Capture(view.record):view.adjustmentProfile;
            classJob.text=ClassName(InspectedJob);classLevel.text=profile?.level?.ToString()??"—";classExperience.text=profile?.growth?.experience.ToString()??"—";
            classCurrent.texture=CommanderIcon(InspectedJob);classCurrent.enabled=classCurrent.texture!=null;if(classCurrent.texture!=null)classCurrent.uvRect=NativeEntryLayout.Uv(classCurrent.texture,new Rect(32,32,32,32));
            for(int i=0;i<3;i++)
            {
                var candidate=options?.Candidate(i,i==2?classIndex:0);var cell=classCandidates[i];cell.hit.interactable=!pending&&candidate!=null;
                cell.image.texture=candidate==null?null:art.Picture(candidate.job)??campaign?.Picture(candidate.job);cell.image.enabled=cell.image.texture!=null;
                if(cell.image.texture!=null)cell.image.uvRect=NativeEntryLayout.Uv(cell.image.texture,new Rect(32,32,32,32));
            }
            var selected=options?.Candidate(classSlot,classSlot==2?classIndex:0);
            classDetail.text=selected==null?"":(definition.growthCatalog?.Job(selected.job)?.name??selected.job.ToString())+"\n"+(selected.description??"").Replace('_',' ');
            PaintMagicEditor(pending);
            foreach(var c in classControls)c.hit.interactable=c.id==37||c.id==38||c.id==54||c.id==19&&!pending
                ||!pending&&((c.id==77||c.id==79)&&CanEditMagic(c.id)||c.id==56&&classIndex>0||c.id==57&&classIndex+1<(options?.options?.Length??0)||c.id==81&&selected!=null&&options.classPoints>0&&view.statAdjustment?.Dirty!=true&&view.magicLoadout?.Dirty!=true
                    ||(c.id==78||c.id==80)&&view.statAdjustment?.TryStep(c.stat,c.id==80?1:2,out _,out _)==true);
            PaintStatEditor();
        }
    }
}
