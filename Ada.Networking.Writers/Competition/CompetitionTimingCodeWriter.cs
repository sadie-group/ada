using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Competition;

[PacketId(ServerPacketId.CompetitionTimingCode)]
public class CompetitionTimingCodeWriter : AbstractPacketWriter
{
    public required string Schedule { get; init; }
    public required string Code { get; init; }
}