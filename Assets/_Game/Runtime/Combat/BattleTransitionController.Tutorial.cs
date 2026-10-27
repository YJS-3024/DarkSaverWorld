using DarkSaver.Core;
using DarkSaver.Network;
using UnityEngine.SceneManagement;
namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        public bool HasPendingTutorial=>character?.tutorial!=null && !character.tutorial.IsComplete;
        public bool TrySaveTutorial(int expectedStep,bool moved,bool skip,bool checkpoint,out string reason)
        {
            return TryTutorialInput(new TutorialProgressRequest(expectedStep,moved,skip,checkpoint),out reason);
        }
        public bool TryTutorialInput(TutorialProgressRequest request,out string reason)
        {
            reason="튜토리얼을 진행할 수 없습니다.";
            if(!isActiveAndEnabled || transitioning || character?.tutorial==null || saveStore==null
                || activeBattle!=null)return false;
            var server=EnsureSessionServer();server.BindLocalTutorialService(saveStore.FilePath,character);
            if(!server.TryRequest(request,out var response)
                || response is not TutorialProgressResponse progress)
            {reason=response?.Reason ?? reason;return false;}
            var state=progress.CaptureState();
            if(state?.character?.tutorial==null || state.character.name!=character.name || state.character.jobId!=character.jobId)return false;
            character.tutorial=state.character.tutorial.Clone();reason=progress.Reason;return true;
        }
    }
}
