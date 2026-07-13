using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserDance)]
public class RoomUserDanceWriter : AbstractPacketWriter
{
    public required long UserId { get; init; }
    public required int DanceId { get; init; }
}