using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSystem : MonoSingleton<GameSystem>
{
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => InitManger(UIManager.I));
        yield return new WaitUntil(() => InitManger(SceneController.I));
        yield return new WaitUntil(() => InitManger(ResourceManager.I));
        yield return new WaitUntil(() => InitManger(TableManager.I));
        yield return new WaitUntil(() => InitManger(PlayerManager.I));
        yield return new WaitUntil(() => InitManger(InputManager.I));
        yield return new WaitUntil(() => InitManger(TilemapManager.I));
        yield return new WaitUntil(() => InitManger(EnemyManager.I));
        yield return new WaitUntil(() => InitManger(CameraManager.I));
        yield return new WaitUntil(() => InitManger(EffectManager.I));
        yield return new WaitUntil(() => InitManger(UIManager.I));
        yield return new WaitUntil(() => InitManger(EventManager.I));
    }

    public override bool Initialize()
    {
        return true;
    }

    protected override void Destroy()
    {
    }

    private bool InitManger<T>(T instance) where T : MonoSingleton<T>
    {
        var isLoad = instance.Initialize();
        instance.SetParent(transform);
        return isLoad;
    }
}