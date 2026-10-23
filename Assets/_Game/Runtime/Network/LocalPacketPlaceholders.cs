namespace DarkSaver.Network
{
    // Native planning placeholders only. Original packet names/opcodes/payloads are NOT verified.
    // Keep empty until analysis or an explicit native design supplies their contracts.
    public sealed class LoginRequest : ClientPacket { }
    public sealed class LoginResponse : ServerPacket { }
    public sealed class CharacterListRequest : ClientPacket { }
    public sealed class CharacterListResponse : ServerPacket { }
    public sealed class CharacterCreateRequest : ClientPacket { }
    public sealed class CharacterCreateResponse : ServerPacket { }
    public sealed class WorldMoveRequest : ClientPacket { }
    public sealed class WorldMoveNotification : ServerPacket { }
    public sealed class MapEnterRequest : ClientPacket { }
    public sealed class MapEnterNotification : ServerPacket { }
    public sealed class ItemUseRequest : ClientPacket { }
    public sealed class ItemUseNotification : ServerPacket { }
    public sealed class EquipmentNotification : ServerPacket { }
    public sealed class ScenarioEnterRequest : ClientPacket { }
    public sealed class ScenarioEnterNotification : ServerPacket { }
}
