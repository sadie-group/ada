using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideSessionStarted)]
public class GuideSessionStartedWriter : AbstractPacketWriter
{
    public required int RequesterId { get; init; }
    public required string RequesterUsername { get; init; }
    public required string RequesterFigureCode { get; init; }
    public required int HelperId { get; init; }
    public required string HelperUsername { get; init; }
    public required string HelperFigureCode { get; init; }
}
