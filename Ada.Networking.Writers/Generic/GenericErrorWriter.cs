using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Generic;

[PacketId(ServerPacketId.GenericError)]
public class GenericErrorWriter : AbstractPacketWriter
{
    public required int ErrorCode { get; init; }
}