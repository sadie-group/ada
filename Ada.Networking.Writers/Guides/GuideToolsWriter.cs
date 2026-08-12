using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Guides;

[PacketId(ServerPacketId.GuideTools)]
public class GuideToolsWriter : AbstractPacketWriter
{
    public required bool OnDuty { get; init; }
    public required int GuidesOnDuty { get; init; }
    public required int HelpersOnDuty { get; init; }
    public required int GuardiansOnDuty { get; init; }
}
