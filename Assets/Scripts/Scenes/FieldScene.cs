using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldScene : MonoBehaviour
{
    // Start is called before the first frame update
    private IEnumerator Start()
    {
        if (GameSystem.I is null)
        {
            yield return new WaitUntil(()=>GameSystem.I.Initialize());
        }

        yield return new WaitUntil(()=>TilemapManager.I.Initialize());
        yield return new WaitUntil(()=>CameraManager.I.Initialize());

        PlayerManager.I.CreatePlayer(true);
    }
}
