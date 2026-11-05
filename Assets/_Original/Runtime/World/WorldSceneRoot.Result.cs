using DarkSaver.Original.Campaign;
using DarkSaver.Original.Entry;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform scenarioResult;
        private string scenarioResultId;
        private RectTransform scenarioKillLayer;
        private Text scenarioKillCaption;
        private readonly System.Collections.Generic.List<RawImage> scenarioKillImages=new System.Collections.Generic.List<RawImage>();
        private NativeResultKillAnimation scenarioKillAnimation;
        private long scenarioKillRevision=-1;
        private int scenarioKillFrame=-1;
        private void ResetResultKillPresentation()
        {scenarioKillLayer=null;scenarioKillCaption=null;scenarioKillImages.Clear();scenarioKillAnimation=null;scenarioKillRevision=-1;scenarioKillFrame=-1;}
        private bool WaitingScenarioResult=>view?.record.scenarioVisit!=null&&view.record.campaignPhase=="complete";
        private void PaintScenarioResult()
        {
            var result=WaitingScenarioResult?view.record.scenarioVisit.result:null;
            if(result==null||scenarioResultId!=result.id)
            {
                if(scenarioResult!=null){scenarioResult.gameObject.SetActive(false);Destroy(scenarioResult.gameObject);}
                scenarioResult=null;scenarioResultId=null;
                ResetResultKillPresentation();
            }
            if(result==null)return;
            if(scenarioResult==null)
            {
                var spec=art.ResultLayout;
                if(spec==null||art.resultWindow==null||art.resultButtons==null)return;
                ResetResultKillPresentation();
                scenarioResultId=result.id;
                scenarioResult=Group(overlay,"Original scenario result input blocker",new Rect(0,0,640,480));
                scenarioResult.gameObject.AddComponent<Image>().color=Color.clear;
                var window=Group(scenarioResult,"Glib gscene8 result report",new Rect(110,10,spec.width,spec.height));
                Picture(window,"Native result background",new Rect(0,0,spec.width,spec.height),art.resultWindow);
                if(result.native!=null)NativeResultRows(window,result.native);else ResultRows(window,new Rect(13,56,408,108),result);
                scenarioKillLayer=Group(window,"Original result kill-list pass",new Rect(0,0,434,264));
                scenarioKillCaption=Label(scenarioKillLayer,"Original result kill caption",new Rect(81,215,353,18),"");
                scenarioKillCaption.color=new Color32(16,16,16,255);
                if(result.native!=null)
                {
                    foreach(var text in OriginalResultPackets.Texts(result.native))ResultText(window,"Native result text",new Rect(text.x,text.y,434-text.x,18),text.text);
                }
                else
                {
                    Label(window,"Received outcome",new Rect(78,274,49,18),result.victory?"승리":result.IsRetreat?"도망":result.IsTerminated?"종료":"패배",TextAnchor.MiddleCenter);
                    Label(window,"Received spoils",new Rect(204,274,70,18),result.spoils?.ToString()??"",TextAnchor.MiddleRight);
                    Label(window,"Received events",new Rect(349,274,70,18),result.eventTotal.HasValue?result.eventCompleted+"/"+result.eventTotal:"",TextAnchor.MiddleRight);
                }
                foreach(var control in spec.buttons)
                {
                    string resultId=result.id;
                    var hit=Hit(window,"Confirm result and return",new Rect(control.x,control.y,control.width,control.height),()=>Handle(server.Request_ConfirmScenarioResult(resultId,System.Guid.NewGuid().ToString("N"))));
                    var image=Picture((RectTransform)hit.transform,"Native three-state confirm",new Rect(0,0,control.width,control.height),art.resultButtons);
                    hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,art.resultButtons,new Rect(control.sx,control.sy,control.width,control.height),
                        new Rect(control.sx+control.width,control.sy,control.width,control.height),new Rect(control.sx+2*control.width,control.sy,control.width,control.height));
                }
                showingActions=false;trainingMode=null;
            }
            PaintResultKills(result);
            if(notice==null)scenarioResult.SetAsLastSibling();
        }
        private void PaintResultKills(CampaignResultRecord report)
        {
            if(scenarioKillLayer==null||scenarioKillCaption==null)return;
            if(report.nativeKills==null&&(report.native==null||report.kills.HasValue))
            {
                Place((RectTransform)scenarioKillCaption.transform,new Rect(80,201,326,32));
                scenarioKillCaption.text=report.kills.HasValue?"죽인 몬스터 : 총 "+report.kills+"명 격퇴":"";return;
            }
            Place((RectTransform)scenarioKillCaption.transform,new Rect(81,215,353,18));
            if(scenarioKillAnimation==null)scenarioKillAnimation=new NativeResultKillAnimation();
            if(report.nativeKills!=null&&report.nativeKills.revision!=scenarioKillRevision)
            {scenarioKillAnimation.Receive(report.nativeKills);scenarioKillRevision=report.nativeKills.revision;}
            // A source draw advances one step. Avoid advancing twice when several
            // notifications paint during the same Unity frame; no original FPS claim.
            if(scenarioKillFrame==Time.frameCount)return;scenarioKillFrame=Time.frameCount;
            var frame=scenarioKillAnimation.Step(job=>definition.combatCatalog?.Job(job)?.picture);
            scenarioKillCaption.text=frame.text??"";int used=0;
            foreach(var icon in frame.icons)
            {
                var texture=icon.kind=="body"?campaign?.Picture(icon.id):art.battle?.effects;
                if(texture==null||icon.sourceX<0||icon.sourceY<0||icon.sourceX+icon.width>texture.width||icon.sourceY+icon.height>texture.height)continue;
                if(used==scenarioKillImages.Count)scenarioKillImages.Add(Picture(scenarioKillLayer,"Native result kill frame",new Rect(0,0,0,0),texture));
                var image=scenarioKillImages[used++];image.texture=texture;image.gameObject.SetActive(true);
                Place((RectTransform)image.transform,new Rect(icon.x,icon.y,icon.width,icon.height));
                image.uvRect=NativeEntryLayout.Uv(texture,new Rect(icon.sourceX,icon.sourceY,icon.width,icon.height));
            }
            for(int i=used;i<scenarioKillImages.Count;i++)scenarioKillImages[i].gameObject.SetActive(false);
        }
        private void ResultText(RectTransform parent,string name,Rect rect,string text)
        {var label=Label(parent,name,rect,text,TextAnchor.UpperLeft);label.color=new Color32(16,16,16,255);}
        private void NativeResultRows(RectTransform parent,NativeResultReport report)
        {
            EnsureBattleWindowArt();
            foreach(var icon in OriginalResultPackets.Icons(report,item=>definition.Item(item)?.picture))
            {
                var texture=icon.kind=="body"?campaign?.Picture(icon.id):icon.kind=="marker"?battleWindowArt?.markers:ItemIcon(definition.Item(icon.id)?.picture??-1);
                if(texture!=null)Picture(parent,"Native result "+icon.kind+" "+icon.id,new Rect(icon.x,icon.y,icon.width,icon.height),texture,
                    icon.kind=="item"?(Rect?)null:new Rect(icon.sourceX,icon.sourceY,icon.width,icon.height));
            }
        }
        private void ResultRows(RectTransform parent,Rect area,CampaignResultRecord report)
        {
            var units=report.allies;
            var viewport=Group(parent,"Allies original region",area);viewport.gameObject.AddComponent<Image>().color=Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=Group(viewport,"Received rows",new Rect(0,0,area.width,Mathf.Max(area.height,units.Length*34+26)));
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=34;
            if(units.Length==0)
                Label(content,"Received total experience",new Rect(190,26,215,32),report.experience.HasValue?"경험치 획득 : "+report.experience:"",TextAnchor.MiddleLeft);
            for(int i=0;i<units.Length;i++)
            {
                var unit=units[i];int y=i*34;var texture=campaign?.Picture(unit.job);
                if(texture!=null)Picture(content,"Original unit body "+unit.unit,new Rect(12,y+26,32,32),texture,new Rect(texture.width/3,texture.width/3,texture.width/3,texture.width/3));
                var experience=unit.experience??(unit.unit==0?report.experience:null);
                Label(content,"Received row experience",new Rect(190,y+26,215,32),experience.HasValue?"경험치 획득 : "+experience:"",TextAnchor.MiddleLeft);
            }
        }
    }
}
