using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Constants;
using Ada.Networking.Writers.Players.Messenger;

namespace Ada.Networking.Events.Handlers.Players.Friendships;

[PacketId(EventHandlerId.PlayerFindNewFriends)]
public class PlayerFindNewFriendsEventHandler(IPlayerRepository playerRepository)
    : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    private const int _maxSuggestions = 20;

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        if ((DateTime.UtcNow - player.State.LastPlayerSearch).TotalMilliseconds < CooldownIntervals.PlayerSearch)
        {
            return;
        }

        player.State.LastPlayerSearch = DateTime.UtcNow;

        var friendIds = player.Player.OutgoingFriendships
            .Where(x => x.Status == PlayerFriendshipStatus.Accepted)
            .Select(x => x.TargetPlayerId)
            .Concat(player.Player.IncomingFriendships
                .Where(x => x.Status == PlayerFriendshipStatus.Accepted)
                .Select(x => x.OriginPlayerId))
            .ToHashSet();

        friendIds.Add(player.Player.Id);

        var suggestions = playerRepository
            .GetAll()
            .Where(x => !friendIds.Contains(x.Player.Id))
            .Take(_maxSuggestions)
            .Select(x => x.Player)
            .ToList();

        await client.WriteToStreamAsync(new PlayerSearchResultWriter
        {
            Friends = [],
            Strangers = suggestions
        });
    }
}
