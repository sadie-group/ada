using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Handshake;

[PacketId(ServerPacketId.CompleteDiffieHandshake)]
public class CompleteDiffieHandshakeWriter : AbstractPacketWriter
{
    public required string PublicKey { get; init; }
    public bool? ClientEncryption { get; init; }
}