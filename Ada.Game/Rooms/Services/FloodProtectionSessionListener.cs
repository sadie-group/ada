using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Plugins;

namespace Ada.Game.Rooms.Services;

public class FloodProtectionSessionListener(IRoomFloodProtectionService floodProtectionService)
    : IPlayerSessionListener
{
    public Task OnLoginAsync(INetworkClient client, IPlayerLogic player, bool resumed) => Task.CompletedTask;

    public Task OnDisconnectedAsync(IPlayerLogic player, IRoomUser? roomUser)
    {
        floodProtectionService.Clear(player.Player.Id);

        return Task.CompletedTask;
    }
}
