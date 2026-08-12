using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideSessionInvitedToGuideRoom)]
public class GuideSessionInvitedToGuideRoomWriter : AbstractPacketWriter
{
    public required int RoomId { get; init; }
    public required string RoomName { get; init; }
}
