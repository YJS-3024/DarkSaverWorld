using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoSingleton<EnemyManager>
{
    protected override void Destroy()
    {
        throw new System.NotImplementedException();
    }

    public override bool Initialize()
    {
        return true;
    }
}
