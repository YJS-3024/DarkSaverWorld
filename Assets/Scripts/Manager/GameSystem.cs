using System.Collections;
using UnityEngine;

public class GameSystem : MonoSingleton<GameSystem>
{
    private IEnumerator Start()
    {
        var isLoad = Initialize();
        yield return new WaitUntil(() => isLoad);

        isLoad = ResourceManager.I.Initialize();
        ResourceManager.I.SetParent(transform);
        yield return new WaitUntil(() => isLoad);

        isLoad = TableManager.I.Initialize();
        TableManager.I.SetParent(transform);
        yield return new WaitUntil(() => isLoad);

        isLoad = PlayerManager.I.Initialize();
        PlayerManager.I.SetParent(transform);
        yield return new WaitUntil(() => isLoad);
        
        isLoad = InputManager.I.Initialize();
        InputManager.I.SetParent(transform);
        yield return new WaitUntil(() => isLoad);
     
        isLoad = TilemapManager.I.Initialize();
        TilemapManager.I.SetParent(transform);
        yield return new WaitUntil(() => isLoad);

        isLoad = EnemyManager.I.Initialize();
        EnemyManager.I.SetParent(transform);
        yield return new WaitUntil(() => isLoad);

        isLoad = CameraManager.I.Initialize();
        CameraManager.I.SetParent(transform);
        yield return new WaitUntil(() => isLoad);
        
        isLoad = EffectManager.I.Initialize();
        EffectManager.I.SetParent(transform);
        yield return new WaitUntil(() => isLoad);
        
        isLoad = UIManager.I.Initialize();
        UIManager.I.SetParent(transform);
        yield return new WaitUntil(() => isLoad);
    }

    public override bool Initialize()
    {
        return true;
    }

    protected override void Destroy()
    {
    }
}