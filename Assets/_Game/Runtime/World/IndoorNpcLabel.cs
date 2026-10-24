using DarkSaver.Player;
using DarkSaver.UI;
using UnityEngine;
namespace DarkSaver.World
{
    // Authored single-player staff labels; not recovered original identities.
    public sealed class IndoorNpcLabel : MonoBehaviour
    {
        private string title;
        private InGameUi ui;
        public void Configure(string value)=>title=value;
        private void LateUpdate()
        {
            var player=FindAnyObjectByType<TopDownPlayerController>();
            if(player==null || player.UiInputBlocked || Camera.main==null || Vector2.Distance(player.transform.position,transform.position)>6)
            {ui?.Hide();return;}
            var screen=Camera.main.WorldToScreenPoint(transform.position+new Vector3(0,1.2f,0));
            if(screen.z<=0 || !DarkSaver.Rendering.GameScreen.TryScreenToGui(screen,out var point)){ui?.Hide();return;}
            InGameUi.Ensure(ref ui,this,19);ui.Begin();ui.Label("name",new Rect(point.x-90,point.y-16,180,24),title,14);ui.End();
        }
        private void OnDisable()=>ui?.Hide();
    }
}
