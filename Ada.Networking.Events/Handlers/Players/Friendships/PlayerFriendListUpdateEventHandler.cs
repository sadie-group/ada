using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Players.Friendships;

[PacketId(EventHandlerId.PlayerFriendListUpdate)]
public class PlayerFriendListUpdateEventHandler(
    IPlayerRepository playerRepository,
    IPlayerHelperService playerHelperService)
    : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await playerHelperService.SendPlayerFriendListUpdate(
            client.Player!, 
            playerRepository);
    }
}