using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Events.Attributes;

namespace Ada.Networking.Events.Handlers.Handshake;

[PacketId(EventHandlerId.ClientVersion)]
[AllowUnauthenticated]
public class ClientVersionEventHandler : INetworkPacketEventHandler
{
    public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
}