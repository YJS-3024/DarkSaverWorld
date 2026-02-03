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
    /// <summary>
    /// 클릭 입력 상태
    /// </summary>
    /// <param name="screenPosition"></param>
    public override void ClickEvent(Vector2 screenPosition)
    {
        base.ClickEvent(screenPosition);
        
        MovePlayer(screenPosition);
    }

    /// <summary>
    /// 프레스 입력 상태
    /// </summary>
    /// <param name="eventDataPosition"></param>
    public override void PressEvent(Vector2 eventDataPosition)
    {
        base.PressEvent(eventDataPosition);

        MovePlayer(eventDataPosition);
    }

    /// <summary>
    /// 플레이어가 움직이는 기능
    /// </summary>
    /// <param name="eventDataPosition"></param>
    private void MovePlayer(Vector2 eventDataPosition)
    {
        var mainPlayer = PlayerManager.I.MainPlayer;
        if (mainPlayer.IsMoving)
            return;
        
        var uiPos = eventDataPosition;
        var worldPos = Camera.main.ScreenToWorldPoint(uiPos);
        
        ServerManager.I.Request_MovePlayer(mainPlayer.CharIdx, mainPlayer.transform.position, worldPos);
    }
}