using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomSettingsUpdated)]
public class RoomSettingsUpdatedWriter : AbstractPacketWriter
{
    public required long RoomId { get; init; }
}