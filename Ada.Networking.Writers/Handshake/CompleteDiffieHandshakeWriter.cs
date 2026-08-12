using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Handshake;

[PacketId(ServerPacketId.CompleteDiffieHandshake)]
public class CompleteDiffieHandshakeWriter : AbstractPacketWriter
{
    public required string PublicKey { get; init; }
    public bool? ClientEncryption { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteString(PublicKey);
        writer.WriteBool(ClientEncryption ?? false);
    }
}
