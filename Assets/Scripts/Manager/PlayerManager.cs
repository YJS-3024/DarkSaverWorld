using System;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.Serialization;

public partial class PlayerManager : MonoSingleton<PlayerManager>
{
    public PlayerChar MainPlayer { get; private set; }
    public CreateActionPlate ActionPlate;

    public override bool Initialize()
    {
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

    public void CreatePlayer(bool isMainPlayer)
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
    }

    public void CreatePlates(Vector2 centerPos, eCharAction actionType, int range)
    {
        MainPlayer.CharAction = actionType;
        switch (actionType)
        {
            case eCharAction.Move:
            {
                ActionPlate.CreateMovePlate(centerPos, range);
                break;
            }
            case eCharAction.Attack:
            {
                ActionPlate.CreateAttackPlate(centerPos, range);
                break;
            }
            case eCharAction.Magic_Attack:
            case eCharAction.Magic_Buff:
            case eCharAction.Magic_JobSkill:
            {
                ActionPlate.CreateSkillPlate(centerPos, range);
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

public partial class PlayerManager
{
    public int SelectSkillId { get; set; }
}