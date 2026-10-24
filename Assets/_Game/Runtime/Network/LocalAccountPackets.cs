namespace DarkSaver.Network
{
    // Local registration contract only; credentials are never included in packet trace entries.
    public sealed class AccountRegisterRequest : ClientPacket
    {
        public string UserId {get;} public string Password {get;} public string Confirmation {get;}
        public AccountRegisterRequest(string userId,string password,string confirmation)
        {UserId=userId;Password=password;Confirmation=confirmation;}
    }
    public sealed class AccountRegisterResponse : LocalCommandResponse
    {
        public string UserId {get;}
        public AccountRegisterResponse(bool accepted,string reason=null,string userId=null):base(accepted,reason){UserId=userId;}
    }
}
