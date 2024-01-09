using System.Collections;
using System.Collections.Generic;
using Scene;
using UnityEngine;

public class BattleSceneController : SceneController
{
    public override Scene.SceneType GetSceneType() => SceneType.Scene_Battle;

    protected override void Awake()
    {
        base.Awake();
    }

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        yield return new WaitUntil(()=>TilemapManager.I.Initialize());
        yield return new WaitUntil(()=>CameraManager.I.Initialize());
        
        PlayerManager.I.CreatePlayer(true);
    }
}
