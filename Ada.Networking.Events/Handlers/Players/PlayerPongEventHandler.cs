using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerPong)]
public class PlayerPongEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        client.LastPong = DateTime.Now;
    }
}