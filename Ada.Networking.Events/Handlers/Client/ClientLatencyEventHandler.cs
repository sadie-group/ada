using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Events.Attributes;

namespace Ada.Networking.Events.Handlers.Client;

[PacketId(EventHandlerId.ClientLatency)]
[AllowUnauthenticated]
public class ClientLatencyEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
    }
}