using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerActivity)]
public class PlayerActivityEventHandler : INetworkPacketEventHandler
{
    public Task HandleAsync(INetworkClient networkClient)
    {
        return Task.CompletedTask;
    }
}