using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserLeft)]
public class RoomUserLeftWriter : AbstractPacketWriter
{
    public required string UserId { get; init; }
}