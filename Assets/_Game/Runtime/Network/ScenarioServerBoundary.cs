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
        public bool RetryableSaveFailure { get; }
        public Scenario01Rules.StepResult StepResult { get; }
        public Scenario01Progress CaptureState()=>state?.Clone();
        public ScenarioStateNotification(string key,int revision,bool accepted,string reason,Scenario01Progress progress,
            bool changed=false,Scenario01Rules.StepResult result=Scenario01Rules.StepResult.Waiting,bool retryableSaveFailure=false):base(accepted,reason)
        { Key=key;Revision=revision;Changed=changed;StepResult=result;state=progress?.Clone();RetryableSaveFailure=retryableSaveFailure; }
    }
    public delegate bool SaveScenarioCandidate(Scenario01Progress state,NativeInventory inventory,out string reason);

    // Authoritative script clock/RNG, economic candidates and cursor live here. The director
    // receives detached snapshots and only constructs actors/camera/dialogue presentation.
    public sealed class ScenarioServerBoundary
    {
        public string LastFailureDiagnostic=>null;
        public ScenarioServerBoundary(Scenario01Definition definition,Func<Scenario01Progress> capture,Func<NativeInventory> inventory,
            Func<int> revision,Func<int> job,Func<bool> alreadyCleared,SaveScenarioCandidate save,Func<bool> canRetrySave=null){}
        public LocalCommandResponse Handle(ScenarioCommandRequest request)=>new LocalCommandResponse(false,OriginalRestorationPolicy.PendingServerRule);
    }
}
