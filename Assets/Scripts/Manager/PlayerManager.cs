using GlobalEnum;
using UnityEngine;

public class PlayerManager : MonoSingleton<PlayerManager>
{
    public PlayerChar MainPlayer { get; private set; }
    public CreateActionPlate ActionPlate;

    public override bool Initialize()
    {
        if (ActionPlate == null)
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
        if (MainPlayer == null)
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

    public void CreatePlates(Vector2 centerPos,eCharAction actionType, int range)
    {
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
            case eCharAction.Magic:
            {
                ActionPlate.CreateAttackPlate(centerPos, range);
                break;
            }
        }
    }

    public PlayerChar GetPlayerChar(long playerId)
    {
        if (MainPlayer.CharIdx == playerId)
            return MainPlayer;

        //  TODO :: 플레이어 리스트 생기면 추가
        return null;
    }

    public void ClearPlates()
    {
        ActionPlate.ClearPlate();
    }

    protected override void Destroy()
    {

    }
}
