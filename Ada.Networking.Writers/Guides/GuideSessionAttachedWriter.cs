using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideSessionAttached)]
public class GuideSessionAttachedWriter : AbstractPacketWriter
{
    public required bool IsHelper { get; init; }
    public required int TourType { get; init; }
    public required string HelpRequest { get; init; }
    public required int SecondsRemaining { get; init; }
}
