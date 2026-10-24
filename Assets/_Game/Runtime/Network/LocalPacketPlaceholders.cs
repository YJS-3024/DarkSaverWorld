namespace DarkSaver.Network
{
    // Local-only startup/world contracts. Original wire names/opcodes are not inferred.
    // Remaining empty item/scenario placeholders are compatibility definitions, not handlers.
    public sealed class LoginRequest : ClientPacket
    {
        public string UserId {get;} public string Password {get;}
        public LoginRequest(string userId=null,string password=null){UserId=userId;Password=password;}
    }
    public sealed class LoginStatusRequest : ClientPacket { }
    public sealed class LoginResponse : LocalCommandResponse
    {
        public bool RequiresCharacterSelection {get;} public string ResumePath {get;}
        public bool HasAccount {get;} public string AccountId {get;} public bool Authenticated {get;}
        public LoginResponse(bool ready=false,string reason=null,bool requiresCharacterSelection=true,string resumePath=null,bool hasAccount=false,string accountId=null,bool authenticated=false):base(ready,reason)
        {RequiresCharacterSelection=requiresCharacterSelection;ResumePath=resumePath;HasAccount=hasAccount;AccountId=accountId;Authenticated=authenticated;}
    }
    public sealed class CharacterListRequest : ClientPacket { }
    public sealed class CharacterListResponse : LocalCommandResponse
    {
        private readonly bool[] occupied;private readonly DarkSaver.Core.NativeCharacterProfile[] profiles;
        public int SlotCount=>profiles?.Length ?? 0;
        public bool IsOccupied(int slot)=>occupied!=null && slot>=0 && slot<occupied.Length && occupied[slot];
        public DarkSaver.Core.NativeCharacterProfile CaptureProfile(int slot)=>profiles!=null && slot>=0 && slot<profiles.Length?profiles[slot]?.Clone():null;
        public CharacterListResponse():base(false){}
        public CharacterListResponse(bool[] occupied,DarkSaver.Core.NativeCharacterProfile[] profiles):base(true)
        {this.occupied=(bool[])occupied.Clone();this.profiles=System.Array.ConvertAll(profiles,p=>p?.Clone());}
    }
    public sealed class CharacterCreateRequest : ClientPacket
    {
        public string Name {get;} public int Job {get;} public string LocalSavePath {get;}
        public CharacterCreateRequest(string name=null,int job=100,string localSavePath=null){Name=name;Job=job;LocalSavePath=localSavePath;}
    }
    public sealed class CharacterSelectRequest : ClientPacket
    {public string LocalSavePath {get;} public CharacterSelectRequest(string path){LocalSavePath=path;}}
    public sealed class CharacterCreateResponse : LocalCommandResponse {public CharacterCreateResponse(bool accepted=false,string reason=null):base(accepted,reason){} }
    // Local world coordinates/speed validation; no original binary packing is claimed.
    public sealed class WorldMoveRequest : ClientPacket
    {
        public float X { get; } public float Y { get; }
        public WorldMoveRequest(float x=0,float y=0) { X=x;Y=y; }
    }
    public sealed class WorldMoveNotification : LocalCommandResponse
    {
        public float X { get; } public float Y { get; }
        public WorldMoveNotification(bool accepted=false,float x=0,float y=0,string reason=null):base(accepted,reason) { X=x;Y=y; }
    }
    // Native map transition contract: local names, not recovered wire fields/opcodes.
    public sealed class MapEnterRequest : ClientPacket
    {
        public string Target { get; } public string SpawnId { get; }
        public MapEnterRequest(string target=null,string spawnId=null) { Target=target;SpawnId=spawnId; }
    }
    public enum MapEnterStage { Started, Ready, Failed }
    public sealed class MapEnterNotification : LocalCommandResponse
    {
        public MapEnterStage Stage { get; } public string Map { get; } public string Runtime { get; }
        public string SpawnId { get; } public float Width { get; } public float Height { get; }
        public float X { get; } public float Y { get; }
        public MapEnterNotification(bool accepted=false,MapEnterStage stage=MapEnterStage.Failed,string map=null,string runtime=null,
            string spawnId=null,float width=0,float height=0,float x=0,float y=0,string reason=null):base(accepted,reason)
        { Stage=stage;Map=map;Runtime=runtime;SpawnId=spawnId;Width=width;Height=height;X=x;Y=y; }
    }
    public sealed class ItemUseRequest : ClientPacket { }
    public sealed class ItemUseNotification : ServerPacket { }
    public sealed class EquipmentNotification : ServerPacket { }
    public sealed class ScenarioEnterRequest : ClientPacket { }
    public sealed class ScenarioEnterNotification : ServerPacket { }
}
