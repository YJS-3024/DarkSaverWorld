using System.Collections;
using UnityEngine;

public class BattleScene : MonoBehaviour
{
    protected void Awake()
    {

    }

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        yield return new WaitUntil(()=>TilemapManager.I.Initialize());
        yield return new WaitUntil(()=>CameraManager.I.Initialize());
        
        PlayerManager.I.CreatePlayer(true);
    }
}
