using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoSingleton<EffectManager>
{
    protected override void Destroy()
    {
        
    }

    public override bool Initialize()
    {
        return true;
    }
}
