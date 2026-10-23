using System;
using System.Collections.Generic;

namespace DarkSaver.Network
{
    // In-memory messages, not original binary layouts, IDs, padding or pointers.
    public enum LocalPacketDirection { ClientToServer, ServerToClient }
    public abstract class LocalPacket { public abstract LocalPacketDirection Direction { get; } }
    public abstract class ClientPacket : LocalPacket
    {
        public sealed override LocalPacketDirection Direction => LocalPacketDirection.ClientToServer;
    }
    public abstract class ServerPacket : LocalPacket
    {
        public sealed override LocalPacketDirection Direction => LocalPacketDirection.ServerToClient;
    }

    // normal_attack: original prefix 0x5C, opcode 0x82, followed by four int16 values.
    public sealed class AttackRequest : ClientPacket
    {
        public short TargetUnitId { get; }
        public short AttackerUnitId { get; }
        public short TargetTileAttribute { get; }
        public short AttackerTileAttribute { get; }
        public AttackRequest(short targetUnitId, short attackerUnitId, short targetTileAttribute, short attackerTileAttribute)
        {
            TargetUnitId = targetUnitId; AttackerUnitId = attackerUnitId;
            TargetTileAttribute = targetTileAttribute; AttackerTileAttribute = attackerTileAttribute;
        }
    }

    // PGSATTACKFX / AttackChar. Effect is not a reconstructed damage value.
    public sealed class AttackEffectNotification : ServerPacket
    {
        public short TargetUnitId { get; }
        public short AttackerUnitId { get; }
        public short Effect { get; }
        public AttackEffectNotification(short targetUnitId, short attackerUnitId, short effect)
        { TargetUnitId = targetUnitId; AttackerUnitId = attackerUnitId; Effect = effect; }
    }

    public sealed class MagicRequest : ClientPacket
    {
        // Original opcode variants: 0x89 / 0x92. Their selection/mode meaning is unresolved.
        // Nullable local metadata does not assert an original wire layout for Mode.
        public byte? OriginalOpcode { get; }
        public byte? Mode { get; }
        public short MagicId { get; }
        public short CasterUnitId { get; }
        // Local initiating actor; CasterUnitId retains the original request reference
        // (which may be -1 or team*50). This field is not an added wire word.
        public short ActingUnitId { get; }
        public short X { get; }
        public short Y { get; }
        public int ComputedCost { get; } // Original serialized value; not assumed AP or MP.
        public IReadOnlyList<short> TargetUnitIds { get; }
        public byte TargetCount => (byte)TargetUnitIds.Count;
        public MagicRequest(short magicId, short casterUnitId, short x, short y, int computedCost,
            short[] targets, byte? originalOpcode = null, byte? mode = null, short? actingUnitId = null)
        {
            if (originalOpcode.HasValue && originalOpcode != 0x89 && originalOpcode != 0x92)
                throw new ArgumentOutOfRangeException(nameof(originalOpcode));
            MagicId = magicId; CasterUnitId = casterUnitId; X = x; Y = y; ComputedCost = computedCost;
            ActingUnitId = actingUnitId ?? casterUnitId;
            OriginalOpcode = originalOpcode; Mode = mode; TargetUnitIds = LocalPacketTargets.Copy(targets);
        }
    }

    // PGSMAGIC / Magic. The original client-side pointer is replaced by an owned target list.
    // Value is uninterpreted; client-structure size is not a network payload size.
    public sealed class MagicEffectNotification : ServerPacket
    {
        public short MagicId { get; }
        public short CasterUnitId { get; }
        public short X { get; }
        public short Y { get; }
        public int Value { get; }
        public IReadOnlyList<short> TargetUnitIds { get; }
        public byte TargetCount => (byte)TargetUnitIds.Count;
        public MagicEffectNotification(short magicId, short casterUnitId, short x, short y, int value, short[] targets)
        {
            MagicId = magicId; CasterUnitId = casterUnitId; X = x; Y = y; Value = value;
            TargetUnitIds = LocalPacketTargets.Copy(targets);
        }
    }

    // PGSHPSET / SetHpDisplay: absolute replacement, not damage subtraction.
    public sealed class HpSetNotification : ServerPacket
    {
        public short UnitId { get; }
        public short AbsoluteHp { get; }
        public HpSetNotification(short unitId, short absoluteHp) { UnitId = unitId; AbsoluteHp = absoluteHp; }
    }
    // PGSMPSET / SetMpDisplay: absolute replacement.
    public sealed class MpSetNotification : ServerPacket
    {
        public short UnitId { get; }
        public short AbsoluteMp { get; }
        public MpSetNotification(short unitId, short absoluteMp) { UnitId = unitId; AbsoluteMp = absoluteMp; }
    }
    // PGSNEWEXP / SetExp: alignment padding omitted from local data.
    public sealed class ExperienceSetNotification : ServerPacket
    {
        public short UnitId { get; }
        public uint AbsoluteExperience { get; }
        public ExperienceSetNotification(short unitId, uint absoluteExperience)
        { UnitId = unitId; AbsoluteExperience = absoluteExperience; }
    }
    // dmKillChar clears the record/occupancy; separate from the 0x80 flag operation.
    public sealed class UnitRemoveNotification : ServerPacket
    {
        public short UnitId { get; }
        public UnitRemoveNotification(short unitId) { UnitId = unitId; }
    }
    // dmKillChar2 sets 0x80. Corpse is our native state mapping, not proof that
    // ordinary gameplay displays a corpse or death animation. The original
    // handler also reads a second word whose meaning is not established here.
    public sealed class UnitCorpseNotification : ServerPacket
    {
        public short UnitId { get; }
        public UnitCorpseNotification(short unitId) { UnitId = unitId; }
    }
    // SetTickMenu takes three signed words: unit, absolute tick, raw menu flags.
    // Local decoded fields only: wire opcode/packing and flag command names are unconfirmed.
    public sealed class TickMenuNotification : ServerPacket
    {
        public bool HasPayload { get; }
        public short UnitId { get; }
        public short AbsoluteTick { get; }
        public short RawFlags { get; }
        public TickMenuNotification() { } // Retained empty boundary; never applies guessed data.
        public TickMenuNotification(short unitId, short absoluteTick, short rawFlags)
        { HasPayload = true; UnitId = unitId; AbsoluteTick = absoluteTick; RawFlags = rawFlags; }
    }
    public sealed class UnknownClientPacket : ClientPacket { }
    public sealed class UnknownServerPacket : ServerPacket { }

    internal static class LocalPacketTargets
    {
        public static IReadOnlyList<short> Copy(short[] targets)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (targets.Length > byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(targets));
            return Array.AsReadOnly((short[])targets.Clone());
        }
    }
}
