using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoSingleton<EnemyManager>
{
    protected override void Destroy()
    {
        
    }

    public override bool Initialize()
    {
        return true;
    }
}
