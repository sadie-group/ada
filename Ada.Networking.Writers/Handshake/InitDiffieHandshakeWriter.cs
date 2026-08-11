using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Handshake;

[PacketId(ServerPacketId.InitDiffieHandshake)]
public class InitDiffieHandshakeWriter : AbstractPacketWriter
{
    public required string SignedPrime { get; init; }
    public required string SignedGenerator { get; init; }
}