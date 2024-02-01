using System;
using System.Collections.Generic;
using GlobalEnum;
using UnityEngine;
using UnityEngine.Serialization;

public partial class PlayerManager : MonoSingleton<PlayerManager>
{
    public PlayerChar MainPlayer { get; private set; }

    public List<PlayerChar> playerList = new List<PlayerChar>();

    public override bool Initialize()
    {
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

    public bool GetIsPlayer(int posX, int posY)
    {
        var pos = new Vector2(posX, posY);
        var checkPos =  TilemapManager.I.GetNode_WorldPos(pos).centerPos;
        var targetPos = TilemapManager.I.GetNode_WorldPos(MainPlayer.transform.position).centerPos;
        if (targetPos.Equals(checkPos))
            return true;

        //플레이어들 위치 추가할곳

        return false;
    }

    protected override void Destroy()
    {

    }
}

public partial class PlayerManager
{
    public int SelectSkillId { get; set; }
}