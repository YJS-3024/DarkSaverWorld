using UnityEngine;

public abstract class LocalPacketBase
{
    
}

/// <summary>
/// TODO :: 플레이어만 아닌 몬스터 엔피씨도 이동 가능
/// </summary>
public class Server_MovePlayer : LocalPacketBase
{
    public long PlayerID { private set; get; }
    public Vector2 CurPos { private set; get; }
    public Vector2 DestinationPos { private set; get; }

    public Server_MovePlayer(long playerID, Vector2 currentPosition, Vector2 destinationPosition)
    {
        PlayerID = playerID;
        CurPos = currentPosition;
        DestinationPos = destinationPosition;
    }
}

public class Server_Attack : LocalPacketBase
{
    /// <summary>
    /// 공격 ID (기본공격은 0)
    /// </summary>
    public long AttackId { get; private set; }
    
    /// <summary>
    /// 공격을 시도한 자
    /// </summary>
    public long AttackerId { get; private set; }
    
    /// <summary>
    /// 공격을 받는 자
    /// </summary>
    public long ReceiverId { get; private set; }

    public Server_Attack(long attackId, long attackerId, long receiverId)
    {
        AttackId = attackId;
        AttackerId = attackerId;
        ReceiverId = receiverId;
    }
}
