using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserTags)]
public class RoomUserTagsWriter : AbstractPacketWriter
{
    public required long UserId { get; init; }
    public required List<string> Tags { get; init; }
}