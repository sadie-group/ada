using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Encryption;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Options;
using Ada.Networking.Writers.Handshake;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ada.Networking.Events.Handlers.Handshake;

[PacketId(EventHandlerId.CompleteDiffieHandshake)]
[AllowUnauthenticated]
public class CompleteDiffieHandshakeEventHandler(
    HabboEncryption habboEncryption,
    IOptions<NetworkOptions> networkOptions,
    ILogger<CompleteDiffieHandshakeEventHandler> logger) : INetworkPacketEventHandler
{
    public string? PublicKey { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (string.IsNullOrEmpty(PublicKey))
        {
            return;
        }

        if (!habboEncryption.TryCalculateDiffieHellmanSharedKey(PublicKey, out var sharedKey))
        {
            await client.DisposeAsync();
            return;
        }

        await client.WriteToStreamAsync(new CompleteDiffieHandshakeWriter
        {
            PublicKey = habboEncryption.GetRsaDiffieHellmanPublicKey(),
            ClientEncryption = false
        });

        if (!client.TryApplyNegotiatedKey(sharedKey) &&
            !networkOptions.Value.UseWss)
        {
            logger.LogWarning(
                "Client {Guid} completed the Diffie-Hellman handshake, but the negotiated key is " +
                "not applied to the stream and the listener is plaintext, so this connection is " +
                "readable on the wire. Set NetworkOptions:UseWss for real confidentiality.",
                client.Guid);
        }
    }
}
