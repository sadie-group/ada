using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomBannedUsers)]
public class RoomBannedUsersWriter : AbstractPacketWriter
{
    public required int RoomId { get; init; }
    public required Dictionary<long, string> BannedUsersMap { get; init; }
}