using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Doorbell;

[PacketId(ServerPacketId.RoomDoorbell)]
public class RoomDoorbellWriter : AbstractPacketWriter
{
    public required string Username { get; init; }
}