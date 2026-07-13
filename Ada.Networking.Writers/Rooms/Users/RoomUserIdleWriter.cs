using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserIdle)]
public class RoomUserIdleWriter : AbstractPacketWriter
{
    public required long UserId { get; set; }
    public required bool IsIdle { get; set; }
}