using DarkSaver.Combat;

namespace DarkSaver.Network
{
    // Single-player protocol extensions. These are not assigned original wire opcodes.
    public sealed class BattleAttackCommand : ClientPacket
    {
        public short TargetId { get; } public short ActorId { get; }
        public BattleAttackCommand(short target,short actor) { TargetId=target; ActorId=actor; }
    }
    public sealed class BattleMoveRequest : ClientPacket
    {
        public short UnitId { get; } public int X { get; } public int Y { get; }
        public BattleMoveRequest(short unit, int x, int y) { UnitId=unit; X=x; Y=y; }
    }
    public sealed class BattleRestRequest : ClientPacket
    {
        public short UnitId { get; }
        public BattleRestRequest(short id) { UnitId=id; }
    }
    public sealed class BattleClockRequest : ClientPacket
    {
        public float Seconds { get; }
        public BattleClockRequest(float seconds) { Seconds=seconds; }
    }
    public sealed class BattleSimulationTickRequest : ClientPacket
    {
        public float Seconds {get;} public float MovementSeconds {get;}
        public BattleSimulationTickRequest(float seconds,float movementSeconds){Seconds=seconds;MovementSeconds=movementSeconds;}
    }
    public sealed class BattleStateNotification : ServerPacket
    {
        public System.Collections.Generic.IReadOnlyList<CombatUnitSnapshot> Units {get;}
        public BattleStateNotification(CombatUnitSnapshot[] units){Units=System.Array.AsReadOnly((CombatUnitSnapshot[])units.Clone());}
    }
    public sealed class LocalCombatResolvedNotification : ServerPacket
    {
        public CombatResult Result { get; }
        public LocalCombatResolvedNotification(CombatResult result) { Result=result; }
    }
    public sealed class LocalVitalsDisplayNotification : ServerPacket
    {
        public short UnitId { get; } public int Delta { get; } public bool Mp { get; }
        public LocalVitalsDisplayNotification(short id,int delta,bool mp) { UnitId=id; Delta=delta; Mp=mp; }
    }
    public sealed class LocalMagicPresentationNotification : ServerPacket
    {
        public CombatRequest Action { get; } public MagicRequest Cast { get; }
        public LocalMagicPresentationNotification(CombatRequest action,MagicRequest cast=null) { Action=action; Cast=cast; }
    }
}
