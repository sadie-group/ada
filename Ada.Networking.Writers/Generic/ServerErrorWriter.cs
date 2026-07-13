using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Generic;

[PacketId(ServerPacketId.ServerError)]
public class ServerErrorWriter : AbstractPacketWriter
{
    public required int MessageId { get; init; }
    public required int ErrorCode { get; init; }
    public required string DateTime { get; init; }
}