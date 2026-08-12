using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideSessionError)]
public class GuideSessionErrorWriter : AbstractPacketWriter
{
    public required int ErrorCode { get; init; }
}
