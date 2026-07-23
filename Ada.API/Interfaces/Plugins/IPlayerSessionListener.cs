using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking.Client;

namespace Ada.API.Interfaces.Plugins;

public interface IPlayerSessionListener
{
    Task OnLoginAsync(INetworkClient client, IPlayerLogic player, bool resumed);
    Task OnDisconnectedAsync(IPlayerLogic player, IRoomUser? roomUser);
}
