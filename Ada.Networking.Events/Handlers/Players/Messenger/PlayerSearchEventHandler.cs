using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Constants;
using Ada.Core.Shared.Extensions;
using Ada.Networking.Writers.Players.Messenger;

namespace Ada.Networking.Events.Handlers.Players.Messenger;

[PacketId(EventHandlerId.PlayerSearch)]
public class PlayerSearchEventHandler(IPlayerRepository playerRepository) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public string? SearchQuery { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        if ((DateTime.Now - client.Player.State.LastPlayerSearch).TotalMilliseconds < CooldownIntervals.PlayerSearch)
        {
            return;
        }
        
        client.Player.State.LastPlayerSearch = DateTime.Now;

        if (string.IsNullOrEmpty(SearchQuery))
        {
            return;
        }

        SearchQuery = SearchQuery.Truncate(20);

        var outgoingFriends = client
            .Player!
            .Player.OutgoingFriendships
            .Select(x => x.TargetPlayer!);
        
        var incomingFriends = client
            .Player!
            .Player.IncomingFriendships
            .Select(x => x.OriginPlayer!);

        var friendsList = outgoingFriends
            .Concat(incomingFriends)
            .DistinctBy(x => x.Id)
            .Where(x => x.Username.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var strangers = await playerRepository
            .GetPlayersForSearchAsync(SearchQuery, friendsList.Select(x => x.Id).ToArray());

        await client.WriteToStreamAsync(new PlayerSearchResultWriter
        {
            Friends = friendsList,
            Strangers = strangers
        });
    }
}