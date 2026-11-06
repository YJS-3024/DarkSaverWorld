using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private NativeCampaignArt campaignArt;
        // Optional authoritative providers; no guessed roster HP/AP, clear criteria or rewards.
        public Func<WorldView,TrainingUnitsNotice> ScenarioInitialAuthority {get;set;}
        public Func<WorldView,CampaignClearNotice> ScenarioClearAuthority {get;set;}
        public Func<WorldView,CampaignResultNotice> ScenarioResultAuthority {get;set;}
        public Func<CampaignScriptRequest,WorldView,bool> ScenarioScriptAuthority {get;set;}
        public event Action<CampaignScriptRequest> Notify_ScenarioScriptRequest;
        public event Action<WorldView> Notify_ScenarioInitialRequested;
        private readonly HashSet<string> campaignDispatch=new HashSet<string>();
        private string campaignDispatchSession;
        private bool pumpingCampaign;
        public event Action<WorldReply> Notify_FieldEntered;
        public event Action<WorldReply> Notify_ScenarioEntered;
        public event Action<CampaignCommand> Notify_ScenarioEffect;
        public Func<CampaignRetreatRequest,WorldView,CampaignExitNotice> ScenarioRetreatAuthority { get; set; }
        public event Action<WorldReply> Notify_ScenarioExited;
        public void ConfigureCampaign(NativeCampaignArt value) { campaignArt = value; }
        public WorldReply Request_EnterMap(int map)
        {
            var reply = WorldRequest(s => s.EnterCampaignMap(map), true);
            if (reply.Accepted)
            { if (campaignArt?.Definition.Map(map)?.kind == "scenario") Notify_ScenarioEntered?.Invoke(reply); else Notify_FieldEntered?.Invoke(reply); }
            return reply;
        }
        public void PublishScenarioEffect(CampaignCommand command) { Notify_ScenarioEffect?.Invoke(command); }
        public WorldReply Request_ContinueScenario() => WorldRequest(s => s.ContinueCampaignPresentation());
        public WorldReply Request_ClearScenario()
        {
            var view=WorldSnapshot;
            if(ScenarioClearAuthority==null)return WorldRequest(s=>s.RequestCampaignClear());
            if(view?.record.campaignPhase!="explore"||view.presentation!=null&&!view.presentation.completed)return WorldRequest(s=>s.RequestCampaignClear());
            var notice=ScenarioClearAuthority(view);
            return notice==null?WorldRequest(s=>s.RequestCampaignClear()):Receive_ScenarioClear(notice);
        }
        public WorldReply Notify_ScenarioEvents(CampaignEventsNotice notice)=>WorldRequest(s=>s.ReceiveCampaignEvents(notice));
        public WorldReply Request_RetryScenarioPresentation()=>WorldRequest(s=>s.RetryCampaignPresentation());
        public void Request_RetryScenarioFlow(){campaignDispatch.Clear();PumpScenarioFlow();}
        public WorldReply Notify_ScenarioScriptHandled(CampaignScriptRequest request)=>WorldRequest(s=>s.AcknowledgeCampaignRequest(request));
        public WorldReply Request_ProcessScenarioRecruit(CampaignScriptRequest request)
        {
            string character=null,session=null,commanderSession=null;int map=0;long roster=0,delivery=0;
            var result=WorldRequest(s=>
            {
                var reply=s.ProcessLocalScenarioRecruit(request);
                if(reply.Accepted)
                {
                    var v=reply.View;character=v.characterId;session=v.campaignSession;commanderSession=v.commanderSession;map=v.record.map;
                    roster=v.record.roster?.revision??0;delivery=v.record.campaignDelivery?.revision??0;
                }
                return reply;
            });
            var current=WorldSnapshot;
            if(result.Accepted&&current!=null&&isActiveAndEnabled&&current.characterId==character&&current.campaignSession==session
                &&current.commanderSession==commanderSession&&current.record.map==map&&(current.record.roster?.revision??0)==roster
                &&(current.record.campaignDelivery?.revision??0)==delivery)Notify_Commanders?.Invoke(new WorldReply(WorldFailure.None,current));
            return result;
        }
        public WorldReply Request_CampaignUnitAction(int unit) => WorldRequest(s => s.CampaignUnitAction(unit));
        public WorldReply Request_JoinScenarioRoom(string room,string token,string request)
        {
            var reply=WorldRequest(s=>s.JoinScenarioRoom(room,token,request),true);
            if(reply.Accepted)Notify_ScenarioEntered?.Invoke(reply);return reply;
        }
        // Trusted local-server notification adapter, never called by map clicks.
        public WorldReply Receive_ScenarioClear(CampaignClearNotice notice) => WorldRequest(s => s.ReceiveCampaignClear(notice));
        public WorldReply Notify_ScenarioResult(CampaignResultNotice notice)=>WorldRequest(s=>s.ReceiveScenarioResult(notice));
        public WorldReply Request_ConfirmScenarioResult(string resultId,string request)
        {
            bool native=WorldSnapshot?.record.scenarioVisit?.nativeEnd!=null;
            NativeScenarioReturn? returned=null;
            var result=WorldRequest(s=>{var reply=s.ConfirmScenarioResult(resultId,request);if(reply.Accepted)returned=NativeScenarioReturn.Capture(reply.View);return reply;});
            if(returned.HasValue)PublishNativeScenarioReturn(returned.Value,native);
            return result;
        }
        private void AdvanceScenarioWorld(double ms)
        {
            var before=WorldSnapshot;service?.AdvanceWorld(ms);var current=WorldSnapshot;
            if(before?.record.scenarioVisit?.nativeEnd?.stage!=NativeScenarioEndRecord.Outro||current==null||current.record.scenarioVisit!=null
                ||before.characterId!=current.characterId||before.commanderSession!=current.commanderSession||before.record.map==current.record.map)return;
            var returned=NativeScenarioReturn.Capture(current);var result=new WorldReply(WorldFailure.None,current);Notify_World?.Invoke(result);
            if(returned.HasValue)PublishNativeScenarioReturn(returned.Value,true);
        }
        public WorldReply Request_EscapeScenario(string visit)=>ScenarioExitResult(s=>s.ExecuteScenarioRetreat(visit,ScenarioRetreatAuthority));
        public WorldReply Notify_ScenarioExit(CampaignExitNotice notice)=>ScenarioExitResult(s=>s.ReceiveScenarioExit(notice));
        private WorldReply ScenarioExitResult(Func<EntryAccountService,WorldReply> apply)
        {var result=WorldRequest(apply);if(result.Accepted&&result.View?.record.scenarioVisit==null)Notify_ScenarioExited?.Invoke(result);return result;}
        private bool CampaignContextCurrent(WorldView expected)
        {
            var current=WorldSnapshot;
            return isActiveAndEnabled&&current!=null&&current.characterId==expected.characterId
                &&current.campaignSession==expected.campaignSession&&current.record.map==expected.record.map;
        }
        private void PumpScenarioFlow()
        {
            if(pumpingCampaign||!isActiveAndEnabled||service==null)return;
            var view=WorldSnapshot;
            if(view==null)return;
            bool scenario=campaignArt?.Definition.Map(view.record.map)?.kind=="scenario";
            if(campaignDispatchSession!=view.campaignSession){campaignDispatchSession=view.campaignSession;campaignDispatch.Clear();}
            var dispatchContext=view;
            pumpingCampaign=true;
            try
            {
                bool idle=view.record.scenarioVisit?.nativeEnd?.Started!=true&&(view.presentation==null||view.presentation.completed)&&view.record.campaignProgress?.events.Any(e=>!e.completed)!=true;
                if(scenario&&idle&&view.record.campaignPhase=="explore"&&view.battle==null)
                {
                    if(ScenarioInitialAuthority!=null&&campaignDispatch.Add("initial"))
                    {var notice=ScenarioInitialAuthority(view);if(!CampaignContextCurrent(dispatchContext))return;if(notice!=null)Notify_BattleUnits(notice);}
                    if(!CampaignContextCurrent(dispatchContext))return;
                    if(campaignDispatch.Add("initial-notify"))Notify_ScenarioInitialRequested?.Invoke(view);
                }
                if(!CampaignContextCurrent(dispatchContext))return;
                foreach(var request in service.PendingCampaignRequests())
                {
                    bool localRecruit=ScenarioScriptAuthority==null&&request.packet.Length==3&&request.packet[0]==0x3c;
                    string dispatchKey="command:"+request.id+(localRecruit?":roster:"+(WorldSnapshot?.record.roster?.revision??0):"");
                    if(!campaignDispatch.Add(dispatchKey))continue;
                    Notify_ScenarioScriptRequest?.Invoke(request.Copy());
                    if(!CampaignContextCurrent(dispatchContext))return;
                    if(!service.PendingCampaignRequests().Any(r=>r.id==request.id))continue;
                    view=WorldSnapshot;
                    if(view.campaignSession!=request.session)break;
                    bool handled=false;
                    if(request.packet.SequenceEqual(new byte[]{0x5c,0x20,1})||request.packet.SequenceEqual(new byte[]{0x5c,0x20,2}))
                    {
                        if(request.visit==view.record.scenarioVisit?.id&&ScenarioClearAuthority!=null&&view.record.campaignPhase=="explore"&&(view.presentation==null||view.presentation.completed))
                        {var clear=ScenarioClearAuthority(view);if(!CampaignContextCurrent(dispatchContext))return;if(clear!=null)handled=Receive_ScenarioClear(clear).Accepted;}
                        else if(request.visit==view.record.scenarioVisit?.id&&(view.record.campaignPhase=="outro"||view.record.campaignPhase=="complete")||request.visit==view.record.lastScenarioResult?.visit)handled=true;
                        // A clear request emitted inside an event waits until that event has ended.
                        else if(view.presentation!=null&&!view.presentation.completed)campaignDispatch.Remove(dispatchKey);
                    }
                    else if(ScenarioScriptAuthority!=null)handled=ScenarioScriptAuthority(request.Copy(),view);
                    else if(request.packet.Length==3&&request.packet[0]==0x3c)Request_ProcessScenarioRecruit(request);
                    if(!CampaignContextCurrent(dispatchContext))return;
                    if(handled)Notify_ScenarioScriptHandled(request);
                    if(!CampaignContextCurrent(dispatchContext))return;
                }
                if(!CampaignContextCurrent(dispatchContext))return;
                view=WorldSnapshot;
                if(view!=null&&scenario&&view.record.campaignPhase=="complete"&&view.record.scenarioVisit?.result==null&&ScenarioResultAuthority!=null&&campaignDispatch.Add("result"))
                {var result=ScenarioResultAuthority(view);if(!CampaignContextCurrent(dispatchContext))return;if(result!=null)Notify_ScenarioResult(result);}
            }
            finally{if(!CampaignContextCurrent(dispatchContext))campaignDispatch.Clear();pumpingCampaign=false;}
        }
        private void FlushCampaignEffects()
        {
            if(service==null)return;
            var commands=service.DrainCampaignEffects();var context=WorldSnapshot;
            if(context==null||!isActiveAndEnabled)return;
            foreach(var command in commands)
            {
                if(!CampaignContextCurrent(context))break;
                PublishScenarioEffect(command);
            }
        }
    }
}
