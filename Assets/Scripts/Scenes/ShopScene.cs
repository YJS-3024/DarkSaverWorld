using System.Collections;
using GlobalEnum;
using UnityEngine;

public class ShopScene : ViliageScene
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
    
    public override SceneType SceneType() => GlobalEnum.SceneType.Scene_Shop;
}
