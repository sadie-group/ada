using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserRespect)]
public class RoomUserRespectWriter : AbstractPacketWriter
{
    public required int UserId { get; init; }
    public required int TotalRespects { get; init; }
}