using System;
using System.Collections.Generic;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform classPanel,classFull,classMini;
        private int classMap,classSlot=-1,classIndex;
        private bool classMinimized;
        private WaitingWindowDefinition classNativeSpec;
        private NativeWindowClickClock classClicks;
        private string classOptionsId,classCommanderIdentity;
        private Text className,classJob,classLevel,classExperience,classDetail;
        private RawImage classCurrent,classFace,classHeader;
        private readonly List<(Button hit,RawImage image,Image selected)> classCandidates=new List<(Button,RawImage,Image)>();
        private readonly List<(Button hit,int id,int stat)> classControls=new List<(Button,int,int)>();
        private string InspectedIdentity=>InspectedCommander?.id??view?.characterId;
        private bool ClassTargetCurrent=>view!=null&&classCommanderIdentity==InspectedIdentity
            &&(view.adjustmentTarget==null?view.inspectedCommander==0:view.adjustmentTarget.index==view.inspectedCommander
                &&view.adjustmentTarget.identity==InspectedIdentity&&view.adjustmentTarget.rosterRevision==view.commanders?.revision);
        private NativeClassLabelCatalog classLabels;
        private NativeClassLabel ClassSourceLabel(int job)
        {if(classLabels==null){var data=Resources.Load<TextAsset>("OriginalClassLabels");if(data!=null)classLabels=JsonUtility.FromJson<NativeClassLabelCatalog>(data.text);}return classLabels?.Find(job);}
        private ClassCandidate ClassDisplayCandidate(int slot)
        {
            if(view.classOptions!=null)return view.classOptions.Candidate(slot,slot==2?classIndex:0);
            var source=server.ClassClientView?.options;if(source==null)return null;
            int job=slot==0?source.left:slot==1?source.right:slot==2&&classIndex>=0&&classIndex<source.choices.Length?source.choices[classIndex]:-1;
            var label=job<0?null:ClassSourceLabel(job);return label==null?null:new ClassCandidate{job=job,description=label.description};
        }
        private void OpenClassWindow()
        {
            if(!PersonalAvailable||view.moving||BattleMap())return;
            if(classPanel!=null&&classCommanderIdentity==InspectedIdentity)
            {classMinimized=false;classPanel.SetAsLastSibling();server.Request_OpenClassClient();QueryAdjustmentData();PaintClassWindow();return;}
            if(classPanel!=null)CloseClassWindow();
            var spec=art.ClassLayout;
            if(spec==null||art.classWindow==null||art.classMinimized==null){ShowFailure(WorldFailure.RulesUnavailable);return;}
            var layout=NativeFacilitySpec(40,spec.scene,spec.width,spec.height);if(layout==null){ShowFailure(WorldFailure.RulesUnavailable);return;}
            var binding=server.Request_SelectAdjustmentCommander(view.inspectedCommander,InspectedIdentity,view.commanders?.revision??0);Handle(binding);if(!binding.Accepted)return;
            classNativeSpec=layout;classClicks=new NativeWindowClickClock();var saved=OriginalWindowPreferences.LoadState(layout);classMinimized=saved.Minimized;
            classMap=view.record.map;classCommanderIdentity=InspectedIdentity;classPanel=Group(worldWindows,"Original scene23",new Rect(saved.Position.X,saved.Position.Y,spec.width,spec.height));
            classFull=Group(classPanel,"Full adjustment",new Rect(0,0,spec.width,spec.height));classFull.gameObject.AddComponent<Image>().color=Color.clear;
            classFull.gameObject.AddComponent<NativeWindowClickInput>().Configure(classClicks);
            BlackCell(classFull,new Rect(11,31,518,393));
            Picture(classFull,"Native scene23 frame",new Rect(0,0,spec.width,spec.height),art.classWindow);
            className=ClassText("Name",new Rect(50,128,140,16),InspectedName);
            classJob=ClassText("Job",new Rect(50,145,140,16),"");classLevel=ClassText("Received level",new Rect(50,162,140,16),"");
            classExperience=ClassText("Received experience",new Rect(50,179,140,16),"");classDetail=ClassText("Received candidate description",new Rect(30,349,171,39),"");
            classFace=Picture(classFull,"Commander face64",new Rect(20,47,64,64),InspectedFace);
            classHeader=Picture(classFull,"Original current header body32",new Rect(127,79,32,32),CommanderIcon(InspectedJob),new Rect(32,32,32,32));
            classCurrent=Picture(classFull,"Current class32",new Rect(99,228,32,32),CommanderIcon(InspectedJob),new Rect(32,32,32,32));
            var cells=new[]{NativeClassPresentation.ChoiceOrigin(0),NativeClassPresentation.ChoiceOrigin(1),NativeClassPresentation.ChoiceOrigin(2)};
            for(int slot=0;slot<3;slot++)
            {
                int selectedSlot=slot;var hit=Hit(classFull,"Received class slot"+slot,new Rect(cells[slot].X,cells[slot].Y,32,32),()=>{classSlot=selectedSlot;PaintClassWindow();});
                hit.GetComponent<Image>().color=Color.clear;var selected=Group((RectTransform)hit.transform,"Original class choice fill",new Rect(0,0,NativeClassPresentation.ChoiceSize,NativeClassPresentation.ChoiceSize)).gameObject.AddComponent<Image>();selected.color=new Color32(50,50,200,255);selected.raycastTarget=false;selected.enabled=false;
                var image=Picture((RectTransform)hit.transform,"Class icon",new Rect(0,0,32,32),null);classCandidates.Add((hit,image,selected));
            }
            BuildStatEditor();BuildMagicEditor();
            foreach(var b in spec.buttons)
            {
                int stat=(b.id==78||b.id==80)?(b.y-224)/22:-1;
                int id=b.id;var hit=Hit(classFull,"Native class control"+id,new Rect(b.x,b.y,b.width,b.height),()=>ClassControl(id,stat));
                var image=Picture((RectTransform)hit.transform,"Native button states",new Rect(0,0,b.width,b.height),art.buttons);
                hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,art.buttons,new Rect(b.sx,b.sy,b.width,b.height),new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));
                classControls.Add((hit,id,stat));
                hit.gameObject.AddComponent<NativeWindowClickInput>().Configure(classClicks);
            }
            var region=new Rect(layout.dragX,layout.dragY,layout.dragWidth,layout.dragHeight);var drag=Hit(classFull,"Drag class header",region,()=>{});drag.transform.SetSiblingIndex(2);
            drag.gameObject.AddComponent<NativeWindowClickInput>().Configure(classClicks);drag.gameObject.AddComponent<NativeWindowDragInput>().Configure(screen,classPanel,region,()=>NativeWindowDragAllowed);
            classMini=Group(classPanel,"Native minimized",new Rect(0,0,layout.minWidth,layout.minHeight));var restore=Hit(classMini,"Restore class window",new Rect(0,0,layout.minWidth,layout.minHeight),()=>{});
            restore.gameObject.AddComponent<NativeWindowClickInput>().Configure(classClicks,()=>{classMinimized=false;classPanel.SetAsLastSibling();PaintClassWindow();});
            restore.gameObject.AddComponent<NativeWindowDragInput>().Configure(screen,classPanel,new Rect(layout.minDragX,layout.minDragY,layout.minDragWidth,layout.minDragHeight),()=>NativeWindowDragAllowed);
            Picture((RectTransform)restore.transform,"Native scene23m",new Rect(0,0,layout.minWidth,layout.minHeight),art.classMinimized);
            server.Request_OpenClassClient();QueryAdjustmentData();PaintClassWindow();
        }
        private Text ClassText(string name,Rect rect,string text)
        {var bounds=Group(classFull,name+" bounds",rect);bounds.gameObject.AddComponent<RectMask2D>();var label=Label(bounds,name,new Rect(0,0,rect.width,rect.height),text);label.color=Color.black;label.alignment=TextAnchor.UpperLeft;return label;}
        private void ForgetClassWindow()
        {
            ClearClassMagicDrag();server?.Request_CancelClassClient();SaveClassWindowPosition();
            if(classPanel!=null){classPanel.gameObject.SetActive(false);Destroy(classPanel.gameObject);}
            classPanel=classFull=classMini=null;classNativeSpec=null;classClicks=null;classCandidates.Clear();classControls.Clear();classOptionsId=classCommanderIdentity=null;classSlot=-1;classIndex=0;classMinimized=false;magicCells.Clear();
        }
        private void SaveClassWindowPosition()
        {if(classPanel!=null&&classNativeSpec!=null)OriginalWindowPreferences.Save(classNativeSpec,Mathf.RoundToInt(classPanel.anchoredPosition.x),Mathf.RoundToInt(-classPanel.anchoredPosition.y),minimized:classMinimized);}
        private void CloseClassWindow()
        {
            ForgetClassWindow();
            if(server!=null){Handle(server.Request_CloseClassOptions());Handle(server.Request_CancelStatAllocation());Handle(server.Request_CancelMagicLoadout());}
        }
        private void ClassControl(int id,int stat)
        {
            if(classPanel==null)return;
            if(!ClassTargetCurrent){CloseClassWindow();return;}
            if(id==37){classMinimized=true;PaintClassWindow();return;}
            if(id==54){server.Request_CloseClassClient(false);CloseClassWindow();return;}
            if(id==19||id==38)
            {
                bool dirty=view.statAdjustment?.Dirty==true;
                if(dirty)
                {var reply=server.Request_ConfirmStatAllocation(RequestId());Handle(reply);if(!reply.Accepted){PaintClassWindow();return;}}
                if(!dirty)server.Request_CloseClassClient(true);CloseClassWindow();return;
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
            if(classNativeSpec!=null)Place(classPanel,new Rect(classPanel.anchoredPosition.x,-classPanel.anchoredPosition.y,classMinimized?classNativeSpec.minWidth:classNativeSpec.width,classMinimized?classNativeSpec.minHeight:classNativeSpec.height));
            classFull.gameObject.SetActive(!classMinimized);classMini.gameObject.SetActive(classMinimized);
            var options=view.classOptions;bool pending=view.classOptionsRequest!=null||view.classChangeRequest!=null||view.statAdjustmentRequest!=null||view.magicLoadoutRequest!=null;
            var raw=server.ClassClientView;string optionsKey=options?.id??(raw?.options==null?null:raw.token+":"+raw.optionsRevision);
            if(classOptionsId!=optionsKey)
            {
                classOptionsId=optionsKey;classIndex=0;
                classSlot=options!=null?(options.right!=null?1:options.options?.Length>0?2:options.left!=null?0:-1):raw?.options?.DefaultSlot??-1;
            }
            className.text=InspectedName;classFace.texture=InspectedFace;classFace.enabled=classFace.texture!=null;
            var profile=view.adjustmentTarget==null?CommanderProfile.Capture(view.record):view.adjustmentProfile;
            classJob.text=ClassName(InspectedJob);classLevel.text=NativeClassPresentation.Level(profile);classExperience.text=NativeClassPresentation.Experience(profile);
            classHeader.texture=CommanderIcon(InspectedJob);classHeader.enabled=classHeader.texture!=null;if(classHeader.texture!=null)classHeader.uvRect=NativeEntryLayout.Uv(classHeader.texture,new Rect(32,32,32,32));
            classCurrent.texture=CommanderIcon(InspectedJob);classCurrent.enabled=classCurrent.texture!=null;if(classCurrent.texture!=null)classCurrent.uvRect=NativeEntryLayout.Uv(classCurrent.texture,new Rect(32,32,32,32));
            for(int i=0;i<3;i++)
            {
                var candidate=ClassDisplayCandidate(i);var cell=classCandidates[i];cell.hit.interactable=!pending&&candidate!=null;cell.selected.enabled=candidate!=null&&i==classSlot;
                cell.image.texture=candidate==null?null:art.Picture(candidate.job)??campaign?.Picture(candidate.job);cell.image.enabled=cell.image.texture!=null;
                if(cell.image.texture!=null)cell.image.uvRect=NativeEntryLayout.Uv(cell.image.texture,new Rect(32,32,32,32));
            }
            var selected=ClassDisplayCandidate(classSlot);
            classDetail.text=selected==null?"":(definition.growthCatalog?.Job(selected.job)?.name??selected.job.ToString())+"\n"+(ClassSourceLabel(selected.job)?.description??selected.description??"").Replace('_',' ');
            PaintMagicEditor(pending);
            foreach(var c in classControls)c.hit.interactable=c.id==37||c.id==38||c.id==54||c.id==19&&!pending
                ||!pending&&((c.id==77||c.id==79)&&CanEditMagic(c.id)||c.id==56&&classIndex>0||c.id==57&&classIndex+1<(options?.options?.Length??raw?.options?.choices.Length??0)||c.id==81&&selected!=null&&options?.classPoints>0&&view.statAdjustment?.Dirty!=true&&view.magicLoadout?.Dirty!=true
                    ||(c.id==78||c.id==80)&&view.statAdjustment?.TryStep(c.stat,c.id==80?1:2,out _,out _)==true);
            PaintStatEditor();
        }
    }
}
