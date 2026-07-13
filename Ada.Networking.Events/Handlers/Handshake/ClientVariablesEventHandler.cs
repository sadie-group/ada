using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Handshake;

[PacketId(EventHandlerId.ClientVariables)]
public class ClientVariablesEventHandler : INetworkPacketEventHandler
{
    public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
}