using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserNameChanged)]
public class RoomUserNameChangedWriter : AbstractPacketWriter
{
    public required int UserId { get; set; }
    public required int RoomId { get; set; }
    public required string Name { get; set; }
}