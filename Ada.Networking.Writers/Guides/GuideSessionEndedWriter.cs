using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideSessionEnded)]
public class GuideSessionEndedWriter : AbstractPacketWriter
{
    public required int Reason { get; init; }
}
