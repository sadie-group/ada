using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

[PacketId(ServerPacketId.CallForHelperError)]
public class CallForHelperErrorWriter : AbstractPacketWriter
{
    public required int ErrorCode { get; init; }
}
