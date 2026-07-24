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
            foreach (var originId in Ids) 
            {
                var targetId = playerId;
                
                await dbContext.Set<PlayerFriendship>()
                    .Where(x => x.OriginPlayerId == originId && x.TargetPlayerId == targetId)
                    .ExecuteDeleteAsync();

                var origin = await playerRepository.GetPlayerByIdAsync(originId);
                var request = origin?.OutgoingFriendships.FirstOrDefault(x => x.TargetPlayerId == targetId);

                if (request != null)
                {
                    origin?.OutgoingFriendships.Remove(request);
                }
            }
        }
    }
}