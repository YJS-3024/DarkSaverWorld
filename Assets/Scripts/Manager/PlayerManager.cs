using System;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    public PlayerChar MainPlayer { get; private set; }
    public CreateActionPlate ActionPlate;

    public override bool Initialize()
    {
        if (MainPlayer is null)
        {
            var prefab = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, "Character/MainPlayer");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.transform.localPosition = new Vector3(0.5f, -0.5f, 0);
                MainPlayer = go.GetComponent<PlayerChar>();
            }
        }

        if (ActionPlate is null)
        {
            var prefab = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, "ActionPlates");
            if (prefab != null)
            {
                var go = Instantiate(prefab, transform);
                go.transform.localPosition = new Vector3(0, 0, 0);
                ActionPlate = go.GetComponent<CreateActionPlate>();
            }
        }

        return true;
    }

    public void CreatePlates(Vector2 pos)
    {
        switch (MainPlayer.CharAction)
        {
            case eCharAction.Move:
            {
                ActionPlate.CreateMovePlate(pos, MainPlayer.MoveRange);
                break;
            }
            case eCharAction.Attack:
            {
                ActionPlate.CreateAttackPlate(pos, MainPlayer.AttackRange);
                break;
            }
        }
    }

    public void ClearPlates()
    {
        ActionPlate.ClearPlate();
    }

    protected override void Destroy()
    {

    }
}
