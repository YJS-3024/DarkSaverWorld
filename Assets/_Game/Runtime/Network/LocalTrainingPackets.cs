using System;
using System.Collections.Generic;
using DarkSaver.Combat;

namespace DarkSaver.Network
{
    // Local training contracts only. No original opcode, binary layout, timer or
    // class-to-map assignment is inferred. Existing HP/MP/XP/Tick/corpse/remove
    // notifications remain independent authoritative updates.
    public sealed class Request_EnterTraining : ClientPacket { }
    public sealed class Request_ReadyTraining : ClientPacket
    {
        public int SessionId { get; }
        public bool Loaded { get; }
        public Request_ReadyTraining(int sessionId, bool loaded) { SessionId = sessionId; Loaded = loaded; }
    }
    public sealed class Request_GetTrainingState : ClientPacket
    {
        public int SessionId { get; }
        public Request_GetTrainingState(int sessionId) { SessionId = sessionId; }
    }
    public sealed class Request_RetreatTraining : ClientPacket
    {
        public int SessionId { get; }
        public Request_RetreatTraining(int sessionId) { SessionId = sessionId; }
    }
    public sealed class Response_EnterTraining : LocalCommandResponse
    {
        public int SessionId { get; }
        public int MapId { get; }
        public int X { get; }
        public int Y { get; }
        public Response_EnterTraining(string reason) : base(false, reason) { }
        public Response_EnterTraining(int sessionId, int mapId, int x, int y) : base(true)
        {
            TrainingPacketValues.Session(sessionId);
            if (!OriginalTrainingMaps.Contains(mapId) || x < 0 || x >= 20 || y < 0 || y >= 15)
                throw new ArgumentException("Invalid native training destination.");
            SessionId = sessionId; MapId = mapId; X = x; Y = y;
        }
    }
    public sealed class Response_ReadyTraining : LocalCommandResponse
    {
        public int SessionId { get; }
        public Response_ReadyTraining(int sessionId, bool accepted, string reason = null) : base(accepted, reason)
        {
            if (accepted) TrainingPacketValues.Session(sessionId);
            SessionId = sessionId;
        }
    }
    public sealed class Response_GetTrainingState : LocalCommandResponse
    {
        public int SessionId { get; }
        public int MapId { get; }
        public int Revision { get; }
        public IReadOnlyList<CombatUnitSnapshot> Units { get; }
        public Response_GetTrainingState(string reason) : base(false, reason)
        { Units = Array.AsReadOnly(Array.Empty<CombatUnitSnapshot>()); }
        public Response_GetTrainingState(int sessionId, int mapId, int revision, CombatUnitSnapshot[] units) : base(true)
        {
            TrainingPacketValues.Session(sessionId);
            if (!OriginalTrainingMaps.Contains(mapId) || revision <= 0) throw new ArgumentException("Invalid training state metadata.");
            Units = TrainingPacketValues.Copy(units);
            SessionId = sessionId; MapId = mapId; Revision = revision;
        }
    }
    public enum TrainingUnitUpdateKind { Initial, Spawn, Respawn }
    // Server push: a full explicitly supplied record for each affected unit.
    // Does not imply an XP reward, HP recovery, animation or client-side timer.
    public sealed class Notify_TrainingUnits : ServerPacket
    {
        public int SessionId { get; }
        public int Revision { get; }
        public TrainingUnitUpdateKind Kind { get; }
        public IReadOnlyList<CombatUnitSnapshot> Units { get; }
        public Notify_TrainingUnits(int sessionId, int revision, TrainingUnitUpdateKind kind, CombatUnitSnapshot[] units)
        {
            TrainingPacketValues.Session(sessionId);
            if (revision <= 0 || !Enum.IsDefined(typeof(TrainingUnitUpdateKind), kind)) throw new ArgumentException("Invalid training update metadata.");
            Units = TrainingPacketValues.Copy(units);
            if (kind != TrainingUnitUpdateKind.Initial && Units.Count == 0) throw new ArgumentException("Spawn records are required.");
            SessionId = sessionId; Revision = revision; Kind = kind;
        }
    }
    public sealed class Response_RetreatTraining : LocalCommandResponse
    {
        public int SessionId { get; }
        public string MapScene { get; }
        public string RuntimeScene { get; }
        public int X { get; }
        public int Y { get; }
        public Response_RetreatTraining(string reason) : base(false, reason) { }
        public Response_RetreatTraining(int sessionId, string mapScene, string runtimeScene, int x, int y) : base(true)
        {
            TrainingPacketValues.Session(sessionId);
            if (string.IsNullOrWhiteSpace(mapScene) || string.IsNullOrWhiteSpace(runtimeScene) || x < 0 || y < 0)
                throw new ArgumentException("An explicit retreat destination is required.");
            SessionId = sessionId; MapScene = mapScene; RuntimeScene = runtimeScene; X = x; Y = y;
        }
    }
    internal static class TrainingPacketValues
    {
        internal static void Session(int sessionId)
        { if (sessionId <= 0) throw new ArgumentOutOfRangeException(nameof(sessionId)); }
        internal static IReadOnlyList<CombatUnitSnapshot> Copy(CombatUnitSnapshot[] units)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));
            var ids = new HashSet<short>();
            foreach (var unit in units)
                if (unit.Id < 0 || !ids.Add(unit.Id) || unit.JobId < 0
                    || unit.X < 0 || unit.X >= 20 || unit.Y < 0 || unit.Y >= 15
                    || unit.MaxHp <= 0 || unit.Hp < 0 || unit.Hp > unit.MaxHp
                    || unit.MaxMp < 0 || unit.Mp < 0 || unit.Mp > unit.MaxMp
                    || unit.Ap < 0 || unit.MaxAp < 0 || (unit.MaxAp > 0 && unit.Ap > unit.MaxAp)
                    || !Enum.IsDefined(typeof(UnitLifeState), unit.LifeState))
                    throw new ArgumentException("Duplicate or invalid training unit state.");
            return Array.AsReadOnly((CombatUnitSnapshot[])units.Clone());
        }
    }
}
