using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserAction)]
public class RoomUserActionWriter : AbstractPacketWriter
{
    public required long UserId { get; set; }
    public required int Action { get; set; }
}