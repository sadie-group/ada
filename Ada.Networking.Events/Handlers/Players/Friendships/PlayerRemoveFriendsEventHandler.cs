using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Players.Friendships;

namespace Ada.Networking.Events.Handlers.Players.Friendships;

[PacketId(EventHandlerId.PlayerRemoveFriends)]
public class PlayerRemoveFriendsEventHandler(
    IPlayerRepository playerRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory)
    : INetworkPacketEventHandler
{
    public List<long> Ids { get; init; } = [];
    
    public async Task HandleAsync(INetworkClient client)
    {
        var playerId = client.Player.Player.Id;
        
        foreach (var currentId in Ids)
        {
            var target = playerRepository.GetPlayerLogicById(currentId);
            
            if (target == null)
            {
                continue;
            }

            var friendship = target.TryGetAcceptedFriendshipFor(target.Player.Id);

            if (friendship != null)
            {
                target.DeleteFriendshipFor(playerId);
            }
                
            await target.NetworkObject!.WriteToStreamAsync(new PlayerRemoveFriendsWriter
            {
                Unknown1 = 0,
                PlayerIds = [playerId]
            });
            
            client.Player.DeleteFriendshipFor(currentId);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        foreach (var currentId in Ids)
        {
            await dbContext
                .Set<PlayerFriendship>()
                .Where(x => 
                    x.OriginPlayerId == currentId && x.TargetPlayerId == playerId ||
                    x.TargetPlayerId == currentId && x.OriginPlayerId == playerId)
                .ExecuteDeleteAsync();
        }
        
        await client.WriteToStreamAsync(new PlayerRemoveFriendsWriter
        {
            Unknown1 = 0,
            PlayerIds = Ids
        });
    }
}