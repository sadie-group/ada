using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomForwardEntry)]
public class RoomForwardEntryWriter : AbstractPacketWriter
{
    public required long RoomId { get; init; }
}