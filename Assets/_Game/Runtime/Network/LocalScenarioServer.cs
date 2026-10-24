using System;
using DarkSaver.Core;
using DarkSaver.Events;

namespace DarkSaver.Network
{
    public enum ScenarioOperation { Initialize, Step, Dialogue, Clock, Probe, BeginEncounter, CancelEncounter }
    public sealed class ScenarioCommandRequest : ClientPacket
    {
        public string Key { get; } public int Revision { get; } public ScenarioOperation Operation { get; }
        public int X { get; } public int Y { get; } public double Seconds { get; }
        public ScenarioCommandRequest(string key,int revision,ScenarioOperation operation,int x=0,int y=0,double seconds=0)
        { Key=key;Revision=revision;Operation=operation;X=x;Y=y;Seconds=seconds; }
    }
    public sealed class ScenarioStateNotification : LocalCommandResponse
    {
        private readonly Scenario01Progress state;
        public string Key { get; } public int Revision { get; } public bool Changed { get; }
        public Scenario01Rules.StepResult StepResult { get; }
        public Scenario01Progress CaptureState()=>state?.Clone();
        public ScenarioStateNotification(string key,int revision,bool accepted,string reason,Scenario01Progress progress,
            bool changed=false,Scenario01Rules.StepResult result=Scenario01Rules.StepResult.Waiting):base(accepted,reason)
        { Key=key;Revision=revision;Changed=changed;StepResult=result;state=progress?.Clone(); }
    }
    public delegate bool SaveScenarioCandidate(Scenario01Progress state,NativeInventory inventory,out string reason);

    // Authoritative script clock/RNG, economic candidates and cursor live here. The director
    // receives detached snapshots and only constructs actors/camera/dialogue presentation.
    public sealed class LocalScenarioServer
    {
        private readonly Scenario01Definition definition;
        private readonly Func<Scenario01Progress> capture;
        private readonly Func<NativeInventory> inventory;
        private readonly Func<int> revision,job;
        private readonly SaveScenarioCandidate save;
        private readonly Func<bool> alreadyCleared;
        private Scenario01Progress working;
        private int knownRevision=-1;
        public LocalScenarioServer(Scenario01Definition definition,Func<Scenario01Progress> capture,Func<NativeInventory> inventory,
            Func<int> revision,Func<int> job,Func<bool> alreadyCleared,SaveScenarioCandidate save)
        { this.definition=definition;this.capture=capture;this.inventory=inventory;this.revision=revision;this.job=job;this.alreadyCleared=alreadyCleared;this.save=save; }
        public LocalCommandResponse Handle(ScenarioCommandRequest request)
        {
            int current=revision();
            if(knownRevision!=current){working=capture()?.Clone();knownRevision=current;}
            ScenarioStateNotification Reply(bool accepted,string reason=null,bool changed=false,Scenario01Rules.StepResult result=Scenario01Rules.StepResult.Waiting)
                =>new ScenarioStateNotification(definition.Key,knownRevision,accepted,reason,working,changed,result);
            if(request.Key!=definition.Key || request.Revision!=current)return Reply(false,"시나리오 요청 버전이 다릅니다.");
            var items=inventory();if(items==null || !items.IsValid)return Reply(false,"소지품 상태가 올바르지 않습니다.");
            Scenario01Progress next=null;bool changed=false;var step=Scenario01Rules.StepResult.Waiting;
            try
            {
                if(request.Operation==ScenarioOperation.Initialize)
                {
                    if(working!=null)return working.Matches(definition)?Reply(true):Reply(false,"시나리오 콘텐츠 버전이 다릅니다.");
                    next=Scenario01Progress.Create(definition,alreadyCleared());changed=true;
                }
                else
                {
                    if(working==null || !working.Matches(definition))return Reply(false,"시나리오 실행 상태가 없습니다.");
                    switch(request.Operation)
                    {
                        case ScenarioOperation.Step:
                            step=Scenario01Rules.Step(definition,working,items,job(),out next,out items);
                            changed=step!=Scenario01Rules.StepResult.Waiting;break;
                        case ScenarioOperation.Dialogue:
                            changed=Scenario01Rules.AdvanceDialogue(definition,working,out next);break;
                        case ScenarioOperation.Probe:
                            if(request.X<0 || request.X>=definition.width || request.Y<0 || request.Y>=definition.height)return Reply(false,"맵 밖의 이벤트 조회입니다.");
                            changed=Scenario01Rules.TryActivate(definition,working,items,request.X,request.Y,out next,out items);break;
                        case ScenarioOperation.Clock:
                            if(double.IsNaN(request.Seconds) || double.IsInfinity(request.Seconds) || request.Seconds<0 || request.Seconds>60)return Reply(false,"잘못된 시나리오 시간입니다.");
                            next=working.Clone();Scenario01Rules.AdvanceTime(next,request.Seconds,GameConstants.PlayerMoveSpeed);
                            working=next;return Reply(true,changed:true); // Volatile clock; next command persists it atomically.
                        case ScenarioOperation.BeginEncounter:
                            var unit=Array.Find(definition.units,u=>u.id==request.X && u.wave==working.wave);
                            if(working.phase!=Scenario01Progress.Playing || working.activeEvent!=0 || working.pendingBattle!=-1 || unit==null || working.defeated.Contains(unit.id))return Reply(false,"시작할 수 없는 조우입니다.");
                            next=working.Clone();next.pendingBattle=unit.id;changed=true;break;
                        case ScenarioOperation.CancelEncounter:
                            if(working.pendingBattle==-1)return Reply(true);
                            next=working.Clone();next.pendingBattle=-1;changed=true;break;
                        default:return Reply(false,"지원하지 않는 시나리오 요청입니다.");
                    }
                }
                if(!changed)return Reply(true,result:step);
                if(!save(next,items,out var reason))return Reply(false,reason);
                knownRevision=revision();working=next.Clone();return Reply(true,changed:true,result:step);
            }
            catch(Exception ex){return Reply(false,"시나리오 서버 처리 실패: "+ex.Message);}
        }
    }
}
