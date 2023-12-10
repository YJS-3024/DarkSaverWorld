using System;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    public PlayerChar PlayerChar { get; private set; }
    public CreateActionPlate ActionPlate;

    public override bool Initialize()
    {
        if (PlayerChar is null)
        {
            var prefab = ResourceManager.I.Load<GameObject>(eResourceType.Prefabs, "Character/MainPlayer");
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.transform.localPosition = new Vector3(0.5f, -0.5f, 0);
                PlayerChar = go.GetComponent<PlayerChar>();
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
        switch (PlayerChar.CharAction)
        {
            case eCharAction.Move:
            {
                ActionPlate.CreateMovePlate(pos, PlayerChar.MoveRange);
                break;
            }
            case eCharAction.Attack:
            {
                ActionPlate.CreateAttackPlate(pos, PlayerChar.AttackRange);
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
