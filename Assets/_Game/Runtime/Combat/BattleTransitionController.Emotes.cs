using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.World;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        private void RegisterEmoteHandler()
        {
            sessionBindings.Add(sessionProtocol.RegisterRequestHandler<LocalEmoteRequest>(p=>
            {
                var player=FindAnyObjectByType<TopDownPlayerController>();
                if(!isActiveAndEnabled || !HasCreatedCharacter || transitioning || SceneManager.GetSceneByName(BattleSceneName).isLoaded
                    || DarkSaver.Events.Scenario01Controller.BlocksWorldActions || DarkSaver.UI.FirstPlayTutorial.BlocksWorldActions
                    || player==null || !player.isActiveAndEnabled || player.UiInputBlocked || FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling==true
                    || !WorldEmoteVisual.Has(p.Id))return new LocalCommandResponse(false,"지금은 감정표현을 사용할 수 없습니다.");
                return new LocalEmoteResponse(p.Id);
            }));
        }
        public bool TryEmote(int id,out string reason)
        {
            if(!EnsureSessionServer().TryRequest(new LocalEmoteRequest(id),out var response) || response is not LocalEmoteResponse accepted)
            {reason=response?.Reason ?? "감정표현 응답이 없습니다.";return false;}
            var player=FindAnyObjectByType<TopDownPlayerController>();if(player==null){reason="캐릭터가 없습니다.";return false;}
            var view=player.GetComponent<WorldEmoteVisual>() ?? player.gameObject.AddComponent<WorldEmoteVisual>();
            bool result=view.Play(accepted.Id);reason=result?"":"감정표현 그림이 없습니다.";return result;
        }
    }
}
