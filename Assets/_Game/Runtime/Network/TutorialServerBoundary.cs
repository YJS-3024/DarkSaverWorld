using DarkSaver.Core;
using DarkSaver.Events;
namespace DarkSaver.Network
{
    // A selected-character-scoped tutorial service. Requests carry no arbitrary file path.
    public sealed class TutorialServerBoundary
    {
        private readonly NativeSaveStore store;
        private readonly string name;
        private readonly int job;
        private readonly OriginalTutorialScript script;
        public string SavePath=>store.FilePath;
        public bool Matches(NativeCharacterProfile profile)=>profile!=null && profile.name==name && profile.jobId==job;
        public TutorialServerBoundary(string savePath,NativeCharacterProfile profile,string source)
        {
            if(profile==null || !profile.IsValid)throw new System.ArgumentException("Selected character required.");
            store=new NativeSaveStore(savePath);name=profile.name;job=profile.jobId;script=new OriginalTutorialScript(source,job);
        }
        public LocalCommandResponse Handle(TutorialProgressRequest request)
        {
            if(request==null || !store.TryLoad(out var state,out _) || !state.hasCharacter || !Matches(state.character)
                || state.character.tutorial==null || !state.character.tutorial.IsValid)
                return new TutorialProgressResponse(false,reason:"선택한 캐릭터의 튜토리얼을 확인할 수 없습니다.");
            var progress=state.character.tutorial;
            if(request.Skip || request.Step!=progress.step)
                return new TutorialProgressResponse(false,reason:"현재 안내 순서를 확인해 주세요.");
            if(progress.IsComplete)return new TutorialProgressResponse(false,reason:"이미 완료한 튜토리얼입니다.");
            if(progress.version==1)state.character.tutorial=progress=new NativeTutorialProgress();
            try
            {
                if(request.Checkpoint)script.Prepare(progress);
                else if(request.Kind=="MovementCompleted")
                {
                    var actor=OriginalTutorialScript.Actor(progress,request.Actor);
                    if(!actor.moving || actor.targetX!=request.X || actor.targetY!=request.Y)
                        return new TutorialProgressResponse(false,reason:"현재 이동 목표와 다릅니다.");
                    OriginalTutorialScript.CompleteMovement(progress,actor.id,request.Key);script.Prepare(progress);
                }
                else if(!script.Input(progress,request.Kind,request.Actor,request.Target,request.X,request.Y,request.Key,request.Stage))
                    return new TutorialProgressResponse(false,reason:"대본에서 기다리는 동작을 수행해 주세요.");
            }
            catch(System.Exception ex) when(ex is System.InvalidOperationException || ex is System.ArgumentException || ex is System.FormatException)
            {return new TutorialProgressResponse(false,reason:"튜토리얼 명령을 실행할 수 없습니다: "+ex.Message);}
            if(!store.TrySave(state,out var reason))return new TutorialProgressResponse(false,reason:reason);
            return new TutorialProgressResponse(true,state);
        }
    }
    public sealed class TutorialProgressResponse:LocalCommandResponse
    {
        private readonly NativeSaveData state;
        public NativeSaveData CaptureState()=>NativeSaveStore.CopyForTransfer(state);
        public TutorialProgressResponse(bool accepted,NativeSaveData state=null,string reason=null):base(accepted,reason)
        {this.state=NativeSaveStore.CopyForTransfer(state);}
    }
}
