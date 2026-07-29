using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Friendships;

[PacketId(EventHandlerId.PlayerDeclineFriendRequest)]
public class PlayerDeclineFriendRequestEventHandler(
    IPlayerRepository playerRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory)
    : INetworkPacketEventHandler
{
    public bool DeclineAll { get; set; }
    public required List<int> Ids { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;
        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        if (DeclineAll)
        {
            player.Player.IncomingFriendships.Clear();
            
            await dbContext.Set<PlayerFriendship>()
                .Where(x => x.TargetPlayerId == playerId && x.Status == PlayerFriendshipStatus.Pending)
                .ExecuteDeleteAsync();
        }
        else
        {
            var originIds = Ids.Select(id => (long) id).ToList();

            await dbContext.Set<PlayerFriendship>()
                .Where(x => originIds.Contains(x.OriginPlayerId) && x.TargetPlayerId == playerId)
                .ExecuteDeleteAsync();

            foreach (var originId in Ids)
            {
                // Only online origins have in-memory state to update; the rows are
                // already deleted above.
                var origin = playerRepository.GetPlayerLogicById(originId)?.Player;
                var request = origin?.OutgoingFriendships.FirstOrDefault(x => x.TargetPlayerId == playerId);

                if (request != null)
                {
                    origin?.OutgoingFriendships.Remove(request);
                }
            }
        }
    }
}