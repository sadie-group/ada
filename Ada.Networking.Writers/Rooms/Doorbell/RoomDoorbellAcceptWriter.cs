using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Doorbell;

[PacketId(ServerPacketId.RoomDoorbellAccept)]
public class RoomDoorbellAcceptWriter : AbstractPacketWriter
{
    public required string Username { get; init; }
}