using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideSessionMessage)]
public class GuideSessionMessageWriter : AbstractPacketWriter
{
    public required string Message { get; init; }
    public required int SenderId { get; init; }
}
