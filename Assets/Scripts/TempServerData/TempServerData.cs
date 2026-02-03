using UnityEngine;

public abstract class LocalPacketBase
{
    
}

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
