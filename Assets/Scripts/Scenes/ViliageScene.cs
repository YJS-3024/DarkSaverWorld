using System.Collections;
using GlobalEnum;
using UnityEngine;

public partial class ViliageScene : BaseScene
{
    // Start is called before the first frame update
    private IEnumerator Start()
    {
        if (GameSystem.I is null)
        {
            yield return new WaitUntil(()=>GameSystem.Instance.Initialize());
        }

        yield return new WaitUntil(()=>CameraManager.I.Initialize());
        
        PlayerManager.I.CreatePlayer(true);
    }

    public override SceneType SceneType() => GlobalEnum.SceneType.Scene_Village;
}

public partial class ViliageScene
{
    public override void PressEvent(Vector2 eventDataPosition)
    {
        base.PressEvent(eventDataPosition);
        
        var mainPlayer = PlayerManager.I.MainPlayer;
        if (mainPlayer.IsMoving)
            return;
        
        var uiPos = eventDataPosition;
        var worldPos = Camera.main.ScreenToWorldPoint(uiPos);
        var posList = SceneController.I.CurScene.Path.FindPath(mainPlayer.transform.position, worldPos, false);
        if (posList != null)
        {
            PlayerManager.I.MainPlayer.Move(posList);
        }
    }

}