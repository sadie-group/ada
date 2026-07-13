using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Handshake;

[PacketId(ServerPacketId.NoobnessLevel)]
public class NoobnessLevelWriter : AbstractPacketWriter
{
    public required int Level { get; init; }
}