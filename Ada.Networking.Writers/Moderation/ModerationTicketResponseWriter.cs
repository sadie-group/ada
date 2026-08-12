using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.ModerationTicketResponse)]
public class ModerationTicketResponseWriter : AbstractPacketWriter
{
    public required int Result { get; init; }
}
