using System;
namespace DarkSaver.Network
{
    // User-authorized local lobby contracts. Original wire opcodes are unconfirmed.
    public sealed class ServerListRequest:ClientPacket {}
    public sealed class ServerSelectRequest:ClientPacket
    {
        public int ServerId {get;}
        public ServerSelectRequest(int serverId){ServerId=serverId;}
    }
    public sealed class LocalServerInfo
    {
        public int Id {get;}
        public string Name {get;}
        public bool Available {get;}
        public LocalServerInfo(int id,string name,bool available){Id=id;Name=name;Available=available;}
    }
    public sealed class ServerListResponse:LocalCommandResponse
    {
        private readonly LocalServerInfo[] servers;
        public string AccountId {get;}
        public LocalServerInfo[] CaptureServers()=>(LocalServerInfo[])servers.Clone();
        public ServerListResponse(bool accepted,string accountId=null,LocalServerInfo[] servers=null,string reason=null):base(accepted,reason)
        {AccountId=accountId;this.servers=servers==null?Array.Empty<LocalServerInfo>():(LocalServerInfo[])servers.Clone();}
    }
    public sealed class ServerSelectResponse:LocalCommandResponse
    {
        public int ServerId {get;}
        public string AccountId {get;}
        public ServerSelectResponse(bool accepted,int serverId=0,string accountId=null,string reason=null):base(accepted,reason)
        {ServerId=serverId;AccountId=accountId;}
    }
}
