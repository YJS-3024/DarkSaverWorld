using DarkSaver.Events;
namespace DarkSaver.Network
{
    // Local lobby messages; no online rooms or remote players are fabricated.
    public sealed class ScenarioRoomListRequest:ClientPacket
    {
        public string Key { get; } public int Generation { get; }
        public ScenarioRoomListRequest(string key,int generation){Key=key;Generation=generation;}
    }
    public sealed class ScenarioRoomEnterRequest:ClientPacket
    {
        public string Key { get; } public int Generation { get; }
        public ScenarioRoomEnterRequest(string key,int generation){Key=key;Generation=generation;}
    }
    public sealed class ScenarioRoomState:LocalCommandResponse
    {
        public string Key { get; } public bool HasOngoingRoom { get; }
        public bool HasCompletedRecord { get; }
        public bool CanEnter { get; } public int Generation { get; }
        public ScenarioRoomState(bool accepted,string key,int generation,bool ongoing=false,bool canEnter=false,string reason=null,bool completed=false):base(accepted,reason)
        {Key=key;Generation=generation;HasOngoingRoom=ongoing;CanEnter=canEnter;HasCompletedRecord=completed;}
    }
    public static class ScenarioRoomRules
    {
        public static bool IsOngoing(Scenario01Progress progress)=>progress!=null
            && progress.phase>=Scenario01Progress.Intro && progress.phase<Scenario01Progress.Complete;
    }
}
