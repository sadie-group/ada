using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Encryption;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Writers.Handshake;

namespace Ada.Networking.Events.Handlers.Handshake;

[PacketId(EventHandlerId.InitDiffieHandshake)]
[AllowUnauthenticated]
public class InitDiffieHandshakeEventHandler(HabboEncryption habboEncryption) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new InitDiffieHandshakeWriter
        {
            SingedPrime = habboEncryption.GetRsaDiffieHellmanPrimeKey(),
            SignedGenerator = habboEncryption.GetRsaDiffieHellmanGeneratorKey()
        });
    }
}