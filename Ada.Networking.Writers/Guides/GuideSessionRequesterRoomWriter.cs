using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideSessionRequesterRoom)]
public class GuideSessionRequesterRoomWriter : AbstractPacketWriter
{
    public required int RoomId { get; init; }
}
