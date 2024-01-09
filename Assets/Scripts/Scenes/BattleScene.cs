using System.Collections;
using System.Collections.Generic;
using Scene;
using UnityEngine;

public class BattleScene : SceneController
{
    public override Scene.SceneType GetSceneType() => SceneType.Scene_Battle;

    protected override void Awake()
    {
        base.Awake();
    }

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        yield return null;

        PlayerManager.I.CreatePlayer(true);
    }
}
