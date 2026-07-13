using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Handshake;

namespace Ada.Networking.Events.Handlers.Handshake;

[PacketId(EventHandlerId.UniqueId)]
public class UniqueIdEventHandler : INetworkPacketEventHandler
{
    public required string Fingerprint { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new UniqueIdWriter
        {
            MachineId = Fingerprint
        });
    }
}