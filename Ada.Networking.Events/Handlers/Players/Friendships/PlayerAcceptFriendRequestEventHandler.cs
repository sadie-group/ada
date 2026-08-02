using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Events.Dtos;
using Microsoft.EntityFrameworkCore;
using PlayerFriendship = Ada.Db.Models.Players.PlayerFriendship;

namespace Ada.Networking.Events.Handlers.Players.Friendships;

[PacketId(EventHandlerId.PlayerAcceptFriendRequest)]
public class PlayerAcceptFriendRequestEventHandler(
    IPlayerRepository playerRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IPlayerHelperService playerHelperService)
    : INetworkPacketEventHandler
{
    public List<int> Ids { get; set; } = [];
    
    public async Task HandleAsync(INetworkClient client)
    {
        foreach (var originId in Ids)
        {
            await AcceptRequestAsync(client, originId);
        }
    }

    private async Task AcceptRequestAsync(INetworkClient client, int originId)
    {
        var player = client.Player;

        if (player?.Player.AvatarData == null)
        {
            return;
        }

        var playerId = player.Player.Id;

        var request = player
            .Player
            .IncomingFriendships
            .FirstOrDefault(x => x.OriginPlayerId == originId && x.Status == PlayerFriendshipStatus.Pending);

        if (request == null || request.TargetPlayerId != playerId)
        {
            return;
        }

        request.Status = PlayerFriendshipStatus.Accepted;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.Set<PlayerFriendship>()
            .Where(x => x.OriginPlayerId == originId && x.TargetPlayerId == playerId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, PlayerFriendshipStatus.Accepted));
        
        var targetPlayer = playerRepository.GetPlayerLogicById(originId);
        var targetOnline = targetPlayer != null;
        var targetInRoom = targetPlayer is { State.CurrentRoomId: not 0 };

        var targetData = targetPlayer?.Player ?? await playerRepository.GetPlayerByIdAsync(originId);

        if (targetData?.AvatarData == null)
        {
            return;
        }

        var targetRelationship = targetPlayer?
            .Player
            .OriginRelationships
            .FirstOrDefault(x => x.TargetPlayerId == request.OriginPlayerId || x.TargetPlayerId == request.TargetPlayerId);

        await playerHelperService.SendFriendUpdatesToPlayerAsync(player, [
            new PlayerFriendshipUpdate
            {
                Type = 0,
                Friend = new FriendData
                {
                    Username = targetData.Username,
                    FigureCode = targetData.AvatarData.FigureCode ?? string.Empty,
                    Motto = targetData.AvatarData.Motto ?? string.Empty,
                    Gender = targetData.AvatarData.Gender
                },
                FriendOnline = targetOnline,
                FriendInRoom = targetInRoom,
                Relation = (PlayerRelationshipType?)targetRelationship?.TypeId ?? PlayerRelationshipType.None
            }
        ]);

        if (targetPlayer != null)
        {
            var targetRequest = targetPlayer.
                Player.OutgoingFriendships.
                FirstOrDefault(x => x.TargetPlayerId == playerId);

            if (targetRequest == null)
            {
                return;
            }

            var relationship = targetPlayer
                .Player
                .OriginRelationships
                .FirstOrDefault(x =>
                    x.TargetPlayerId == targetRequest.OriginPlayerId || x.TargetPlayerId == targetRequest.TargetPlayerId);

            await playerHelperService.SendFriendUpdatesToPlayerAsync(targetPlayer, [
                new PlayerFriendshipUpdate
                {
                    Type = 0,
                    Friend = new FriendData
                    {
                        Username = player.Player.Username,
                        FigureCode = player.Player.AvatarData.FigureCode ?? string.Empty,
                        Motto = player.Player.AvatarData.Motto ?? string.Empty,
                        Gender = player.Player.AvatarData.Gender
                    },
                    FriendOnline = true,
                    FriendInRoom = player.State.CurrentRoomId != 0,
                    Relation = (PlayerRelationshipType?)relationship?.TypeId ?? PlayerRelationshipType.None
                }
            ]);
        }
    }
}