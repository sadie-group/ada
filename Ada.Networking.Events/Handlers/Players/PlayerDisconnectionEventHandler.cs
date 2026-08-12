using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerDisconnection)]
public class PlayerDisconnectionEventHandler(IClientDisposalService clientDisposalService)
    : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public async Task HandleAsync(INetworkClient client)
    {
        await clientDisposalService.HandleDisconnectAsync(client);
    }
}
