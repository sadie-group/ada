using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.ModToolIssueInfo)]
public class ModToolIssueInfoWriter : AbstractPacketWriter
{
    public required int UserId { get; init; }
    public required int SanctionCount { get; init; }
    public required string SuggestedSanction { get; init; }
}
