using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Encryption;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Writers.Handshake;

namespace Ada.Networking.Events.Handlers.Handshake;

[PacketId(EventHandlerId.CompleteDiffieHandshake)]
[AllowUnauthenticated]
public class CompleteDiffieHandshakeEventHandler(
    HabboEncryption habboEncryption) : INetworkPacketEventHandler
{
    public string? PublicKey { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (string.IsNullOrEmpty(PublicKey))
        {
            return;
        }

        var sharedKey = habboEncryption.CalculateDiffieHellmanSharedKey(PublicKey);

        await client.WriteToStreamAsync(new CompleteDiffieHandshakeWriter
        {
            PublicKey = habboEncryption.GetRsaDiffieHellmanPublicKey(),
            ClientEncryption = true
        });

        client.EnableEncryption(sharedKey);
    }
}