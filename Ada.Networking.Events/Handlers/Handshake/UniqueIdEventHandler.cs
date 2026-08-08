using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Writers.Handshake;

namespace Ada.Networking.Events.Handlers.Handshake;

[PacketId(EventHandlerId.UniqueId)]
[AllowUnauthenticated]
public class UniqueIdEventHandler : INetworkPacketEventHandler
{
    public required string Fingerprint { get; set; }

    private const int _maxFingerprintLength = 64;

    public async Task HandleAsync(INetworkClient client)
    {
        var fingerprint = Fingerprint.Trim();

        if (fingerprint.Length is 0 or > _maxFingerprintLength ||
            !fingerprint.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            return;
        }

        if (client.MachineId != null)
        {
            return;
        }

        client.MachineId = fingerprint;

        await client.WriteToStreamAsync(new UniqueIdWriter
        {
            MachineId = fingerprint
        });
    }
}
