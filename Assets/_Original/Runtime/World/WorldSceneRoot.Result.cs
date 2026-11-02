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
        private bool WaitingScenarioResult=>view?.record.scenarioVisit!=null&&view.record.campaignPhase=="complete";
        private void PaintScenarioResult()
        {
            var result=WaitingScenarioResult?view.record.scenarioVisit.result:null;
            if(result==null||scenarioResultId!=result.id)
            {
                if(scenarioResult!=null){scenarioResult.gameObject.SetActive(false);Destroy(scenarioResult.gameObject);}
                scenarioResult=null;scenarioResultId=null;
            }
            if(result==null)return;
            if(scenarioResult==null)
            {
                var spec=art.ResultLayout;
                if(spec==null||art.resultWindow==null||art.resultButtons==null)return;
                scenarioResultId=result.id;
                scenarioResult=Group(overlay,"Original scenario result input blocker",new Rect(0,0,640,480));
                scenarioResult.gameObject.AddComponent<Image>().color=Color.clear;
                var window=Group(scenarioResult,"Glib gscene8 result report",new Rect(110,10,spec.width,spec.height));
                Picture(window,"Native result background",new Rect(0,0,spec.width,spec.height),art.resultWindow);
                ResultRows(window,new Rect(13,56,408,108),result);
                Label(window,"Received kill count",new Rect(80,201,326,32),result.kills.HasValue?"죽인 몬스터 : 총 "+result.kills+"명 격퇴":"",TextAnchor.MiddleLeft);
                Label(window,"Received outcome",new Rect(78,274,49,18),result.victory?"승리":result.IsRetreat?"도망":"패배",TextAnchor.MiddleCenter);
                Label(window,"Received spoils",new Rect(204,274,70,18),result.spoils?.ToString()??"",TextAnchor.MiddleRight);
                Label(window,"Received events",new Rect(349,274,70,18),result.eventTotal.HasValue?result.eventCompleted+"/"+result.eventTotal:"",TextAnchor.MiddleRight);
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
            if(notice==null)scenarioResult.SetAsLastSibling();
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
