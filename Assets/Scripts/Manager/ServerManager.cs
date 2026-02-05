using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public partial class ServerManager : MonoSingleton<ServerManager>
{
    protected override void Destroy()
    {
    }

    public override bool Initialize()
    {
        return true;
    }

    private void SendLocalPacket<T>(T packetData)
    {
        if (typeof(T).IsSubclassOf(typeof(LocalPacketBase)) == false)
            return;

        var name = typeof(T).Name.Replace("Server", "");
        var method = typeof(ServerManager).GetMethod($"Response{name}");
        if (method == null)
            return;

        method.Invoke(this, new object[] { packetData });
    }

    public void Request_MovePlayer(long playerID,
        Vector2 currentPosition,
        Vector2 destinationPosition)
    {
        // if (playerID == 0)
        //     return;

        SendLocalPacket(new Server_MovePlayer(playerID, currentPosition, destinationPosition));
    }

    public void Response_MovePlayer(Server_MovePlayer packetData)
    {
        var player = PlayerManager.I.GetPlayer(packetData.PlayerID);

        var posList = SceneController.I.CurScene.Path.FindPath(packetData.CurPos, packetData.DestinationPos, false);
        if (posList != null)
        {
            PlayerManager.I.MainPlayer.Move(posList);
        }
    }

    public void Request_Attack(long skillId, long attackerID, long receiverId)
    {
        SendLocalPacket(new Server_Attack(skillId, attackerID, receiverId));
    }

    public void Response_Attack(Server_Attack packetData)
    {
        if (packetData.AttackerId == 0 || packetData.ReceiverId == 0)
            return;
        
        
    }
}